"""LangChain callback handler: print each step to console (LLM, retriever, chain)."""
from __future__ import annotations

import time
from typing import Any, Optional, Sequence

from langchain_core.callbacks import BaseCallbackHandler
from langchain_core.outputs import LLMResult


class ConsoleCallbackHandler(BaseCallbackHandler):
    """Prints LangChain run steps to stdout (LLM start/end, retriever, chain)."""

    def __init__(self) -> None:
        super().__init__()
        self._llm_start_ts: float | None = None
        self._llm_model_name: str | None = None

    def on_llm_start(
        self,
        serialized: dict[str, Any],
        prompts: list[str],
        **kwargs: Any,
    ) -> None:
        self._llm_start_ts = time.perf_counter()
        # Best-effort model name extraction from serialized config.
        model = None
        try:
            model = (
                serialized.get("kwargs", {}).get("model")
                or serialized.get("kwargs", {}).get("model_name")
            )
        except Exception:
            model = None
        self._llm_model_name = str(model) if model else None

        total_chars = sum(len(p) for p in prompts)
        print(
            "[LangChain] LLM start",
            "model:", self._llm_model_name or "(unknown)",
            "prompts:", len(prompts),
            "chars:", total_chars,
        )
        if prompts:
            # Log first prompt (truncated for readability).
            p = prompts[0]
            preview = p if len(p) <= 2000 else p[:2000] + "…"
            print("[LangChain] LLM prompt[0]:", preview)

    def on_llm_end(self, response: LLMResult, **kwargs: Any) -> None:
        elapsed_ms = (
            (time.perf_counter() - self._llm_start_ts) * 1000.0
            if self._llm_start_ts is not None
            else None
        )
        gen = response.generations
        n = len(gen) if gen else 0
        total = 0
        first_text: str = ""
        if gen:
            for g_list in gen:
                for g in g_list:
                    text = getattr(g, "text", "") or ""
                    total += len(text)
                    if not first_text and text:
                        first_text = text
        print(
            "[LangChain] LLM end",
            "model:", self._llm_model_name or "(unknown)",
            "generations:", n,
            "outputs_chars:", total,
            "elapsed_ms:", f"{elapsed_ms:.1f}" if elapsed_ms is not None else "n/a",
        )
        if first_text:
            preview = first_text.strip().replace("\n", "\\n")
            if len(preview) > 2000:
                preview = preview[:2000] + "…"
            print("[LangChain] LLM output[0]:", preview)

    def on_llm_error(self, error: Exception, **kwargs: Any) -> None:
        print("[LangChain] LLM error:", error)

    def on_chain_start(
        self,
        serialized: dict[str, Any],
        inputs: dict[str, Any],
        **kwargs: Any,
    ) -> None:
        name = serialized.get("name", serialized.get("id", ["Chain"])[-1])
        print(
            "[LangChain] Chain start:",
            name,
            "inputs keys:",
            list(inputs.keys()) if inputs else [],
        )

    def on_chain_end(
        self,
        outputs: dict[str, Any],
        **kwargs: Any,
    ) -> None:
        print(
            "[LangChain] Chain end",
            "outputs keys:",
            list(outputs.keys()) if outputs else [],
        )

    def on_retriever_start(
        self,
        serialized: dict[str, Any],
        query: str,
        **kwargs: Any,
    ) -> None:
        q = query if isinstance(query, str) else str(query)
        preview = (q[:80] + "…") if len(q) > 80 else q
        print(
            "[LangChain] Retriever start",
            "query len:", len(q),
            "preview:", preview,
        )

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
