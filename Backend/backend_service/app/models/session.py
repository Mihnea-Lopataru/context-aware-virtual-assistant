from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import TYPE_CHECKING, List

from sqlalchemy import DateTime, Enum as SqlEnum, ForeignKey, Index, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.db.base import Base

if TYPE_CHECKING:
    from app.models.user import User
    from app.models.event import Event
    from app.models.message import Message


class SessionStatus(str, Enum):
    ACTIVE = "active"
    CLOSED = "closed"
    EXPIRED = "expired"


class Session(Base):
    
    __tablename__ = "sessions"

    id: Mapped[int] = mapped_column(primary_key=True, index=True)

    user_id: Mapped[int] = mapped_column(
        ForeignKey("users.id", ondelete="CASCADE"),
        nullable=False,
        index=True
    )

    user: Mapped["User"] = relationship(
        back_populates="sessions"
    )

    events: Mapped[List["Event"]] = relationship(
        "Event",
        back_populates="session",
        cascade="all, delete-orphan",
        passive_deletes=True
    )

    messages: Mapped[List["Message"]] = relationship(
        "Message",
        back_populates="session",
        cascade="all, delete-orphan",
        passive_deletes=True
    )

    status: Mapped[SessionStatus] = mapped_column(
        SqlEnum(SessionStatus, name="session_status"),
        default=SessionStatus.ACTIVE,
        nullable=False,
        index=True
    )

    started_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        nullable=False
    )

    last_activity_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        nullable=False,
        index=True
    )

    ended_at: Mapped[datetime | None] = mapped_column(
        DateTime(timezone=True),
        nullable=True
    )

    current_scene: Mapped[str | None] = mapped_column(
        String(100),
        nullable=True
    )

    current_objective: Mapped[str | None] = mapped_column(
        String(255),
        nullable=True
    )

    __table_args__ = (
        Index("ix_sessions_user_status", "user_id", "status"),
        Index("ix_sessions_user_last_activity", "user_id", "last_activity_at"),
    )