from textwrap import dedent

from schema_loader import build_schema_markdown, build_schema_markdown_focused


def test_build_schema_markdown_single_entity() -> None:
    schema = {
        "entities": [
            {
                "Id": 1,
                "Name": "Customer",
                "DisplayName": "Customers",
                "Description": "Customer table",
                "Fields": [
                    {
                        "Name": "Id",
                        "DataType": "int",
                        "IsPrimaryKey": True,
                        "IsNullable": False,
                    },
                    {
                        "Name": "Name",
                        "DataType": "nvarchar(100)",
                        "IsPrimaryKey": False,
                        "IsNullable": False,
                    },
                ],
            }
        ],
        "relations": [],
    }

    markdown = build_schema_markdown(schema)

    assert "# Customers" in markdown
    assert "**Entity**: `Customer`" in markdown
    assert "- **Id** : `int` (PK, Required)" in markdown
    assert "- **Name** : `nvarchar(100)` (Required)" in markdown


def test_build_schema_markdown_focused_filters_entities() -> None:
    schema = {
        "entities": [
            {"Id": 1, "Name": "Customer", "DisplayName": "Customers"},
            {"Id": 2, "Name": "Order", "DisplayName": "Orders"},
        ],
        "relations": [
            {
                "FromEntityId": 2,
                "ToEntityId": 1,
                "FromFieldName": "CustomerId",
                "ToFieldName": "Id",
                "Name": "FK_Order_Customer",
            }
        ],
    }

    markdown = build_schema_markdown_focused(schema, {"Customer"})

    # Должна быть только сущность Customer и её связи
    assert "# Customers" in markdown
    assert "Orders" in markdown  # упоминается в разделе Relationships
    # Вторая сущность как отдельный заголовок не должна появляться
    assert markdown.count("# Orders") == 0

