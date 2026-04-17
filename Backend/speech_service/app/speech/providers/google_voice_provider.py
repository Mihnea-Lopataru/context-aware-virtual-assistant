from typing import Optional

from google.cloud import texttospeech

from app.speech.interfaces.tts_interface import TTSProvider


class GoogleTTSProvider(TTSProvider):
    def __init__(self):
        try:
            self.client = texttospeech.TextToSpeechClient()
        except Exception as e:
            raise RuntimeError(f"Failed to initialize Google TTS client: {str(e)}")

        self.voice = texttospeech.VoiceSelectionParams(
            language_code="en-US",
            name="en-US-Neural2-C"
        )

        self.audio_config = texttospeech.AudioConfig(
            audio_encoding=texttospeech.AudioEncoding.LINEAR16
        )

    def synthesize(self, text: str) -> bytes:
        if not text or not text.strip():
            raise ValueError("Empty text input received.")

        try:
            synthesis_input = texttospeech.SynthesisInput(text=text)

            response = self.client.synthesize_speech(
                input=synthesis_input,
                voice=self.voice,
                audio_config=self.audio_config
            )

            if not response.audio_content:
                raise RuntimeError("Google TTS returned empty audio.")

            return response.audio_content

        except ValueError:
            raise

        except Exception as e:
            raise RuntimeError(f"Google TTS synthesis failed: {str(e)}")