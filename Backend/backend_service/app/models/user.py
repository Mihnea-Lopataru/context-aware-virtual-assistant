from datetime import datetime, timezone

from sqlalchemy import String, DateTime, Boolean
from sqlalchemy.orm import Mapped, mapped_column

from app.models.db.base import Base


class User(Base):
    """
    ORM model for application users.

    Fields are designed to support:
    - identification (username)
    - lifecycle tracking (created_at, updated_at)
    - soft deletion (is_active)
    - future personalization / analytics
    """

    __tablename__ = "users"

    # =========================
    # PRIMARY KEY
    # =========================
    id: Mapped[int] = mapped_column(
        primary_key=True,
        index=True
    )

    # =========================
    # CORE FIELDS
    # =========================
    username: Mapped[str] = mapped_column(
        String(100),
        unique=True,
        index=True,
        nullable=False
    )

    # =========================
    # STATUS
    # =========================
    is_active: Mapped[bool] = mapped_column(
        Boolean,
        default=True,
        nullable=False
    )

    # =========================
    # TIMESTAMPS
    # =========================
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        nullable=False
    )

    updated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        default=lambda: datetime.now(timezone.utc),
        onupdate=lambda: datetime.now(timezone.utc),
        nullable=False
    )