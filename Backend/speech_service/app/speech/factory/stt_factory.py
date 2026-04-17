from app.speech.interfaces.stt_interface import STTProvider
from app.speech.providers.vosk_provider import VoskSTTProvider
from app.speech.providers.google_provider import GoogleSTTProvider

class STTFactory:
    @staticmethod
    def get_provider(provider_name: str = "vosk") -> STTProvider:
        provider_name = provider_name.lower()

        if provider_name == "vosk":
            return VoskSTTProvider()
        
        if provider_name == "google":
            return GoogleSTTProvider()

        raise ValueError(f"Unsupported STT provider: {provider_name}")