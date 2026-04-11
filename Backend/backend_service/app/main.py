from fastapi import FastAPI

from app.core.database import engine
from app.models.db.base import Base

# =========================
# IMPORT MODELS (IMPORTANT)
# =========================
from app.models import user
from app.models import session

# =========================
# IMPORT ROUTES
# =========================
from app.routes import users
from app.routes import sessions


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
app.include_router(sessions.router)


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