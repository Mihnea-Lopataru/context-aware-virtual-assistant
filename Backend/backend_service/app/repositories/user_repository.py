from typing import List, Optional

from sqlalchemy.orm import Session

from app.models.user import User


class UserRepository:
    def __init__(self, db: Session):
        self.db = db

    def create(self, username: str) -> User:
        user = User(username=username)

        self.db.add(user)
        self.db.commit()
        self.db.refresh(user)

        return user

    def get_by_id(self, user_id: int) -> Optional[User]:
        return (
            self.db.query(User)
            .filter(User.id == user_id)
            .first()
        )

    def get_by_username(self, username: str) -> Optional[User]:
        return (
            self.db.query(User)
            .filter(User.username == username)
            .first()
        )

    def get_all(self) -> List[User]:
        return self.db.query(User).all()

    def update(self, user: User, **kwargs) -> User:
        for field, value in kwargs.items():
            if hasattr(user, field):
                setattr(user, field, value)

        self.db.commit()
        self.db.refresh(user)

        return user

    def delete(self, user: User) -> None:
        self.db.delete(user)
        self.db.commit()