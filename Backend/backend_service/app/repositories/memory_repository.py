from typing import Any, Dict, List

from qdrant_client.models import (
    FieldCondition,
    Filter,
    FilterSelector,
    MatchValue,
    PointStruct
)

from app.core.config import settings
from app.core.qdrant import ensure_chat_memory_collection, get_qdrant_client


class MemoryRepository:
    def __init__(self):
        ensure_chat_memory_collection()
        self.client = get_qdrant_client()
        self.collection_name = settings.QDRANT_COLLECTION_NAME

    def upsert_message(
        self,
        point_id: str,
        vector: List[float],
        payload: Dict[str, Any]
    ) -> None:
        self.client.upsert(
            collection_name=self.collection_name,
            points=[
                PointStruct(
                    id=point_id,
                    vector=vector,
                    payload=payload
                )
            ]
        )

    def search_messages(
        self,
        query_vector: List[float],
        user_id: str,
        session_id: str,
        limit: int = 5
    ) -> List[Dict[str, Any]]:
        query_filter = Filter(
            must=[
                FieldCondition(
                    key="user_id",
                    match=MatchValue(value=user_id)
                ),
                FieldCondition(
                    key="session_id",
                    match=MatchValue(value=session_id)
                ),
                FieldCondition(
                    key="source",
                    match=MatchValue(value="chat")
                )
            ]
        )

        if hasattr(self.client, "search"):
            results = self.client.search(
                collection_name=self.collection_name,
                query_vector=query_vector,
                query_filter=query_filter,
                limit=limit,
                with_payload=True
            )
        else:
            response = self.client.query_points(
                collection_name=self.collection_name,
                query=query_vector,
                query_filter=query_filter,
                limit=limit,
                with_payload=True
            )
            results = response.points

        return [
            {
                "id": str(point.id),
                "score": point.score,
                "payload": point.payload or {}
            }
            for point in results
        ]

    def delete_messages_by_user(self, user_id: str) -> None:
        self.client.delete(
            collection_name=self.collection_name,
            points_selector=FilterSelector(
                filter=Filter(
                    must=[
                        FieldCondition(
                            key="user_id",
                            match=MatchValue(value=user_id)
                        ),
                        FieldCondition(
                            key="source",
                            match=MatchValue(value="chat")
                        )
                    ]
                )
            )
        )

    def delete_messages_by_session(self, user_id: str, session_id: str) -> None:
        self.client.delete(
            collection_name=self.collection_name,
            points_selector=FilterSelector(
                filter=Filter(
                    must=[
                        FieldCondition(
                            key="user_id",
                            match=MatchValue(value=user_id)
                        ),
                        FieldCondition(
                            key="session_id",
                            match=MatchValue(value=session_id)
                        ),
                        FieldCondition(
                            key="source",
                            match=MatchValue(value="chat")
                        )
                    ]
                )
            )
        )
