import logging
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional
from uuid import uuid4

from app.embeddings.embedding_provider_factory import get_embedding_provider
from app.repositories.memory_repository import MemoryRepository


logger = logging.getLogger(__name__)


class MemoryService:
    def __init__(self):
        self.repository: Optional[MemoryRepository] = None
        self.embedding_provider = None

        try:
            self.repository = MemoryRepository()
        except Exception as e:
            logger.error("Vector memory initialization failed: %s", str(e))

    def _get_embedding_provider(self):
        if self.embedding_provider:
            return self.embedding_provider

        self.embedding_provider = get_embedding_provider()
        return self.embedding_provider

    def search_relevant_messages(
        self,
        user_id: int,
        session_id: int,
        query: str,
        limit: int = 5,
        exclude_content: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        if not self.repository:
            return []

        try:
            vector = self._get_embedding_provider().embed_text(query)
            results = self.repository.search_messages(
                query_vector=vector,
                user_id=str(user_id),
                session_id=str(session_id),
                limit=limit
            )

            normalized_exclude = (exclude_content or "").strip()
            messages = []

            for result in results:
                payload = result.get("payload", {})
                content = (payload.get("content") or "").strip()

                if normalized_exclude and content == normalized_exclude:
                    continue

                messages.append({
                    "role": payload.get("role"),
                    "content": content,
                    "score": result.get("score"),
                    "created_at": payload.get("created_at"),
                    "metadata": {
                        key: value
                        for key, value in payload.items()
                        if key not in {"role", "content", "created_at"}
                    }
                })

            return messages

        except Exception as e:
            logger.error("Vector memory search failed: %s", str(e))
            return []

    def store_message(
        self,
        user_id: int,
        session_id: int,
        role: str,
        content: str,
        scene_id: Optional[str] = None,
        provider: Optional[str] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> None:
        if not self.repository:
            return

        try:
            vector = self._get_embedding_provider().embed_text(content)

            payload: Dict[str, Any] = {
                "user_id": str(user_id),
                "session_id": str(session_id),
                "role": role,
                "content": content,
                "created_at": datetime.now(timezone.utc).isoformat(),
                "source": "chat"
            }

            if scene_id:
                payload["scene_id"] = scene_id

            if provider:
                payload["provider"] = provider

            if metadata:
                payload["metadata"] = metadata

            self.repository.upsert_message(
                point_id=str(uuid4()),
                vector=vector,
                payload=payload
            )

        except Exception as e:
            logger.error("Vector memory store failed: %s", str(e))

    def delete_user_memory(self, user_id: int) -> None:
        if not self.repository:
            return

        try:
            self.repository.delete_messages_by_user(str(user_id))
        except Exception as e:
            logger.error(
                "Vector memory deletion failed for user_id=%s: %s",
                user_id,
                str(e)
            )

    def delete_session_memory(self, user_id: int, session_id: int) -> None:
        if not self.repository:
            return

        try:
            self.repository.delete_messages_by_session(
                user_id=str(user_id),
                session_id=str(session_id)
            )
        except Exception as e:
            logger.error(
                "Vector memory deletion failed for user_id=%s session_id=%s: %s",
                user_id,
                session_id,
                str(e)
            )
