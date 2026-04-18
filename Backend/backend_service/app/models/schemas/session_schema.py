from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field, ConfigDict

from app.models.session import SessionStatus


class SessionBase(BaseModel):

    current_scene: Optional[str] = Field(
        default=None,
        max_length=100
    )

    current_objective: Optional[str] = Field(
        default=None,
        max_length=255
    )


class SessionCreate(SessionBase):

    user_id: int


class SessionUpdate(BaseModel):

    current_scene: Optional[str] = Field(
        default=None,
        max_length=100
    )

    current_objective: Optional[str] = Field(
        default=None,
        max_length=255
    )


class SessionResponse(SessionBase):

    id: int
    user_id: int
    status: SessionStatus

    started_at: datetime
    last_activity_at: datetime
    ended_at: Optional[datetime]

    model_config = ConfigDict(from_attributes=True)