"""
AI SQL Service — FastAPI app.

Entry point; routes and logic live in routes, dto, config, llm_utils, schema_loader.
"""
import logging
import os
import time

import uvicorn
from fastapi import FastAPI

from rag_index import get_vector_store, start_background_reindex
from routes import router

logger = logging.getLogger(__name__)

app = FastAPI(title="AI SQL Service", version="0.1.0")
app.include_router(router)

DEFAULT_PORT = 8000
RAG_STARTUP_DELAY_DEFAULT_SEC = 5


@app.on_event("startup")
def startup_rag() -> None:
    """Build RAG index via separate endpoints and start background reindex."""
    delay_sec = int(os.getenv("RAG_STARTUP_DELAY_SEC", str(RAG_STARTUP_DELAY_DEFAULT_SEC)))
    if delay_sec > 0:
        logger.info(
            "Waiting %ds for Ollama before RAG index build...",
            delay_sec,
        )
        time.sleep(delay_sec)
    get_vector_store(use_separate_endpoints=True)
    start_background_reindex(use_separate_endpoints=True)


if __name__ == "__main__":
    uvicorn.run(
        "app:app",
        host="0.0.0.0",
        port=int(os.getenv("PORT", str(DEFAULT_PORT))),
        reload=True,
    )
