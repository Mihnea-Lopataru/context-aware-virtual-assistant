from abc import ABC, abstractmethod


class STTProvider(ABC):
    """
    Abstract interface for Speech-to-Text providers.

    Any concrete STT provider (for example Vosk or Google Cloud Speech-to-Text)
    must implement this interface in order to be used by the speech service.
    """

    @abstractmethod
    def transcribe(self, audio_bytes: bytes) -> str:
        """
        Transcribes raw audio input into text.

        Args:
            audio_bytes (bytes): Raw audio file content.

        Returns:
            str: The transcribed text.

        Raises:
            ValueError: If the input audio is invalid.
            RuntimeError: If the transcription process fails.
        """
        raise NotImplementedError