import os
from dataclasses import dataclass

from dotenv import load_dotenv


load_dotenv()


def _env(name: str, default: str) -> str:
    return os.getenv(name, default)


def _env_int(name: str, default: int) -> int:
    value = os.getenv(name)
    if value is None:
        return default

    try:
        return int(value)
    except ValueError:
        return default


@dataclass(frozen=True)
class Settings:
    DB_USER: str = _env("DB_USER", "admin")
    DB_PASSWORD: str = _env("DB_PASSWORD", "admin")
    DB_HOST: str = _env("DB_HOST", "localhost")
    DB_PORT: str = _env("DB_PORT", "5433")
    DB_NAME: str = _env("DB_NAME", "ai_assistant")

    OLLAMA_BASE_URL: str = _env("OLLAMA_BASE_URL", "http://localhost:11434")
    OLLAMA_MODEL: str = _env("OLLAMA_MODEL", "llama3.1:8b")
    OLLAMA_NUM_CTX: int = _env_int("OLLAMA_NUM_CTX", 2048)
    OLLAMA_NUM_PREDICT: int = _env_int("OLLAMA_NUM_PREDICT", 160)

    OPENAI_API_KEY_PATH: str = _env("OPENAI_API_KEY_PATH", "")
    OPENAI_BASE_URL: str = _env("OPENAI_BASE_URL", "https://api.openai.com/v1")
    OPENAI_MODEL: str = _env("OPENAI_MODEL", "gpt-4.1-mini")

    QDRANT_HOST: str = _env("QDRANT_HOST", "localhost")
    QDRANT_PORT: int = _env_int("QDRANT_PORT", 6333)
    QDRANT_COLLECTION_NAME: str = _env("QDRANT_COLLECTION_NAME", "chat_memory")

    EMBEDDING_PROVIDER: str = _env("EMBEDDING_PROVIDER", "openai")
    OPENAI_EMBEDDING_MODEL: str = _env(
        "OPENAI_EMBEDDING_MODEL",
        "text-embedding-3-small"
    )


settings = Settings()


def build_database_url() -> str:
    return (
        "postgresql+psycopg2://"
        f"{settings.DB_USER}:{settings.DB_PASSWORD}@"
        f"{settings.DB_HOST}:{settings.DB_PORT}/{settings.DB_NAME}"
    )


def load_secret_file(path: str, secret_name: str) -> str:
    if not path:
        raise ValueError(f"{secret_name} path is not set")

    if not os.path.exists(path):
        raise ValueError(f"{secret_name} file not found at {path}")

    with open(path, "r", encoding="utf-8") as f:
        value = f.read().strip()

    if not value:
        raise ValueError(f"{secret_name} file is empty")

    return value


def load_openai_api_key() -> str:
    return load_secret_file(settings.OPENAI_API_KEY_PATH, "OpenAI API key")
