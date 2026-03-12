"""
Tests for deterministic autocomplete suggestion functions (no LLM).
All *_suggestions_for_context functions are pure: schema/SQL in -> list[str] out.
"""
from __future__ import annotations

import pytest

from rag_models import RagEntity, RagField, RagRelation, RagSchema
from autocomplete_deterministic import (
    function_suggestions_for_context,
    join_suggestions_for_context,
    keyword_suggestions_for_context,
    schema_suggestions_for_context,
    table_suggestions_for_context,
    group_order_suggestions_for_context,
)
from sql_context import CursorContext, CursorContextType


def _make_entity(
    eid: str,
    name: str,
    display_name: str = "",
    fields: list[dict] | None = None,
) -> RagEntity:
    display_name = display_name or name
    raw_fields = fields or [{"name": "Id", "dataType": "int", "isPrimaryKey": True}]
    return RagEntity(
        id=eid,
        name=name,
        display_name=display_name,
        fields=[RagField(**f) for f in raw_fields],
    )


@pytest.fixture
def schema_two_tables() -> RagSchema:
    """Schema: Customers (id, email, name), Orders (id, customer_id, amount)."""
    customers = _make_entity(
        "ent-customers",
        "Customer",
        "Customers",
        [
            {"name": "Id", "dataType": "int", "isPrimaryKey": True},
            {"name": "Email", "dataType": "nvarchar(255)", "isPrimaryKey": False},
            {"name": "Name", "dataType": "nvarchar(100)", "isPrimaryKey": False},
        ],
    )
    orders = _make_entity(
        "ent-orders",
        "Order",
        "Orders",
        [
            {"name": "Id", "dataType": "int", "isPrimaryKey": True},
            {"name": "CustomerId", "dataType": "int", "isPrimaryKey": False},
            {"name": "Amount", "dataType": "decimal(18,2)", "isPrimaryKey": False},
        ],
    )
    rel = RagRelation(
        from_entity_id="ent-orders",
        to_entity_id="ent-customers",
        from_field_name="CustomerId",
        to_field_name="Id",
    )
    return RagSchema(entities=[customers, orders], relations=[rel])


# --- schema_suggestions_for_context ---


def test_schema_suggestions_select_list_no_from_suggests_tables(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.SELECT_LIST,
        prefix="",
        tables=None,
        aliases=None,
    )
    out = schema_suggestions_for_context(ctx, schema_two_tables, max_columns_per_table=10)
    assert "Customers" in out
    assert "Orders" in out


def test_schema_suggestions_select_list_with_prefix_filters_tables(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.SELECT_LIST,
        prefix="Ord",
        tables=None,
        aliases=None,
    )
    out = schema_suggestions_for_context(ctx, schema_two_tables, max_columns_per_table=10)
    assert out == ["Orders"]


def test_schema_suggestions_select_list_with_aliases_suggests_columns(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.SELECT_LIST,
        prefix="c.",
        tables=["Customers"],
        aliases={"c": "Customers"},
    )
    out = schema_suggestions_for_context(ctx, schema_two_tables, max_columns_per_table=10)
    assert "c.Id" in out
    assert "c.Email" in out
    assert "c.Name" in out


def test_schema_suggestions_column_context_table_alias_suggests_columns(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.COLUMN,
        prefix="em",
        table="Customers",
        table_alias="c",
    )
    out = schema_suggestions_for_context(ctx, schema_two_tables, max_columns_per_table=10)
    assert "Email" in out


def test_schema_suggestions_order_by_suggests_aliases(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.ORDER_BY,
        prefix="c",
        tables=["Customers"],
        aliases={"c": "Customers"},
    )
    out = schema_suggestions_for_context(ctx, schema_two_tables, max_columns_per_table=10)
    assert "c." in out


# --- join_suggestions_for_context ---


def test_join_suggestions_join_table_context_returns_join_snippets(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.JOIN_TABLE,
        prefix="",
        tables=["Orders"],
        existing_tables=["Orders"],
    )
    out = join_suggestions_for_context(ctx, schema_two_tables)
    assert any("JOIN" in s and "Customers" in s and "ON" in s for s in out)


def test_join_suggestions_wrong_context_returns_empty() -> None:
    ctx = CursorContext(type=CursorContextType.SELECT_LIST, prefix="")
    schema = RagSchema(entities=[], relations=[])
    out = join_suggestions_for_context(ctx, schema)
    assert out == []


