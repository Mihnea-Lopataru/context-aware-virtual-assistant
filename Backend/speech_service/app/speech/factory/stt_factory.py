from app.speech.interfaces.stt_interface import STTProvider
from app.speech.providers.vosk_provider import VoskSTTProvider
from app.speech.providers.google_provider import GoogleSTTProvider

class STTFactory:
    """
    Factory responsible for instantiating Speech-to-Text providers.

    This allows switching between different STT implementations
    (e.g., Vosk, Google Cloud) without modifying the API layer.
    """

    @staticmethod
    def get_provider(provider_name: str = "vosk") -> STTProvider:
        """
        Returns an STT provider instance based on the given name.

        Args:
            provider_name (str): Name of the STT provider

        Returns:
            STTProvider: Concrete implementation of STTProvider

        Raises:
            ValueError: If provider is not supported
        """

        provider_name = provider_name.lower()

        if provider_name == "vosk":
            return VoskSTTProvider()
        
        if provider_name == "google":
            return GoogleSTTProvider()

        raise ValueError(f"Unsupported STT provider: {provider_name}")