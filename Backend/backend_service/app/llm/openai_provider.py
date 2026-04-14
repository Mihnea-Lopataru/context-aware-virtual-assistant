from app.llm.base_provider import BaseLLMProvider


class OpenAIProvider(BaseLLMProvider):
    def generate(self, prompt: str) -> str:
        # placeholder
        return "OpenAI response placeholder"