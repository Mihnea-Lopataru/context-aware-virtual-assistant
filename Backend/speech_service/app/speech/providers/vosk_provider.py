import os
import io
import json
import wave
from typing import Optional

from vosk import Model, KaldiRecognizer

from app.speech.utils.audio_utils import convert_to_wav
from app.speech.interfaces.stt_interface import STTProvider

MODEL_PATH = os.path.join("data", "models", "vosk-model-small-en-us-0.15")

_model: Optional[Model] = Model(MODEL_PATH)

class VoskSTTProvider(STTProvider):
    """
    Vosk-based implementation of the STTProvider interface.

    This provider performs offline speech recognition using the Vosk model.
    """

    def transcribe(self, audio_bytes: bytes) -> str:
        """
        Transcribes audio input into text using Vosk.

        Args:
            audio_bytes (bytes): Raw audio file content

        Returns:
            str: Transcribed text

        Raises:
            ValueError: If input audio is invalid
            RuntimeError: If transcription fails
        """

        if not audio_bytes:
            raise ValueError("Empty audio input received.")

        try:
            wav_bytes = convert_to_wav(audio_bytes)

            with wave.open(io.BytesIO(wav_bytes), "rb") as wf:

                sample_rate = wf.getframerate()
                channels = wf.getnchannels()

                if channels != 1:
                    raise ValueError("Audio must be mono after conversion.")

                recognizer = KaldiRecognizer(_model, sample_rate)

                transcription_chunks = []

                while True:
                    data = wf.readframes(4000)

                    if len(data) == 0:
                        break

                    if recognizer.AcceptWaveform(data):
                        result = json.loads(recognizer.Result())
                        text_chunk = result.get("text", "").strip()

                        if text_chunk:
                            transcription_chunks.append(text_chunk)

                final_result = json.loads(recognizer.FinalResult())
                final_text = final_result.get("text", "").strip()

                if final_text:
                    transcription_chunks.append(final_text)

                return " ".join(transcription_chunks).strip()

        except ValueError:
            raise

        except Exception as e:
            raise RuntimeError(f"Vosk transcription failed: {str(e)}")