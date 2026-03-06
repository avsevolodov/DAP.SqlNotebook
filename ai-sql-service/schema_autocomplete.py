"""
Schema text for autocomplete: focus tables + related tables (RAG), columns ordered by PK/FK then measures.
"""
from __future__ import annotations

import re
from typing import Optional

# Numeric / aggregate-friendly types (for "measures")
MEASURE_TYPE_PATTERN = re.compile(
    r"^(int|integer|bigint|smallint|tinyint|decimal|numeric|float|real|double|money|smallmoney|sum|count|avg)\b",
    re.I,
)


def _is_measure_type(data_type: str) -> bool:
    if not (data_type or "").strip():
        return False
    return bool(MEASURE_TYPE_PATTERN.search(data_type.strip()))


def _sort_columns_for_autocomplete(fields: list[dict], max_columns: int = 15) -> list[str]:
    """
    Order columns: PK first, then FK-like (id, *_id), then measure types, then rest.
    Return up to max_columns column names.
    """
    pk: list[str] = []
    fk_like: list[str] = []
    measures: list[str] = []
    rest: list[str] = []
    for f in fields:
        name = (f.get("name") or f.get("Name") or "").strip()
        if not name:
            continue
        dt = (f.get("dataType") or f.get("DataType") or "").strip()
        is_pk = f.get("isPrimaryKey") or f.get("IsPrimaryKey") or False
        if is_pk:
            pk.append(name)
        elif name.lower().endswith("_id") or name.lower() == "id":
            fk_like.append(name)
        elif _is_measure_type(dt):
            measures.append(name)
        else:
            rest.append(name)
    ordered = pk + fk_like + measures + rest
    return ordered[:max_columns]


def expand_focus_entities_with_related(
    schema: dict,
    focus_entity_names: list[str],
    retriever,
    related_k: int = 5,
) -> set[str]:
    """
    Add related table names to focus set: for each focus table, RAG-search similar tables.
    focus_entity_names: display names or logical names user is working with.
    Returns set of entity names (display or logical) to include in autocomplete schema.
    """
    names_lower = {n.strip().lower() for n in focus_entity_names if n.strip()}
    if not names_lower:
        return set()
    out = set(focus_entity_names)
    if not retriever:
        return out
    entities = schema.get("entities") or schema.get("Entities") or []
    id_to_name: dict[str, str] = {}
    for e in entities:
        display = (e.get("displayName") or e.get("DisplayName") or e.get("name") or e.get("Name") or "").strip()
        logical = (e.get("name") or e.get("Name") or "").strip()
        eid = str(e.get("id") or e.get("Id") or "")
        if display:
            id_to_name[eid] = display
        if logical and logical != display:
            id_to_name[eid] = id_to_name.get(eid) or logical
    for name in list(names_lower):
        try:
            docs = retriever.invoke(name)
            for d in docs[:related_k]:
                display = (d.metadata.get("displayName") or d.metadata.get("name") or "").strip()
                if display and display.lower() not in names_lower:
                    out.add(display)
        except Exception:
            continue
    return out


def build_schema_compact_for_autocomplete(
    schema: dict,
    focus_entity_names: Optional[list[str]] = None,
    retriever=None,
    related_k: int = 5,
    max_columns_per_table: int = 15,
) -> str:
    """
    Build short schema for autocomplete: table(col1, col2, ...).
    - focus_entity_names: top tables user is working with (from body.entities).
    - Expand with related tables via RAG (retriever).
    - For each table, include columns in order: PK, FK-like, measure types, rest; max max_columns_per_table.
    """
    entities = schema.get("entities") or schema.get("Entities") or []
    if not entities:
        return "tables: (none)"
    if focus_entity_names:
        focus_set = expand_focus_entities_with_related(
            schema, focus_entity_names, retriever, related_k=related_k
        )
        focus_lower = {n.strip().lower() for n in focus_set if n.strip()}
        entities = [
            e for e in entities
            if ((e.get("name") or e.get("Name") or "").strip().lower() in focus_lower)
            or ((e.get("displayName") or e.get("DisplayName") or "").strip().lower() in focus_lower)
        ]
    lines = []
    for e in entities:
        name = (e.get("displayName") or e.get("Name") or e.get("name") or "?").strip()
        fields = e.get("fields") or e.get("Fields") or []
        cols = _sort_columns_for_autocomplete(fields, max_columns_per_table)
        col_str = ", ".join(cols)
        if len(fields) > max_columns_per_table:
            col_str += ", ..."
        lines.append(f"{name}({col_str})")
    return "\n".join(lines) if lines else "tables: (none)"
