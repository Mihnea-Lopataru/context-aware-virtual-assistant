import os
import io
from pydub import AudioSegment

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))

FFMPEG_PATH = os.path.join(BASE_DIR, "data", "ffmpeg", "ffmpeg.exe")
FFPROBE_PATH = os.path.join(BASE_DIR, "data", "ffmpeg", "ffprobe.exe")

if os.name == "nt":
    if os.path.exists(FFMPEG_PATH):
        AudioSegment.converter = FFMPEG_PATH

    if os.path.exists(FFPROBE_PATH):
        AudioSegment.ffprobe = FFPROBE_PATH

else:
    # Linux / Docker → use system ffmpeg
    pass


def convert_to_wav(audio_bytes: bytes) -> bytes:
    """
    Converts an input audio file to a normalized WAV format compatible with the Vosk STT engine.

    The function:
    1. Loads the input audio from raw bytes
    2. Resamples to 16kHz
    3. Converts to mono
    4. Exports as WAV PCM

    Args:
        audio_bytes (bytes): Raw audio content

    Returns:
        bytes: WAV PCM (16kHz mono)

    Raises:
        ValueError: If input is empty
        RuntimeError: If conversion fails
    """

    if not audio_bytes:
        raise ValueError("Empty audio input received.")

    try:
        audio = AudioSegment.from_file(io.BytesIO(audio_bytes))

        audio = audio.set_frame_rate(16000).set_channels(1)

        wav_io = io.BytesIO()
        audio.export(wav_io, format="wav")

        return wav_io.getvalue()

    except Exception as e:
        raise RuntimeError(f"Audio conversion failed: {str(e)}")