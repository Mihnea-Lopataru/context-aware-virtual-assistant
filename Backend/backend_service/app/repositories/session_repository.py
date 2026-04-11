from datetime import datetime, timezone
from typing import Optional, List

from sqlalchemy.orm import Session

from app.models.session import Session as SessionModel, SessionStatus


class SessionRepository:
    """
    Repository for Session entity.
    Handles database operations only (no business logic).
    """

    def __init__(self, db: Session):
        self.db = db

    # =========================
    # CREATE
    # =========================
    def create(self, user_id: int, **kwargs) -> SessionModel:
        """
        Creates a new session for a user.
        """

        session = SessionModel(
            user_id=user_id,
            status=SessionStatus.ACTIVE,
            started_at=datetime.now(timezone.utc),
            last_activity_at=datetime.now(timezone.utc),
            **kwargs
        )

        self.db.add(session)
        self.db.commit()
        self.db.refresh(session)

        return session

    # =========================
    # READ
    # =========================
    def get_by_id(self, session_id: int) -> Optional[SessionModel]:
        return self.db.query(SessionModel).filter(SessionModel.id == session_id).first()

    def get_active_by_user(self, user_id: int) -> Optional[SessionModel]:
        """
        Returns the currently active session for a user.
        """

        return (
            self.db.query(SessionModel)
            .filter(
                SessionModel.user_id == user_id,
                SessionModel.status == SessionStatus.ACTIVE
            )
            .order_by(SessionModel.started_at.desc())
            .first()
        )

    def get_all_by_user(self, user_id: int) -> List[SessionModel]:
        """
        Returns all sessions for a user.
        """

        return (
            self.db.query(SessionModel)
            .filter(SessionModel.user_id == user_id)
            .order_by(SessionModel.started_at.desc())
            .all()
        )

    # =========================
    # UPDATE
    # =========================
    def update(self, session: SessionModel, **kwargs) -> SessionModel:
        """
        Generic update method.
        """

        for field, value in kwargs.items():
            if hasattr(session, field) and value is not None:
                setattr(session, field, value)

        # update activity timestamp automatically
        session.last_activity_at = datetime.now(timezone.utc)

        self.db.commit()
        self.db.refresh(session)

        return session

    def update_activity(self, session: SessionModel) -> SessionModel:
        """
        Updates only the activity timestamp.
        """

        session.last_activity_at = datetime.now(timezone.utc)

        self.db.commit()
        self.db.refresh(session)

        return session

    # =========================
    # CLOSE SESSION
    # =========================
    def close(self, session: SessionModel, expired: bool = False) -> SessionModel:
        """
        Closes a session.
        """

        session.status = (
            SessionStatus.EXPIRED if expired else SessionStatus.CLOSED
        )
        session.ended_at = datetime.now(timezone.utc)

        self.db.commit()
        self.db.refresh(session)

        return session

    # =========================
    # DELETE (rarely used)
    # =========================
    def delete(self, session: SessionModel) -> None:
        """
        Deletes a session (normally not used).
        """

        self.db.delete(session)
        self.db.commit()