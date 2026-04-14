from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.database import get_db
from app.models.schemas.event_schema import EventBatch
from app.services.event_service import EventService


router = APIRouter(
    prefix="/events",
    tags=["Events"]
)


def get_event_service(db: Session = Depends(get_db)) -> EventService:
    return EventService(db)


@router.post(
    "",
    status_code=status.HTTP_201_CREATED,
    summary="Ingest player events"
)
def ingest_events(
    data: EventBatch,
    service: EventService = Depends(get_event_service)
):
    try:
        service.ingest_events(
            session_id=data.session_id,
            events=[event.model_dump() for event in data.events]
        )
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(e)
        )

    return {"status": "ok"}