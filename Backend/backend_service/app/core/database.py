import os
from typing import Generator

from dotenv import load_dotenv
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, Session

# =========================
# LOAD ENVIRONMENT VARIABLES
# =========================

load_dotenv()

# =========================
# DATABASE URL BUILDER
# =========================

def _build_database_url() -> str:
    """
    Constructs the SQLAlchemy database URL from environment variables.

    Expected environment variables:
        DB_USER
        DB_PASSWORD
        DB_HOST
        DB_PORT
        DB_NAME

    Returns:
        str: A SQLAlchemy-compatible PostgreSQL connection string
    """

    db_user = os.getenv("DB_USER", "admin")
    db_password = os.getenv("DB_PASSWORD", "admin")
    db_host = os.getenv("DB_HOST", "localhost")
    db_port = os.getenv("DB_PORT", "5433")
    db_name = os.getenv("DB_NAME", "ai_assistant")

    return (
        f"postgresql+psycopg2://"
        f"{db_user}:{db_password}@{db_host}:{db_port}/{db_name}"
    )


DATABASE_URL = _build_database_url()

# =========================
# ENGINE CONFIGURATION
# =========================

engine = create_engine(
    DATABASE_URL,

    pool_pre_ping=True,

    future=True
)

# =========================
# SESSION FACTORY
# =========================

SessionLocal = sessionmaker(
    bind=engine,
    autocommit=False,
    autoflush=False,
    future=True
)

# =========================
# FASTAPI DEPENDENCY
# =========================

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