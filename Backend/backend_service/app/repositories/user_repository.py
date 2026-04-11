from typing import List, Optional

from sqlalchemy.orm import Session

from app.models.user import User


class UserRepository:
    """
    Repository for User entity.
    """

    def __init__(self, db: Session):
        self.db = db

    # =========================
    # CREATE
    # =========================
    def create(self, username: str) -> User:
        """
        Creates a new user.

        Args:
            username (str): Unique username

        Returns:
            User: Created user instance
        """

        user = User(username=username)

        self.db.add(user)
        self.db.commit()
        self.db.refresh(user)

        return user

    # =========================
    # READ
    # =========================
    def get_by_id(self, user_id: int) -> Optional[User]:
        """
        Retrieves a user by ID.
        """

        return self.db.query(User).filter(User.id == user_id).first()

    def get_by_username(self, username: str) -> Optional[User]:
        """
        Retrieves a user by username.
        """

        return self.db.query(User).filter(User.username == username).first()

    def get_all(self) -> List[User]:
        """
        Retrieves all users.
        """

        return self.db.query(User).all()

    # =========================
    # UPDATE
    # =========================
    def update(self, user: User, **kwargs) -> User:
        """
        Updates user fields dynamically.

        Args:
            user (User): Existing user instance
            **kwargs: Fields to update

        Returns:
            User: Updated user
        """

        for field, value in kwargs.items():
            if hasattr(user, field) and value is not None:
                setattr(user, field, value)

        self.db.commit()
        self.db.refresh(user)

        return user

    # =========================
    # DELETE
    # =========================
    def delete(self, user: User) -> None:
        """
        Deletes a user from the database.
        Also triggers cascade delete for related sessions.
        """

        if user is None:
            return

        self.db.delete(user)
        self.db.commit()