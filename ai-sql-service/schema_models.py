# Pydantic models for schema API (entities and fields). Accept camelCase or PascalCase from API.
from typing import Optional

from pydantic import AliasChoices, BaseModel, Field


class SchemaField(BaseModel):
    """A column/field of an entity."""

    name: str = ""
    data_type: Optional[str] = Field(None, validation_alias=AliasChoices("dataType", "DataType"))
    is_primary_key: bool = Field(False, validation_alias=AliasChoices("isPrimaryKey", "IsPrimaryKey"))
    is_nullable: bool = Field(True, validation_alias=AliasChoices("isNullable", "IsNullable"))
    description: Optional[str] = None

    model_config = {"populate_by_name": True}


class SchemaEntity(BaseModel):
    """An entity (table) from the schema API."""

    id: Optional[str] = None
    name: str = ""
    display_name: Optional[str] = Field(None, validation_alias=AliasChoices("displayName", "DisplayName"))
    description: Optional[str] = None
    markdown_description: Optional[str] = Field(
        None, validation_alias=AliasChoices("markdownDescription", "MarkdownDescription")
    )
    fields: list[SchemaField] = Field(default_factory=list)

    model_config = {"populate_by_name": True}

    @property
    def display_or_name(self) -> str:
        return (self.display_name or self.name or "?").strip()

    @property
    def logical_name(self) -> str:
        return (self.name or "").strip()
