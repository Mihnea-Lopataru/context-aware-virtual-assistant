from datetime import datetime, timezone
from typing import Optional, List

from sqlalchemy.orm import Session

from app.models.session import Session as SessionModel, SessionStatus


class SessionRepository:
    
    def __init__(self, db: Session):
        self.db = db

    def create(self, user_id: int, **kwargs) -> SessionModel:
        now = datetime.now(timezone.utc)

        session = SessionModel(
            user_id=user_id,
            status=SessionStatus.ACTIVE,
            started_at=now,
            last_activity_at=now,
            **kwargs
        )

        self.db.add(session)
        self.db.commit()
        self.db.refresh(session)

        return session

    def get_by_id(self, session_id: int) -> Optional[SessionModel]:
        return (
            self.db.query(SessionModel)
            .filter(SessionModel.id == session_id)
            .first()
        )

    def get_active_by_user(self, user_id: int) -> Optional[SessionModel]:
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
        return (
            self.db.query(SessionModel)
            .filter(SessionModel.user_id == user_id)
            .order_by(SessionModel.started_at.desc())
            .all()
        )

    def update(self, session: SessionModel, **kwargs) -> SessionModel:
        for field, value in kwargs.items():
            if hasattr(session, field):
                setattr(session, field, value)

        session.last_activity_at = datetime.now(timezone.utc)

        self.db.commit()
        self.db.refresh(session)

        return session

    def update_activity(self, session: SessionModel) -> SessionModel:
        session.last_activity_at = datetime.now(timezone.utc)

        self.db.commit()
        self.db.refresh(session)

        return session

    def close(self, session: SessionModel, expired: bool = False) -> SessionModel:
        now = datetime.now(timezone.utc)

        session.status = (
            SessionStatus.EXPIRED if expired else SessionStatus.CLOSED
        )
        session.ended_at = now
        session.last_activity_at = now

        self.db.commit()
        self.db.refresh(session)

        return session

    def delete(self, session: SessionModel) -> None:
        self.db.delete(session)
        self.db.commit()