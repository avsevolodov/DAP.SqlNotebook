"""LangChain callback handler: print each step to console (LLM, retriever, chain)."""
from __future__ import annotations

from typing import Any, Optional, Sequence

from langchain_core.callbacks import BaseCallbackHandler
from langchain_core.outputs import LLMResult


class ConsoleCallbackHandler(BaseCallbackHandler):
    """Prints LangChain run steps to stdout (LLM start/end, retriever, chain)."""

    def on_llm_start(
        self,
        serialized: dict[str, Any],
        prompts: list[str],
        **kwargs: Any,
    ) -> None:
        print("[LangChain] LLM start", "prompts:", len(prompts), "chars:", sum(len(p) for p in prompts))

    def on_llm_end(self, response: LLMResult, **kwargs: Any) -> None:
        gen = response.generations
        n = len(gen) if gen else 0
        total = sum(len(g) for g in gen) if gen else 0
        print("[LangChain] LLM end", "generations:", n, "outputs:", total)

    def on_llm_error(self, error: Exception, **kwargs: Any) -> None:
        print("[LangChain] LLM error:", error)

    def on_chain_start(
        self,
        serialized: dict[str, Any],
        inputs: dict[str, Any],
        **kwargs: Any,
    ) -> None:
        name = serialized.get("name", serialized.get("id", ["Chain"])[-1])
        print("[LangChain] Chain start:", name, "inputs keys:", list(inputs.keys()) if inputs else [])

    def on_chain_end(
        self,
        outputs: dict[str, Any],
        **kwargs: Any,
    ) -> None:
        print("[LangChain] Chain end", "outputs keys:", list(outputs.keys()) if outputs else [])

    def on_retriever_start(
        self,
        serialized: dict[str, Any],
        query: str,
        **kwargs: Any,
    ) -> None:
        q = query if isinstance(query, str) else str(query)[:80]
        print("[LangChain] Retriever start", "query len:", len(q), "preview:", (q[:80] + "…") if len(q) > 80 else q)

    def on_retriever_end(
        self,
        documents: Sequence[Any],
        **kwargs: Any,
    ) -> None:
        print("[LangChain] Retriever end", "documents:", len(documents))

    def on_tool_start(
        self,
        serialized: dict[str, Any],
        input_str: str,
        **kwargs: Any,
    ) -> None:
        name = serialized.get("name", "tool")
        print("[LangChain] Tool start:", name)

    def on_tool_end(self, output: str, **kwargs: Any) -> None:
        print("[LangChain] Tool end", "output len:", len(output))


# Singleton for reuse
_console_handler: Optional[ConsoleCallbackHandler] = None


def get_console_handler() -> ConsoleCallbackHandler:
    global _console_handler
    if _console_handler is None:
        _console_handler = ConsoleCallbackHandler()
    return _console_handler
