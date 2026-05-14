from abc import ABC, abstractmethod
from typing import List


class BaseEmbeddingProvider(ABC):
    vector_size: int

    @abstractmethod
    def embed_text(self, text: str) -> List[float]:
        raise NotImplementedError
