from typing import Optional

from app.llm.ollama_provider import OllamaProvider
from app.llm.openai_provider import OpenAIProvider
from app.llm.base_provider import BaseLLMProvider


class LLMClient:
    """
    Generic LLM client supporting multiple providers.
    """

    def __init__(self, provider_name: str = "ollama"):
        self.provider = self._resolve_provider(provider_name)

    def _resolve_provider(self, provider_name: str) -> BaseLLMProvider:
        provider_name = provider_name.lower()

        if provider_name == "ollama":
            return OllamaProvider()

        if provider_name == "openai":
            return OpenAIProvider()

        raise ValueError(f"Unsupported LLM provider: {provider_name}")

    def generate(self, prompt: str) -> str:
        return self.provider.generate(prompt)