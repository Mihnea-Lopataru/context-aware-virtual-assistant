from typing import Optional, List

from sqlalchemy.orm import Session

from app.models.user import User
from app.repositories.user_repository import UserRepository


class UserService:
    def __init__(self, db: Session):
        self.repo = UserRepository(db)

    def create_user(self, username: str) -> User:
        existing_user = self.repo.get_by_username(username)

        if existing_user:
            raise ValueError("Username already exists.")

        return self.repo.create(username=username)

    def get_user(self, user_id: int) -> Optional[User]:
        return self.repo.get_by_id(user_id)

    def get_all_users(self) -> List[User]:
        return self.repo.get_all()

    def update_user(self, user_id: int, **kwargs) -> Optional[User]:
        user = self.repo.get_by_id(user_id)

        if not user:
            return None

        if "username" in kwargs:
            existing = self.repo.get_by_username(kwargs["username"])
            if existing and existing.id != user_id:
                raise ValueError("Username already exists.")

        return self.repo.update(user, **kwargs)

    def delete_user(self, user_id: int) -> Optional[User]:
        user = self.repo.get_by_id(user_id)

        if not user:
            return None

        self.repo.delete(user)
        return user