from typing import List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.database import get_db
from app.models.schemas.user_schema import (
    UserCreate,
    UserUpdate,
    UserResponse
)
from app.services.user_service import UserService


router = APIRouter(
    prefix="/users",
    tags=["Users"]
)


# =========================
# CREATE
# =========================
@router.post(
    "/",
    response_model=UserResponse,
    status_code=status.HTTP_201_CREATED,
    summary="Create a new user"
)
def create_user(
    user_data: UserCreate,
    db: Session = Depends(get_db)
):
    """
    Creates a new user.
    """

    service = UserService(db)

    try:
        return service.create_user(user_data.username)
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )


# =========================
# READ ALL
# =========================
@router.get(
    "/",
    response_model=List[UserResponse],
    summary="Get all users"
)
def get_users(
    db: Session = Depends(get_db)
):
    """
    Retrieves all users.
    """

    service = UserService(db)
    return service.get_all_users()


# =========================
# READ ONE
# =========================
@router.get(
    "/{user_id}",
    response_model=UserResponse,
    summary="Get user by ID"
)
def get_user(
    user_id: int,
    db: Session = Depends(get_db)
):
    """
    Retrieves a single user by ID.
    """

    service = UserService(db)

    user = service.get_user(user_id)

    if not user:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="User not found."
        )

    return user


# =========================
# UPDATE
# =========================
@router.put(
    "/{user_id}",
    response_model=UserResponse,
    summary="Update user"
)
def update_user(
    user_id: int,
    user_data: UserUpdate,
    db: Session = Depends(get_db)
):
    """
    Updates a user (partial update supported).
    """

    service = UserService(db)

    try:
        user = service.update_user(
            user_id,
            username=user_data.username,
            is_active=user_data.is_active
        )
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )

    if not user:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="User not found."
        )

    return user


# =========================
# DELETE
# =========================
@router.delete(
    "/{user_id}",
    status_code=status.HTTP_204_NO_CONTENT,
    summary="Delete user"
)
def delete_user(
    user_id: int,
    db: Session = Depends(get_db)
):
    """
    Deletes a user.
    """

    service = UserService(db)

    success = service.delete_user(user_id)

    if not success:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="User not found."
        )