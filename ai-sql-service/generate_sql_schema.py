"""
Build focused schema for generate-sql: extract tables from previous SQL + RAG by user question,
then include those tables, their descriptions, relations, and related table descriptions.
"""
from __future__ import annotations

import logging
from typing import Optional, Union

from rag_models import RagSchema
from schema_loader import build_schema_markdown, build_schema_markdown_focused, get_schema_object
from sql_parser import extract_sql_from_chat_history, extract_table_names_from_sql

from rag_index import get_retriever

logger = logging.getLogger(__name__)


def _entity_key(e: dict, key: str) -> str:
    v = e.get(key) or e.get(key[0].upper() + key[1:] if key else key)
    return (v or "").strip()


def _entity_names_in_schema(schema: Union[RagSchema, dict]) -> set[str]:
    """All entity names (name + displayName) present in schema."""
    if isinstance(schema, RagSchema):
        out = set()
        for e in schema.entities:
            if e.name:
                out.add(e.name)
            if e.display_name:
                out.add(e.display_name)
        return out
    out = set()
    for e in schema.get("entities") or schema.get("Entities") or []:
        name = _entity_key(e, "name")
        display = _entity_key(e, "displayName")
        if name:
            out.add(name)
        if display:
            out.add(display)
    return out


def _normalize_to_schema_name(name: str, allowed_names: set[str]) -> Optional[str]:
    """Return the schema entity name (from allowed_names) that matches name (case-insensitive), or None."""
    if not (name or "").strip():
        return None
    n = name.strip().lower()
    for a in allowed_names:
        if a.lower() == n:
            return a
    return None


def build_focused_schema_for_generate_sql(
    prompt: str,
    sql_context: Optional[str] = None,
    chat_history: Optional[list[dict]] = None,
    catalog_node_id: Optional[str] = None,
    entity: Optional[str] = None,
    entities: Optional[list[str]] = None,
    retriever_k: int = 12,
) -> str:
    """
    Build schema markdown for generate-sql by:
    1) Extracting table names from sql_context and assistant SQL in chat_history
    2) Using RAG retriever with user prompt to get relevant tables
    3) Adding entity/entities from request
    4) Including relations and related tables (descriptions)
    Returns markdown string for the LLM prompt.
    """
    schema_obj = get_schema_object(catalog_node_id=catalog_node_id)
    schema_entities = schema_obj.entities
    if not schema_entities:
        return build_schema_markdown(schema_obj)

    allowed_names = _entity_names_in_schema(schema_obj)
    focus_names: set[str] = set()

    # From request
    if entities:
        for e in entities:
            norm = _normalize_to_schema_name(e or "", allowed_names)
            if norm:
                focus_names.add(norm)
    if entity:
        norm = _normalize_to_schema_name(entity, allowed_names)
        if norm:
            focus_names.add(norm)

    # From SQL in context and chat history
    sql_list = [sql_context] if (sql_context or "").strip() else []
    sql_list.extend(extract_sql_from_chat_history(chat_history))
    for sql in sql_list:
        for t in extract_table_names_from_sql(sql):
            norm = _normalize_to_schema_name(t, allowed_names)
            if norm:
                focus_names.add(norm)

    # From RAG (user question)
    try:
        retriever = get_retriever(use_separate_endpoints=True, k=retriever_k)
        if retriever and (prompt or "").strip():
            docs = retriever.invoke((prompt or "").strip()[:500])
            for d in docs:
                display = (d.metadata.get("displayName") or d.metadata.get("name") or "").strip()
                norm = _normalize_to_schema_name(display, allowed_names)
                if norm:
                    focus_names.add(norm)
    except Exception as ex:
        logger.debug("RAG focus for generate_sql: %s", ex)

    if focus_names:
        logger.debug("generate_sql focused tables: %s", sorted(focus_names))
        return build_schema_markdown_focused(schema_obj, focus_names)
    return build_schema_markdown(schema_obj)
