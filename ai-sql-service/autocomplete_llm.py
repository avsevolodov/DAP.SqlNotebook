"""
LLM autocomplete: ranker, completion prompt, timeout invocation, output parsing and filtering.
"""
from __future__ import annotations

import os
import re
import time
from concurrent.futures import ThreadPoolExecutor, TimeoutError as FuturesTimeoutError
from typing import Any

import sqlglot
from langchain_core.prompts import ChatPromptTemplate

from autocomplete_deterministic import (
    MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE,
    build_entity_indexes,
    get_entity_for_table_name,
)
from callbacks_console import get_console_handler
from rag_models import RagSchema
from schema_autocomplete import _sort_columns_for_autocomplete
from sql_context import CursorContext, CursorContextType

# LLM timeout (seconds). If LLM does not respond in time, orchestrator returns deterministic suggestions.
LLM_AUTOCOMPLETE_TIMEOUT_SECONDS = float(os.environ.get("LLM_AUTOCOMPLETE_TIMEOUT_SECONDS", "0.5"))

MAX_AUTOCOMPLETE_SUGGESTIONS = 5
MAX_SNIPPET_LENGTH = 500


def _build_ranker_tables_block(cursor_ctx: CursorContext, schema: RagSchema) -> str:
    """Build minimal tables block for ranker prompt: table_name(col1, col2, ...)."""
    table_names = list(cursor_ctx.tables or [])
    if not table_names:
        return ""
    by_logical, by_display, _ = build_entity_indexes(schema)
    lines: list[str] = []
    for tname in table_names:
        ent = get_entity_for_table_name(tname, by_logical, by_display)
        if not ent:
            continue
        raw_fields = [{"name": f.name, "dataType": f.data_type, "isPrimaryKey": f.is_primary_key} for f in (ent.fields or [])]
        cols = _sort_columns_for_autocomplete(raw_fields, max_columns=MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE)
        name = (ent.display_name or ent.name or tname).strip()
        if not name:
            continue
        lines.append(f"{name}({', '.join(cols)})")
    return "\n".join(lines)


def build_llm_ranker_prompt(
    sql_before_cursor: str,
    tables_block: str,
    candidates: list[str],
) -> str:
    """Strict prompt for LLM ranking task."""
    candidates = [c.strip() for c in candidates if c and c.strip()]
    block = "\n".join(candidates)
    return (
        "SQL autocomplete ranking task.\n\n"
        "Reorder the candidate completions by likelihood.\n\n"
        "Rules:\n"
        "- Only use candidates from the list\n"
        "- Do not invent new SQL\n"
        "- Return the candidates in best order\n"
        "- One per line\n"
        "- No numbering\n"
        "- No explanations\n\n"
        f"SQL before cursor:\n{sql_before_cursor}\n\n"
        f"Tables:\n{tables_block}\n\n"
        "Candidates:\n"
        f"{block}\n\n"
        "Return reordered candidates:"
    )


def parse_llm_ranker_output(raw: str, candidates: list[str]) -> list[str]:
    """Parse LLM ranker output: strip numbering, match to candidates (case-insensitive), return originals."""
    if not raw:
        return []
    norm_to_original: dict[str, str] = {}
    for c in candidates:
        if not c:
            continue
        key = c.strip().lower()
        if key and key not in norm_to_original:
            norm_to_original[key] = c
    if not norm_to_original:
        return []
    result: list[str] = []
    seen: set[str] = set()
    for line in raw.split("\n"):
        s = line.strip()
        if not s:
            continue
        s = re.sub(r"^\s*(?:[-*•]\s+|\d+[.)]\s+)", "", s).strip()
        if not s:
            continue
        orig = norm_to_original.get(s.lower())
        if not orig or orig in seen:
            continue
        seen.add(orig)
        result.append(orig)
    return result


