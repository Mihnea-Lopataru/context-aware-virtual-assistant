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


def get_user_service(db: Session = Depends(get_db)) -> UserService:
    return UserService(db)


@router.post(
    "",
    response_model=UserResponse,
    status_code=status.HTTP_201_CREATED,
    summary="Create a new user"
)
def create_user(
    user_data: UserCreate,
    service: UserService = Depends(get_user_service)
):
    try:
        return service.create_user(user_data.username)
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )


@router.get(
    "",
    response_model=List[UserResponse],
    summary="Get all users"
)
def get_users(
    service: UserService = Depends(get_user_service)
):
    return service.get_all_users()


@router.get(
    "/{user_id}",
    response_model=UserResponse,
    summary="Get user by ID"
)
def get_user(
    user_id: int,
    service: UserService = Depends(get_user_service)
):
    user = service.get_user(user_id)

    if not user:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="User not found."
        )

    return user


@router.put(
    "/{user_id}",
    response_model=UserResponse,
    summary="Update user"
)
def update_user(
    user_id: int,
    user_data: UserUpdate,
    service: UserService = Depends(get_user_service)
):
    try:
        update_data = user_data.model_dump(exclude_unset=True)

        user = service.update_user(user_id, **update_data)
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


@router.delete(
    "/{user_id}",
    status_code=status.HTTP_204_NO_CONTENT,
    summary="Delete user"
)
def delete_user(
    user_id: int,
    service: UserService = Depends(get_user_service)
):
    success = service.delete_user(user_id)

    if not success:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="User not found."
        )