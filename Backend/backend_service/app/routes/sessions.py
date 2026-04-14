from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.database import get_db
from app.models.schemas.session_schema import (
    SessionCreate,
    SessionUpdate,
    SessionResponse
)
from app.services.session_service import SessionService


router = APIRouter(
    prefix="/sessions",
    tags=["Sessions"]
)


def get_session_service(db: Session = Depends(get_db)) -> SessionService:
    return SessionService(db)


@router.post(
    "/start",
    response_model=SessionResponse,
    status_code=status.HTTP_201_CREATED,
    summary="Start a new session"
)
def start_session(
    data: SessionCreate,
    service: SessionService = Depends(get_session_service)
):
    return service.start_session(
        user_id=data.user_id,
        **data.model_dump(exclude_unset=True, exclude={"user_id"})
    )


@router.get(
    "/active/{user_id}",
    response_model=SessionResponse,
    summary="Get active session for user"
)
def get_active_session(
    user_id: int,
    service: SessionService = Depends(get_session_service)
):
    session = service.get_active_session(user_id)

    if not session:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="No active session found."
        )

    return session


@router.patch(
    "/{session_id}",
    response_model=SessionResponse,
    summary="Update session (heartbeat)"
)
def update_session(
    session_id: int,
    data: SessionUpdate,
    service: SessionService = Depends(get_session_service)
):
    update_data = data.model_dump(exclude_unset=True)

    session = service.update_session(
        session_id,
        **update_data
    )

    if not session:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Session not found."
        )

    return session


@router.post(
    "/{session_id}/end",
    response_model=SessionResponse,
    summary="End session"
)
def end_session(
    session_id: int,
    service: SessionService = Depends(get_session_service)
):
    session = service.end_session(session_id)

    if not session:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Session not found."
        )

    return session