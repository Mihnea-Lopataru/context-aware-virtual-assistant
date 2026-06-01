import logging
import re
import time
from datetime import datetime, timezone
from typing import Any, Dict, List
from uuid import NAMESPACE_URL, uuid4, uuid5

from app.core.config import settings
from app.llm.llm_client import LLMClient
from app.models.schemas.evaluation_schema import (
    EvaluationMemoryResetRequest,
    EvaluationMemorySeedRequest,
    EvaluationRunRequest
)
from app.services.memory_service import MemoryService
from app.services.prompt_builder_service import PromptBuilder


logger = logging.getLogger(__name__)

MEMORY_LIMIT = 5


class EvaluationService:
    def __init__(self):
        self.memory_service = MemoryService()
        self.prompt_builder = PromptBuilder()

    def run(self, data: EvaluationRunRequest) -> Dict[str, Any]:
        request_id = str(uuid4())
        timestamp = datetime.now(timezone.utc).isoformat()
        started_at = time.perf_counter()

        context_started_at = time.perf_counter()
        context = self._build_context(data)
        context_latency_ms = self._elapsed_ms(context_started_at)

        memory_started_at = time.perf_counter()
        semantic_memory = []
        if data.memory_enabled:
            semantic_memory = self.memory_service.search_relevant_messages(
                user_id=data.user_id,
                session_id=data.session_id,
                query=data.user_query,
                limit=MEMORY_LIMIT,
                exclude_content=data.user_query
            )
        memory_latency_ms = self._elapsed_ms(memory_started_at) if data.memory_enabled else 0

        prompt = self.prompt_builder.build(
            context=context,
            knowledge=data.knowledge,
            semantic_memory=semantic_memory if data.memory_enabled else []
        )

        llm_started_at = time.perf_counter()
        error_message = None
        fallback_used = False

        try:
            generated_response = self._clean_response_text(
                LLMClient(provider_name=data.provider).generate(prompt)
            )

            if not generated_response:
                raise RuntimeError("LLM returned empty evaluation response")
        except Exception as e:
            error_message = str(e)
            fallback_used = True
            generated_response = self._fallback_response(context)
            logger.error("Evaluation LLM generation failed: %s", error_message)

        llm_latency_ms = self._elapsed_ms(llm_started_at)
        total_latency_ms = self._elapsed_ms(started_at)

        retrieved_memory_count = len(semantic_memory) if data.memory_enabled else 0

        return {
            "evaluation_run_id": data.evaluation_run_id,
            "evaluation_id": data.evaluation_id,
            "test_case_id": data.test_case_id,
            "test_case_description": data.description,
            "context_summary": context.get("summary"),
            "generated_response": generated_response,
            "success": error_message is None,
            "error_message": error_message,
            "fallback_used": fallback_used,
            "metrics": {
                "request_id": request_id,
                "timestamp": timestamp,
                "user_id": data.user_id,
                "session_id": data.session_id,
                "provider": data.provider,
                "model": self._model_name(data.provider),
                "context_mode": data.context_mode,
                "context_enabled": data.context_enabled,
                "memory_enabled": data.memory_enabled,
                "user_query": data.user_query,
                "total_latency_ms": total_latency_ms,
                "context_building_latency_ms": context_latency_ms,
                "memory_retrieval_latency_ms": memory_latency_ms,
                "llm_generation_latency_ms": llm_latency_ms,
                "prompt_length_chars": len(prompt),
                "response_length_chars": len(generated_response or ""),
                "retrieved_memory_count": retrieved_memory_count,
                "retrieved_memories_included": data.memory_enabled and retrieved_memory_count > 0,
                "estimated_prompt_tokens": self._estimate_tokens(prompt),
                "estimated_response_tokens": self._estimate_tokens(generated_response)
            }
        }

    def seed_memory(self, data: EvaluationMemorySeedRequest) -> Dict[str, Any]:
        if data.reset_before_seed:
            self.memory_service.delete_session_memory(data.user_id, data.session_id)

        seeded_count = 0
        for memory in data.memories:
            metadata = dict(memory.metadata or {})
            metadata["evaluation_seed"] = True
            metadata["memory_id"] = memory.memory_id

            point_id = str(uuid5(
                NAMESPACE_URL,
                f"evaluation-memory:{data.user_id}:{data.session_id}:{memory.memory_id}"
            ))

            stored = self.memory_service.store_message(
                user_id=data.user_id,
                session_id=data.session_id,
                role=memory.role,
                content=memory.content,
                scene_id="PuzzleScene",
                provider="evaluation_seed",
                metadata=metadata,
                point_id=point_id
            )

            if stored:
                seeded_count += 1

        return {
            "user_id": data.user_id,
            "session_id": data.session_id,
            "seeded_memory_count": seeded_count,
            "reset": data.reset_before_seed
        }

    def reset_memory(self, data: EvaluationMemoryResetRequest) -> Dict[str, Any]:
        self.memory_service.delete_session_memory(data.user_id, data.session_id)
        return {
            "user_id": data.user_id,
            "session_id": data.session_id,
            "seeded_memory_count": 0,
            "reset": True
        }

    def _build_context(self, data: EvaluationRunRequest) -> Dict[str, Any]:
        if not data.context_enabled or data.context_mode == "none":
            return {
                "user_message": data.user_query,
                "recent_failures": 0,
                "recent_successes": 0,
                "struggling": False,
                "last_event_type": None,
                "last_event_context": None,
                "aggregated_context": {},
                "recent_events": [],
                "total_events": 0,
                "scene_state": None,
                "summary": "Evaluation context disabled for this configuration."
            }

        extras = data.model_extra or {}
        recent_events = self._normalize_recent_events(extras.get("recent_events") or [])
        scene_state = self._build_scene_state(data, extras)

        if data.context_mode == "minimal":
            return self._build_minimal_context(data, scene_state)

        recent_failures = self._count_boolean_context(recent_events, False)
        recent_successes = self._count_boolean_context(recent_events, True)

        for slot in scene_state.get("slot_states") or []:
            is_correct = slot.get("is_correct")
            if is_correct is True:
                recent_successes += 1
            elif is_correct is False:
                recent_failures += 1

        last_event = recent_events[-1] if recent_events else {}
        aggregated_context = self._build_aggregated_context(scene_state, recent_events)

        return {
            "user_message": data.user_query,
            "recent_failures": recent_failures,
            "recent_successes": recent_successes,
            "struggling": self._is_struggling(data, recent_failures, recent_successes),
            "last_event_type": last_event.get("type"),
            "last_event_context": last_event.get("context"),
            "aggregated_context": aggregated_context,
            "recent_events": recent_events[-5:],
            "total_events": len(recent_events),
            "scene_state": scene_state,
            "summary": self._build_summary(data, scene_state, recent_failures, recent_successes)
        }

    def _build_minimal_context(
        self,
        data: EvaluationRunRequest,
        scene_state: Dict[str, Any]
    ) -> Dict[str, Any]:
        puzzle_progress = scene_state.get("puzzle_progress") or {}
        minimal_scene_state = {
            "scene_id": scene_state.get("scene_id"),
            "currently_held_object": scene_state.get("currently_held_object"),
            "puzzle_progress": puzzle_progress
        }

        return {
            "user_message": data.user_query,
            "recent_failures": 0,
            "recent_successes": puzzle_progress.get("correct_placements", 0),
            "struggling": False,
            "last_event_type": None,
            "last_event_context": None,
            "aggregated_context": {"scene_state": minimal_scene_state},
            "recent_events": [],
            "total_events": 0,
            "scene_state": minimal_scene_state,
            "summary": "Minimal evaluation context includes only held object and puzzle progress."
        }

    def _build_scene_state(
        self,
        data: EvaluationRunRequest,
        extras: Dict[str, Any]
    ) -> Dict[str, Any]:
        explicit_scene_state = extras.get("scene_state")
        if isinstance(explicit_scene_state, dict):
            return explicit_scene_state

        slot_states = extras.get("slot_states") or []
        puzzle_progress = extras.get("puzzle_progress") or {}

        total_slots = puzzle_progress.get("total_slots") or len(slot_states)
        filled_slots = sum(1 for slot in slot_states if slot.get("occupied_by"))
        correct_slots = (
            puzzle_progress.get("correct_placements")
            if puzzle_progress.get("correct_placements") is not None
            else sum(1 for slot in slot_states if slot.get("is_correct") is True)
        )
        incorrect_slots = sum(1 for slot in slot_states if slot.get("is_correct") is False)

        return {
            "scene_id": extras.get("scene_name") or "PuzzleScene",
            "player_position": extras.get("player_position"),
            "player_rotation": extras.get("player_rotation"),
            "gaze_direction": extras.get("gaze_direction"),
            "currently_held_object": extras.get("currently_held_object"),
            "inventory_state": extras.get("inventory_state") or [],
            "pipe_states": extras.get("pipe_states") or [],
            "slot_states": slot_states,
            "puzzle_progress": puzzle_progress,
            "total_slots": total_slots,
            "filled_slots": filled_slots,
            "correct_slots": correct_slots,
            "incorrect_slots": incorrect_slots,
            "remaining_slots": max(total_slots - correct_slots, 0)
        }

    def _normalize_recent_events(
        self,
        events: List[Dict[str, Any]]
    ) -> List[Dict[str, Any]]:
        normalized = []
        for event in events:
            normalized.append({
                "type": event.get("event_type") or event.get("type"),
                "context": event.get("context") or {}
            })
        return normalized

    def _build_aggregated_context(
        self,
        scene_state: Dict[str, Any],
        recent_events: List[Dict[str, Any]]
    ) -> Dict[str, Any]:
        aggregated = {"scene_state": scene_state}

        for event in recent_events:
            for key, value in (event.get("context") or {}).items():
                if key not in aggregated:
                    aggregated[key] = value

        return aggregated

    def _count_boolean_context(
        self,
        recent_events: List[Dict[str, Any]],
        expected_value: bool
    ) -> int:
        return sum(
            1
            for event in recent_events
            if (event.get("context") or {}).get("is_correct") is expected_value
        )

    def _is_struggling(
        self,
        data: EvaluationRunRequest,
        recent_failures: int,
        recent_successes: int
    ) -> bool:
        if recent_failures >= 2 and recent_failures >= recent_successes:
            return True

        text = f"{data.user_query} {data.description or ''}".lower()
        return "still don't understand" in text or "repeated" in text

    def _build_summary(
        self,
        data: EvaluationRunRequest,
        scene_state: Dict[str, Any],
        recent_failures: int,
        recent_successes: int
    ) -> str:
        progress = scene_state.get("puzzle_progress") or {}
        status = progress.get("status") or "unknown"
        held_object = scene_state.get("currently_held_object") or "nothing"
        correct = scene_state.get("correct_slots")
        total = scene_state.get("total_slots")

        return (
            f"{data.description or data.test_case_id}. "
            f"Status={status}, Held={held_object}, "
            f"Correct={correct}/{total}, "
            f"RecentFailures={recent_failures}, RecentSuccesses={recent_successes}."
        )

    def _fallback_response(self, context: Dict[str, Any]) -> str:
        if context.get("struggling"):
            return (
                "Take it one step at a time: compare the pipe's shape with the "
                "slot's connection direction before placing it."
            )

        return (
            "Look closely at the pipe shape and the slot direction; the best match "
            "should follow the path the connection needs to take."
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

    def _model_name(self, provider: str) -> str:
        if provider.lower() == "openai":
            return settings.OPENAI_MODEL

        if provider.lower() == "ollama":
            return settings.OLLAMA_MODEL

        return provider

    def _estimate_tokens(self, value: str | None) -> int:
        if not value:
            return 0

        return max(round(len(value) / 4), 1)

    def _elapsed_ms(self, started_at: float) -> float:
        return round((time.perf_counter() - started_at) * 1000, 3)
