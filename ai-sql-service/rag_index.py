"""
RAG index over schema (tables/entities). Uses Qdrant (in-memory or QDRANT_URL).

Indexing flow: 1) get list of DBs (catalog nodes), 2) get list of tables and descriptions,
3) for each table get fields and relations; build one document per table.
Uses OllamaEmbeddings when LLM_API_URL points to Ollama to avoid "invalid input type".
"""
from __future__ import annotations

import logging
import os
import threading
import time
from typing import Any, Callable, Optional

from langchain_core.documents import Document
from langchain_core.embeddings import Embeddings
from langchain_core.vectorstores import VectorStore

from rag_models import RagCatalogNode, RagEntity, RagField, RagRelation, RagSchema
from rag_vectorstore_inmemory import build_inmemory_vector_store
from rag_vectorstore_qdrant import build_qdrant_vector_store

logger = logging.getLogger(__name__)

# Reindex interval in seconds (default 5 min)
RAG_REINDEX_INTERVAL_SEC = int(os.getenv("RAG_REINDEX_INTERVAL_SECONDS", "300"))
# Retry building RAG index when Ollama is unreachable (e.g. Docker startup order)
RAG_BUILD_RETRIES = int(os.getenv("RAG_BUILD_RETRIES", "5"))
RAG_BUILD_RETRY_DELAY_SEC = int(os.getenv("RAG_BUILD_RETRY_DELAY_SEC", "10"))

_embedding: Optional[Embeddings] = None
_vector_store: Optional[VectorStore] = None
_collection_name = "sqlnotebook_schema"

# Debug: last index build stats (tables count, docs count)
_index_stats: dict[str, Any] = {"databases_count": 0, "tables_count": 0, "docs_count": 0, "error": None}


def get_index_stats() -> dict[str, Any]:
    """Return last index build stats for debug (databases_count, tables_count, docs_count, error)."""
    return dict(_index_stats)


def _is_ollama_base_url(base_url: str) -> bool:
    """True if base_url is likely Ollama (avoids OpenAI-compatible embedding 400 invalid input type)."""
    if not base_url:
        return False
    u = base_url.lower().rstrip("/")
    return "ollama" in u or ":11434" in u or "11434/" in u


def _get_embedding() -> Embeddings:
    global _embedding

    if _embedding is None:
        api_key = os.getenv("LLM_API_KEY", "ollama")
        base_url = (os.getenv("LLM_API_URL") or "http://localhost:7869/v1").strip().rstrip("/")
        model = os.getenv("EMBEDDING_MODEL", "nomic-embed-text")
        if _is_ollama_base_url(base_url):
            from langchain_ollama import OllamaEmbeddings

            ollama_base = base_url.replace("/v1", "").rstrip("/") or "http://localhost:11434"
            _embedding = OllamaEmbeddings(base_url=ollama_base, model=model)
            logger.info("RAG using OllamaEmbeddings: %s model=%s", ollama_base, model)
        else:
            from langchain_openai import OpenAIEmbeddings

            _embedding = OpenAIEmbeddings(
                api_key=api_key,
                openai_api_base=base_url,
                model=model,
            )
    return _embedding


def _safe_metadata(meta: dict[str, Any]) -> dict[str, str]:
    """Ensure metadata is only str values for Qdrant (avoids EnumTypeWrapper etc)."""
    return {k: str(v) if v is not None else "" for k, v in meta.items()}


def _str_or_empty(value: Any) -> str:
    """Return stripped string or empty string for None."""
    if value is None:
        return ""
    return str(value).strip()


def _norm_entity_field(f: dict) -> dict:
    """Normalize entity field dict to rag_models shape (name, dataType, isPrimaryKey)."""
    return {
        "name": _str_or_empty(f.get("name") or f.get("Name")),
        "dataType": _str_or_empty(f.get("dataType") or f.get("DataType")),
        "isPrimaryKey": bool(f.get("isPrimaryKey") or f.get("IsPrimaryKey") or False),
    }


