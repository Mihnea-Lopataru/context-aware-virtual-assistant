from typing import List
from sqlalchemy.orm import Session
from datetime import datetime, timezone

from app.models.event import Event


class EventRepository:
    def __init__(self, db: Session):
        self.db = db

    def _convert_timestamp(self, ms: int | None):
        if not ms:
            return None
        return datetime.fromtimestamp(ms / 1000, tz=timezone.utc)

    def create_batch(
        self,
        session_id: int,
        events: List[dict]
    ) -> List[Event]:
        db_events: List[Event] = []

        for e in events:
            timestamp = self._convert_timestamp(e.get("timestamp"))

            db_event = Event(
                session_id=session_id,
                event_type=e.get("event_type"),
                player_state=e.get("player_state"),
                context_data=e.get("context"),
                timestamp=timestamp or datetime.now(timezone.utc)  # fallback safe
            )

            db_events.append(db_event)

        self.db.add_all(db_events)
        self.db.commit()

        for ev in db_events:
            self.db.refresh(ev)

        return db_events

    def get_last_n_by_session(
        self,
        session_id: int,
        limit: int = 20
    ) -> List[Event]:
        return (
            self.db.query(Event)
            .filter(Event.session_id == session_id)
            .order_by(Event.timestamp.desc())
            .limit(limit)
            .all()
        )