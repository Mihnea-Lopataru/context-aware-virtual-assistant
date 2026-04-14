from datetime import datetime, timezone, timedelta
from typing import Optional

from sqlalchemy.orm import Session

from app.models.session import Session as SessionModel, SessionStatus
from app.repositories.session_repository import SessionRepository


SESSION_TIMEOUT_MINUTES = 60


class SessionService:
    def __init__(self, db: Session):
        self.repo = SessionRepository(db)

    def start_session(self, user_id: int, **kwargs) -> SessionModel:
        active_session = self.repo.get_active_by_user(user_id)

        if active_session:
            self.repo.close(active_session)

        return self.repo.create(user_id=user_id, **kwargs)

    def get_active_session(self, user_id: int) -> Optional[SessionModel]:
        session = self.repo.get_active_by_user(user_id)

        if not session:
            return None

        if self._is_expired(session):
            self.repo.close(session, expired=True)
            return None

        return session

    def update_session(self, session_id: int, **kwargs) -> Optional[SessionModel]:
        session = self.repo.get_by_id(session_id)

        if not session:
            return None

        if session.status != SessionStatus.ACTIVE:
            return None

        return self.repo.update(session, **kwargs)

    def end_session(self, session_id: int) -> Optional[SessionModel]:
        session = self.repo.get_by_id(session_id)

        if not session:
            return None

        if session.status != SessionStatus.ACTIVE:
            return session

        return self.repo.close(session)

    def _is_expired(self, session: SessionModel) -> bool:
        now = datetime.now(timezone.utc)

        return (
            now - session.last_activity_at
        ) > timedelta(minutes=SESSION_TIMEOUT_MINUTES)