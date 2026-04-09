from typing import List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.database import get_db
from app.models.schemas.user_schema import (
    UserCreate,
    UserUpdate,
    UserResponse
)
from app.repositories.user_repository import UserRepository


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

    Steps:
    1. Validate uniqueness
    2. Create user via repository
    """

    repo = UserRepository(db)

    existing_user = repo.get_by_username(user_data.username)

    if existing_user:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Username already exists."
        )

    return repo.create(username=user_data.username)


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

    repo = UserRepository(db)
    return repo.get_all()


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

    repo = UserRepository(db)

    user = repo.get_by_id(user_id)

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

    repo = UserRepository(db)

    user = repo.get_by_id(user_id)

    if not user:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="User not found."
        )

    if user_data.username:
        existing_user = repo.get_by_username(user_data.username)

        if existing_user and existing_user.id != user_id:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Username already exists."
            )

    updated_user = repo.update(
        user,
        username=user_data.username,
        is_active=user_data.is_active
    )

    return updated_user


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

    repo = UserRepository(db)

    user = repo.get_by_id(user_id)

    if not user:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="User not found."
        )

    repo.delete(user)