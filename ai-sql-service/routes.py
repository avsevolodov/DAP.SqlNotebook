"""API route handlers for AI SQL service."""
import logging
import re
from typing import Any, Dict, Optional

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
    get_llm_autocomplete,
    get_llm_find_tables,
    strip_sql_markdown,
)
from rag_index import get_retriever
from schema_autocomplete import build_schema_compact_for_autocomplete
from schema_client import (
    get_databases,
    get_entity_fields,
    get_entity_relations,
    get_tables,
    search_entities,
)
from schema_loader import build_schema_markdown, get_schema_object

logger = logging.getLogger(__name__)
router = APIRouter()

# Prompt/response limits (single responsibility, avoid magic numbers)
MAX_CHAT_TURNS_IN_PROMPT = 20
MAX_SNIPPET_LENGTH = 500
# Vector search: return top N tables from embedding(query)
MAX_FIND_TABLES_RESULT = 20
# How many candidate tables to send into LLM reasoning step
MAX_FIND_TABLES_CANDIDATES = 10
AUTOCOMPLETE_PREVIEW_LENGTH = 52
RAG_RETRIEVER_K_FIND_TABLES = 20
RAG_RETRIEVER_K_AUTOCOMPLETE = 10
RAG_RELATED_K_AUTOCOMPLETE = 5
FOCUSED_SCHEMA_RETRIEVER_K = 12
MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE = 15
MAX_AUTOCOMPLETE_SUGGESTIONS = 5
MAX_TABLE_DESC_PREVIEW_CHARS = 200


def _detect_scope(description: str) -> Dict[str, Optional[str]]:
    """
    Detect search scope from user description (rule-based).

    Returns dict with optional keys:
    - database: explicit database name if mentioned
    - schema: explicit schema name if mentioned
    - domain: logical domain hint (not yet used for filtering)
    """
    text = (description or "").strip()
    lower = text.lower()
    database: Optional[str] = None
    schema: Optional[str] = None
    domain: Optional[str] = None

    # 1) Fully qualified name: db.schema.table
    m_full = re.search(r"([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)", text)
    if m_full:
        database = m_full.group(1)
        schema = m_full.group(2)

    # 2) Simple patterns in natural language (ru/en) like "в БД billing" / "in database billing".
    if database is None:
        m_db = re.search(
            r"(?:в\s+бд|в\s+базе\s+данных|из\s+бд|из\s+базы\s+данных|database|db)\s+([A-Za-z0-9_]+)",
            lower,
        )
        if m_db:
            database = m_db.group(1)

    if schema is None:
        m_schema = re.search(
            r"(?:в\s+схеме|из\s+схемы|schema)\s+([A-Za-z0-9_]+)",
            lower,
        )
        if m_schema:
            schema = m_schema.group(1)

    # 3) Domain hint (e.g. "финансы", "payments", "billing", etc.) – kept for future tag-based filtering.
    # For now we simply leave it as None; can be extended later.

    return {
        "database": database,
        "schema": schema,
        "domain": domain,
    }


def _build_metadata_filter(scope: Dict[str, Optional[str]]) -> Optional[dict]:
    """
    Build vector-store metadata filter for Qdrant based on detected scope.

    Example output:
    {"must": [{"key": "database", "match": {"value": "billing"}}, {"key": "schema", "match": {"value": "dbo"}}]}
    """
    if not scope:
        return None
    clauses = []
    db = (scope.get("database") or "").strip()
    if db:
        clauses.append({"key": "database", "match": {"value": db}})
    sch = (scope.get("schema") or "").strip()
    if sch:
        clauses.append({"key": "schema", "match": {"value": sch}})
    # Domain/Tags can be added here later when available in metadata.
    if not clauses:
        return None
    return {"must": clauses}


