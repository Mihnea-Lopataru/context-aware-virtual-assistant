from datetime import datetime
from typing import Any, Dict, Optional
from enum import Enum

from pydantic import BaseModel, Field


# =========================
# ENUM
# =========================
class SessionStatus(str, Enum):
    active = "active"
    closed = "closed"
    expired = "expired"


# =========================
# BASE SCHEMA
# =========================
class SessionBase(BaseModel):
    """
    Shared session fields.
    """

    current_scene: Optional[str] = Field(
        default=None,
        max_length=100,
        description="Current Unity scene"
    )

    current_objective: Optional[str] = Field(
        default=None,
        max_length=255,
        description="Current objective or task"
    )

    context_data: Optional[Dict[str, Any]] = Field(
        default=None,
        description="Dynamic context data (position, inventory, puzzle state, etc.)"
    )

    behavior_summary: Optional[Dict[str, Any]] = Field(
        default=None,
        description="Aggregated behavior data for adaptation"
    )


# =========================
# CREATE SCHEMA
# =========================
class SessionCreate(SessionBase):
    """
    Schema used when creating a session.
    """

    user_id: int


# =========================
# UPDATE SCHEMA
# =========================
class SessionUpdate(BaseModel):
    """
    Schema used for updating session state.
    Supports partial updates.
    """

    current_scene: Optional[str] = None
    current_objective: Optional[str] = None
    context_data: Optional[Dict[str, Any]] = None
    behavior_summary: Optional[Dict[str, Any]] = None


# =========================
# RESPONSE SCHEMA
# =========================
class SessionResponse(SessionBase):
    """
    Schema returned to the client.
    """

    id: int
    user_id: int
    status: SessionStatus

    started_at: datetime
    last_activity_at: datetime
    ended_at: Optional[datetime]

    class Config:
        from_attributes = True