"""
Pydantic models for RAG index: entities, relations, catalog nodes.
Accept camelCase/PascalCase from API and coerce to plain types (str/bool).
"""
from __future__ import annotations

from typing import Any

from pydantic import AliasChoices, BaseModel, Field, field_validator, model_validator


def _str_or_empty(v: Any) -> str:
    if v is None:
        return ""
    return str(v).strip()


def _coerce_id(v: Any) -> str:
    if v is None:
        return ""
    return str(v).strip()


class RagField(BaseModel):
    """Column/field for RAG document building."""

    name: str = Field("", description="Column name", validation_alias=AliasChoices("name", "Name"))
    data_type: str = Field("", validation_alias=AliasChoices("dataType", "DataType"))
    is_primary_key: bool = Field(False, validation_alias=AliasChoices("isPrimaryKey", "IsPrimaryKey"))

    model_config = {"populate_by_name": True}

    @field_validator("name", "data_type", mode="before")
    @classmethod
    def str_fields(cls, v: Any) -> str:
        return _str_or_empty(v)

    @field_validator("is_primary_key", mode="before")
    @classmethod
    def bool_pk(cls, v: Any) -> bool:
        if v is None:
            return False
        if isinstance(v, bool):
            return v
        return bool(v)


class RagRelation(BaseModel):
    """Relation between two entities for RAG document building."""

    from_entity_id: str = Field("", validation_alias=AliasChoices("fromEntityId", "FromEntityId"))
    to_entity_id: str = Field("", validation_alias=AliasChoices("toEntityId", "ToEntityId"))
    from_field_name: str = Field("", validation_alias=AliasChoices("fromFieldName", "FromFieldName"))
    to_field_name: str = Field("", validation_alias=AliasChoices("toFieldName", "ToFieldName"))
    name: str = ""

    model_config = {"populate_by_name": True}

    @field_validator("from_entity_id", "to_entity_id", "from_field_name", "to_field_name", "name", mode="before")
    @classmethod
    def str_fields(cls, v: Any) -> str:
        return _str_or_empty(v)


class RagEntity(BaseModel):
    """Entity (table) with fields and optional relations for RAG indexing."""

    id: str = Field("", description="Entity id (string)", validation_alias=AliasChoices("id", "Id"))
    name: str = Field("", description="Logical name", validation_alias=AliasChoices("name", "Name"))
    display_name: str = Field("", validation_alias=AliasChoices("displayName", "DisplayName"))
    description: str = Field("", validation_alias=AliasChoices("description", "Description"))
    schema_name: str = Field(
        "",
        description="Schema name (e.g. dbo)",
        validation_alias=AliasChoices("schemaName", "SchemaName"),
    )
    database_name: str = Field(
        "",
        description="Database name (initial catalog)",
        validation_alias=AliasChoices("databaseName", "DatabaseName"),
    )
    sample_sql: str = Field(
        "",
        description="Sample SELECT text representing table contents.",
    )
    fields: list[RagField] = Field(default_factory=list, validation_alias=AliasChoices("fields", "Fields"))
    relations: list[RagRelation] = Field(default_factory=list)

    model_config = {"populate_by_name": True}

    @field_validator("id", mode="before")
    @classmethod
    def id_str(cls, v: Any) -> str:
        return _coerce_id(v)

    @field_validator("name", "display_name", "description", "schema_name", "database_name", "sample_sql", mode="before")
    @classmethod
    def str_fields(cls, v: Any) -> str:
        return _str_or_empty(v)

    @field_validator("fields", "relations", mode="before")
    @classmethod
    def list_or_default(cls, v: Any) -> Any:
        if v is None:
            return []
        return v


class RagCatalogNode(BaseModel):
    """Catalog node (database/source) for RAG context."""

    id: str = Field("", validation_alias=AliasChoices("id", "Id"))
    name: str = Field("", validation_alias=AliasChoices("name", "Name"))
    node_type: str = Field("", validation_alias=AliasChoices("type", "Type"))

    model_config = {"populate_by_name": True}

    @field_validator("id", "name", mode="before")
    @classmethod
    def str_fields(cls, v: Any) -> str:
        return _str_or_empty(v)

    @field_validator("node_type", mode="before")
    @classmethod
    def norm_type(cls, v: Any) -> str:
        return _str_or_empty(v).lower()


class RagSchema(BaseModel):
    """Schema payload: entities + relations (from GET /api/v1/schema or dict)."""

    entities: list[RagEntity] = Field(default_factory=list)
    relations: list[RagRelation] = Field(default_factory=list)

    model_config = {"populate_by_name": True}

    @model_validator(mode="before")
    @classmethod
    def normalize_schema_keys(cls, data: Any) -> Any:
        if not isinstance(data, dict):
            return data
        entities = data.get("entities") or data.get("Entities") or []
        relations = data.get("relations") or data.get("Relations") or []
        return {"entities": entities, "relations": relations}