def _fetch_indexing_data_via_endpoints() -> tuple[list[RagCatalogNode], list[RagEntity], list[RagRelation]]:
    """
    Fetch data via separate endpoints: 1) databases, 2) tables, 3) for each table fields and relations.
    Returns (db_nodes, entities_with_fields_and_relations, relations_flat) as Pydantic models.
    Fallback: if GET catalog/tables returns empty, load entities from GET /api/v1/schema.
    """
    from schema_client import get_databases, get_tables, get_entity_fields, get_entity_relations

    raw_db = get_databases()
    db_nodes = [RagCatalogNode.model_validate(n) for n in raw_db]

    raw_tables = get_tables()

    # Fallback: catalog/tables can be empty. Try full schema from /api/v1/schema.
    if not raw_tables:
        try:
            from schema_loader import get_schema_object

            schema = get_schema_object(force_refresh=True)
            entities = schema.get("entities") or schema.get("Entities") or []
            if entities:
                raw_tables = []
                for e in entities:
                    eid = e.get("id") or e.get("Id")
                    if eid is None:
                        continue
                    eid_str = str(eid).strip()
                    raw_fields = e.get("fields") or e.get("Fields") or []
                    raw_tables.append({
                        "id": eid_str,
                        "name": _str_or_empty(e.get("name") or e.get("Name")),
                        "displayName": _str_or_empty(e.get("displayName") or e.get("DisplayName")),
                        "description": _str_or_empty(e.get("description") or e.get("Description")),
                        "fields": [_norm_entity_field(f) for f in raw_fields],
                    })
                logger.info(
                    "RAG index: using schema from GET /api/v1/schema (catalog/tables empty), entities=%d",
                    len(raw_tables),
                )
        except Exception as ex:
            logger.warning("RAG fallback schema fetch failed: %s", ex)

    entities_with_fields: list[RagEntity] = []
    relations_flat: list[RagRelation] = []
    for t in raw_tables:
        eid = t.get("id") or t.get("Id")
        if not eid:
            continue
        eid_str = str(eid).strip()
        raw_fields = t.get("fields") or t.get("Fields")
        if raw_fields is None:
            raw_fields = get_entity_fields(eid_str)
        raw_relations = get_entity_relations(eid_str)
        fields = [RagField.model_validate(f) for f in raw_fields] if raw_fields else []
        rels = [RagRelation.model_validate(r) for r in raw_relations] if raw_relations else []
        ent = RagEntity(
            id=eid_str,
            name=str(t.get("name") or t.get("Name") or "").strip(),
            display_name=str(t.get("displayName") or t.get("DisplayName") or "").strip(),
            description=str(t.get("description") or t.get("Description") or "").strip(),
            fields=fields,
            relations=rels,
        )
        entities_with_fields.append(ent)
        relations_flat.extend(rels)
    return db_nodes, entities_with_fields, relations_flat


def _build_documents_for_tables(
    entities: list[RagEntity],
    relations: list[RagRelation],
    db_names: Optional[list[str]] = None,
) -> list[Document]:
    """
    For each entity build one document: name, displayName, description, fields (name, type, PK), relations.
    """
    ent_id_to_name: dict[str, str] = {}
    for e in entities:
        display = e.display_name or e.name
        ent_id_to_name[e.id] = display or e.name

    rel_by_from: dict[str, list[RagRelation]] = {}
    rel_by_to: dict[str, list[RagRelation]] = {}
    for r in relations:
        if r.from_entity_id:
            rel_by_from.setdefault(r.from_entity_id, []).append(r)
        if r.to_entity_id:
            rel_by_to.setdefault(r.to_entity_id, []).append(r)

    docs: list[Document] = []
    for ent in entities:
        name = ent.name
        display = ent.display_name or name
        desc = ent.description
        ent_id = ent.id

        field_parts = []
        for f in ent.fields:
            part = f"{f.name} {f.data_type}" + (" PK" if f.is_primary_key else "")
            field_parts.append(part)
        fields_text = "; ".join(field_parts) if field_parts else ""

        out_rels = rel_by_from.get(ent_id, [])
        in_rels = rel_by_to.get(ent_id, [])
        rel_parts = []
        for r in out_rels:
            to_name = ent_id_to_name.get(r.to_entity_id, r.to_entity_id)
            rel_parts.append(f"-> {to_name}({r.from_field_name} -> {r.to_field_name})")
        for r in in_rels:
            from_name = ent_id_to_name.get(r.from_entity_id, r.from_entity_id)
            rel_parts.append(f"<- {from_name}({r.from_field_name} -> {r.to_field_name})")
        rels_text = "; ".join(rel_parts) if rel_parts else ""

        parts = [name, display, desc, "Fields: " + fields_text if fields_text else "", "Relations: " + rels_text if rels_text else ""]
        page_content = " | ".join(str(p) for p in parts if p)
        if not page_content:
            continue
        meta = _safe_metadata({"name": name, "displayName": display, "description": desc})
        if db_names:
            meta["database"] = ", ".join(str(x) for x in db_names[:3])
        docs.append(Document(page_content=page_content, metadata=meta))
    return docs


