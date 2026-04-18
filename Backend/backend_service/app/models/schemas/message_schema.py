from pydantic import BaseModel, Field, field_validator
from typing import Optional, Dict, Any
from enum import Enum


class MessageRole(str, Enum):
    USER = "user"
    ASSISTANT = "assistant"
    SYSTEM = "system"

class MessageCreate(BaseModel):
    session_id: int
    role: MessageRole
    content: str = Field(..., min_length=1, max_length=5000)
    message_metadata: Optional[Dict[str, Any]] = None

    @field_validator("content")
    @classmethod
    def strip_content(cls, v: str) -> str:
        v = v.strip()
        if not v:
            raise ValueError("Content cannot be empty")
        return v