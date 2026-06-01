using System.Collections.Generic;
using UnityEngine;

public class SceneStateBuilder : MonoBehaviour
{
    public static SceneStateBuilder Instance;

    private PipeSlot[] slots;
    private bool warnedNoSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SceneStateBuilder] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        slots = FindObjectsByType<PipeSlot>(FindObjectsSortMode.None);
        Debug.Log($"[SceneStateBuilder] Initialized. SlotCount={slots?.Length ?? 0}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Dictionary<string, object> BuildState()
    {
        if (slots == null || slots.Length == 0)
        {
            if (!warnedNoSlots)
            {
                Debug.LogWarning("[SceneStateBuilder] No pipe slots found.");
                warnedNoSlots = true;
            }

            return new Dictionary<string, object>();
        }

        int totalSlots = slots.Length;
        int filledSlots = 0;
        int correctSlots = 0;
        int incorrectSlots = 0;

        foreach (var slot in slots)
        {
            if (!slot.HasPipe)
                continue;

            filledSlots++;

            if (IsCorrect(slot))
            {
                correctSlots++;
            }
            else
            {
                incorrectSlots++;
            }
        }

        var state = new Dictionary<string, object>
        {
            { "total_slots", totalSlots },
            { "filled_slots", filledSlots },
            { "correct_slots", correctSlots },
            { "incorrect_slots", incorrectSlots },
            { "remaining_slots", totalSlots - filledSlots }
        };

        return state;
    }

    private bool IsCorrect(PipeSlot slot)
    {
        if (!slot.HasPipe)
            return false;

        Pipe pipe = slot.CurrentPipe;

        if (pipe == null)
            return false;

        bool isCorrect =
            pipe.Color.ToString().ToLower() == slot.RequiredColorString() &&
            pipe.Type.ToString().ToLower() == slot.RequiredTypeString();

        return isCorrect;
    }
}
