from sqlalchemy.orm import DeclarativeBase


class Base(DeclarativeBase):
    """
    Base class for all SQLAlchemy ORM models.

    This class serves as the foundation for all database entities.
    It provides:
    - Table metadata tracking
    - ORM mapping capabilities
    - Centralized schema management
    """

    pass