from abc import ABC, abstractmethod


class TTSProvider(ABC):
    @abstractmethod
    def synthesize(self, text: str) -> bytes:
        raise NotImplementedError