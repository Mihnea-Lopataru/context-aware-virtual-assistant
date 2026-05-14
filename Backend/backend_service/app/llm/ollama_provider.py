import requests
import logging

from app.core.config import settings
from app.llm.base_provider import BaseLLMProvider

logger = logging.getLogger(__name__)

class OllamaProvider(BaseLLMProvider):

    def __init__(self):
        self.base_url = settings.OLLAMA_BASE_URL
        self.model = settings.OLLAMA_MODEL
        self.num_ctx = settings.OLLAMA_NUM_CTX
        self.num_predict = settings.OLLAMA_NUM_PREDICT

    def generate(self, prompt: str) -> str:
            url = f"{self.base_url}/api/generate"

            payload = {
                "model": self.model,
                "prompt": prompt,
                "stream": False,
                "options": {
                    "num_ctx": self.num_ctx,
                    "num_predict": self.num_predict
                }
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
