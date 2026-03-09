"""
DEPRECATED: in-memory vector store backend for schema RAG index.

RAG now requires Qdrant (`rag_vectorstore_qdrant.py`) and `QDRANT_URL` to be configured.
This module is kept only for reference and is not used by the application.
"""
from __future__ import annotations

from typing import List

from langchain_core.documents import Document
from langchain_core.embeddings import Embeddings
from langchain_core.vectorstores import InMemoryVectorStore, VectorStore


def build_inmemory_vector_store(docs: List[Document], embedding: Embeddings) -> VectorStore:
    """
    Build an in-memory vector store from documents using the provided embedding model.
    """
    return InMemoryVectorStore.from_documents(docs, embedding)

