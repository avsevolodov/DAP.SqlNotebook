"""Schema fetch from C# backend and markdown building."""
import logging
import time
from typing import Optional, Union

import requests

from config import schema_api_url, schema_cache_ttl_seconds
from rag_models import RagSchema
from schema_client import get_entity, search_entities
from schema_models import SchemaEntity

logger = logging.getLogger(__name__)

# Cache key: catalog_node_id or "all" for full schema. Value: {"expires_at", "value" (RagSchema)}
_schema_cache: dict[str, dict] = {}

REQUEST_TIMEOUT_SEC = 5


def get_schema_object(force_refresh: bool = False, catalog_node_id: Optional[str] = None) -> RagSchema:
    """
    Fetch schema JSON from C# service (DbSchemaDto) and return as RagSchema.
    When catalog_node_id is set, requests only entities for that source (GET .../schema?catalogNodeId=xxx).
    Cached per catalog_node_id (or full schema when None) for SCHEMA_CACHE_TTL_SECONDS.
    """
    base_url = schema_api_url()
    cache_key = (catalog_node_id or "").strip() or "all"
    if catalog_node_id:
        url = base_url.rstrip("/") + ("&" if "?" in base_url else "?") + "catalogNodeId=" + catalog_node_id
    else:
        url = base_url
    ttl_sec = schema_cache_ttl_seconds()
    entry = _schema_cache.get(cache_key)
    if not force_refresh and entry and entry.get("value") is not None and time.time() < entry.get("expires_at", 0):
        logger.debug("Schema from cache: %s", cache_key)
        return entry["value"]

    try:
        resp = requests.get(url, timeout=REQUEST_TIMEOUT_SEC)
        resp.raise_for_status()
        schema = resp.json()
        parsed = RagSchema.model_validate(schema)
        _schema_cache[cache_key] = {
            "value": parsed,
            "expires_at": time.time() + ttl_sec,
        }
        logger.debug("Schema fetched: %s entities=%d", url[:80], len(parsed.entities))
        return parsed
    except Exception as exc:
        logger.exception("Failed to fetch schema from %s: %s", url, exc)
        if "Connection refused" in str(exc) or "111" in str(exc):
            logger.info(
                "Hint: When ai-sql-service runs in Docker, start C# backend with: "
                "dotnet run --urls http://0.0.0.0:5175 (or ASPNETCORE_URLS=http://0.0.0.0:5175)."
            )
        return RagSchema()


def load_schema_on_demand(
    prompt: str,
    entity: Optional[str] = None,
    entities: Optional[list[str]] = None,
) -> str:
    """Load only relevant schema: search by prompt/entity, then fetch entity details."""
    names_to_load: list[str] = []
    if entities:
        names_to_load = [e.strip() for e in entities if e.strip()]
    elif entity and entity.strip():
        names_to_load = [entity.strip()]
    if not names_to_load and prompt.strip():
        search_results = search_entities(prompt.strip()[:80])
        for e in search_results[:5]:
            try:
                schema_ent = SchemaEntity.model_validate(e)
                n = schema_ent.display_or_name
                if n and n != "?" and n not in names_to_load:
                    names_to_load.append(n)
            except Exception:
                continue
    if not names_to_load:
        search_results = search_entities("")
        for e in search_results[:8]:
            try:
                schema_ent = SchemaEntity.model_validate(e)
                n = schema_ent.display_or_name
                if n and n != "?":
                    names_to_load.append(n)
            except Exception:
                continue
    parts = []
    seen = set()
    for name in names_to_load:
        if name.lower() in seen:
            continue
        seen.add(name.lower())
        ent = get_entity(name)
        if not ent:
            continue
        try:
            schema_ent = SchemaEntity.model_validate(ent)
        except Exception:
            continue
        md = schema_ent.markdown_description or schema_ent.description
        if md:
            parts.append(md)
            continue
        display = schema_ent.display_or_name
        logical = schema_ent.logical_name
        lines = [f"# {display}", f"Logical: `{logical}`", ""]
        for f in schema_ent.fields:
            fn = f.name
            ft = f.data_type or ""
            pk = " PK" if f.is_primary_key else ""
            lines.append(f"- **{fn}**: {ft}{pk}")
        parts.append("\n".join(lines))
    return "\n\n".join(parts) if parts else "No schema loaded. Use search_schema to find tables."


def build_schema_markdown(schema: Union[RagSchema, dict]) -> str:
    """
    Build markdown description from DbSchemaDto shape (RagSchema or dict from API).
    Uses full real structure: all entities, fields, relations.
    """
    parsed = RagSchema.model_validate(schema) if isinstance(schema, dict) else schema
    entities = parsed.entities
    relations = parsed.relations

    rel_by_from: dict[str, list] = {}
    for r in relations:
        rel_by_from.setdefault(r.from_entity_id, []).append(r)

    parts: list[str] = []

    for ent in entities:
        if ent.description:
            parts.append("")
            continue

        ent_id = ent.id
        name = ent.display_name or ent.name or "Entity"
        full_name = ent.name or name
        fields = ent.fields
        ent_rels = rel_by_from.get(ent_id, [])

        lines: list[str] = []
        lines.append(f"# {name}")
        lines.append("")
        lines.append(f"**Entity**: `{full_name}`")
        lines.append("")

        if fields:
            lines.append("## Fields")
            for f in fields:
                fname = f.name
                ftype = f.data_type or ""
                flags = ["PK"] if f.is_primary_key else []
                flags_text = f" ({', '.join(flags)})" if flags else ""
                lines.append(f"- **{fname}** : `{ftype}`{flags_text}")
            lines.append("")

        if ent_rels:
            lines.append("## Relationships")
            for r in ent_rels:
                to_name = next(
                    (e.display_name or e.name for e in entities if e.id == r.to_entity_id),
                    r.to_entity_id,
                )
                lines.append(
                    f"- **{r.name}**: `{r.from_field_name}` → `{to_name}.{r.to_field_name}`"
                )
            lines.append("")

        parts.append("\n".join(lines))
        parts.append("")

    return "\n".join(parts)


def build_schema_markdown_focused(schema: Union[RagSchema, dict], entity_names_set: set) -> str:
    """Build markdown for entities whose name/displayName is in entity_names_set; include their relations."""
    parsed = RagSchema.model_validate(schema) if isinstance(schema, dict) else schema
    all_entities = parsed.entities
    all_relations = parsed.relations
    names_lower = {n.strip().lower() for n in entity_names_set if n.strip()}
    if not names_lower:
        return build_schema_markdown(parsed)
    selected_ids = set()
    for e in all_entities:
        name = (e.name or "").strip()
        display = (e.display_name or name).strip()
        if name.lower() in names_lower or display.lower() in names_lower:
            if e.id:
                selected_ids.add(e.id)
    for r in all_relations:
        if r.from_entity_id in selected_ids or r.to_entity_id in selected_ids:
            if r.from_entity_id:
                selected_ids.add(r.from_entity_id)
            if r.to_entity_id:
                selected_ids.add(r.to_entity_id)
    entities = [e for e in all_entities if e.id in selected_ids]
    relations = [
        r for r in all_relations
        if r.from_entity_id in selected_ids and r.to_entity_id in selected_ids
    ]
    return build_schema_markdown(RagSchema(entities=entities, relations=relations))
