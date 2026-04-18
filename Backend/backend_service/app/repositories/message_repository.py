from sqlalchemy.orm import Session
from typing import List

from app.models.message import Message
from app.models.schemas.message_schema import MessageCreate


class MessageRepository:

    def create(self, db: Session, message: MessageCreate) -> Message:
        db_message = Message(**message.model_dump())

        db.add(db_message)
        db.flush()
        db.refresh(db_message)

        return db_message

    def bulk_create(self, db: Session, messages: List[MessageCreate]) -> List[Message]:
        db_messages = [Message(**m.model_dump()) for m in messages]

        db.add_all(db_messages)
        db.flush()

        for m in db_messages:
            db.refresh(m)

        return db_messages

    def get_last_messages(
        self,
        db: Session,
        session_id: int,
        limit: int = 5
    ) -> List[Message]:
        messages = (
            db.query(Message)
            .filter(Message.session_id == session_id)
            .order_by(Message.created_at.desc())
            .limit(limit)
            .all()
        )

        return list(reversed(messages))