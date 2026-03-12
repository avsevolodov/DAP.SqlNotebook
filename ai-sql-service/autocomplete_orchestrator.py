"""
Autocomplete orchestrator: runs deterministic suggestions, LLM (with timeout), merges and returns result.
"""
from __future__ import annotations

import logging

from autocomplete_deterministic import (
    MAX_AUTOCOMPLETE_SUGGESTIONS,
    MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE,
    MAX_RANKER_CANDIDATES,
    compute_deterministic_suggestions,
    rank_merge_suggestions,
)
from autocomplete_llm import (
    LLM_AUTOCOMPLETE_TIMEOUT_SECONDS,
    build_autocomplete_prompt,
    process_llm_autocomplete_response,
    run_autocomplete,
    run_ranker,
)
from rag_index import get_retriever
from schema_autocomplete import build_schema_compact_for_autocomplete
from schema_loader import get_schema_object
from sql_context import CursorContextType, get_cursor_context

logger = logging.getLogger(__name__)

RAG_RETRIEVER_K_AUTOCOMPLETE = 10
RAG_RELATED_K_AUTOCOMPLETE = 5
MAX_SNIPPET_LENGTH = 500


def run_autocomplete_sql(
    sql: str,
    cursor_position: int | None,
    entities: list[str] | None,
) -> list[str]:
    """
    Run full autocomplete pipeline: deterministic suggestions + optional LLM (with timeout).
    Returns ordered list of suggestion insert texts.
    """
    if not (sql or "").strip():
        return []
    cursor_pos = cursor_position if cursor_position is not None else len(sql)
    cursor_pos = max(0, min(len(sql), cursor_pos))
    cursor_ctx = get_cursor_context(sql, cursor_pos)
    schema_obj = get_schema_object()
    retriever = get_retriever(use_separate_endpoints=False, k=RAG_RETRIEVER_K_AUTOCOMPLETE)

    # Deterministic suggestions (no LLM)
    det = compute_deterministic_suggestions(
        cursor_ctx=cursor_ctx,
        schema=schema_obj,
        sql=sql,
        retriever=retriever,
        max_columns_per_table=MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE,
        max_ranker_candidates=MAX_RANKER_CANDIDATES,
    )
    logger.debug(
        "autocomplete deterministic candidates (top %d): %s",
        len(det.deterministic_candidates),
        det.deterministic_candidates,
    )

    # LLM: ranker (only for SELECT_LIST) and autocomplete (with timeout)
    from llm_utils import get_llm_autocomplete
    model = get_llm_autocomplete()
    llm_ranked_candidates: list[str] | None = None
    llm_ranker_expansions: list[str] = []
    if model and cursor_ctx.type == CursorContextType.SELECT_LIST and det.deterministic_candidates:
        sql_before = sql[:cursor_pos]
        llm_ranked_candidates, llm_ranker_expansions = run_ranker(
            model,
            sql_before_cursor=sql_before,
            cursor_ctx=cursor_ctx,
            schema=schema_obj,
            deterministic_candidates=det.deterministic_candidates,
            timeout_seconds=LLM_AUTOCOMPLETE_TIMEOUT_SECONDS,
        )

    # Schema text for LLM autocomplete prompt
    schema_text = build_schema_compact_for_autocomplete(
        schema_obj,
        focus_entity_names=entities or [],
        retriever=retriever,
        related_k=RAG_RELATED_K_AUTOCOMPLETE,
        max_columns_per_table=MAX_COLUMNS_PER_TABLE_AUTOCOMPLETE,
    )
    sql_before = sql[:cursor_pos]
    sql_after = sql[cursor_pos:]
    half = MAX_SNIPPET_LENGTH // 2
    sql_around = sql_before[-half:] + "|CURSOR|" + sql_after[:half]
    entities_hint = ", ".join(entities or []) or ""
    allowed_block = (
        "Cursor is inside SELECT list.\n\n"
        "Allowed completions:\n- columns\n- expressions\n- functions\n- commas\n"
    ) if cursor_ctx.type == CursorContextType.SELECT_LIST else ""

    # LLM autocomplete (with timeout); on timeout we keep llm_lines=[]
    llm_lines: list[str] = []
    if model:
        prompt = build_autocomplete_prompt(
            schema_text=schema_text,
            sql_around=sql_around,
            cursor_ctx=cursor_ctx,
            allowed_block=allowed_block,
            entities_hint=entities_hint,
        )
        llm_result = run_autocomplete(
            model,
            prompt,
            timeout_seconds=LLM_AUTOCOMPLETE_TIMEOUT_SECONDS,
        )
        llm_lines = process_llm_autocomplete_response(
            llm_result,
            ranker_expansions=llm_ranker_expansions,
            cursor_ctx=cursor_ctx,
            schema=schema_obj,
        )

    # Merge and rank: deterministic + LLM
    merged = rank_merge_suggestions(
        cursor_ctx=cursor_ctx,
        schema_suggestions=det.schema_suggestions,
        table_suggestions=det.table_suggestions,
        join_suggestions=det.join_suggestions,
        function_suggestions=det.function_suggestions,
        keyword_suggestions=det.keyword_suggestions,
        group_order_suggestions=det.group_order_suggestions,
        rag_suggestions=det.rag_suggestions,
        llm_suggestions=llm_lines,
        max_results=MAX_AUTOCOMPLETE_SUGGESTIONS,
        llm_ranked_candidates=llm_ranked_candidates or [],
    )
    logger.debug(
        "autocomplete summary deterministic=%s llm_order=%s final=%s",
        det.deterministic_candidates,
        llm_ranked_candidates,
        merged,
    )
    return merged
