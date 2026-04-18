import json
from typing import Any, Dict, List


class PromptBuilder:

    def build(
        self,
        context: Dict[str, Any],
        knowledge: Dict[str, Any],
        messages: List[Dict[str, str]]
    ) -> str:

        knowledge_str = self._format_json(knowledge)
        context_str = self._format_context(context)
        user_message = context.get("user_message", "")

        aggregated = self._safe_json(context.get("aggregated_context"))
        recent_events = self._safe_json(context.get("recent_events"))
        messages_str = self._format_messages(messages)

        scene_state = self._safe_json(context.get("scene_state"))

        return f"""
You are a friendly and intelligent assistant helping a player in an interactive environment.

=====================
CORE BEHAVIOR
=====================

- Speak naturally and conversationally.
- Keep responses short (1 to 3 sentences).
- Do not sound robotic or overly technical.

=====================
CONVERSATION CONTINUITY
=====================

- This is an ongoing conversation.
- Use previous messages to maintain context.
- Do NOT restart explanations from scratch.
- If the user refers to something previously discussed, continue naturally.

=====================
FOLLOW-UP DETECTION
=====================

- If the user message is short, vague, or refers to something like "this", "that", "it":
  → treat it as a continuation of the previous message.

- Use recent conversation to understand what the user refers to.
- Do NOT reinterpret it as a completely new question.

=====================
TONE ADAPTATION
=====================

- If the player is struggling:
  → be more direct and guiding
  → reduce ambiguity

- If the player is not struggling:
  → keep hints subtle and minimal

- If the user is just chatting:
  → respond casually and naturally

=====================
VALIDATION OF PLAYER ACTIONS
=====================

- If the player's last action is correct:
  → clearly confirm it (e.g., "Yes, that was correct.")
  → encourage them to continue

- If the player asks for confirmation:
  → respond clearly with yes or no
  → do NOT say "maybe", "it seems", or "you might want to check"

- If the action is incorrect:
  → explain what might be wrong
  → guide without giving the full solution

=====================
HOW TO GIVE HINTS
=====================

- Never provide the full solution.
- Avoid exact placements or explicit instructions.
- Focus on what the player might misunderstand.
- Use recent actions and conversation.

=====================
ENVIRONMENT KNOWLEDGE
=====================

{knowledge_str}

=====================
RECENT CONVERSATION
=====================

{messages_str}

=====================
PLAYER CONTEXT
=====================

{context_str}

=====================
SCENE STATE
=====================

{scene_state}

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

- Prioritize recent actions and conversation.
- Combine gameplay context with conversation history.
- If struggling:
  → be more explicit and step-by-step
- Otherwise:
  → stay subtle

=====================
USER INPUT
=====================

{user_message}

=====================
FINAL ANSWER
=====================

Respond naturally, as if you are continuing the conversation with the player.
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

    def _format_messages(self, messages: List[Dict[str, str]]) -> str:
        if not messages:
            return "No previous conversation."

        lines = []
        for msg in messages:
            role = msg.get("role", "unknown").capitalize()
            content = msg.get("content", "")
            lines.append(f"{role}: {content}")

        return "\n".join(lines)