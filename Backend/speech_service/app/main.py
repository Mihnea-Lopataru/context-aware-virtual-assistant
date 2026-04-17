from fastapi import FastAPI
from app.routes import speech_to_text
from app.routes import text_to_speech

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
app.include_router(text_to_speech.router, prefix="/text-to-speech")