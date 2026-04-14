import requests

from app.llm.base_provider import BaseLLMProvider


class OllamaProvider(BaseLLMProvider):
    """
    Ollama LLM provider.
    """

    def __init__(
        self,
        base_url: str = "http://localhost:11434",
        model: str = "llama3.1:8b"
    ):
        self.base_url = base_url
        self.model = model

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
                timeout=10
            )

            if response.status_code != 200:
                raise RuntimeError(f"Ollama error: {response.text}")

            data = response.json()

            return data.get("response", "").strip()

        except requests.RequestException as e:
            raise RuntimeError(f"Ollama request failed: {str(e)}") from e

        except Exception as e:
            raise RuntimeError(f"Ollama unexpected error: {str(e)}") from e