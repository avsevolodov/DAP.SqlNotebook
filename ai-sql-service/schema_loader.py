"""Schema fetch from C# backend and markdown building."""
import logging
import time
from typing import Optional

import requests

from config import schema_api_url, schema_cache_ttl_seconds
from schema_client import get_entity, search_entities
from schema_models import SchemaEntity

logger = logging.getLogger(__name__)

# Cache key: catalog_node_id or "all" for full schema. Value: {"expires_at", "value"}
_schema_cache: dict[str, dict] = {}

REQUEST_TIMEOUT_SEC = 5


def get_schema_object(force_refresh: bool = False, catalog_node_id: Optional[str] = None) -> dict:
    """
    Fetch schema JSON from C# service (DbSchemaDto).
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
    if not force_refresh and entry and entry.get("value") and time.time() < entry.get("expires_at", 0):
        logger.debug("Schema from cache: %s", cache_key)
        return entry["value"]

    try:
        resp = requests.get(url, timeout=REQUEST_TIMEOUT_SEC)
        resp.raise_for_status()
        schema = resp.json()
        _schema_cache[cache_key] = {
            "value": schema,
            "expires_at": time.time() + ttl_sec,
        }
        keys_preview = list(schema.keys()) if isinstance(schema, dict) else "n/a"
        logger.debug("Schema fetched: %s keys=%s", url[:80], keys_preview)
        return schema
    except Exception as exc:
        logger.exception("Failed to fetch schema from %s: %s", url, exc)
        if "Connection refused" in str(exc) or "111" in str(exc):
            logger.info(
                "Hint: When ai-sql-service runs in Docker, start C# backend with: "
                "dotnet run --urls http://0.0.0.0:5175 (or ASPNETCORE_URLS=http://0.0.0.0:5175)."
            )
        return {}


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


def build_schema_markdown(schema: dict) -> str:
    """
    Build markdown description from DbSchemaDto shape (camelCase or PascalCase from API).
    Uses full real structure: all entities, fields, relations.
    """
    entities = schema.get("entities") or schema.get("Entities") or []
    relations = schema.get("relations") or schema.get("Relations") or []

    rel_by_from: dict[str, list[dict]] = {}
    for r in relations:
        from_id = str(r.get("fromEntityId") or r.get("FromEntityId") or "")
        rel_by_from.setdefault(from_id, []).append(r)

    parts: list[str] = []

    for ent in entities:
        md = ent.get("markdownDescription") or ent.get("description") or ent.get("MarkdownDescription") or ent.get("Description")
        if md:
            # parts.append(str(md))
            parts.append("")
            continue

        ent_id = str(ent.get("id") or ent.get("Id") or "")
        name = ent.get("displayName") or ent.get("Name") or "Entity"
        full_name = ent.get("name") or ent.get("Name") or name
        fields = ent.get("fields") or ent.get("Fields") or []
        ent_rels = rel_by_from.get(ent_id, [])

        lines: list[str] = []
        lines.append(f"# {name}")
        lines.append("")
        lines.append(f"**Entity**: `{full_name}`")
        lines.append("")

        if fields:
            lines.append("## Fields")
            for f in fields:
                fname = f.get("name") or f.get("Name") or ""
                ftype = f.get("dataType") or f.get("DataType") or ""
                flags = []
                if f.get("isPrimaryKey") or f.get("IsPrimaryKey"):
                    flags.append("PK")
                if not f.get("isNullable", f.get("IsNullable", True)):
                    flags.append("Required")
                flags_text = f" ({', '.join(flags)})" if flags else ""
                lines.append(f"- **{fname}** : `{ftype}`{flags_text}")
            lines.append("")

        if ent_rels:
            lines.append("## Relationships")
            for r in ent_rels:
                rname = r.get("name") or r.get("Name") or ""
                from_field = r.get("fromFieldName") or r.get("FromFieldName") or ""
                to_field = r.get("toFieldName") or r.get("ToFieldName") or ""
                to_entity_id = str(r.get("toEntityId") or r.get("ToEntityId") or "")
                target = next(
                    (e for e in entities if str(e.get("id") or e.get("Id") or "") == to_entity_id),
                    None,
                )
                target_name = (target or {}).get("displayName") or (target or {}).get("Name") or ""
                if not target_name:
                    target_name = (target or {}).get("name") or (target or {}).get("DisplayName") or ""
                lines.append(
                    f"- **{rname}**: `{from_field}` → `{target_name}.{to_field}`"
                )
            lines.append("")

        parts.append("\n".join(lines))
        parts.append("")

    return "\n".join(parts)


def _entity_key(e: dict, key: str) -> str:
    v = e.get(key) or e.get(key[0].upper() + key[1:] if key else key)
    return (v or "").strip()


def build_schema_markdown_focused(schema: dict, entity_names_set: set) -> str:
    """Build markdown for entities whose name/displayName is in entity_names_set; include their relations."""
    all_entities = schema.get("entities") or schema.get("Entities") or []
    all_relations = schema.get("relations") or schema.get("Relations") or []
    names_lower = {n.strip().lower() for n in entity_names_set if n.strip()}
    if not names_lower:
        return build_schema_markdown(schema)
    selected_ids = set()
    for e in all_entities:
        name = _entity_key(e, "name")
        display = _entity_key(e, "displayName") or name
        if name.lower() in names_lower or display.lower() in names_lower:
            eid = str(e.get("id") or e.get("Id") or "")
            if eid:
                selected_ids.add(eid)
    for r in all_relations:
        from_id = str(r.get("fromEntityId") or r.get("FromEntityId") or "")
        to_id = str(r.get("toEntityId") or r.get("ToEntityId") or "")
        if from_id in selected_ids or to_id in selected_ids:
            if from_id:
                selected_ids.add(from_id)
            if to_id:
                selected_ids.add(to_id)
    entities = [e for e in all_entities if str(e.get("id") or e.get("Id") or "") in selected_ids]
    relations = [
        r for r in all_relations
        if str(r.get("fromEntityId") or r.get("FromEntityId") or "") in selected_ids
        and str(r.get("toEntityId") or r.get("ToEntityId") or "") in selected_ids
    ]
    return build_schema_markdown({"entities": entities, "relations": relations})
