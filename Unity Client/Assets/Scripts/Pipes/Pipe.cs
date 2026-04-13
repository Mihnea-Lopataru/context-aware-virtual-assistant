using UnityEngine;

public class Pipe : MonoBehaviour, IInteractable
{
    [Header("Identification")]
    [SerializeField] private string id;

    [Header("Pipe Properties")]
    [SerializeField] private PipeColor color;
    [SerializeField] private PipeType type;

    [Header("State")]
    [SerializeField] private bool isPlacedCorrectly = false;

    private Transform initialParent;
    private Vector3 initialWorldPosition;
    private Quaternion initialWorldRotation;
    private bool initialTransformCached = false;

    private PipeSlot currentSlot;
    private bool isHeld = false;

    public string Id => id;
    public PipeColor Color => color;
    public PipeType Type => type;
    public bool IsPlacedCorrectly => isPlacedCorrectly;

    public Transform InitialParent => initialParent;
    public Vector3 InitialWorldPosition => initialWorldPosition;
    public Quaternion InitialWorldRotation => initialWorldRotation;

    public PipeSlot CurrentSlot => currentSlot;
    public bool IsHeld => isHeld;

    private void Awake()
    {
        CacheInitialTransform();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = gameObject.name;
        }
    }
#endif

    private void CacheInitialTransform()
    {
        if (initialTransformCached)
            return;

        initialParent = transform.parent;
        initialWorldPosition = transform.position;
        initialWorldRotation = transform.rotation;
        initialTransformCached = true;
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null)
            return;

        if (player.HeldPipe == null)
        {
            player.PickPipe(this);
        }
    }

    public void SetPlacedCorrectly(bool value)
    {
        isPlacedCorrectly = value;
    }

    public void SetHeldState(bool value)
    {
        isHeld = value;
    }

    public void SetCurrentSlot(PipeSlot slot)
    {
        currentSlot = slot;
    }

    public void ClearCurrentSlot()
    {
        currentSlot = null;
    }

    public void DetachFromCurrentSlot()
    {
        if (currentSlot != null)
        {
            currentSlot.ClearPipeReference(this);
            currentSlot = null;
        }
    }

    public override string ToString()
    {
        return $"Pipe[{id}] - {color} {type}";
    }
}