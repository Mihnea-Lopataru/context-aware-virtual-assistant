using UnityEngine;

public class PipeSlot : MonoBehaviour, IInteractable
{
    [Header("Required Pipe")]
    [SerializeField] private PipeColor requiredColor;
    [SerializeField] private PipeType requiredType;

    [Header("Placement")]
    [SerializeField] private Transform placementPoint;

    private Pipe currentPipe;

    private int originalLayer;
    private int nonInteractableLayer;

    public bool HasPipe => currentPipe != null;
    public Pipe CurrentPipe => currentPipe;

    private void Awake()
    {
        originalLayer = gameObject.layer;
        nonInteractableLayer = LayerMask.NameToLayer("NonInteractable");
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null || player.HeldPipe == null)
            return;

        if (currentPipe != null)
            return;

        player.PlacePipe(this);
    }

    public void PlacePipe(Pipe pipe)
    {
        if (pipe == null || placementPoint == null)
            return;

        ValidateAgainstKnowledge(pipe);

        currentPipe = pipe;

        pipe.SetCurrentSlot(this);
        pipe.SetHeldState(false);

        pipe.transform.SetParent(placementPoint, false);
        pipe.transform.localPosition = Vector3.zero;
        pipe.transform.localRotation = Quaternion.identity;

        gameObject.layer = nonInteractableLayer;

        bool isCorrect = IsCorrectPipe(pipe);
        pipe.SetPlacedCorrectly(isCorrect);

        Debug.Log(
            $"[PipeSlot] Placed {pipe.Type} ({pipe.Color}) in slot ({requiredType}, {requiredColor}) -> {(isCorrect ? "CORRECT" : "WRONG")}"
        );

        LogPlacementEvent(pipe, isCorrect);
    }

    public void ClearPipeReference(Pipe pipe)
    {
        if (currentPipe == pipe)
        {
            currentPipe = null;
            gameObject.layer = originalLayer;
        }
    }

    private bool IsCorrectPipe(Pipe pipe)
    {
        if (pipe == null)
            return false;

        return pipe.Color == requiredColor && pipe.Type == requiredType;
    }

    private void ValidateAgainstKnowledge(Pipe pipe)
    {
        var knowledge = PuzzleKnowledgeLoader.Instance;

        if (knowledge == null)
        {
            Debug.LogWarning("[Knowledge] PuzzleKnowledge not loaded.");
            return;
        }

        var pipeDef = knowledge.GetPipeType(pipe.Type.ToString());

        if (pipeDef == null)
        {
            Debug.LogError($"[Knowledge] Pipe type {pipe.Type} not found in knowledge!");
            return;
        }

        if (!pipeDef.enabled)
        {
            Debug.LogWarning($"[Knowledge] Pipe type {pipe.Type} is disabled!");
        }
    }

    private void LogPlacementEvent(Pipe pipe, bool isCorrect)
    {
        if (ContextLogger.Instance == null)
            return;

        var context = new EventContextDTO
        {
            ObjectId = pipe.name,
            ObjectType = "pipe",

            PipeColor = pipe.Color.ToString(),
            PipeType = pipe.Type.ToString(),

            RequiredColor = requiredColor.ToString(),
            RequiredType = requiredType.ToString(),

            Success = isCorrect
        };

        ContextLogger.Instance.LogEvent("PLACE_PIPE", context);
    }
}