def _candidate_from_doc(doc: Any) -> Optional[Dict[str, str]]:
    """Build a compact candidate table description from RAG document for LLM reasoning."""
    meta = getattr(doc, "metadata", {}) or {}
    name = (meta.get("name") or "").strip()
    display = (meta.get("displayName") or name).strip()
    database = (meta.get("database") or "").strip()
    schema = (meta.get("schema") or "").strip()

    qualified_parts = [p for p in [database, schema, name] if p]
    qualified_name = ".".join(qualified_parts) if qualified_parts else (display or name)
    if not qualified_name:
        return None

    desc = (meta.get("description") or "").strip()
    if len(desc) > MAX_TABLE_DESC_PREVIEW_CHARS:
        desc_preview = desc[:MAX_TABLE_DESC_PREVIEW_CHARS] + "..."
    else:
        desc_preview = desc

    # Extract columns block from page_content (between "Columns:" and next header/blank).
    columns_lines: list[str] = []
    page = (getattr(doc, "page_content", "") or "").splitlines()
    in_columns = False
    for line in page:
        if line.startswith("Columns:"):
            in_columns = True
            continue
        if in_columns:
            if not line.strip():
                break
            if line.startswith(("Relations:", "Sample SQL:", "Database:", "Schema:", "Table:", "Type:")):
                break
            columns_lines.append(line.strip())
    columns = "; ".join(columns_lines)

    return {
        "qualified_name": qualified_name,
        "display_name": display,
        "description": desc_preview,
        "columns": columns,
    }


def _reason_tables_with_llm(description: str, candidates: list[Dict[str, str]]) -> list[str]:
    """
    Step 4: LLM reasoning over candidate tables.

    LLM получает:
      - вопрос пользователя
      - top 5–10 таблиц с описанием и колонками

    И возвращает упорядоченный список наиболее релевантных таблиц
    (возможно, несколько, если нужны join'ы).
    """
    if not candidates:
        return []

    model = get_llm_find_tables()

    lines: list[str] = []
    for idx, c in enumerate(candidates, start=1):
        header = f"{idx}. {c['qualified_name']} — {c['description']}".rstrip()
        lines.append(header)
        if c["columns"]:
            lines.append(f"   Columns: {c['columns']}")
    tables_block = "\n".join(lines)

    prompt_tmpl = ChatPromptTemplate.from_messages(
        [
            (
                "system",
                "You help choose the most relevant database tables for answering a user question. "
                "You are given a small list of candidate tables with their descriptions and columns. "
                "Decide which table or tables are most appropriate, whether joins are needed, and which columns matter. "
                "Return ONLY the chosen table names, one per line, most relevant first. "
                "Start each line with the fully qualified table name (e.g. db.schema.table). "
                "Do not add explanations, bullet markers, or numbering.",
            ),
            (
                "human",
                "User question:\n"
                "{question}\n\n"
                "Here are candidate tables:\n"
                "{candidates}\n\n"
                "List the tables you would use (one per line).",
            ),
        ]
    )

    prompt = prompt_tmpl.format(question=description, candidates=tables_block)
    result = model.invoke(prompt, config={"callbacks": [get_console_handler()]})
    raw = result.content if isinstance(result.content, str) else str(result.content)

    final_tables: list[str] = []
    seen: set[str] = set()
    for line in raw.strip().split("\n"):
        s = line.strip()
        if not s:
            continue
        # Assume first token (up to ' —' or ' - ') is the table name.
        name_part = s.split("—", 1)[0].split("-", 1)[0].strip()
        if not name_part:
            continue
        key = name_part.lower()
        if key in seen:
            continue
        seen.add(key)
        final_tables.append(name_part)
        if len(final_tables) >= MAX_FIND_TABLES_RESULT:
            break
    return final_tables


@router.get("/health")
def health() -> dict[str, str]:
    """Lightweight readiness check. LLM is initialized on first use."""
    return {"status": "ok"}


@router.get("/databases")
def api_get_databases() -> list[dict[str, Any]]:
    """List all catalog nodes of type Database. Proxies to backend catalog/databases."""
    return get_databases()


@router.get("/tables")
def api_get_tables() -> list[dict[str, Any]]:
    """List all tables (entities) with id, name, displayName, description."""
    return get_tables()


@router.get("/tables/{entity_id}/fields")
def api_get_table_fields(entity_id: str) -> list[dict[str, Any]]:
    """Get fields (columns) for a table."""
    return get_entity_fields(entity_id)


@router.get("/tables/{entity_id}/relations")
def api_get_table_relations(entity_id: str) -> list[dict[str, Any]]:
    """Get relations for a table."""
    return get_entity_relations(entity_id)


def _get_rag_table_retriever(metadata_filter: Optional[dict] = None):
    """Retriever over schema RAG index (for find_tables), optionally scoped by metadata filter."""
    return get_retriever(
        use_separate_endpoints=True,
        k=RAG_RETRIEVER_K_FIND_TABLES,
        metadata_filter=metadata_filter,
    )


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
        logger.debug("LLM request length=%d", len(prompt))

        callbacks = [get_console_handler()]
        result = model.invoke(prompt, config={"callbacks": callbacks})
    except Exception as exc:
        raise HTTPException(
            status_code=500, detail=format_llm_error(exc, generate_sql_model())
        ) from exc

    sql_text = result.content if isinstance(result.content, str) else str(result.content)
    sql_text = strip_sql_markdown(sql_text)
    logger.debug("LLM response length=%d", len(sql_text))

    return GenerateSqlResponse(sql=sql_text.strip(), explanation=None)


