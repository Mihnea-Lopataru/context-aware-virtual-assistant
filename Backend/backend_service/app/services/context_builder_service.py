from typing import Any

from app.models.event import Event


class ContextBuilder:
    def build(self, events: list[Event], user_message: str) -> dict[str, Any]:
        if not events:
            return {
                "user_message": user_message,
                "recent_event_count": 0,
                "recent_failures": 0,
                "recent_successes": 0,
                "last_event_type": None,
                "last_event_context": None,
                "struggling": False,
                "summary": "No recent gameplay events available."
            }

        recent_failures = 0
        recent_successes = 0

        last_event = events[0]

        for event in events:
            context = event.context_data or {}

            success = context.get("success")

            if success is True:
                recent_successes += 1
            elif success is False:
                recent_failures += 1

        struggling = recent_failures >= 3 and recent_failures > recent_successes

        return {
            "user_message": user_message,
            "recent_event_count": len(events),
            "recent_failures": recent_failures,
            "recent_successes": recent_successes,
            "last_event_type": last_event.event_type,
            "last_event_context": last_event.context_data,
            "struggling": struggling,
            "summary": self._build_summary(
                recent_failures,
                recent_successes,
                last_event.event_type,
                struggling
            )
        }

    def _build_summary(
        self,
        failures: int,
        successes: int,
        last_event_type: str,
        struggling: bool
    ) -> str:
        if struggling:
            return (
                f"The player has {failures} recent failed attempts and may be struggling. "
                f"The last action was {last_event_type}."
            )

        return (
            f"The player has {successes} successful actions and {failures} failed attempts. "
            f"The last action was {last_event_type}."
        )