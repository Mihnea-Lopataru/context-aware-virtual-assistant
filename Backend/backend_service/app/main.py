"""
Application entry point.

Initializes:
- FastAPI app
- Database tables
- API routes
"""

from fastapi import FastAPI

from app.core.database import engine
from app.models.db.base import Base

from app.models import user

from app.routes import users


app = FastAPI(
    title="Context-Aware AI Backend",
    description=(
        "Backend service responsible for user management, "
        "session handling, adaptation logic, and LLM integration."
    ),
    version="1.0.0"
)


# =========================
# DATABASE INITIALIZATION
# =========================

Base.metadata.create_all(bind=engine)


# =========================
# ROUTES
# =========================

app.include_router(users.router)


# =========================
# HEALTH CHECK
# =========================
@app.get(
    "/health",
    summary="Health check",
    description="Verifies that the backend service is running correctly.",
    tags=["Health"]
)
def health_check():
    return {
        "status": "ok",
        "service": "backend-service"
    }