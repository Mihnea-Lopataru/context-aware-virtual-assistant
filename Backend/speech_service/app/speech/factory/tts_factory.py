from app.speech.interfaces.tts_interface import TTSProvider
from app.speech.providers.google_voice_provider import GoogleTTSProvider
from app.speech.providers.piper_voice_provider import PiperTTSProvider



class TTSFactory:
    @staticmethod
    def get_provider(provider_name: str = "piper") -> TTSProvider:
        provider_name = provider_name.lower()

        if provider_name == "piper":
            return PiperTTSProvider()

        if provider_name == "google":
            return GoogleTTSProvider();

        raise ValueError(f"Unsupported TTS provider: {provider_name}")