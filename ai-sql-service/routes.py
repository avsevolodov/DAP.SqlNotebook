"""API route handlers for AI SQL service."""
import logging
from typing import Any

from fastapi import APIRouter, HTTPException
from langchain_core.prompts import ChatPromptTemplate

from callbacks_console import get_console_handler
from config import autocomplete_model, find_tables_model, generate_sql_model
from dto import (
    AutocompleteSqlRequest,
    AutocompleteSqlResponse,
    AutocompleteSuggestionItem,
    FindTablesRequest,
    FindTablesResponse,
    GenerateSqlRequest,
    GenerateSqlResponse,
)
from generate_sql_schema import build_focused_schema_for_generate_sql
from llm_utils import (
    format_llm_error,
    get_llm,
    get_llm_find_tables,
    strip_sql_markdown,
)
from schema_client import (
    get_databases,
    get_entity_fields,
    get_entity_relations,
    get_tables,
    search_entities,
)
from rag_index import get_retriever
from schema_loader import build_schema_markdown

logger = logging.getLogger(__name__)
router = APIRouter()

# Prompt/response limits (single responsibility, avoid magic numbers)
MAX_CHAT_TURNS_IN_PROMPT = 20
MAX_SNIPPET_LENGTH = 500
MAX_FIND_TABLES_RESULT = 15
AUTOCOMPLETE_PREVIEW_LENGTH = 52
RAG_RETRIEVER_K_FIND_TABLES = 15
FOCUSED_SCHEMA_RETRIEVER_K = 12
MAX_TABLE_DESC_PREVIEW_CHARS = 200


def _get_rag_table_retriever():
    """Retriever over schema RAG index (for find_tables).

    Index is (re)built on startup and in background; here we only reuse it.
    """
    return get_retriever(use_separate_endpoints=False, k=RAG_RETRIEVER_K_FIND_TABLES)


@router.post("/generate-sql", response_model=GenerateSqlResponse)
def generate_sql(body: GenerateSqlRequest) -> GenerateSqlResponse:
    model = get_llm()
    # Focused schema: tables from RAG (user question) + tables from previous SQL (sql_context, chat_history) + relations
    schema_text = build_focused_schema_for_generate_sql(
        prompt=body.prompt,
        sql_context=body.sql_context,
        chat_history=body.chat_history,
        catalog_node_id=body.catalog_node_id,
        entity=body.entity,
        entities=body.entities,
        retriever_k=FOCUSED_SCHEMA_RETRIEVER_K,
    )

    prompt_tmpl = ChatPromptTemplate.from_messages(
        [
            (
                "system",
                "You are an assistant that writes SQL Server compatible SQL queries. "
                "User works with a logical SQL notebook. "
                "Use ONLY the tables and columns from the provided JSON schema when possible. "
                "When an entity focus is provided, prefer that entity as the main FROM table "
                "and use its direct relations when needed. "
                "Return ONLY SQL code, without any explanations or markdown.",
            ),
        (
            "human",
            "Database schema (JSON from /api/v1/schema, in markdown):\n"
            "{schema}\n\n"
            "Entity focus (may be empty): {entity_hint}\n\n"
            "Use database (optional): {database_name}. If provided, start the query with USE [database_name] or use fully qualified table names.\n\n"
            "Current SQL context (may be empty):\n"
            "{sql_context}\n\n"
            "{chat_history_block}"
            "User request:\n"
            "{question}\n\n"
            "Write a single SQL query that best satisfies the request.",
        ),
        ]
    )

    try:
        if body.entities:
            entity_hint = "The main entities are: " + ", ".join(body.entities)
        elif body.entity:
            entity_hint = (
                f"The main entity is '{body.entity}'. "
                "Focus on this table and its direct relations."
            )
        else:
            entity_hint = "(none)"

        sql_context = body.sql_context or "(none)"
        database_name = body.database_name or "(none)"

        chat_history_block = ""
        if body.chat_history:
            lines = []
            for t in body.chat_history[-MAX_CHAT_TURNS_IN_PROMPT:]:
                who = "User" if t.role == 0 else "Assistant"
                text = (t.content or "").strip()
                if text:
                    trunc = text[:MAX_SNIPPET_LENGTH]
                    suffix = "..." if len(text) > MAX_SNIPPET_LENGTH else ""
                    lines.append(f"{who}: {trunc}{suffix}")
            if lines:
                chat_history_block = "Previous conversation:\n" + "\n".join(lines) + "\n\n"

        prompt = prompt_tmpl.format(
            schema=schema_text,
            question=body.prompt,
            entity_hint=entity_hint,
            database_name=database_name,
            sql_context=sql_context,
            chat_history_block=chat_history_block,
        )
        model_name = getattr(model, "model_name", getattr(model, "model", "unknown"))
        logger.debug(
            "LLM generate-sql model=%s request_length=%d",
            model_name,
            len(prompt),
        )

        callbacks = [get_console_handler()]
        result = model.invoke(prompt, config={"callbacks": callbacks})
    except Exception as exc:
        raise HTTPException(
            status_code=500, detail=format_llm_error(exc, generate_sql_model())
        ) from exc

    sql_text = result.content if isinstance(result.content, str) else str(result.content)
    sql_text = strip_sql_markdown(sql_text)
    preview = sql_text.strip().replace("\n", "\\n")
    if len(preview) > 500:
        preview = preview[:500] + "…"
    logger.debug("LLM generate-sql response length=%d preview=%s", len(sql_text), preview)

    return GenerateSqlResponse(sql=sql_text.strip(), explanation=None)


