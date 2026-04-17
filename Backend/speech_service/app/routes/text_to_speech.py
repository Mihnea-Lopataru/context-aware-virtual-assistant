from fastapi import APIRouter, HTTPException, status, Query
from fastapi.responses import StreamingResponse
import io

from app.speech.factory.tts_factory import TTSFactory

router = APIRouter(
    tags=["Text-to-Speech"]
)


@router.post(
    "",
    summary="Convert text to speech",
    description=(
        "Receives text input and returns synthesized speech audio using the selected TTS provider."
    ),
    response_description="Audio (WAV) result"
)
async def text_to_speech(
    text: str = Query(..., description="Text to synthesize into speech"),
    provider: str = Query("piper", description="TTS provider to use (e.g., piper, google)")
):
    if not text or not text.strip():
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Text input is empty."
        )

    try:
        tts_provider = TTSFactory.get_provider(provider)

        audio_bytes = tts_provider.synthesize(text)

        if not audio_bytes:
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail="Failed to generate audio."
            )

        return StreamingResponse(
            io.BytesIO(audio_bytes),
            media_type="audio/wav",
            headers={
                "Content-Disposition": "attachment; filename=tts.wav"
            }
        )

    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )

    except RuntimeError as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=str(e)
        )

    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Unexpected error during TTS processing: {str(e)}"
        )