"""Configuration from environment: LLM, schema, RAG."""
import os

# Default: Ollama (docker-compose maps 7869 -> 11434)
LLM_API_URL = "http://localhost:7869/v1"
LLM_API_KEY = "ollama"
# SQL generator (generate-sql). Env: LLM_GENERATE_SQL_MODEL or LLM_MODEL.
DEFAULT_LLM_MODEL = "tinyllama"
# Autocomplete. Env: LLM_AUTOCOMPLETE_MODEL (no fallback to LLM_MODEL).
LLM_AUTOCOMPLETE_MODEL = "tinyllama"
# Smaller/faster model for find-tables (RAG fallback); e.g. tinyllama, phi, qwen2:0.5b
LLM_FIND_TABLES_MODEL = "tinyllama"

SCHEMA_CACHE_TTL_SECONDS_DEFAULT = 600  # 10 minutes


def llm_base_url() -> str:
    return os.getenv("LLM_API_URL", LLM_API_URL)


def llm_api_key() -> str:
    return os.getenv("LLM_API_KEY", LLM_API_KEY)


def llm_model() -> str:
    """SQL generator model. Backward compat: prefer LLM_GENERATE_SQL_MODEL, fallback LLM_MODEL."""
    return os.getenv("LLM_GENERATE_SQL_MODEL") or os.getenv("LLM_MODEL", DEFAULT_LLM_MODEL)


def generate_sql_model() -> str:
    """Model for generate-sql endpoint. Env: LLM_GENERATE_SQL_MODEL or LLM_MODEL."""
    return llm_model()


def autocomplete_base_url() -> str:
    return os.getenv("LLM_AUTOCOMPLETE_API_URL") or llm_base_url()


def autocomplete_api_key() -> str:
    return os.getenv("LLM_AUTOCOMPLETE_API_KEY") or llm_api_key()


def autocomplete_model() -> str:
    """Model for autocomplete-sql. Env: LLM_AUTOCOMPLETE_MODEL (own default, no fallback to LLM_MODEL)."""
    return os.getenv("LLM_AUTOCOMPLETE_MODEL", LLM_AUTOCOMPLETE_MODEL)


def find_tables_base_url() -> str:
    return os.getenv("LLM_FIND_TABLES_API_URL") or llm_base_url()


def find_tables_api_key() -> str:
    return os.getenv("LLM_FIND_TABLES_API_KEY") or llm_api_key()


def find_tables_model() -> str:
    """Smaller model for find-tables (faster for tests). Override with LLM_FIND_TABLES_MODEL."""
    return os.getenv("LLM_FIND_TABLES_MODEL", LLM_FIND_TABLES_MODEL)


def embedding_model() -> str:
    """Model for RAG embeddings; Ollama often uses nomic-embed-text."""
    return os.getenv("EMBEDDING_MODEL", "nomic-embed-text")


def schema_api_url() -> str:
    return os.getenv("SCHEMA_API_URL") or "http://172.17.112.1:5175/api/v1/schema"


def schema_cache_ttl_seconds() -> int:
    return int(os.getenv("SCHEMA_CACHE_TTL_SECONDS", str(SCHEMA_CACHE_TTL_SECONDS_DEFAULT)))
