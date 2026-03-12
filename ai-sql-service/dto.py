"""Request/response DTOs for AI SQL API."""
from typing import Optional

from pydantic import BaseModel


class ChatTurn(BaseModel):
    role: int  # 0=user, 1=assistant
    content: Optional[str] = None


class GenerateSqlRequest(BaseModel):
    prompt: str
    entity: Optional[str] = None
    entities: Optional[list[str]] = None
    sql_context: Optional[str] = None
    database_name: Optional[str] = None
    catalog_node_id: Optional[str] = None  # When set, fetch schema for this source only
    chat_history: Optional[list[ChatTurn]] = None


class GenerateSqlResponse(BaseModel):
    sql: str
    explanation: Optional[str] = None


class AutocompleteSqlRequest(BaseModel):
    sql: str
    entities: Optional[list[str]] = None
    cursor_position: Optional[int] = None


class AutocompleteSuggestionItem(BaseModel):
    label: str  # preview for UI
    insertText: str


class AutocompleteSqlResponse(BaseModel):
    suggestion: str = ""  # backward compat, single first suggestion
    suggestions: list[AutocompleteSuggestionItem] = []


class FindTablesRequest(BaseModel):
    """User description of what they are looking for; AI ranks tables by relevance."""

    description: str


class FindTablesResponse(BaseModel):
    """Ordered list of table names (display or logical) most relevant to the description."""

    tables: list[str] = []
