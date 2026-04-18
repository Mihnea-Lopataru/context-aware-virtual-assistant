from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.core.database import get_db
from app.models.schemas.hint_schema import HintRequest, HintResponse
from app.services.hint_service import HintService


router = APIRouter(
    prefix="/hints",
    tags=["Hints"]
)


def get_hint_service(db: Session = Depends(get_db)) -> HintService:
    return HintService(db)


@router.post(
    "",
    response_model=HintResponse,
    status_code=status.HTTP_200_OK,
    summary="Generate contextual hint"
)
def generate_hint(data: HintRequest, service: HintService = Depends(get_hint_service)):

    try:
        return service.generate_hint(data)

    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(e)
        )

    except Exception as e:
        print("HINT ERROR:", str(e))
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=str(e)
        )