def extract_llm_ranker_expansions(raw: str, base_candidates: list[str]) -> list[str]:
    """Extract new candidates proposed by LLM ranker (not in base list), validated as SQL expressions."""
    if not raw:
        return []
    base_norm = {(c or "").strip().lower() for c in base_candidates if (c or "").strip()}
    expansions: list[str] = []
    seen: set[str] = set()
    for line in raw.split("\n"):
        s = line.strip()
        if not s:
            continue
        s = re.sub(r"^\s*(?:[-*•]\s+|\d+[.)]\s+)", "", s).strip()
        if not s or s.lower() in base_norm or s.lower() in seen:
            continue
        if len(s) > 80 or ";" in s:
            continue
        low = s.lower()
        if any(
            kw in low
            for kw in (
                "select", " from ", " where ", " group by ", " order by ",
                " join ", " insert ", " update ", " delete ",
            )
        ):
            continue
        try:
            sqlglot.parse_one(f"SELECT {s}", read="tsql")
        except Exception:
            continue
        seen.add(low)
        expansions.append(s)
    return expansions


def _normalize_identifier(name: str) -> str:
    name = (name or "").strip()
    if name.startswith("[") and name.endswith("]") and len(name) >= 2:
        name = name[1:-1].strip()
    return name


def filter_llm_candidates_by_schema(
    candidates: list[str],
    cursor_ctx: CursorContext,
    schema: RagSchema,
) -> list[str]:
    """Drop candidates that reference non-existent alias.column or table.column."""
    if not candidates:
        return []
    by_logical, by_display, _ = build_entity_indexes(schema)
    alias_map = cursor_ctx.aliases or {}
    safe: list[str] = []
    for cand in candidates:
        text = (cand or "").strip()
        if not text:
            continue
        valid = True
        for m in re.finditer(r"([A-Za-z0-9_\[\]]+)\.([A-Za-z0-9_\[\]]+)", text):
            left = _normalize_identifier(m.group(1))
            col = _normalize_identifier(m.group(2))
            table_name = alias_map.get(left)
            if table_name is None:
                ent = get_entity_for_table_name(left, by_logical, by_display)
                table_name = (ent.display_name or ent.name) if ent else None
            if not table_name:
                valid = False
                break
            ent = get_entity_for_table_name(table_name, by_logical, by_display)
            if not ent or not ent.fields:
                valid = False
                break
            col_norm = col.lower()
            has_col = any(
                _normalize_identifier(f.name).lower() == col_norm
                for f in (ent.fields or [])
                if f and f.name
            )
            if not has_col:
                valid = False
                break
        if valid:
            safe.append(text)
    return safe


def clean_llm_autocomplete_lines(raw: str) -> list[str]:
    """Split raw LLM output into lines, drop boilerplate, trim."""
    lines = [s.strip() for s in (raw or "").split("\n") if s.strip()]
    cleaned: list[str] = []
    for line in lines:
        low = line.lower()
        if "no explanations" in low or low.startswith("do not ") or low.startswith("don't "):
            continue
        if low.startswith("you must "):
            continue
        if low in ("assistant", "assistant.", "assistant:") or low.startswith("assistant "):
            continue
        if low in ("*", "-", "•"):
            continue
        if line.startswith(("* ", "- ", "• ")):
            line = line[2:].strip()
            low = line.lower()
            if not line:
                continue
        cleaned.append(line)
    return cleaned


def invoke_llm_with_timeout(model: Any, prompt: str, *, timeout_seconds: float, log_prefix: str) -> Any | None:
    """Invoke LLM with hard timeout. Returns model response or None on timeout/error."""
    callbacks = [get_console_handler()]

    def _call():
        return model.invoke(prompt, config={"callbacks": callbacks})

    with ThreadPoolExecutor(max_workers=1) as executor:
        future = executor.submit(_call)
        start = time.perf_counter()
        try:
            result = future.result(timeout=timeout_seconds)
            return result
        except FuturesTimeoutError:
            return None
        except Exception:
            return None


