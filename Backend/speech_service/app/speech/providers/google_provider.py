from typing import Optional

from google.cloud import speech

from app.speech.interfaces.stt_interface import STTProvider
from app.speech.utils.audio_utils import convert_to_wav


class GoogleSTTProvider(STTProvider):
    def __init__(self):
        try:
            self.client = speech.SpeechClient()
        except Exception as e:
            raise RuntimeError(f"Failed to initialize Google STT client: {str(e)}")

        self.config = speech.RecognitionConfig(
            encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
            sample_rate_hertz=16000,
            language_code="en-US",
            model="default",
            enable_automatic_punctuation=True
        )

    def transcribe(self, audio_bytes: bytes) -> str:
        if not audio_bytes:
            raise ValueError("Empty audio input received.")

        try:
            wav_bytes = convert_to_wav(audio_bytes)

            audio = speech.RecognitionAudio(content=wav_bytes)

            response = self.client.recognize(
                config=self.config,
                audio=audio
            )

            transcription_chunks = []

            for result in response.results:
                if result.alternatives:
                    transcription_chunks.append(
                        result.alternatives[0].transcript.strip()
                    )

            return " ".join(transcription_chunks).strip()

        except ValueError:
            raise

        except Exception as e:
            raise RuntimeError(f"Google STT transcription failed: {str(e)}")