def test_join_suggestions_no_existing_tables_returns_empty(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.JOIN_TABLE,
        prefix="",
        tables=[],
        existing_tables=[],
    )
    out = join_suggestions_for_context(ctx, schema_two_tables)
    assert out == []


# --- table_suggestions_for_context ---


def test_table_suggestions_from_table_returns_table_names(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.FROM_TABLE,
        prefix="",
    )
    out = table_suggestions_for_context(ctx, schema_two_tables)
    assert "Customers" in out
    assert "Orders" in out


def test_table_suggestions_join_table_returns_table_names(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(
        type=CursorContextType.JOIN_TABLE,
        prefix="Cus",
    )
    out = table_suggestions_for_context(ctx, schema_two_tables)
    assert "Customers" in out


def test_table_suggestions_select_list_returns_empty(
    schema_two_tables: RagSchema,
) -> None:
    ctx = CursorContext(type=CursorContextType.SELECT_LIST, prefix="")
    out = table_suggestions_for_context(ctx, schema_two_tables)
    assert out == []


# --- function_suggestions_for_context ---


def test_function_suggestions_select_list_returns_functions() -> None:
    ctx = CursorContext(type=CursorContextType.SELECT_LIST, prefix="")
    out = function_suggestions_for_context(ctx)
    assert "COUNT(" in out
    assert "SUM(" in out
    assert "AVG(" in out


def test_function_suggestions_prefix_filters() -> None:
    ctx = CursorContext(type=CursorContextType.SELECT_LIST, prefix="COU")
    out = function_suggestions_for_context(ctx)
    assert out == ["COUNT("]


def test_function_suggestions_wrong_context_returns_empty() -> None:
    ctx = CursorContext(type=CursorContextType.FROM_TABLE, prefix="")
    out = function_suggestions_for_context(ctx)
    assert out == []


# --- keyword_suggestions_for_context ---


def test_keyword_suggestions_select_list_returns_distinct_top() -> None:
    ctx = CursorContext(type=CursorContextType.SELECT_LIST, prefix="")
    out = keyword_suggestions_for_context(ctx)
    assert "DISTINCT" in out
    assert "TOP" in out


def test_keyword_suggestions_select_list_prefix_filters() -> None:
    ctx = CursorContext(type=CursorContextType.SELECT_LIST, prefix="DI")
    out = keyword_suggestions_for_context(ctx)
    assert out == ["DISTINCT"]


def test_keyword_suggestions_where_returns_and_or_in() -> None:
    ctx = CursorContext(type=CursorContextType.WHERE, prefix="")
    out = keyword_suggestions_for_context(ctx)
    assert "AND" in out
    assert "OR" in out
    assert "IN" in out


def test_keyword_suggestions_group_by_returns_rollup() -> None:
    ctx = CursorContext(type=CursorContextType.GROUP_BY, prefix="")
    out = keyword_suggestions_for_context(ctx)
    assert "ROLLUP" in out


def test_keyword_suggestions_order_by_returns_empty() -> None:
    ctx = CursorContext(type=CursorContextType.ORDER_BY, prefix="")
    out = keyword_suggestions_for_context(ctx)
    assert out == []


# --- group_order_suggestions_for_context ---


def test_group_order_suggestions_group_by_from_select_aliases() -> None:
    sql = "SELECT c.Id AS id, c.Email AS email FROM Customers c"
    ctx = CursorContext(
        type=CursorContextType.GROUP_BY,
        prefix="",
        tables=["Customers"],
        aliases={"c": "Customers"},
    )
    out = group_order_suggestions_for_context(ctx, sql)
    assert "id" in out
    assert "email" in out


def test_group_order_suggestions_order_by_from_select_aliases() -> None:
    sql = "SELECT Id, Email FROM Customers"
    ctx = CursorContext(
        type=CursorContextType.ORDER_BY,
        prefix="Em",
        tables=["Customers"],
    )
    out = group_order_suggestions_for_context(ctx, sql)
    assert "Email" in out


def test_group_order_suggestions_group_by_skips_aggregates() -> None:
    sql = "SELECT region, COUNT(*) AS cnt FROM t GROUP BY"
    ctx = CursorContext(
        type=CursorContextType.GROUP_BY,
        prefix="",
    )
    out = group_order_suggestions_for_context(ctx, sql)
    assert "region" in out
    assert "cnt" not in out


def test_group_order_suggestions_wrong_context_returns_empty() -> None:
    ctx = CursorContext(type=CursorContextType.SELECT_LIST, prefix="")
    out = group_order_suggestions_for_context(ctx, "SELECT 1")
    assert out == []
