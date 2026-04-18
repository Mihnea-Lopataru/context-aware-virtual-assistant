from typing import List

from sqlalchemy.orm import Session

from app.models.event import Event
from app.models.schemas.hint_schema import HintRequest, HintResponse
from app.models.schemas.message_schema import MessageCreate, MessageRole

from app.repositories.event_repository import EventRepository
from app.repositories.session_repository import SessionRepository
from app.repositories.message_repository import MessageRepository

from app.services.context_builder_service import ContextBuilder
from app.services.prompt_builder_service import PromptBuilder
from app.llm.llm_client import LLMClient


EVENT_LIMIT = 10
MESSAGE_LIMIT = 5


class HintService:
    def __init__(self, db: Session):
        self.db = db

        self.session_repo = SessionRepository(db)
        self.event_repo = EventRepository(db)
        self.message_repo = MessageRepository()

        self.context_builder = ContextBuilder()
        self.prompt_builder = PromptBuilder()

    def generate_hint(self, data: HintRequest) -> HintResponse:
        session = self.session_repo.get_by_id(data.session_id)
        if not session:
            raise ValueError("Session not found.")

        events: List[Event] = self.event_repo.get_last_n_by_session(
            session_id=data.session_id,
            limit=EVENT_LIMIT
        )

        messages = self.message_repo.get_last_messages(
            db=self.db,
            session_id=data.session_id,
            limit=MESSAGE_LIMIT
        )

        formatted_messages = [
            {
                "role": m.role,
                "content": m.content
            }
            for m in messages
        ]

        context = self.context_builder.build(events, data.message)

        prompt = self.prompt_builder.build(
            context=context,
            knowledge=data.knowledge,
            messages=formatted_messages
        )

        try:
            llm_client = LLMClient(provider_name=data.provider)
            response_text = llm_client.generate(prompt)

            hint_type = "llm"

        except Exception:
            response_text = self._fallback_hint(context)
            hint_type = "fallback"

        self.message_repo.bulk_create(
            db=self.db,
            messages=[
                MessageCreate(
                    session_id=data.session_id,
                    role=MessageRole.USER,
                    content=data.message
                ),
                MessageCreate(
                    session_id=data.session_id,
                    role=MessageRole.ASSISTANT,
                    content=response_text,
                    message_metadata={
                        "struggling": context.get("struggling")
                    }
                )
            ]
        )

        self.db.commit()

        return HintResponse(
            hint=response_text,
            confidence=None,
            hint_type=hint_type
        )

    def _fallback_hint(self, context: dict) -> str:
        failures = context.get("recent_failures", 0)
        successes = context.get("recent_successes", 0)
        struggling = context.get("struggling", False)

        if struggling or failures > successes:
            return (
                "It looks like you're struggling. Try reconsidering your last action "
                "and how it relates to the current goal."
            )

        return (
            "Focus on your last action and think about how it influences your progress."
        )