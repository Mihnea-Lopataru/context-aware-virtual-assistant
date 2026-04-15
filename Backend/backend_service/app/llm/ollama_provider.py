import os
import requests
import logging

from app.llm.base_provider import BaseLLMProvider

logger = logging.getLogger(__name__)

class OllamaProvider(BaseLLMProvider):
    """
    Ollama LLM provider.
    """

    def __init__(self):
        self.base_url = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434")
        self.model = os.getenv("OLLAMA_MODEL", "llama3.1:8b")

    def generate(self, prompt: str) -> str:
            url = f"{self.base_url}/api/generate"

            payload = {
                "model": self.model,
                "prompt": prompt,
                "stream": False
            }

            try:
                response = requests.post(
                    url,
                    json=payload,
                    timeout=60
                )

                if response.status_code != 200:
                    logger.error(f"Ollama error: {response.text}")
                    raise RuntimeError("LLM returned non-200 response")

                data = response.json()

                result = data.get("response", "").strip()

                if not result:
                    raise RuntimeError("Empty response from Ollama")

                return result

            except requests.RequestException as e:
                logger.error(f"Ollama request failed: {str(e)}")
                raise RuntimeError("LLM request failed") from e

            except Exception as e:
                logger.error(f"Ollama unexpected error: {str(e)}")
                raise RuntimeError("LLM unexpected error") from e