def get_vector_store(
    schema: Optional[dict] = None,
    get_schema_fn: Optional[Callable[..., dict]] = None,
    use_separate_endpoints: bool = False,
) -> Optional[VectorStore]:
    """
    Return current vector store. To (re)build index:
    - use_separate_endpoints=True: use GET databases, GET tables, GET entities/{id}/fields, GET entities/{id}/relations.
    - get_schema_fn=...: legacy flow (catalog nodes + full schema callback).
    - schema=...: build from in-memory schema dict.
    """
    global _vector_store, _index_stats
    _index_stats = {"databases_count": 0, "tables_count": 0, "docs_count": 0, "error": None}

    if not use_separate_endpoints and not get_schema_fn and not schema:
        return _vector_store

    try:
        if use_separate_endpoints:
            db_nodes, entities_with_fields, relations_flat = _fetch_indexing_data_via_endpoints()
            db_names = [n.name for n in db_nodes if n.name] or None
            docs = _build_documents_for_tables(entities_with_fields, relations_flat, db_names=db_names)
            _index_stats["databases_count"] = len(db_nodes)
            _index_stats["tables_count"] = len(entities_with_fields)
            _index_stats["docs_count"] = len(docs)
        elif get_schema_fn is not None:
            # Legacy 3-step with callback (e.g. single GET schema): still supported
            from schema_client import get_catalog_nodes

            root_nodes_raw = get_catalog_nodes(parent_id=None)
            root_nodes = [RagCatalogNode.model_validate(n) for n in root_nodes_raw]
            db_nodes: list[RagCatalogNode] = []
            for node in root_nodes:
                if node.node_type in ("database", "source", "folder") and node.id:
                    children_raw = get_catalog_nodes(parent_id=node.id)
                    children = [RagCatalogNode.model_validate(ch) for ch in children_raw]
                    for ch in children:
                        if ch.node_type in ("database", "source"):
                            db_nodes.append(ch)
                    if not children and node.node_type in ("database", "source"):
                        db_nodes.append(node)
            schema = get_schema_fn(force_refresh=True)
            parsed = RagSchema.model_validate(schema)
            db_names = [n.name for n in db_nodes if n.name] or None
            docs = _build_documents_for_tables(parsed.entities, parsed.relations, db_names=db_names)
            _index_stats["databases_count"] = len(db_nodes)
            _index_stats["tables_count"] = len(parsed.entities)
            _index_stats["docs_count"] = len(docs)
        else:
            # schema dict provided
            parsed = RagSchema.model_validate(schema)
            docs = _build_documents_for_tables(parsed.entities, parsed.relations)
            _index_stats["databases_count"] = 0
            _index_stats["tables_count"] = len(parsed.entities)
            _index_stats["docs_count"] = len(docs)

        if not docs:
            logger.warning("RAG index: no documents (tables_count=0 or empty schema)")
            return _vector_store

        emb = _get_embedding()
        url = os.getenv("QDRANT_URL", "").strip()
        last_error: Optional[Exception] = None
        for attempt in range(1, RAG_BUILD_RETRIES + 1):
            try:
                if url:
                    logger.info("RAG index (Qdrant) building: %s", _index_stats)
                    dim = int(os.getenv("EMBEDDING_DIM", "4096"))
                    # Ensure metadata is only primitives (str) for Qdrant
                    docs_clean = [
                        Document(page_content=d.page_content, metadata=_safe_metadata(d.metadata))
                        for d in docs
                    ]
                    _vector_store = build_qdrant_vector_store(
                        url=url,
                        collection_name=_collection_name,
                        docs=docs_clean,
                        embedding=emb,
                        dim=dim,
                    )
                    logger.info("RAG index (Qdrant) rebuilt: %s", _index_stats)
                else:
                    logger.info("RAG index (InMemory) building: %s", _index_stats)
                    _vector_store = build_inmemory_vector_store(docs, emb)
                    logger.info("RAG index (InMemory) rebuilt: %s", _index_stats)
                last_error = None
                break
            except Exception as e:
                last_error = e
                err_str = str(e).lower()
                is_connection_error = (
                    "connect" in err_str or "connection" in err_str
                    or "refused" in err_str or "ollama" in err_str
                    or "timeout" in err_str or "unreachable" in err_str
                )
                if is_connection_error and attempt < RAG_BUILD_RETRIES:
                    logger.warning(
                        "RAG index build attempt %s/%s failed (Ollama not ready): %s",
                        attempt,
                        RAG_BUILD_RETRIES,
                        e,
                    )
                    logger.info("Retrying in %ss...", RAG_BUILD_RETRY_DELAY_SEC)
                    time.sleep(RAG_BUILD_RETRY_DELAY_SEC)
                else:
                    raise
        if last_error is not None:
            raise last_error
    except Exception as e:
        _index_stats["error"] = str(e)
        logger.exception("RAG index build failed: %s", e)
        _vector_store = None
    return _vector_store


def get_retriever(
    schema: Optional[dict] = None,
    get_schema_fn: Optional[Callable[..., dict]] = None,
    use_separate_endpoints: bool = False,
    k: int = 15,
):
    """Return a retriever over the schema RAG index. Use use_separate_endpoints=True to build from separate API endpoints."""
    vs = get_vector_store(schema=schema, get_schema_fn=get_schema_fn, use_separate_endpoints=use_separate_endpoints)
    if vs is None:
        return None
    return vs.as_retriever(search_kwargs={"k": k})


def run_background_reindex(use_separate_endpoints: bool = True) -> None:
    """Every RAG_REINDEX_INTERVAL_SEC seconds, rebuild RAG index (via separate endpoints by default)."""
    while True:
        time.sleep(RAG_REINDEX_INTERVAL_SEC)
        try:
            get_vector_store(use_separate_endpoints=use_separate_endpoints)
        except Exception as e:
            logger.warning("Background RAG reindex error: %s", e)


def start_background_reindex(use_separate_endpoints: bool = True) -> None:
    """Start daemon thread that reindexes schema every 5 min via separate endpoints (databases, tables, fields, relations)."""
    t = threading.Thread(target=run_background_reindex, args=(use_separate_endpoints,), daemon=True)
    t.start()
    logger.info("RAG background reindex started (interval_sec=%s)", RAG_REINDEX_INTERVAL_SEC)
