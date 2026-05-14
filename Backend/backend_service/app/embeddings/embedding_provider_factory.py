from app.core.config import settings
from app.embeddings.base_embedding_provider import BaseEmbeddingProvider
from app.embeddings.openai_embedding_provider import OpenAIEmbeddingProvider


def get_embedding_provider() -> BaseEmbeddingProvider:
    provider_name = settings.EMBEDDING_PROVIDER.lower()

    if provider_name == "openai":
        return OpenAIEmbeddingProvider()

    raise ValueError(f"Unsupported embedding provider: {provider_name}")
