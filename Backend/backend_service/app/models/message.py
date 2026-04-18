from __future__ import annotations

from datetime import datetime, timezone
from typing import TYPE_CHECKING, Optional, Dict, Any

from sqlalchemy import String, ForeignKey, DateTime, Text
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.db.base import Base

if TYPE_CHECKING:
    from app.models.session import Session


class Message(Base):
    __tablename__ = "messages"

    id: Mapped[int] = mapped_column(primary_key=True, index=True)

    session_id: Mapped[int] = mapped_column(
        ForeignKey("sessions.id", ondelete="CASCADE"),
        nullable=False,
        index=True
    )

    role: Mapped[str] = mapped_column(String, nullable=False)

    content: Mapped[str] = mapped_column(Text, nullable=False)

    message_metadata: Mapped[Optional[Dict[str, Any]]] = mapped_column(
        JSONB,
        nullable=True
    )

    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        nullable=False
    )

    session: Mapped["Session"] = relationship(
        back_populates="messages"
    )