"""Pydantic DTOs mirroring C# Contract entities used by AI SQL service.

These models provide typed access to data returned by the C# backend
for catalog and schema operations, so that callers avoid ad-hoc dict parsing.
"""
from __future__ import annotations

from typing import List, Optional

from pydantic import AliasChoices, BaseModel, Field


class ContractBaseModel(BaseModel):
    """Base Pydantic config for Contract DTOs."""

    model_config = {"populate_by_name": True}


class CatalogNodeInfoDto(ContractBaseModel):
    """Catalog tree node (source / database / table) for API and UI."""

    id: str = Field(
        "",
        description="Node id (GUID as string)",
        validation_alias=AliasChoices("id", "Id"),
    )
    parent_id: Optional[str] = Field(
        None,
        description="Parent node id",
        validation_alias=AliasChoices("parentId", "ParentId"),
    )
    type: str = Field(
        "",
        description="Node type (Folder/Database/Table/Topic)",
        validation_alias=AliasChoices("type", "Type"),
    )
    name: str = Field(
        "",
        description="Display name of the node",
        validation_alias=AliasChoices("name", "Name"),
    )
    description: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("description", "Description"),
    )
    owner: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("owner", "Owner"),
    )
    provider: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("provider", "Provider"),
    )
    has_children: bool = Field(
        False,
        validation_alias=AliasChoices("hasChildren", "HasChildren"),
    )
    entity_id: Optional[str] = Field(
        None,
        description="Linked DbEntityInfo.Id for table nodes",
        validation_alias=AliasChoices("entityId", "EntityId"),
    )
    qualified_name: Optional[str] = Field(
        None,
        description="Qualified entity name, if available",
        validation_alias=AliasChoices("qualifiedName", "QualifiedName"),
    )
    connection_info: Optional[str] = Field(
        None,
        description="Connection string or server (for source/database nodes)",
        validation_alias=AliasChoices("connectionInfo", "ConnectionInfo"),
    )
    database_name: Optional[str] = Field(
        None,
        description="Database name / initial catalog (for source nodes)",
        validation_alias=AliasChoices("databaseName", "DatabaseName"),
    )
    auth_type: Optional[str] = Field(
        None,
        description='Auth mode: "Basic" or "Kerberos".',
        validation_alias=AliasChoices("authType", "AuthType"),
    )
    login: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("login", "Login"),
    )
    consumer_group_prefix: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("consumerGroupPrefix", "ConsumerGroupPrefix"),
    )
    consumer_group_auto_generate: bool = Field(
        False,
        validation_alias=AliasChoices(
            "consumerGroupAutoGenerate", "ConsumerGroupAutoGenerate"
        ),
    )


class DbEntityInfoDto(ContractBaseModel):
    """Logical database entity (table) description."""

    id: str = Field(
        "",
        description="Entity id (GUID as string)",
        validation_alias=AliasChoices("id", "Id"),
    )
    name: str = Field(
        "",
        description="Logical table name",
        validation_alias=AliasChoices("name", "Name"),
    )
    display_name: Optional[str] = Field(
        None,
        description="Display name for UI",
        validation_alias=AliasChoices("displayName", "DisplayName"),
    )
    schema_name: Optional[str] = Field(
        None,
        description="Schema name (e.g. dbo)",
        validation_alias=AliasChoices("schemaName", "SchemaName"),
    )
    description: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("description", "Description"),
    )


class DbFieldInfoDto(ContractBaseModel):
    """Logical database field (column) description."""

    id: str = Field(
        "",
        description="Field id (GUID as string)",
        validation_alias=AliasChoices("id", "Id"),
    )
    entity_id: str = Field(
        "",
        description="Owning entity id",
        validation_alias=AliasChoices("entityId", "EntityId"),
    )
    name: str = Field(
        "",
        description="Column name",
        validation_alias=AliasChoices("name", "Name"),
    )
    data_type: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("dataType", "DataType"),
    )
    is_nullable: bool = Field(
        True,
        validation_alias=AliasChoices("isNullable", "IsNullable"),
    )
    is_primary_key: bool = Field(
        False,
        validation_alias=AliasChoices("isPrimaryKey", "IsPrimaryKey"),
    )
    description: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("description", "Description"),
    )


class EntitiesPageResultDto(ContractBaseModel):
    """Paged result for entities by source node."""

    items: List[DbEntityInfoDto] = Field(
        default_factory=list,
        validation_alias=AliasChoices("items", "Items"),
    )
    total_count: int = Field(
        0,
        validation_alias=AliasChoices("totalCount", "TotalCount"),
    )


class SchemaImportResultInfoDto(ContractBaseModel):
    """Result of importing schema structure from a source."""

    tables_count: int = Field(
        0,
        validation_alias=AliasChoices("tablesCount", "TablesCount"),
    )
    fields_count: int = Field(
        0,
        validation_alias=AliasChoices("fieldsCount", "FieldsCount"),
    )
    error: Optional[str] = Field(
        None,
        validation_alias=AliasChoices("error", "Error"),
    )

