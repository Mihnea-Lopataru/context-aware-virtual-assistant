from fastapi import APIRouter, UploadFile, File, HTTPException, status, Query

from app.speech.factory.stt_factory import STTFactory

router = APIRouter(
    tags=["Speech-to-Text"]
)


@router.post(
    "/",
    summary="Convert audio to text",
    description=(
        "Receives an audio file and returns the transcribed text using the selected STT provider."
    ),
    response_description="Transcribed text result"
)
async def speech_to_text(
    file: UploadFile = File(...),
    provider: str = Query("vosk", description="STT provider to use (e.g., vosk, google)")
):
    """
    Endpoint for speech-to-text conversion with selectable provider.
    """

    if not file:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="No file provided."
        )

    if not file.content_type or not file.content_type.startswith("audio"):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Invalid file type: {file.content_type}. Expected audio file."
        )

    try:
        audio_bytes = await file.read()

        if not audio_bytes:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Uploaded file is empty."
            )

        stt_provider = STTFactory.get_provider(provider)

        text = stt_provider.transcribe(audio_bytes)

        return {
            "filename": file.filename,
            "content_type": file.content_type,
            "provider": provider,
            "transcription": text
        }

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
            detail=f"Unexpected error during STT processing: {str(e)}"
        )