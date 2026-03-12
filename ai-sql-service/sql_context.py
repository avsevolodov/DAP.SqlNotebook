from __future__ import annotations

import logging
import re
from dataclasses import dataclass
from enum import Enum
from typing import Dict, List, Optional, Tuple

import sqlglot
from sqlglot import expressions as exp


logger = logging.getLogger(__name__)


class CursorContextType(str, Enum):
    SELECT_LIST = "SELECT_LIST"
    COLUMN = "COLUMN"
    JOIN_TABLE = "JOIN_TABLE"
    FROM_TABLE = "FROM_TABLE"
    WHERE = "WHERE"
    GROUP_BY = "GROUP_BY"
    ORDER_BY = "ORDER_BY"
    OTHER = "OTHER"


@dataclass
class CursorContext:
    type: CursorContextType
    prefix: str = ""
    tables: List[str] | None = None
    aliases: Dict[str, str] | None = None  # alias -> table_name
    table: Optional[str] = None
    table_alias: Optional[str] = None
    existing_tables: List[str] | None = None


_IDENT_TAIL_RE = re.compile(r"([A-Za-z0-9_\]\.]+)$", re.IGNORECASE)


def _split_sql_by_cursor(sql: str, cursor_pos: int | None = None) -> Tuple[str, str]:
    if cursor_pos is None:
        cursor_pos = len(sql)
    cursor_pos = max(0, min(len(sql), cursor_pos))
    return sql[:cursor_pos], sql[cursor_pos:]


def _extract_prefix(sql_before: str) -> str:
    if not sql_before:
        return ""
    m = _IDENT_TAIL_RE.search(sql_before.rstrip())
    return m.group(1) if m else ""


def _build_tables_and_aliases(sql_for_parse: str) -> tuple[list[str], dict[str, str]]:
    """
    Use sqlglot to extract tables and aliases from (possibly incomplete) SQL.
    Returns (tables, aliases) where aliases maps alias -> table_name.
    """
    tables: list[str] = []
    aliases: dict[str, str] = {}

    if not sql_for_parse.strip():
        return tables, aliases

    try:
        parsed = sqlglot.parse_one(sql_for_parse, read="tsql")
    except Exception:
        return tables, aliases

    for t in parsed.find_all(exp.Table):
        name = t.name
        if not name:
            continue
        tables.append(name)
        if t.alias:
            alias_name = t.alias_or_name
            if alias_name and alias_name != name:
                aliases[alias_name] = name

    # Also allow using table name itself as alias in simple cases
    for name in tables:
        aliases.setdefault(name, name)

    return tables, aliases


