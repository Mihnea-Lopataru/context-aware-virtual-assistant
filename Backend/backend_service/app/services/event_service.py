from typing import List

from sqlalchemy.orm import Session

from app.models.event import Event
from app.models.session import SessionStatus
from app.repositories.event_repository import EventRepository
from app.repositories.session_repository import SessionRepository


class EventService:
    def __init__(self, db: Session):
        self.event_repo = EventRepository(db)
        self.session_repo = SessionRepository(db)

    def ingest_events(
        self,
        session_id: int,
        events: List[dict]
    ) -> List[Event]:
        session = self.session_repo.get_by_id(session_id)

        if not session:
            raise ValueError("Session not found.")

        if session.status != SessionStatus.ACTIVE:
            return []

        if not events:
            return []

        db_events = self.event_repo.create_batch(
            session_id=session_id,
            events=events
        )

        self.session_repo.update_activity(session)

        return db_events

    def get_recent_events(
        self,
        session_id: int,
        limit: int = 20
    ) -> List[Event]:
        session = self.session_repo.get_by_id(session_id)

        if not session:
            raise ValueError("Session not found.")

        return self.event_repo.get_last_n_by_session(
            session_id=session_id,
            limit=limit
        )