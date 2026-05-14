import logging

from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams

from app.core.config import settings


logger = logging.getLogger(__name__)

CHAT_MEMORY_VECTOR_SIZE = 1536


def get_qdrant_client() -> QdrantClient:
    return QdrantClient(
        host=settings.QDRANT_HOST,
        port=settings.QDRANT_PORT,
        prefer_grpc=False,
        timeout=5
    )


def ensure_chat_memory_collection() -> None:
    client = get_qdrant_client()
    collection_name = settings.QDRANT_COLLECTION_NAME

    if client.collection_exists(collection_name):
        return

    client.create_collection(
        collection_name=collection_name,
        vectors_config=VectorParams(
            size=CHAT_MEMORY_VECTOR_SIZE,
            distance=Distance.COSINE
        )
    )

    logger.info("Created Qdrant collection '%s'", collection_name)
