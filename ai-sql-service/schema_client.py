"""
Minimal client for schema API: get_entity(name), search_entities(query).

Uses SCHEMA_API_URL (default http://localhost:5175/api/v1/schema).
Catalog API: same host, path /api/v1/catalog/nodes.
"""
import os
import re
from typing import Any

import requests

_SCHEMA_CACHE: dict[str, Any] = {}

REQUEST_TIMEOUT_SEC = 5


def _schema_url() -> str:
    return os.getenv("SCHEMA_API_URL", "http://localhost:5175/api/v1/schema")


def _catalog_base_url() -> str:
    """Base URL for catalog API (nodes). Same host as schema, path /api/v1/catalog/nodes."""
    u = _schema_url().rstrip("/")
    if u.endswith("/api/v1/schema"):
        return u[: -len("/api/v1/schema")].rstrip("/")
    if u.endswith("/schema"):
        return u[: -len("/schema")].rstrip("/")
    return u.split("/api/")[0] if "/api/" in u else u


def get_catalog_nodes(parent_id: str | None = None) -> list[dict]:
    """Get catalog tree nodes. parent_id=None for roots, else children of that node."""
    base = _catalog_base_url()
    url = f"{base}/api/v1/catalog/nodes"
    if parent_id:
        url += f"?parentId={parent_id}"
    try:
        r = requests.get(url, timeout=REQUEST_TIMEOUT_SEC)
        r.raise_for_status()
        return r.json() or []
    except Exception:
        return []


def get_databases() -> list[dict]:
    """List all catalog nodes of type Database (separate endpoint for DB list)."""
    base = _catalog_base_url()
    url = f"{base}/api/v1/catalog/databases"
    try:
        r = requests.get(url, timeout=REQUEST_TIMEOUT_SEC)
        r.raise_for_status()
        return r.json() or []
    except Exception:
        return []


def get_tables() -> list[dict]:
    """List all tables (entities) with id, name, displayName, description."""
    base = _catalog_base_url()
    url = f"{base}/api/v1/catalog/tables"
    try:
        r = requests.get(url, timeout=REQUEST_TIMEOUT_SEC)
        r.raise_for_status()
        return r.json() or []
    except Exception:
        return []


def get_entity_fields(entity_id: str) -> list[dict]:
    """Get fields (columns) for a table (entity)."""
    base = _catalog_base_url()
    url = f"{base}/api/v1/catalog/entities/{entity_id}/fields"
    try:
        r = requests.get(url, timeout=REQUEST_TIMEOUT_SEC)
        r.raise_for_status()
        return r.json() or []
    except Exception:
        return []


def get_entity_relations(entity_id: str) -> list[dict]:
    """Get relations for a table (entity)."""
    base = _catalog_base_url()
    url = f"{base}/api/v1/catalog/entities/{entity_id}/relations"
    try:
        r = requests.get(url, timeout=REQUEST_TIMEOUT_SEC)
        r.raise_for_status()
        return r.json() or []
    except Exception:
        return []


def _fetch_schema() -> dict:
    if _SCHEMA_CACHE:
        return _SCHEMA_CACHE
    try:
        r = requests.get(_schema_url(), timeout=REQUEST_TIMEOUT_SEC)
        r.raise_for_status()
        data = r.json()
        _SCHEMA_CACHE["entities"] = data.get("entities") or data.get("Entities") or []
        return _SCHEMA_CACHE
    except Exception:
        _SCHEMA_CACHE["entities"] = []
        return _SCHEMA_CACHE


def _entity_key(e: dict, key: str) -> str:
    return (e.get(key) or e.get(key[0].upper() + key[1:]) or "").strip()


def get_entity(name: str) -> dict | None:
    """Return entity by display name or logical name, or None."""
    if not (name or "").strip():
        return None
    name = name.strip().lower()
    for e in _fetch_schema().get("entities", []):
        display = _entity_key(e, "displayName")
        logical = _entity_key(e, "name")
        if display.lower() == name or logical.lower() == name:
            return e
    return None


def search_entities(query: str, limit: int = 20) -> list[dict]:
    """Return entities matching query (in name, displayName, description), or first N if query empty."""
    entities = _fetch_schema().get("entities", [])
    if not (query or "").strip():
        return entities[:limit]
    q = query.strip().lower()
    pattern = re.escape(q)
    out = []
    for e in entities:
        display = _entity_key(e, "displayName")
        logical = _entity_key(e, "name")
        desc = _entity_key(e, "description")
        text = " ".join([display, logical, desc]).lower()
        if re.search(pattern, text):
            out.append(e)
    return out[:limit]
