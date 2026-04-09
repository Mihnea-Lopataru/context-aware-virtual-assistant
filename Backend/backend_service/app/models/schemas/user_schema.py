from datetime import datetime
from pydantic import BaseModel, Field


# =========================
# BASE SCHEMA
# =========================
class UserBase(BaseModel):
    """
    Shared user fields.
    """

    username: str = Field(
        ...,
        min_length=3,
        max_length=100,
        description="Unique username of the user"
    )


# =========================
# CREATE SCHEMA
# =========================
class UserCreate(UserBase):
    """
    Schema used when creating a new user.
    """

    pass


# =========================
# UPDATE SCHEMA
# =========================
class UserUpdate(BaseModel):
    """
    Schema used for updating user data.
    Allows partial updates.
    """

    username: str | None = Field(
        default=None,
        min_length=3,
        max_length=100
    )

    is_active: bool | None = None


# =========================
# RESPONSE SCHEMA
# =========================
class UserResponse(UserBase):
    """
    Schema returned to the client.
    """

    id: int
    is_active: bool
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True  # required for SQLAlchemy ORM