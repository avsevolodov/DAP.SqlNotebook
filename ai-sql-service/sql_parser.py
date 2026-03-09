"""Extract table (and optionally column) names from SQL for schema focus."""
from __future__ import annotations

import re
from typing import Set

# Match table references: FROM/JOIN/INTO/UPDATE [schema.]table, support [bracket] and plain names
_TABLE_PATTERN = re.compile(
    r"\b(?:FROM|JOIN|INTO|UPDATE)\s+"
    r"(?:\[?\w+\]?\.)?"
    r"\[?(\w+)\]?",
    re.IGNORECASE | re.MULTILINE,
)


def extract_table_names_from_sql(sql: str) -> Set[str]:
    """
    Parse SQL text and return set of table names (logical names, no schema prefix).
    Handles FROM, JOIN, INTO, UPDATE. Supports [bracket] and plain identifiers.
    """
    if not (sql or "").strip():
        return set()
    names: Set[str] = set()
    for m in _TABLE_PATTERN.finditer(sql):
        name = (m.group(1) or "").strip()
        if name and name.upper() not in ("SELECT", "WHERE", "ON", "AND", "OR", "AS"):
            names.add(name)
    return names


def extract_sql_from_chat_history(chat_history: list[dict] | None) -> list[str]:
    """
    Collect SQL snippets from chat history: assistant messages that look like SQL.
    Returns list of non-empty SQL strings.
    """
    if not chat_history:
        return []
    sql_list: list[str] = []
    for turn in chat_history:
        if (turn.role or 0) != 1:
            continue  # assistant = 1
        content = (turn.content or "").strip()
        if not content:
            continue
        # Heuristic: contains SELECT/INSERT/UPDATE/DELETE and typical SQL
        upper = content.upper()
        if any(kw in upper for kw in ("SELECT ", "INSERT ", "UPDATE ", "DELETE ", "FROM ", "JOIN ")):
            # Strip markdown code fence if present
            if content.startswith("```"):
                lines = content.split("\n")
                if lines[0].upper().startswith("```SQL"):
                    content = "\n".join(lines[1:])
                else:
                    content = "\n".join(lines[1:])
                if content.endswith("```"):
                    content = content[:-3].strip()
            if content.strip():
                sql_list.append(content.strip())
    return sql_list
