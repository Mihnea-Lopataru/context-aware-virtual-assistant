using UnityEngine;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;

    [Header("Interaction Settings")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayer;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.25f;

    private IInteractable currentTarget;
    private Pipe heldPipe;
    private bool isAnimating = false;

    public Pipe HeldPipe => heldPipe;

    private void Update()
    {
        if (isAnimating)
            return;

        DetectTarget();

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleInteraction();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            HandleDrop();
        }
    }

    private void DetectTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            currentTarget = hit.collider.GetComponentInParent<IInteractable>();
        }
        else
        {
            currentTarget = null;
        }
    }

    private void HandleInteraction()
    {
        if (heldPipe == null)
        {
            currentTarget?.Interact(this);
            return;
        }

        if (currentTarget != null)
        {
            currentTarget.Interact(this);
        }
    }

    private void HandleDrop()
    {
        if (heldPipe == null)
            return;

        ReturnHeldPipe();
    }

    public void PickPipe(Pipe pipe)
    {
        if (pipe == null || heldPipe != null)
            return;

        heldPipe = pipe;

        pipe.DetachFromCurrentSlot();
        pipe.SetHeldState(true);

        StartCoroutine(AnimatePipeToHoldPoint(pipe));
    }

    public void PlacePipe(PipeSlot slot)
    {
        if (heldPipe == null || slot == null)
            return;

        Pipe pipeToPlace = heldPipe;
        heldPipe = null;

        StartCoroutine(AnimatePipeToSlot(pipeToPlace, slot));
    }

    public void ReturnHeldPipe()
    {
        if (heldPipe == null)
            return;

        Pipe pipeToReturn = heldPipe;
        heldPipe = null;

        pipeToReturn.DetachFromCurrentSlot();
        pipeToReturn.SetPlacedCorrectly(false);
        pipeToReturn.SetHeldState(false);

        StartCoroutine(AnimatePipeToInitialTransform(pipeToReturn));
    }

    private IEnumerator AnimatePipeToHoldPoint(Pipe pipe)
    {
        isAnimating = true;

        Transform t = pipe.transform;

        t.SetParent(null, true);

        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        Vector3 targetPos = holdPoint.position;
        Quaternion targetRot = holdPoint.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            float alpha = elapsed / moveDuration;
            t.position = Vector3.Lerp(startPos, targetPos, alpha);
            t.rotation = Quaternion.Slerp(startRot, targetRot, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        t.position = targetPos;
        t.rotation = targetRot;

        t.SetParent(holdPoint, true);

        isAnimating = false;
    }

    private IEnumerator AnimatePipeToSlot(Pipe pipe, PipeSlot slot)
    {
        isAnimating = true;

        Transform t = pipe.transform;
        Transform placementPoint = slot.transform;

        Vector3 targetPos = placementPoint.position;
        Quaternion targetRot = placementPoint.rotation;

        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            float alpha = elapsed / moveDuration;
            t.position = Vector3.Lerp(startPos, targetPos, alpha);
            t.rotation = Quaternion.Slerp(startRot, targetRot, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        slot.PlacePipe(pipe);

        isAnimating = false;
    }

    private IEnumerator AnimatePipeToInitialTransform(Pipe pipe)
    {
        isAnimating = true;

        Transform t = pipe.transform;

        t.SetParent(null, true);

        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;

        Vector3 targetPos = pipe.InitialWorldPosition;
        Quaternion targetRot = pipe.InitialWorldRotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            float alpha = elapsed / moveDuration;
            t.position = Vector3.Lerp(startPos, targetPos, alpha);
            t.rotation = Quaternion.Slerp(startRot, targetRot, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        t.position = targetPos;
        t.rotation = targetRot;

        t.SetParent(pipe.InitialParent, true);

        isAnimating = false;
    }
}