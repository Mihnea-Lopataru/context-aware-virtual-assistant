from typing import Any
from app.models.event import Event


class ContextBuilder:

    def build(self, events: list[Event], user_message: str) -> dict[str, Any]:
        if not events:
            return {
                "user_message": user_message,
                "summary": "No recent gameplay events available."
            }

        last_event = events[0]

        recent_failures = 0
        recent_successes = 0

        aggregated_context = {}
        recent_events_summary = []

        scene_state = None

        for event in events:
            context = event.context_data or {}

            success = context.get("is_correct")

            if success is True:
                recent_successes += 1
            elif success is False:
                recent_failures += 1

            for key, value in context.items():
                aggregated_context[key] = value

            recent_events_summary.append({
                "type": event.event_type,
                "context": context
            })

            if scene_state is None and "scene_state" in context:
                scene_state = context.get("scene_state")

        struggling = (
            recent_failures >= 3 and
            recent_failures >= recent_successes
        )

        return {
            "user_message": user_message,

            "recent_failures": recent_failures,
            "recent_successes": recent_successes,
            "struggling": struggling,

            "last_event_type": last_event.event_type,
            "last_event_context": last_event.context_data,

            "aggregated_context": aggregated_context,
            "recent_events": recent_events_summary[:5],
            "total_events": len(events),

            "scene_state": scene_state,

            "summary": self._build_summary(
                last_event.event_type,
                struggling
            )
        }

    def _build_summary(self, last_event_type: str, struggling: bool) -> str:
        if struggling:
            return f"The player seems to be struggling. Last action: {last_event_type}."

        return f"The last action performed was {last_event_type}."