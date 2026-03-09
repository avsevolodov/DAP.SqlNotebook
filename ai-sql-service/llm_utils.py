"""LLM client build, lazy init, and output helpers."""
from typing import Optional

from langchain_openai import ChatOpenAI

from config import (
    autocomplete_api_key,
    autocomplete_base_url,
    autocomplete_model,
    find_tables_api_key,
    find_tables_base_url,
    find_tables_model,
    generate_sql_model,
    llm_api_key,
    llm_base_url,
)


def format_llm_error(exc: Exception, model_name: str) -> str:
    msg = str(exc)
    if "404" in msg and ("not found" in msg.lower() or "model" in msg.lower()):
        return (
            f"Model '{model_name}' not found in Ollama. "
            f"Pull it with: ollama pull {model_name} "
            f"(or in Docker: docker exec ollama ollama pull {model_name}). Original: {msg}"
        )
    return f"LLM error: {msg}"


def strip_sql_markdown(text: str) -> str:
    """Remove ```sql or ``` code fences from LLM output so we return raw SQL."""
    if not text or not text.strip():
        return text
    s = text.strip()
    if s.lower().startswith("```sql"):
        s = s[6:].strip()
    elif s.startswith("```"):
        s = s[3:].strip()
    if s.endswith("```"):
        s = s[:-3].strip()
    return s


def build_llm() -> ChatOpenAI:
    """LLM for generate-sql. Override via LLM_GENERATE_SQL_MODEL or LLM_MODEL."""
    return ChatOpenAI(
        api_key=llm_api_key(),
        base_url=llm_base_url(),
        model=generate_sql_model(),
        temperature=0.0,
    )


def build_llm_autocomplete() -> ChatOpenAI:
    """LLM for autocomplete; overridable via LLM_AUTOCOMPLETE_* env."""
    return ChatOpenAI(
        api_key=autocomplete_api_key(),
        base_url=autocomplete_base_url(),
        model=autocomplete_model(),
        temperature=0.0,
    )


def build_llm_find_tables() -> ChatOpenAI:
    """Smaller/faster LLM for find-tables only. Override via LLM_FIND_TABLES_* env."""
    return ChatOpenAI(
        api_key=find_tables_api_key(),
        base_url=find_tables_base_url(),
        model=find_tables_model(),
        temperature=0.0,
    )


# Lazy singletons (set by get_llm / get_llm_autocomplete / get_llm_find_tables)
_llm: Optional[ChatOpenAI] = None
_llm_autocomplete: Optional[ChatOpenAI] = None
_llm_find_tables: Optional[ChatOpenAI] = None


def get_llm() -> ChatOpenAI:
    """Lazy init: build LLM on first use so server starts fast."""
    global _llm
    if _llm is None:
        _llm = build_llm()
    return _llm


def get_llm_autocomplete() -> ChatOpenAI:
    """Lazy init: build autocomplete LLM on first use."""
    global _llm_autocomplete
    if _llm_autocomplete is None:
        _llm_autocomplete = build_llm_autocomplete()
    return _llm_autocomplete


def get_llm_find_tables() -> ChatOpenAI:
    """Lazy init: smaller model for find-tables (faster for tests)."""
    global _llm_find_tables
    if _llm_find_tables is None:
        _llm_find_tables = build_llm_find_tables()
    return _llm_find_tables
