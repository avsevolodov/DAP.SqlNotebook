"""
Deterministic autocomplete: schema/join/table/function/keyword/RAG suggestions and ranking.
No LLM; all logic is pure given cursor context, schema, SQL and optional retriever.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Iterable, List

import sqlglot
from sqlglot import expressions as exp

from callbacks_console import get_console_handler
from rag_models import RagEntity, RagRelation, RagSchema
from schema_autocomplete import _sort_columns_for_autocomplete
from sql_context import CursorContext, CursorContextType

MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE = 15
MAX_AUTOCOMPLETE_SUGGESTIONS = 5
MAX_RANKER_CANDIDATES = 15

SQL_FUNCTIONS = [
    "COUNT", "SUM", "AVG", "MIN", "MAX",
    "DATEADD", "DATEDIFF", "CAST", "CONVERT",
]
KEYWORDS_SELECT = ["DISTINCT", "TOP"]
KEYWORDS_WHERE = ["AND", "OR", "IN", "EXISTS", "LIKE", "BETWEEN"]
KEYWORDS_GROUP_BY = ["ROLLUP"]


def _matches_prefix(prefix: str, candidate: str) -> bool:
    if not (prefix or "").strip():
        return True
    prefix = prefix.strip().lower()
    if not candidate:
        return False
    value = candidate.strip().lower()
    if value.startswith(prefix):
        return True
    compact = value.replace("_", "")
    if compact.startswith(prefix):
        return True
    acc: list[str] = []
    take = True
    for ch in candidate:
        if take and ch.isalpha():
            acc.append(ch.lower())
        take = (ch == "_" or ch.isupper())
    acronym = "".join(acc)
    return acronym.startswith(prefix)


def build_entity_indexes(schema: RagSchema) -> tuple[dict[str, RagEntity], dict[str, RagEntity], dict[str, RagEntity]]:
    """Return (by_logical_name, by_display_name, by_id) for fast lookup."""
    by_logical: dict[str, RagEntity] = {}
    by_display: dict[str, RagEntity] = {}
    by_id: dict[str, RagEntity] = {}
    for e in schema.entities:
        if not e:
            continue
        by_id[e.id] = e
        logical = (e.name or "").strip()
        display = (e.display_name or "").strip()
        if logical:
            by_logical[logical.lower()] = e
        if display:
            by_display[display.lower()] = e
    return by_logical, by_display, by_id


def get_entity_for_table_name(
    table_name: str,
    by_logical: dict[str, RagEntity],
    by_display: dict[str, RagEntity],
) -> RagEntity | None:
    if not (table_name or "").strip():
        return None
    key = table_name.strip().lower()
    return by_logical.get(key) or by_display.get(key)


def schema_suggestions_for_context(
    cursor_ctx: CursorContext,
    schema: RagSchema,
    max_columns_per_table: int,
) -> list[str]:
    """Schema-based suggestions (tables/columns) depending on cursor context."""
    by_logical, by_display, _ = build_entity_indexes(schema)
    suggestions: list[str] = []
    prefix = (cursor_ctx.prefix or "").strip().lower()

    if cursor_ctx.type == CursorContextType.COLUMN and cursor_ctx.table:
        ent = get_entity_for_table_name(cursor_ctx.table, by_logical, by_display)
        if not ent:
            return []
        fields = [{"name": f.name, "dataType": f.data_type, "isPrimaryKey": f.is_primary_key} for f in (ent.fields or [])]
        cols = _sort_columns_for_autocomplete(fields, max_columns=max_columns_per_table)
        for c in cols:
            if not _matches_prefix(prefix, c):
                continue
            suggestions.append(c)
        return suggestions

    if cursor_ctx.type in (CursorContextType.SELECT_LIST, CursorContextType.ORDER_BY):
        aliases = cursor_ctx.aliases or {}
        if not aliases and not (cursor_ctx.tables or []):
            entities = schema.entities or []
            for e in entities:
                name = (e.display_name or e.name or "").strip()
                if not name or not _matches_prefix(prefix, name):
                    continue
                suggestions.append(name)
            return suggestions
        for alias in aliases.keys():
            a = alias.strip()
            if not a or not _matches_prefix(prefix, a):
                continue
            suggestions.append(f"{a}.")
        if cursor_ctx.type == CursorContextType.SELECT_LIST:
            table_names = set(cursor_ctx.tables or [])
            for _alias, tname in aliases.items():
                if tname:
                    table_names.add(tname)
            for alias, tname in aliases.items():
                ent = get_entity_for_table_name(tname, by_logical, by_display)
                if not ent:
                    continue
                fields = [{"name": f.name, "dataType": f.data_type, "isPrimaryKey": f.is_primary_key} for f in (ent.fields or [])]
                cols = _sort_columns_for_autocomplete(fields, max_columns=max_columns_per_table)
                for c in cols:
                    text = f"{alias}.{c}"
                    if not _matches_prefix(prefix, text):
                        continue
                    suggestions.append(text)
        return suggestions
    return []


def join_suggestions_for_context(cursor_ctx: CursorContext, schema: RagSchema) -> list[str]:
    """JOIN-table suggestions based on relations."""
    if cursor_ctx.type != CursorContextType.JOIN_TABLE:
        return []
    existing_tables = cursor_ctx.existing_tables or cursor_ctx.tables or []
    if not existing_tables:
        return []
    by_logical, by_display, by_id = build_entity_indexes(schema)
    existing_ids: set[str] = set()
    for tname in existing_tables:
        ent = get_entity_for_table_name(tname, by_logical, by_display)
        if ent and ent.id:
            existing_ids.add(ent.id)
    if not existing_ids:
        return []
    prefix = (cursor_ctx.prefix or "").strip().lower()
    suggestions: list[str] = []
    for r in (schema.relations or []):
        if not r.from_entity_id or not r.to_entity_id:
            continue
        from_ent = by_id.get(r.from_entity_id)
        to_ent = by_id.get(r.to_entity_id)
        if not from_ent or not to_ent:
            continue
        if r.from_entity_id in existing_ids:
            known, other = from_ent, to_ent
            left_col, right_col = r.from_field_name, r.to_field_name
        elif r.to_entity_id in existing_ids:
            known, other = to_ent, from_ent
            left_col, right_col = r.to_field_name, r.from_field_name
        else:
            continue
        other_name = (other.display_name or other.name or "").strip()
        known_name = (known.display_name or known.name or "").strip()
        if not other_name or not known_name or not _matches_prefix(prefix, other_name):
            continue
        suggestions.append(f"JOIN {other_name} ON {known_name}.{left_col} = {other_name}.{right_col}")
    return suggestions


def rag_table_suggestions_for_context(cursor_ctx: CursorContext, retriever: Any) -> list[str]:
    """RAG-based table suggestions; returns [] if retriever is None or on error."""
    if not retriever:
        return []
    parts: list[str] = []
    if cursor_ctx.type == CursorContextType.JOIN_TABLE:
        if cursor_ctx.existing_tables:
            parts.append("join " + ", ".join(cursor_ctx.existing_tables))
        if cursor_ctx.prefix:
            parts.append(cursor_ctx.prefix)
    else:
        if cursor_ctx.tables:
            parts.append(", ".join(cursor_ctx.tables))
        if cursor_ctx.prefix:
            parts.append(cursor_ctx.prefix)
    query = " ".join(p for p in parts if p).strip() or "tables"
    try:
        docs = retriever.invoke(query, config={"callbacks": [get_console_handler()]})
    except Exception:
        return []
    prefix = (cursor_ctx.prefix or "").strip().lower()
    seen: set[str] = set()
    out: list[str] = []
    for d in docs:
        name = (d.metadata.get("displayName") or d.metadata.get("name") or "").strip()
        if not name or name.lower() in seen or not _matches_prefix(prefix, name):
            continue
        seen.add(name.lower())
        out.append(f"JOIN {name}" if cursor_ctx.type == CursorContextType.JOIN_TABLE else name)
    return out


def table_suggestions_for_context(cursor_ctx: CursorContext, schema: RagSchema) -> list[str]:
    """Table suggestions for FROM | and JOIN |."""
    if cursor_ctx.type not in (CursorContextType.FROM_TABLE, CursorContextType.JOIN_TABLE):
        return []
    prefix = (cursor_ctx.prefix or "").strip().lower()
    if not schema.entities:
        return []
    out: list[str] = []
    seen: set[str] = set()
    for e in schema.entities:
        name = (e.display_name or e.name or "").strip()
        if not name:
            continue
        key = name.lower()
        if key in seen or not _matches_prefix(prefix, name):
            continue
        seen.add(key)
        out.append(name)
        if len(out) >= 20:
            break
    return out


def group_order_suggestions_for_context(cursor_ctx: CursorContext, sql: str) -> list[str]:
    """GROUP BY / ORDER BY suggestions from SELECT list."""
    if cursor_ctx.type not in (CursorContextType.GROUP_BY, CursorContextType.ORDER_BY):
        return []
    try:
        parsed = sqlglot.parse_one(sql or "", read="tsql")
    except Exception:
        return []
    select = parsed.find(exp.Select)
    if not select:
        return []
    prefix = (cursor_ctx.prefix or "").strip().lower()
    out: list[str] = []
    for e in select.expressions:
        if isinstance(e, exp.Alias):
            label, inner = e.alias, e.this
        else:
            label, inner = e.sql(), e
        if not label:
            continue
        if cursor_ctx.type == CursorContextType.GROUP_BY and isinstance(inner, exp.AggFunc):
            continue
        label_str = str(label).strip()
        if not label_str or not _matches_prefix(prefix, label_str) or label_str in out:
            continue
        out.append(label_str)
    return out


def function_suggestions_for_context(cursor_ctx: CursorContext) -> list[str]:
    """SQL function suggestions (e.g. COUNT(, SUM()."""
    if cursor_ctx.type != CursorContextType.SELECT_LIST:
        return []
    prefix = (cursor_ctx.prefix or "").strip().lower()
    out: list[str] = []
    for f in SQL_FUNCTIONS:
        if prefix and not f.lower().startswith(prefix):
            continue
        out.append(f + "(")
    return out


def keyword_suggestions_for_context(cursor_ctx: CursorContext) -> list[str]:
    """SQL keyword suggestions (DISTINCT, TOP, AND, OR, ...)."""
    if cursor_ctx.type == CursorContextType.SELECT_LIST:
        source = KEYWORDS_SELECT
    elif cursor_ctx.type == CursorContextType.WHERE:
        source = KEYWORDS_WHERE
    elif cursor_ctx.type == CursorContextType.GROUP_BY:
        source = KEYWORDS_GROUP_BY
    else:
        return []
    prefix = (cursor_ctx.prefix or "").strip()
    out: list[str] = []
    for kw in source:
        if not _matches_prefix(prefix, kw):
            continue
        out.append(kw)
    return out


def smart_select_expansion_suggestions(
    cursor_ctx: CursorContext,
    schema: RagSchema,
    sql: str,
    max_columns_per_table: int,
) -> list[str]:
    """Smart SELECT * expansion: table.col1, table.col2, ... or alias.col1, ...."""
    if cursor_ctx.type != CursorContextType.SELECT_LIST:
        return []
    try:
        parsed = sqlglot.parse_one(sql or "", read="tsql")
    except Exception:
        return []
    select = parsed.find(exp.Select)
    if not select:
        return []
    if not any(isinstance(e, exp.Star) for e in select.expressions):
        return []
    table_expr = next(iter(select.find_all(exp.Table)), None)
    if table_expr is None or not table_expr.name:
        return []
    table_name = table_expr.name
    alias = table_expr.alias_or_name if table_expr.alias else None
    by_logical, by_display, _ = build_entity_indexes(schema)
    ent = get_entity_for_table_name(table_name, by_logical, by_display)
    if not ent or not ent.fields:
        return []
    raw_fields = [{"name": f.name, "dataType": f.data_type, "isPrimaryKey": f.is_primary_key} for f in ent.fields]
    cols = _sort_columns_for_autocomplete(raw_fields, max_columns=max_columns_per_table)
    if not cols:
        return []
    prefix = (alias or table_name).strip()
    return [", ".join(f"{prefix}.{c}" for c in cols)]


def build_deterministic_candidates_for_ranker(
    cursor_ctx: CursorContext,
    schema_suggestions: list[str],
    table_suggestions: list[str],
    join_suggestions: list[str],
    function_suggestions: list[str],
    keyword_suggestions: list[str],
    group_order_suggestions: list[str],
    rag_suggestions: list[str],
    max_candidates: int,
) -> list[str]:
    """Build candidate list for LLM ranker from deterministic sources only."""
    prefix = (cursor_ctx.prefix or "").strip().lower()
    scored: list[tuple[int, str]] = []
    seen: set[str] = set()

    def add(items: list[str], base_score: int) -> None:
        for raw in items:
            s = (raw or "").strip()
            if not s:
                continue
            key = s.lower()
            if key in seen:
                continue
            score = base_score
            if prefix and _matches_prefix(prefix, s):
                score += 3
            scored.append((score, s))
            seen.add(key)

    if cursor_ctx.type == CursorContextType.JOIN_TABLE:
        add(table_suggestions, 30); add(join_suggestions, 25); add(rag_suggestions, 18)
        add(schema_suggestions, 12); add(function_suggestions, 18); add(keyword_suggestions, 10)
    elif cursor_ctx.type == CursorContextType.FROM_TABLE:
        add(table_suggestions, 30); add(rag_suggestions, 18); add(schema_suggestions, 12)
        add(function_suggestions, 18); add(keyword_suggestions, 10)
    elif cursor_ctx.type in (CursorContextType.GROUP_BY, CursorContextType.ORDER_BY):
        add(group_order_suggestions, 35); add(schema_suggestions, 20); add(function_suggestions, 18)
        add(keyword_suggestions, 10); add(rag_suggestions, 15); add(join_suggestions, 10)
    else:
        add(schema_suggestions, 20); add(function_suggestions, 18); add(keyword_suggestions, 10)
        add(rag_suggestions, 15); add(join_suggestions, 10)
    scored.sort(key=lambda x: (-x[0], x[1]))
    return [s for _, s in scored[: max_candidates or MAX_RANKER_CANDIDATES]]


def rank_merge_suggestions(
    cursor_ctx: CursorContext,
    schema_suggestions: list[str],
    table_suggestions: list[str],
    join_suggestions: list[str],
    function_suggestions: list[str],
    keyword_suggestions: list[str],
    group_order_suggestions: list[str],
    rag_suggestions: list[str],
    llm_suggestions: list[str],
    max_results: int,
    llm_ranked_candidates: list[str] | None = None,
) -> list[str]:
    """Merge and rank suggestions from schema/join/LLM. Returns ordered list of insert texts."""
    prefix = (cursor_ctx.prefix or "").strip().lower()
    det_scored: list[tuple[float, str]] = []
    seen: set[str] = set()

    def add(items: List[str], base_score: int) -> None:
        for raw in items:
            s = (raw or "").strip()
            if not s:
                continue
            key = s.lower()
            if key in seen:
                continue
            score = float(base_score)
            if prefix and _matches_prefix(prefix, s):
                score += 3.0
            det_scored.append((score, s))
            seen.add(key)

    if cursor_ctx.type == CursorContextType.JOIN_TABLE:
        add(table_suggestions, 30); add(join_suggestions, 25); add(rag_suggestions, 18)
        add(schema_suggestions, 12); add(function_suggestions, 18); add(keyword_suggestions, 10)
    elif cursor_ctx.type == CursorContextType.FROM_TABLE:
        add(table_suggestions, 30); add(rag_suggestions, 18); add(schema_suggestions, 12)
        add(function_suggestions, 18); add(keyword_suggestions, 10)
    elif cursor_ctx.type in (CursorContextType.GROUP_BY, CursorContextType.ORDER_BY):
        add(group_order_suggestions, 35); add(schema_suggestions, 20); add(function_suggestions, 18)
        add(keyword_suggestions, 10); add(rag_suggestions, 15); add(join_suggestions, 10)
    else:
        add(schema_suggestions, 20); add(function_suggestions, 18); add(keyword_suggestions, 10)
        add(rag_suggestions, 15); add(join_suggestions, 10)
    add(llm_suggestions, 5)

    if not det_scored:
        return []
    llm_score_map: dict[str, float] = {}
    if llm_ranked_candidates:
        n = len(llm_ranked_candidates)
        for idx, value in enumerate(llm_ranked_candidates):
            key = (value or "").strip().lower()
            if key:
                llm_score_map[key] = float(n - idx)
    max_det = max(score for score, _ in det_scored) or 1.0
    max_llm = max(llm_score_map.values()) if llm_score_map else 1.0
    final_scored: list[tuple[float, str]] = []
    for det_score, text in det_scored:
        key = text.strip().lower()
        det_norm = det_score / max_det if max_det > 0 else 0.0
        llm_raw = llm_score_map.get(key, 0.0)
        llm_norm = llm_raw / max_llm if max_llm > 0 else 0.0
        final_scored.append((det_norm * 0.6 + llm_norm * 0.4, text))
    final_scored.sort(key=lambda x: (-x[0], x[1]))
    return [s for _, s in final_scored[:max_results]]


@dataclass
class DeterministicResult:
    schema_suggestions: list[str]
    table_suggestions: list[str]
    join_suggestions: list[str]
    function_suggestions: list[str]
    keyword_suggestions: list[str]
    group_order_suggestions: list[str]
    rag_suggestions: list[str]
    deterministic_candidates: list[str]


def compute_deterministic_suggestions(
    cursor_ctx: CursorContext,
    schema: RagSchema,
    sql: str,
    retriever: Any,
    *,
    max_columns_per_table: int = MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE,
    max_ranker_candidates: int = MAX_RANKER_CANDIDATES,
) -> DeterministicResult:
    """Compute all deterministic suggestion lists and candidate list for ranker. No LLM."""
    schema_suggestions = schema_suggestions_for_context(
        cursor_ctx, schema, max_columns_per_table=max_columns_per_table
    )
    join_suggestions = join_suggestions_for_context(cursor_ctx, schema)
    rag_suggestions = rag_table_suggestions_for_context(cursor_ctx, retriever)
    table_suggestions = table_suggestions_for_context(cursor_ctx, schema)
    group_order_suggestions = group_order_suggestions_for_context(cursor_ctx, sql)
    function_suggestions = function_suggestions_for_context(cursor_ctx)
    keyword_suggestions = keyword_suggestions_for_context(cursor_ctx)
    smart_select = smart_select_expansion_suggestions(
        cursor_ctx, schema, sql, max_columns_per_table=max_columns_per_table
    )
    if smart_select:
        schema_suggestions = smart_select + schema_suggestions
    deterministic_candidates = build_deterministic_candidates_for_ranker(
        cursor_ctx=cursor_ctx,
        schema_suggestions=schema_suggestions,
        table_suggestions=table_suggestions,
        join_suggestions=join_suggestions,
        function_suggestions=function_suggestions,
        keyword_suggestions=keyword_suggestions,
        group_order_suggestions=group_order_suggestions,
        rag_suggestions=rag_suggestions,
        max_candidates=max_ranker_candidates,
    )
    return DeterministicResult(
        schema_suggestions=schema_suggestions,
        table_suggestions=table_suggestions,
        join_suggestions=join_suggestions,
        function_suggestions=function_suggestions,
        keyword_suggestions=keyword_suggestions,
        group_order_suggestions=group_order_suggestions,
        rag_suggestions=rag_suggestions,
        deterministic_candidates=deterministic_candidates,
    )