def run_ranker(
    model: Any,
    sql_before_cursor: str,
    cursor_ctx: CursorContext,
    schema: RagSchema,
    deterministic_candidates: list[str],
    *,
    timeout_seconds: float = LLM_AUTOCOMPLETE_TIMEOUT_SECONDS,
) -> tuple[list[str] | None, list[str]]:
    """
    Run LLM ranker to reorder candidates. Returns (ranked_candidates or None, expansions).
    On timeout/error returns (None, []).
    """
    if not deterministic_candidates:
        return None, []
    tables_block = _build_ranker_tables_block(cursor_ctx, schema)
    prompt = build_llm_ranker_prompt(sql_before_cursor, tables_block, deterministic_candidates)
    result = invoke_llm_with_timeout(
        model, prompt, timeout_seconds=timeout_seconds, log_prefix="LLM autocomplete-sql ranker"
    )
    if result is None:
        return None, []
    raw = result.content if isinstance(result.content, str) else str(result.content)
    ranked = parse_llm_ranker_output(raw, deterministic_candidates)
    expansions = extract_llm_ranker_expansions(raw, deterministic_candidates)
    return ranked, expansions


def build_autocomplete_prompt(
    schema_text: str,
    sql_around: str,
    cursor_ctx: CursorContext,
    *,
    allowed_block: str = "",
    disallowed_block: str = "",
    entities_hint: str = "",
) -> str:
    """Build the human prompt for LLM autocomplete (continue SQL at cursor)."""
    disallowed_block = disallowed_block or (
        "Do NOT output:\n"
        "FROM\nWHERE\nGROUP BY\nORDER BY\nJOIN\n"
    )
    prompt_tmpl = ChatPromptTemplate.from_messages([
        ("system", "SQL Server autocomplete task."),
        (
            "human",
            "Continue the SQL query at the cursor position.\n\n"
            "Rules:\n"
            "- Return 2-3 SQL completion snippets.\n"
            "- One snippet per line.\n"
            "- Only the text to insert at the cursor.\n"
            "- Do not repeat SQL that already exists before the cursor.\n"
            "- Do not output explanations.\n"
            "- Do not output numbering or bullets.\n\n"
            "{allowed_block}\n"
            "{disallowed_block}\n\n"
            "Tables:\n"
            "{schema}\n\n"
            "SQL around cursor (|CURSOR| marks cursor):\n"
            "{sql_around}\n\n"
            "Cursor context:\n"
            "type: {context_type}\n"
            "prefix: {context_prefix}\n"
            "tables: {context_tables}\n"
            "table_alias: {context_table_alias}\n"
            "table: {context_table}\n"
            "existing_tables: {context_existing_tables}\n\n"
            "Return snippets:",
        ),
    ])
    prompt = prompt_tmpl.format(
        schema=schema_text,
        sql_around=sql_around,
        allowed_block=allowed_block,
        disallowed_block=disallowed_block,
        context_type=cursor_ctx.type.value,
        context_prefix=cursor_ctx.prefix or "",
        context_tables=", ".join(cursor_ctx.tables or []),
        context_table_alias=cursor_ctx.table_alias or "",
        context_table=cursor_ctx.table or "",
        context_existing_tables=", ".join(cursor_ctx.existing_tables or []),
    )
    if entities_hint:
        prompt = prompt.replace("Tables:", f"Focus: {entities_hint}\nTables:", 1)
    return prompt


def run_autocomplete(
    model: Any,
    prompt: str,
    *,
    timeout_seconds: float = LLM_AUTOCOMPLETE_TIMEOUT_SECONDS,
) -> Any | None:
    """Invoke LLM for autocomplete. Returns response or None on timeout/error."""
    return invoke_llm_with_timeout(
        model, prompt, timeout_seconds=timeout_seconds, log_prefix="LLM autocomplete-sql"
    )


def process_llm_autocomplete_response(
    llm_result: Any,
    ranker_expansions: list[str],
    cursor_ctx: CursorContext,
    schema: RagSchema,
) -> list[str]:
    """
    Turn raw LLM response into filtered list of suggestions.
    Cleans lines, appends ranker expansions, filters by schema.
    """
    if llm_result is None:
        return []
    raw = llm_result.content if isinstance(llm_result.content, str) else str(llm_result.content)
    lines = clean_llm_autocomplete_lines(raw)[:MAX_AUTOCOMPLETE_SUGGESTIONS]
    if ranker_expansions:
        lines = lines + ranker_expansions
    return filter_llm_candidates_by_schema(lines, cursor_ctx, schema)
