from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field, ConfigDict


class UserBase(BaseModel):
    """
    Base user fields.
    """

    username: str = Field(
        ...,
        min_length=3,
        max_length=100
    )


class UserCreate(UserBase):
    """
    Payload for creating a user.
    """

    pass


class UserUpdate(BaseModel):
    """
    Payload for updating a user.
    """

    username: Optional[str] = Field(
        default=None,
        min_length=3,
        max_length=100
    )

    is_active: Optional[bool] = None


class UserResponse(UserBase):
    """
    User data returned to client.
    """

    id: int
    is_active: bool
    created_at: datetime
    updated_at: datetime

    model_config = ConfigDict(from_attributes=True)