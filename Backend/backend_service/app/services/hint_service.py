import logging
import re
from typing import List

from sqlalchemy.orm import Session

from app.models.event import Event
from app.models.schemas.hint_schema import HintRequest, HintResponse

from app.repositories.event_repository import EventRepository
from app.repositories.session_repository import SessionRepository

from app.services.context_builder_service import ContextBuilder
from app.services.memory_service import MemoryService
from app.services.prompt_builder_service import PromptBuilder
from app.llm.llm_client import LLMClient


logger = logging.getLogger(__name__)

EVENT_LIMIT = 10
MEMORY_LIMIT = 5


class HintService:
    def __init__(self, db: Session):
        self.session_repo = SessionRepository(db)
        self.event_repo = EventRepository(db)

        self.context_builder = ContextBuilder()
        self.memory_service = MemoryService()
        self.prompt_builder = PromptBuilder()

    def generate_hint(self, data: HintRequest) -> HintResponse:
        session = self.session_repo.get_by_id(data.session_id)
        if not session:
            raise ValueError("Session not found.")

        events: List[Event] = self.event_repo.get_last_n_by_session(
            session_id=data.session_id,
            limit=EVENT_LIMIT
        )

        context = self.context_builder.build(events, data.message)
        scene_id = self._extract_scene_id(session, context)

        semantic_memory = self.memory_service.search_relevant_messages(
            user_id=session.user_id,
            session_id=data.session_id,
            query=data.message,
            limit=MEMORY_LIMIT,
            exclude_content=data.message
        )

        prompt = self.prompt_builder.build(
            context=context,
            knowledge=data.knowledge,
            semantic_memory=semantic_memory
        )

        try:
            llm_client = LLMClient(provider_name=data.provider)
            response_text = self._clean_response_text(
                llm_client.generate(prompt)
            )

            if not response_text:
                raise RuntimeError("LLM returned empty player-facing response")

            hint_type = "llm"

        except Exception as e:
            logger.error("LLM hint generation failed, using fallback: %s", str(e))
            response_text = self._fallback_hint(context)
            hint_type = "fallback"

        self.memory_service.store_message(
            user_id=session.user_id,
            session_id=data.session_id,
            role="user",
            content=data.message,
            scene_id=scene_id,
            provider=data.provider
        )

        self.memory_service.store_message(
            user_id=session.user_id,
            session_id=data.session_id,
            role="assistant",
            content=response_text,
            scene_id=scene_id,
            provider=data.provider if hint_type == "llm" else hint_type,
            metadata={
                "struggling": context.get("struggling"),
                "hint_type": hint_type
            }
        )

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

    def _clean_response_text(self, text: str) -> str:
        if not text:
            return ""

        cleaned = text.strip()

        possible_response_match = re.search(
            r"(?:possible response|final answer|player-facing reply)\s*:\s*"
            r"[\r\n\s]*(?P<quote>[\"'])(?P<answer>.+?)(?P=quote)",
            cleaned,
            flags=re.IGNORECASE | re.DOTALL
        )
        if possible_response_match:
            cleaned = possible_response_match.group("answer").strip()

        cleaned = re.sub(
            r"(?is)^based on (?:the )?context.*?(?:possible response\s*:|response\s*:)",
            "",
            cleaned
        ).strip()

        cleaned = re.sub(
            r"(?is)^here(?:'s| is) (?:a )?possible response\s*:\s*",
            "",
            cleaned
        ).strip()

        cleaned = re.sub(
            r"(?is)\n*this response (?:provides|is|should).*$",
            "",
            cleaned
        ).strip()

        cleaned = cleaned.strip("\"' \n\r\t")

        lines = [
            line.strip()
            for line in cleaned.splitlines()
            if line.strip()
        ]

        if len(lines) > 3:
            cleaned = " ".join(lines[:3])
        else:
            cleaned = " ".join(lines)

        return cleaned.strip()

    def _extract_scene_id(self, session, context: dict) -> str | None:
        if getattr(session, "current_scene", None):
            return session.current_scene

        scene_state = context.get("scene_state")
        if isinstance(scene_state, dict):
            scene_id = (
                scene_state.get("scene_id") or
                scene_state.get("scene") or
                scene_state.get("name")
            )
            if scene_id:
                return str(scene_id)

        aggregated_context = context.get("aggregated_context")
        if isinstance(aggregated_context, dict):
            scene_id = (
                aggregated_context.get("scene_id") or
                aggregated_context.get("scene") or
                aggregated_context.get("current_scene")
            )
            if scene_id:
                return str(scene_id)

        return None
