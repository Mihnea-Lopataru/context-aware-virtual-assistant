import json
from typing import Any, Dict


class PromptBuilder:

    def build(
        self,
        context: Dict[str, Any],
        knowledge: Dict[str, Any]
    ) -> str:

        knowledge_str = self._format_json(knowledge)
        context_str = self._format_context(context)
        user_message = context.get("user_message", "")

        aggregated = self._safe_json(context.get("aggregated_context"))
        recent_events = self._safe_json(context.get("recent_events"))

        return f"""
You are a friendly and intelligent assistant helping a player in an interactive environment.

=====================
CORE BEHAVIOR
=====================

- Speak naturally and conversationally.
- Keep responses short (1 to 3 sentences).
- Do not sound robotic or overly technical.

=====================
WHEN TO HELP WITH THE PUZZLE
=====================

- If the user asks for help → provide a hint.
- If the user seems confused or stuck → guide them gently.
- If the user is just chatting → respond normally, do NOT force hints.

=====================
HOW TO GIVE HINTS
=====================

- Never provide the full solution.
- Avoid exact placements or instructions.
- Focus on what the player might be misunderstanding.
- Use recent actions when relevant.

=====================
ENVIRONMENT KNOWLEDGE
=====================

{knowledge_str}

=====================
PLAYER CONTEXT
=====================

{context_str}

=====================
ADDITIONAL CONTEXT
=====================

Aggregated context:
{aggregated}

Recent events:
{recent_events}

=====================
REASONING GUIDELINES
=====================

- Use context only if it helps answer the user.
- Prioritize the latest actions.
- If the player is struggling → be slightly more direct.
- Otherwise → stay subtle.

=====================
USER INPUT
=====================

{user_message}

=====================
FINAL ANSWER
=====================

Respond like you are talking directly to the player.
""".strip()

    def _format_json(self, data: Dict[str, Any]) -> str:
        if not data:
            return "No knowledge provided."

        try:
            return json.dumps(data, indent=2)
        except Exception:
            return "Invalid knowledge format."

    def _format_context(self, context: Dict[str, Any]) -> str:
        if not context:
            return "No context available."

        return f"""
Recent failures: {context.get("recent_failures")}
Recent successes: {context.get("recent_successes")}
Struggling: {context.get("struggling")}

Last event type:
{context.get("last_event_type")}

Last event details:
{self._safe_json(context.get("last_event_context"))}

Summary:
{context.get("summary")}
"""

    def _safe_json(self, data: Any) -> str:
        try:
            return json.dumps(data or {}, indent=2)
        except Exception:
            return "Invalid data"