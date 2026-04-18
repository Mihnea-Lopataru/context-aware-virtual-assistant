from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, TYPE_CHECKING

from sqlalchemy import DateTime, ForeignKey, Index, String
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.db.base import Base

if TYPE_CHECKING:
    from app.models.session import Session


class Event(Base):
    
    __tablename__ = "events"

    id: Mapped[int] = mapped_column(primary_key=True, index=True)

    session_id: Mapped[int] = mapped_column(
        ForeignKey("sessions.id", ondelete="CASCADE"),
        nullable=False,
        index=True
    )

    event_type: Mapped[str] = mapped_column(
        String(50),
        nullable=False,
        index=True
    )

    timestamp: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        nullable=False,
        index=True
    )

    player_state: Mapped[dict[str, Any] | None] = mapped_column(
        JSONB,
        nullable=True
    )

    context_data: Mapped[dict[str, Any] | None] = mapped_column(
        JSONB,
        nullable=True
    )

    session: Mapped["Session"] = relationship("Session", back_populates="events")

    __table_args__ = (
        Index("ix_events_session_timestamp", "session_id", "timestamp"),
        Index("ix_events_session_type", "session_id", "event_type"),
    )