import os
import requests
import logging

from app.llm.base_provider import BaseLLMProvider

logger = logging.getLogger(__name__)


class OpenAIProvider(BaseLLMProvider):

    def __init__(self):
        self.api_key = self._load_api_key()
        self.base_url = os.getenv("OPENAI_BASE_URL", "https://api.openai.com/v1")
        self.model = os.getenv("OPENAI_MODEL", "gpt-4o-mini")

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

        path = os.getenv("OPENAI_API_KEY_PATH")

        if not path:
            raise ValueError("OPENAI_API_KEY_PATH is not set")

        if not os.path.exists(path):
            raise ValueError(f"OpenAI key file not found at {path}")

        try:
            with open(path, "r") as f:
                key = f.read().strip()

            if not key:
                raise ValueError("OpenAI API key file is empty")

            return key

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