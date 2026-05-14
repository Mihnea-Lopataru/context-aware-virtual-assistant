from typing import Generator

from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, Session

from app.core.config import build_database_url

DATABASE_URL = build_database_url()


engine = create_engine(
    DATABASE_URL,

    pool_pre_ping=True,

    future=True
)


SessionLocal = sessionmaker(
    bind=engine,
    autocommit=False,
    autoflush=False,
    future=True
)


def get_db() -> Generator[Session, None, None]:
    """
    Provides a database session for a single request lifecycle.

    This function is used as a FastAPI dependency.

    Workflow:
        1. Create a new database session
        2. Yield it to the request handler
        3. Ensure it is properly closed after request completion

    Yields:
        Session: SQLAlchemy database session
    """

    db = SessionLocal()

    try:
        yield db
    finally:
        db.close()
