from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import TYPE_CHECKING, Any

from sqlalchemy import DateTime, Enum as SqlEnum, ForeignKey, Index, String
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.db.base import Base

if TYPE_CHECKING:
    from app.models.user import User


class SessionStatus(str, Enum):
    ACTIVE = "active"
    CLOSED = "closed"
    EXPIRED = "expired"


class Session(Base):
    """
    ORM model for user gameplay / interaction sessions.

    A session:
    - belongs to exactly one user
    - can be active, closed, or expired
    - stores only compact, relevant contextual information
    - is preserved after closure for future adaptation / LLM context building
    """

    __tablename__ = "sessions"

    # =========================
    # PRIMARY KEY
    # =========================
    id: Mapped[int] = mapped_column(
        primary_key=True,
        index=True
    )

    # =========================
    # RELATIONSHIP / OWNERSHIP
    # =========================
    user_id: Mapped[int] = mapped_column(
        ForeignKey("users.id", ondelete="CASCADE"),
        nullable=False,
        index=True
    )

    user: Mapped["User"] = relationship(
        back_populates="sessions"
    )

    # =========================
    # STATUS
    # =========================
    status: Mapped[SessionStatus] = mapped_column(
        SqlEnum(SessionStatus, name="session_status"),
        default=SessionStatus.ACTIVE,
        nullable=False,
        index=True
    )

    # =========================
    # TIMESTAMPS
    # =========================
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

    # =========================
    # COMPACT CONTEXT FOR LLM / ADAPTATION
    # =========================
    current_scene: Mapped[str | None] = mapped_column(
        String(100),
        nullable=True
    )

    current_objective: Mapped[str | None] = mapped_column(
        String(255),
        nullable=True
    )

    context_data: Mapped[dict[str, Any] | None] = mapped_column(
        JSONB,
        nullable=True
    )

    behavior_summary: Mapped[dict[str, Any] | None] = mapped_column(
        JSONB,
        nullable=True
    )

    session_summary: Mapped[dict[str, Any] | None] = mapped_column(
        JSONB,
        nullable=True
    )

    __table_args__ = (
        Index("ix_sessions_user_status", "user_id", "status"),
        Index("ix_sessions_user_last_activity", "user_id", "last_activity_at"),
    )