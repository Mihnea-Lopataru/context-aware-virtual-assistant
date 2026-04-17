import os
import io
import wave
from typing import Optional

from piper import PiperVoice, SynthesisConfig

from app.speech.interfaces.tts_interface import TTSProvider


MODEL_PATH = os.path.join("data", "models", "piper", "en_US-joe-medium.onnx")

_voice: Optional[PiperVoice] = None

class PiperTTSProvider(TTSProvider):
    def __init__(self):
        global _voice

        if _voice is None:
            if not os.path.exists(MODEL_PATH):
                raise FileNotFoundError(
                    f"Piper model not found at path: {MODEL_PATH}"
                )

            try:
                _voice = PiperVoice.load(MODEL_PATH)
            except Exception as e:
                raise RuntimeError(
                    f"Failed to load Piper model: {str(e)}"
                )

        self.voice = _voice

    def synthesize(self, text: str) -> bytes:
        if not text or not text.strip():
            raise ValueError("Empty text input received.")

        try:
            audio_buffer = io.BytesIO()

            config = SynthesisConfig(
                length_scale=1.1,
                noise_scale=0.7,
                noise_w_scale=0.7
            )

            with wave.open(audio_buffer, "wb") as wav_file:
                self.voice.synthesize_wav(
                    text,
                    wav_file,
                    syn_config=config
                )

            return audio_buffer.getvalue()

        except ValueError:
            raise

        except Exception as e:
            raise RuntimeError(
                f"Piper TTS synthesis failed: {str(e)}"
            )