@router.post("/find-tables", response_model=FindTablesResponse)
def find_tables(body: FindTablesRequest) -> FindTablesResponse:
    """Find tables by user description: RAG index (vector search) + optional LLM re-rank."""
    description = (body.description or "").strip()
    if not description:
        return FindTablesResponse(tables=[])

    tables: list[str] = []
    try:
        retriever = _get_rag_table_retriever()
        if retriever:
            callbacks = [get_console_handler()]
            docs = retriever.invoke(description, config={"callbacks": callbacks})
            seen: set[str] = set()
            for d in docs:
                display = (d.metadata.get("displayName") or d.metadata.get("name") or "").strip()
                if display and display.lower() not in seen:
                    seen.add(display.lower())
                    tables.append(display)
            if tables:
                return FindTablesResponse(tables=tables[:MAX_FIND_TABLES_RESULT])
    except Exception as exc:
        logger.warning("RAG find_tables retriever error (falling back to LLM): %s", exc)

    if not tables:
        model = get_llm_find_tables()
        all_entities = search_entities("")
        if not all_entities:
            return FindTablesResponse(tables=[])
        tables_block = []
        for e in all_entities:
            name = e.get("name") or e.get("Name") or ""
            display = e.get("displayName") or e.get("DisplayName") or name
            desc = (e.get("description") or e.get("Description") or "").strip()
            line = f"- {display} (logical: {name})"
            if desc:
                desc_preview = desc[:MAX_TABLE_DESC_PREVIEW_CHARS]
                line += (
                    f" — {desc_preview}"
                    + ("..." if len(desc) > MAX_TABLE_DESC_PREVIEW_CHARS else "")
                )
            tables_block.append(line)
        prompt_tmpl = ChatPromptTemplate.from_messages(
            [
                (
                    "system",
                    "You help find database tables by user description. "
                    "Given a list of tables with optional Description, return only the table names (display name or logical name) "
                    "most relevant to what the user is looking for. One table per line, most relevant first. No numbering, no explanation.",
                ),
                (
                    "human",
                    "User is looking for: {description}\n\nTables:\n{tables}\n\nReturn only relevant table names, one per line.",
                ),
            ]
        )
        try:
            prompt = prompt_tmpl.format(description=description, tables="\n".join(tables_block))
            model_name = getattr(model, "model_name", getattr(model, "model", "unknown"))
            logger.debug(
                "LLM find-tables model=%s request_length=%d",
                model_name,
                len(prompt),
            )
            result = model.invoke(prompt, config={"callbacks": [get_console_handler()]})
            raw = result.content if isinstance(result.content, str) else str(result.content)
            raw_preview = raw.strip().replace("\n", "\\n")
            if len(raw_preview) > 500:
                raw_preview = raw_preview[:500] + "…"
            logger.debug(
                "LLM find-tables response length=%d preview=%s",
                len(raw),
                raw_preview,
            )
            lines = [s.strip() for s in raw.strip().split("\n") if s.strip()]
            seen = set()
            tables = []
            for line in lines:
                name = line.split("(")[0].strip().rstrip(")")
                if name and name.lower() not in seen:
                    seen.add(name.lower())
                    tables.append(name)
        except Exception as exc:
            raise HTTPException(
                status_code=500,
                detail=format_llm_error(exc, find_tables_model()),
            ) from exc

    return FindTablesResponse(tables=tables[:MAX_FIND_TABLES_RESULT])


@router.post("/autocomplete-sql", response_model=AutocompleteSqlResponse)
def autocomplete_sql(body: AutocompleteSqlRequest) -> AutocompleteSqlResponse:
    from autocomplete_orchestrator import run_autocomplete_sql
    merged = run_autocomplete_sql(
        sql=body.sql or "",
        cursor_position=body.cursor_position,
        entities=body.entities,
    )
    suggestions = [
        AutocompleteSuggestionItem(
            label=(
                t[:AUTOCOMPLETE_PREVIEW_LENGTH] + "…"
                if len(t) > AUTOCOMPLETE_PREVIEW_LENGTH
                else t
            ),
            insertText=t,
        )
        for t in merged
    ]
    first = suggestions[0].insertText if suggestions else ""
    return AutocompleteSqlResponse(suggestion=first, suggestions=suggestions)
