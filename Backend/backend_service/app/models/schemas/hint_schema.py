from typing import Any, Dict, Optional

from pydantic import BaseModel, Field, ConfigDict


class HintRequest(BaseModel):
    """
    Request for generating a contextual hint.
    """

    session_id: int

    message: str = Field(
        ...,
        min_length=1,
        max_length=500
    )

    knowledge: Dict[str, Any]

    provider: Optional[str] = Field(
        default="ollama",
        description="LLM provider (ollama, openai)"
    )


class HintResponse(BaseModel):
    """
    LLM-generated hint response.
    """

    hint: str = Field(...)

    confidence: Optional[float] = None

    hint_type: Optional[str] = None

    model_config = ConfigDict(from_attributes=True)