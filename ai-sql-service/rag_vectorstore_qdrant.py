"""
Qdrant vector store backend for schema RAG index.

Separated from rag_index so that backend-specific imports and potential EnumTypeWrapper
issues are isolated here.
"""
from __future__ import annotations

from typing import List

from langchain_core.documents import Document
from langchain_core.embeddings import Embeddings
from langchain_core.vectorstores import VectorStore
from langchain_qdrant import QdrantVectorStore
from qdrant_client import QdrantClient
from qdrant_client.http.models import Distance, Filter, VectorParams


def build_qdrant_vector_store(
    url: str,
    collection_name: str,
    docs: List[Document],
    embedding: Embeddings,
    dim: int,
) -> VectorStore:
    """
    Build a Qdrant vector store from documents using the provided embedding model.

    To keep the index in sync with the current schema without filesystem-level
    deletes (which can cause Permission denied on some mounts), we:
    - create collection if it does not exist;
    - delete all existing points in the collection;
    - insert fresh documents.
    """

    client = QdrantClient(url=url)

    # Ensure collection exists (idempotent).
    try:
        collections = client.get_collections()
        existing_names = {c.name for c in (collections.collections or [])}
    except Exception:
        existing_names = set()

    if collection_name not in existing_names:
        client.recreate_collection(
            collection_name=collection_name,
            vectors_config=VectorParams(size=dim, distance=Distance.COSINE),
        )

    # Delete all existing points via filter (no filesystem rename).
    try:
        client.delete(
            collection_name=collection_name,
            points_selector=Filter(must=[]),
        )
    except Exception:
        # If delete by filter is not supported, leave old points;
        # index will still work, but may contain stale entries.
        pass

    vs: VectorStore = QdrantVectorStore(
        client=client,
        collection_name=collection_name,
        embedding=embedding,
    )
    vs.add_documents(docs)
    return vs

