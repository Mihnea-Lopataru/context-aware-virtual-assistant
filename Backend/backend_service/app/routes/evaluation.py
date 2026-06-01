import logging
from typing import Any, Dict

from fastapi import APIRouter, HTTPException, status

from app.models.schemas.evaluation_schema import (
    EvaluationMemoryResetRequest,
    EvaluationMemoryResponse,
    EvaluationMemorySeedRequest,
    EvaluationRunRequest
)
from app.services.evaluation_service import EvaluationService


router = APIRouter(
    prefix="/evaluation",
    tags=["Evaluation"]
)

logger = logging.getLogger(__name__)


def get_evaluation_service() -> EvaluationService:
    return EvaluationService()


@router.post(
    "/run",
    status_code=status.HTTP_200_OK,
    summary="Run one deterministic evaluation case"
)
def run_evaluation_case(data: EvaluationRunRequest) -> Dict[str, Any]:
    try:
        return get_evaluation_service().run(data)
    except Exception as e:
        logger.exception("Evaluation run failed")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=str(e)
        )


@router.post(
    "/memory/seed",
    response_model=EvaluationMemoryResponse,
    status_code=status.HTTP_200_OK,
    summary="Seed deterministic evaluation memories"
)
def seed_evaluation_memory(
    data: EvaluationMemorySeedRequest
) -> EvaluationMemoryResponse:
    try:
        return get_evaluation_service().seed_memory(data)
    except Exception as e:
        logger.exception("Evaluation memory seed failed")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=str(e)
        )


@router.post(
    "/memory/reset",
    response_model=EvaluationMemoryResponse,
    status_code=status.HTTP_200_OK,
    summary="Reset deterministic evaluation memories"
)
def reset_evaluation_memory(
    data: EvaluationMemoryResetRequest
) -> EvaluationMemoryResponse:
    try:
        return get_evaluation_service().reset_memory(data)
    except Exception as e:
        logger.exception("Evaluation memory reset failed")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=str(e)
        )
