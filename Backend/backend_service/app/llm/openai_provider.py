import requests
import logging

from app.core.config import load_openai_api_key, settings
from app.llm.base_provider import BaseLLMProvider

logger = logging.getLogger(__name__)


class OpenAIProvider(BaseLLMProvider):

    def __init__(self):
        self.api_key = self._load_api_key()
        self.base_url = settings.OPENAI_BASE_URL
        self.model = settings.OPENAI_MODEL

    def generate(self, prompt: str) -> str:
        url = f"{self.base_url}/responses"

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }

        payload = {
            "model": self.model,
            "input": prompt
        }

        try:
            response = requests.post(
                url,
                headers=headers,
                json=payload,
                timeout=60
            )

            if response.status_code != 200:
                logger.error(f"OpenAI error ({response.status_code}): {response.text}")
                raise RuntimeError("LLM returned non-200 response")

            data = response.json()

            result = self._extract_text(data)

            if not result:
                logger.error(f"OpenAI empty response: {data}")
                raise RuntimeError("Empty response from OpenAI")

            return result.strip()

        except requests.RequestException as e:
            logger.error(f"OpenAI request failed: {str(e)}")
            raise RuntimeError("LLM request failed") from e

        except Exception as e:
            logger.error(f"OpenAI unexpected error: {str(e)}")
            raise RuntimeError("LLM unexpected error") from e

    def _load_api_key(self) -> str:
        """
        Loads OpenAI API key from file path (mounted via Docker secrets).
        """

        try:
            return load_openai_api_key()

        except Exception as e:
            logger.error(f"Failed to read OpenAI API key: {str(e)}")
            raise RuntimeError("Could not load OpenAI API key") from e

    def _extract_text(self, data: dict) -> str:
        """
        Safely extracts text from OpenAI Responses API output.
        """

        try:
            for item in data.get("output", []):
                for content in item.get("content", []):
                    if content.get("type") == "output_text":
                        return content.get("text", "")

            return ""

        except Exception as e:
            logger.error(f"Failed to parse OpenAI response: {str(e)}")
            return ""