def get_cursor_context(sql: str, cursor_pos: int | None = None) -> CursorContext:
    """
    Infer cursor context from SQL:
    - type: SELECT_LIST, COLUMN, JOIN_TABLE, OTHER
    - prefix: current identifier prefix at cursor
    - tables/aliases: tables and aliases discovered by parser
    """
    sql_before, _ = _split_sql_by_cursor(sql or "", cursor_pos)
    sql_before_stripped = sql_before.rstrip()

    prefix = _extract_prefix(sql_before_stripped)
    # Treat SQL keywords as "no identifier" so that SELECT | does not use prefix "select".
    if prefix.lower() in {
        "select",
        "from",
        "where",
        "group",
        "order",
        "by",
        "join",
        "on",
    }:
        prefix = ""

    # For parser, cut off clearly incomplete tail after last JOIN/WHERE/ON close to cursor
    sql_for_parse = sql_before_stripped
    for kw in (" join ", " JOIN ", " where ", " WHERE ", " on ", " ON "):
        idx = sql_for_parse.rfind(kw)
        if idx != -1 and idx + len(kw) > len(sql_for_parse) - 40:
            sql_for_parse = sql_for_parse[:idx]
            break

    tables, aliases = _build_tables_and_aliases(sql_for_parse)

    ctx: CursorContext | None = None

    # COLUMN: alias.|  or  alias.col|
    m_alias_dot = re.search(r"([A-Za-z0-9_\]]+)\.\Z", sql_before_stripped)
    if m_alias_dot:
        alias = m_alias_dot.group(1)
        table = aliases.get(alias)
        ctx = CursorContext(
            type=CursorContextType.COLUMN,
            prefix="",
            tables=tables,
            aliases=aliases,
            table_alias=alias,
            table=table,
        )
    else:
        m_alias_col = re.search(r"([A-Za-z0-9_\]]+)\.([A-Za-z0-9_\]]*)\Z", sql_before_stripped)
        if m_alias_col:
            alias, col_prefix = m_alias_col.group(1), m_alias_col.group(2)
            table = aliases.get(alias)
            ctx = CursorContext(
                type=CursorContextType.COLUMN,
                prefix=col_prefix,
                tables=tables,
                aliases=aliases,
                table_alias=alias,
                table=table,
            )

    # JOIN_TABLE: after JOIN but before table name finished
    if ctx is None:
        join_tail = re.search(r"\bJOIN\s+([A-Za-z0-9_\]]*)\Z", sql_before_stripped, re.IGNORECASE)
        if join_tail:
            join_prefix = join_tail.group(1)
            existing_tables = list(tables)
            ctx = CursorContext(
                type=CursorContextType.JOIN_TABLE,
                prefix=join_prefix,
                tables=tables,
                aliases=aliases,
                existing_tables=existing_tables,
            )

    # FROM_TABLE: after FROM but before table name finished
    if ctx is None:
        from_tail = re.search(r"\bFROM\s+([A-Za-z0-9_\]]*)\Z", sql_before_stripped, re.IGNORECASE)
        if from_tail:
            from_prefix = from_tail.group(1)
            existing_tables = list(tables)
            ctx = CursorContext(
                type=CursorContextType.FROM_TABLE,
                prefix=from_prefix,
                tables=tables,
                aliases=aliases,
                existing_tables=existing_tables,
            )

    # WHERE: after WHERE but before expression finished
    if ctx is None:
        where_tail = re.search(
            r"\bWHERE\s+([A-Za-z0-9_\]\.\[]*)\Z",
            sql_before_stripped,
            re.IGNORECASE | re.DOTALL,
        )
        if where_tail:
            where_prefix = where_tail.group(1)
            ctx = CursorContext(
                type=CursorContextType.WHERE,
                prefix=where_prefix,
                tables=tables,
                aliases=aliases,
                existing_tables=list(tables),
            )

    # GROUP_BY: after GROUP BY but before expression finished
    if ctx is None:
        group_tail = re.search(
            r"\bGROUP\s+BY\s+.*?,\s*([A-Za-z0-9_\]\.\[]*)\Z",
            sql_before_stripped,
            re.IGNORECASE | re.DOTALL,
        )
        if not group_tail:
            group_tail = re.search(
                r"\bGROUP\s+BY\s+([A-Za-z0-9_\]\.\[]*)\Z",
                sql_before_stripped,
                re.IGNORECASE | re.DOTALL,
            )
        if group_tail:
            group_prefix = group_tail.group(1)
            ctx = CursorContext(
                type=CursorContextType.GROUP_BY,
                prefix=group_prefix,
                tables=tables,
                aliases=aliases,
                existing_tables=list(tables),
            )

    # ORDER_BY: after ORDER BY but before expression finished
    if ctx is None:
        order_tail = re.search(
            r"\bORDER\s+BY\s+.*?,\s*([A-Za-z0-9_\]\.\[]*)\Z",
            sql_before_stripped,
            re.IGNORECASE | re.DOTALL,
        )
        if not order_tail:
            order_tail = re.search(
                r"\bORDER\s+BY\s+([A-Za-z0-9_\]\.\[]*)\Z",
                sql_before_stripped,
                re.IGNORECASE | re.DOTALL,
            )
        if order_tail:
            order_prefix = order_tail.group(1)
            ctx = CursorContext(
                type=CursorContextType.ORDER_BY,
                prefix=order_prefix,
                tables=tables,
                aliases=aliases,
                existing_tables=list(tables),
            )

    # SELECT_LIST: after SELECT and before FROM
    if ctx is None:
        select_tail = re.search(
            r"SELECT\s+(.*)\Z",
            sql_before_stripped,
            re.IGNORECASE | re.DOTALL,
        )
        if select_tail and " from " not in select_tail.group(1).lower():
            ctx = CursorContext(
                type=CursorContextType.SELECT_LIST,
                prefix=prefix,
                tables=tables,
                aliases=aliases,
            )

    if ctx is None:
        ctx = CursorContext(
            type=CursorContextType.OTHER,
            prefix=prefix,
            tables=tables,
            aliases=aliases,
        )

    # Log compact debug info about inferred context.
    try:
        logger.debug(
            "sqlglot cursor_ctx type=%s prefix=%r tables=%s aliases=%s table=%r table_alias=%r existing_tables=%s sql_tail=%r",
            ctx.type.value,
            ctx.prefix,
            ctx.tables or [],
            list((ctx.aliases or {}).keys()),
            ctx.table,
            ctx.table_alias,
            ctx.existing_tables or [],
            sql_before_stripped[-160:],
        )
    except Exception:
        # Logging should never break autocomplete.
        pass

    return ctx

