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


# =========================
# START SESSION
# =========================
@router.post(
    "/start",
    response_model=SessionResponse,
    status_code=status.HTTP_201_CREATED,
    summary="Start a new session"
)
def start_session(
    data: SessionCreate,
    db: Session = Depends(get_db)
):
    """
    Starts a new session for a user.

    Rules:
    - Only one active session per user
    - Existing active session is closed automatically
    """

    service = SessionService(db)

    return service.start_session(
        user_id=data.user_id,
        current_scene=data.current_scene,
        current_objective=data.current_objective,
        context_data=data.context_data,
        behavior_summary=data.behavior_summary
    )


# =========================
# GET ACTIVE SESSION
# =========================
@router.get(
    "/active/{user_id}",
    response_model=SessionResponse,
    summary="Get active session for user"
)
def get_active_session(
    user_id: int,
    db: Session = Depends(get_db)
):
    """
    Retrieves active session.
    Applies timeout automatically.
    """

    service = SessionService(db)

    session = service.get_active_session(user_id)

    if not session:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="No active session found."
        )

    return session


# =========================
# UPDATE SESSION (HEARTBEAT)
# =========================
@router.patch(
    "/{session_id}",
    response_model=SessionResponse,
    summary="Update session (heartbeat)"
)
def update_session(
    session_id: int,
    data: SessionUpdate,
    db: Session = Depends(get_db)
):
    """
    Updates session data and refreshes activity.

    Typically called frequently by Unity.
    """

    service = SessionService(db)

    session = service.update_session(
        session_id,
        current_scene=data.current_scene,
        current_objective=data.current_objective,
        context_data=data.context_data,
        behavior_summary=data.behavior_summary
    )

    if not session:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Session not found."
        )

    return session


# =========================
# END SESSION
# =========================
@router.post(
    "/{session_id}/end",
    response_model=SessionResponse,
    summary="End session"
)
def end_session(
    session_id: int,
    db: Session = Depends(get_db)
):
    """
    Manually ends a session.
    """

    service = SessionService(db)

    session = service.end_session(session_id)

    if not session:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Session not found."
        )

    return session