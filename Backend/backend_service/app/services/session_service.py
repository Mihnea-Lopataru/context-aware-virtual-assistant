from datetime import datetime, timezone, timedelta
from typing import Optional

from sqlalchemy.orm import Session

from app.models.session import Session as SessionModel, SessionStatus
from app.repositories.session_repository import SessionRepository


SESSION_TIMEOUT_MINUTES = 60


class SessionService:
    """
    Handles business logic for sessions.
    """

    def __init__(self, db: Session):
        self.db = db
        self.repo = SessionRepository(db)

    # =========================
    # START SESSION
    # =========================
    def start_session(self, user_id: int, **kwargs) -> SessionModel:
        """
        Starts a new session.

        Rules:
        - Only one active session per user
        - Existing active session is closed before creating a new one
        """

        active_session = self.repo.get_active_by_user(user_id)

        if active_session:
            self.repo.close(active_session)

        return self.repo.create(user_id=user_id, **kwargs)

    # =========================
    # GET ACTIVE SESSION
    # =========================
    def get_active_session(self, user_id: int) -> Optional[SessionModel]:
        """
        Returns active session, applying timeout logic.
        """

        session = self.repo.get_active_by_user(user_id)

        if not session:
            return None

        # check timeout
        if self._is_expired(session):
            self.repo.close(session, expired=True)
            return None

        return session

    # =========================
    # UPDATE SESSION
    # =========================
    def update_session(self, session_id: int, **kwargs) -> Optional[SessionModel]:
        """
        Updates session data and refreshes activity timestamp.
        """

        session = self.repo.get_by_id(session_id)

        if not session:
            return None

        return self.repo.update(session, **kwargs)

    # =========================
    # END SESSION
    # =========================
    def end_session(self, session_id: int) -> Optional[SessionModel]:
        """
        Manually closes a session.
        """

        session = self.repo.get_by_id(session_id)

        if not session:
            return None

        return self.repo.close(session)

    # =========================
    # INTERNAL
    # =========================
    def _is_expired(self, session: SessionModel) -> bool:
        """
        Checks if session is expired based on inactivity.
        """

        now = datetime.now(timezone.utc)

        return (
            now - session.last_activity_at
        ) > timedelta(minutes=SESSION_TIMEOUT_MINUTES)