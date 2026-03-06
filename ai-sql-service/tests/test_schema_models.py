import pytest

from schema_models import SchemaEntity, SchemaField


def test_schema_field_aliases_and_defaults() -> None:
    data = {
        "name": "Id",
        "DataType": "int",
        "IsPrimaryKey": True,
        "IsNullable": False,
        "description": "Primary key",
    }

    field = SchemaField.model_validate(data)

    assert field.name == "Id"
    assert field.data_type == "int"
    assert field.is_primary_key is True
    assert field.is_nullable is False
    assert field.description == "Primary key"


def test_schema_entity_aliases_and_helpers() -> None:
    data = {
        "Id": "42",
        "Name": "Customer",
        "DisplayName": "Customers",
        "Description": "Customer table",
        "Fields": [
            {
                "name": "Id",
                "dataType": "int",
                "isPrimaryKey": True,
                "isNullable": False,
            },
            {
                "Name": "Name",
                "DataType": "nvarchar(100)",
                "IsPrimaryKey": False,
                "IsNullable": False,
            },
        ],
    }

    entity = SchemaEntity.model_validate(data)

    assert entity.id == "42"
    assert entity.name == "Customer"
    assert entity.display_name == "Customers"
    assert entity.description == "Customer table"
    assert entity.display_or_name == "Customers"
    assert entity.logical_name == "Customer"

    assert len(entity.fields) == 2
    pk_field = entity.fields[0]
    assert pk_field.name == "Id"
    assert pk_field.is_primary_key is True
    assert pk_field.is_nullable is False

