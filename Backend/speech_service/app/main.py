from fastapi import FastAPI
from app.routes import speech_to_text

app = FastAPI(title="Speech Service")


@app.get(
    "/health",
    tags=["System"],
    summary="Health check"
)
def health_check():
    return {
        "status": "ok",
        "service": "speech-service"
    }


app.include_router(speech_to_text.router, prefix="/speech-to-text")