from generate_sql_schema import _entity_names_in_schema, _normalize_to_schema_name


def test_entity_names_in_schema_collects_name_and_display() -> None:
    schema = {
        "entities": [
            {"Name": "Customer", "DisplayName": "Customers"},
            {"name": "Order", "displayName": "Orders"},
        ]
    }

    names = _entity_names_in_schema(schema)

    assert names == {"Customer", "Customers", "Order", "Orders"}


def test_normalize_to_schema_name_case_insensitive() -> None:
    allowed = {"Customer", "Orders"}

    assert _normalize_to_schema_name("customer", allowed) == "Customer"
    assert _normalize_to_schema_name("ORDERS", allowed) == "Orders"
    assert _normalize_to_schema_name("unknown", allowed) is None

