"""
AI SQL Service — FastAPI app.
Entry point; routes and logic live in routes, dto, config, llm_utils, schema_loader.
"""
import os
import time

import uvicorn
from fastapi import FastAPI

from rag_index import get_vector_store, start_background_reindex
from routes import router

import tiktoken_ext.openai_public
import tiktoken
import inspect


app = FastAPI(title="AI SQL Service", version="0.1.0")

app.include_router(router)


@app.on_event("startup")
def startup_rag():
    """Build RAG index via separate endpoints and start background reindex."""
    delay_sec = int(os.getenv("RAG_STARTUP_DELAY_SEC", "5"))
    if delay_sec > 0:
        print(f"Waiting {delay_sec}s for Ollama to be ready before RAG index build...")
        time.sleep(delay_sec)
    get_vector_store(use_separate_endpoints=True)
    start_background_reindex(use_separate_endpoints=True)


if __name__ == "__main__":

    print(os.environ["TIKTOKEN_CACHE_DIR"])
    print(dir(tiktoken_ext.openai_public))
    encoding = tiktoken.get_encoding("cl100k_base")


    print(inspect.getsource(tiktoken_ext.openai_public.cl100k_base))
    uvicorn.run(
        "app:app",
        host="0.0.0.0",
        port=int(os.getenv("PORT", "8000")),
        reload=True,
    )
