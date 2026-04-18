from typing import List, Optional, Dict, Any

from pydantic import BaseModel, Field


class PlayerState(BaseModel):

    position: Optional[Dict[str, float]] = None
    rotation: Optional[Dict[str, float]] = None
    forward: Optional[Dict[str, float]] = None


class PlayerEvent(BaseModel):

    event_id: Optional[str] = None
    session_id: Optional[int] = None

    event_type: str = Field(...)

    timestamp: Optional[int] = None

    player_state: Optional[PlayerState] = None

    context: Optional[Dict[str, Any]] = None 


class EventBatch(BaseModel):

    session_id: int = Field(...)

    events: List[PlayerEvent]