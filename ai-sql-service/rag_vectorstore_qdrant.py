"""
Qdrant vector store backend for schema RAG index.

Separated from rag_index so that backend-specific imports and potential EnumTypeWrapper
issues are isolated here.
"""
from __future__ import annotations

from typing import List, Optional

from langchain_core.documents import Document
from langchain_core.embeddings import Embeddings
from langchain_core.vectorstores import VectorStore
from qdrant_client import QdrantClient
from qdrant_client.http.models import Distance, VectorParams
from langchain_qdrant import QdrantVectorStore


def build_qdrant_vector_store(
    url: str,
    collection_name: str,
    docs: List[Document],
    embedding: Embeddings,
    dim: int,
) -> VectorStore:
    """
    Build a Qdrant vector store from documents using the provided embedding model.
    """

    client = QdrantClient(url=url)
    try:
        client.delete_collection(collection_name)
    except Exception:
        # Collection may not exist yet; ignore
        pass

    client.create_collection(
        collection_name=collection_name,
        vectors_config=VectorParams(size=dim, distance=Distance.COSINE),
    )
    
    # qdrant = client.from_documents(
    #     documents = docs,
    #     embeddings = embedding,
    #     collection_name=collection_name,
    #     force_recreate=True   # <-- this will overwrite the collection
    # )
    
    # return qdrant

    vs: VectorStore = QdrantVectorStore(
        client=client,
        collection_name=collection_name,
        embedding=embedding,
    )
    vs.add_documents(docs)
    return vs

