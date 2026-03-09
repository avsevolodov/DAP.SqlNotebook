FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app.py routes.py dto.py config.py llm_utils.py schema_loader.py schema_client.py schema_models.py schema_autocomplete.py rag_index.py ./

ENV PORT=8000
EXPOSE 8000

# Ollama in same docker network: http://ollama:11434/v1
ENV LLM_API_URL=http://ollama:11434/v1
ENV LLM_API_KEY=ollama
ENV LLM_MODEL=tinyllama
ENV LLM_AUTOCOMPLETE_MODEL=tinyllama
# Schema API (C# backend) on host — default port 5175 (SqlNotebook.Service)
ENV SCHEMA_API_URL=http://host.docker.internal:5175/api/v1/schema

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000"]