@router.post("/find-tables", response_model=FindTablesResponse)
def find_tables(body: FindTablesRequest) -> FindTablesResponse:
    """Find tables by user description: RAG index (vector search) + optional LLM re-rank."""
    description = (body.description or "").strip()
    if not description:
        return FindTablesResponse(tables=[])

    tables: list[str] = []
    try:
        scope = _detect_scope(description)
        metadata_filter = _build_metadata_filter(scope)
        if metadata_filter:
            logger.debug("find_tables scope detected: %s", scope)
        retriever = _get_rag_table_retriever(metadata_filter=metadata_filter)
        if retriever:
            callbacks = [get_console_handler()]
            docs = retriever.invoke(description, config={"callbacks": callbacks})
            # Step 4: LLM reasoning over top candidate tables (limited to MAX_FIND_TABLES_CANDIDATES).
            candidates: list[Dict[str, str]] = []
            for d in docs:
                c = _candidate_from_doc(d)
                if c:
                    candidates.append(c)
                if len(candidates) >= MAX_FIND_TABLES_CANDIDATES:
                    break

            if candidates:
                try:
                    reasoned_tables = _reason_tables_with_llm(description, candidates)
                    if reasoned_tables:
                        return FindTablesResponse(tables=reasoned_tables[:MAX_FIND_TABLES_RESULT])
                except Exception as ex:
                    logger.warning("LLM reasoning for find_tables failed, falling back to vector order: %s", ex)

            # Fallback: simple vector order by similarity if LLM step failed.
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
            result = model.invoke(prompt, config={"callbacks": [get_console_handler()]})
            raw = result.content if isinstance(result.content, str) else str(result.content)
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
    model = get_llm_autocomplete()
    if not body.sql:
        return AutocompleteSqlResponse(suggestions=[])

    schema_obj = get_schema_object()
    retriever = get_retriever(use_separate_endpoints=True, k=RAG_RETRIEVER_K_AUTOCOMPLETE)
    schema_text = build_schema_compact_for_autocomplete(
        schema_obj,
        focus_entity_names=body.entities,
        retriever=retriever,
        related_k=RAG_RELATED_K_AUTOCOMPLETE,
        max_columns_per_table=MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE,
    )
    entities_hint = ", ".join(body.entities or []) or ""

    sql_tail = (
        body.sql.strip()[-MAX_SNIPPET_LENGTH:]
        if len(body.sql) > MAX_SNIPPET_LENGTH
        else body.sql.strip()
    )

    prompt_tmpl = ChatPromptTemplate.from_messages(
        [
            (
                "system",
                "SQL Server autocomplete. Reply with 2-3 short completion options. "
                "One option per line, no numbering or bullets. Only the snippet text.",
            ),
            (
                "human",
                "Tables:\n{schema}\n\n"
                "SQL so far:\n{sql}\n\n"
                "Give 2-3 possible next snippets (one per line).",
            ),
        ]
    )

    try:
        prompt = prompt_tmpl.format(schema=schema_text, sql=sql_tail)
        if entities_hint:
            prompt = prompt.replace("Tables:", f"Focus: {entities_hint}\nTables:", 1)
        result = model.invoke(prompt, config={"callbacks": [get_console_handler()]})
        raw = result.content if isinstance(result.content, str) else str(result.content)
        lines = [
            s.strip()
            for s in raw.strip().split("\n")
            if s.strip()
        ][:MAX_AUTOCOMPLETE_SUGGESTIONS]
        suggestions = [
            AutocompleteSuggestionItem(
                label=(
                    t[:AUTOCOMPLETE_PREVIEW_LENGTH] + "…"
                    if len(t) > AUTOCOMPLETE_PREVIEW_LENGTH
                    else t
                ),
                insertText=t,
            )
            for t in lines
        ]
        first = suggestions[0].insertText if suggestions else ""
        return AutocompleteSqlResponse(suggestion=first, suggestions=suggestions)
    except Exception as exc:
        raise HTTPException(
            status_code=500,
            detail=format_llm_error(exc, autocomplete_model()),
        ) from exc
