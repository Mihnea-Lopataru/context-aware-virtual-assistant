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

        currentPipe = pipe;

        pipe.SetCurrentSlot(this);
        pipe.SetHeldState(false);

        pipe.transform.SetParent(placementPoint, false);
        pipe.transform.localPosition = Vector3.zero;
        pipe.transform.localRotation = Quaternion.identity;

        gameObject.layer = nonInteractableLayer;

        bool isCorrect = pipe.Color == requiredColor && pipe.Type == requiredType;
        pipe.SetPlacedCorrectly(isCorrect);

        Debug.Log($"Placed {pipe} -> {(isCorrect ? "CORRECT" : "WRONG")}");
    }

    public void ClearPipeReference(Pipe pipe)
    {
        if (currentPipe == pipe)
        {
            currentPipe = null;

            gameObject.layer = originalLayer;
        }
    }
}