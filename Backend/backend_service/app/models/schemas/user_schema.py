from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field, ConfigDict


class UserBase(BaseModel):

    username: str = Field(
        ...,
        min_length=3,
        max_length=100
    )


class UserCreate(UserBase):

    pass


class UserUpdate(BaseModel):

    username: Optional[str] = Field(
        default=None,
        min_length=3,
        max_length=100
    )

    is_active: Optional[bool] = None


class UserResponse(UserBase):

    id: int
    is_active: bool
    created_at: datetime
    updated_at: datetime

    model_config = ConfigDict(from_attributes=True)