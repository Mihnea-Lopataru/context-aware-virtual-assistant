import logging
from typing import List

import requests

from app.core.config import load_openai_api_key, settings
from app.core.qdrant import CHAT_MEMORY_VECTOR_SIZE
from app.embeddings.base_embedding_provider import BaseEmbeddingProvider


logger = logging.getLogger(__name__)


class OpenAIEmbeddingProvider(BaseEmbeddingProvider):
    vector_size = CHAT_MEMORY_VECTOR_SIZE

    def __init__(self):
        self.api_key = load_openai_api_key()
        self.base_url = settings.OPENAI_BASE_URL
        self.model = settings.OPENAI_EMBEDDING_MODEL

    def embed_text(self, text: str) -> List[float]:
        url = f"{self.base_url}/embeddings"

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }

        payload = {
            "model": self.model,
            "input": text
        }

        try:
            response = requests.post(
                url,
                headers=headers,
                json=payload,
                timeout=30
            )

            if response.status_code != 200:
                logger.error(
                    "OpenAI embeddings error (%s): %s",
                    response.status_code,
                    response.text
                )
                raise RuntimeError("Embedding provider returned non-200 response")

            data = response.json()
            embedding = data.get("data", [{}])[0].get("embedding")

            if not embedding:
                logger.error("OpenAI embeddings response did not contain a vector")
                raise RuntimeError("Empty embedding from OpenAI")

            if len(embedding) != self.vector_size:
                raise RuntimeError(
                    f"Embedding size {len(embedding)} does not match "
                    f"expected size {self.vector_size}"
                )

            return embedding

        except requests.RequestException as e:
            logger.error("OpenAI embedding request failed: %s", str(e))
            raise RuntimeError("Embedding request failed") from e
