from typing import Any, Dict, List, Optional

from pydantic import BaseModel, ConfigDict, Field


class EvaluationRunRequest(BaseModel):
    evaluation_run_id: Optional[str] = None
    evaluation_id: Optional[str] = None
    test_case_id: str
    description: Optional[str] = None

    user_id: int = 9001
    session_id: int = 9001

    provider: str = "ollama"
    context_mode: str = "full"
    context_enabled: bool = True
    memory_enabled: bool = False

    user_query: str = Field(..., min_length=1)
    knowledge: Dict[str, Any] = Field(default_factory=dict)

    model_config = ConfigDict(extra="allow")


class EvaluationMemorySeedItem(BaseModel):
    memory_id: str
    role: str = "assistant"
    content: str = Field(..., min_length=1)
    metadata: Dict[str, Any] = Field(default_factory=dict)


class EvaluationMemorySeedRequest(BaseModel):
    user_id: int = 9001
    session_id: int = 9001
    reset_before_seed: bool = False
    memories: List[EvaluationMemorySeedItem]


class EvaluationMemoryResetRequest(BaseModel):
    user_id: int = 9001
    session_id: int = 9001


class EvaluationMemoryResponse(BaseModel):
    user_id: int
    session_id: int
    seeded_memory_count: int = 0
    reset: bool = False
