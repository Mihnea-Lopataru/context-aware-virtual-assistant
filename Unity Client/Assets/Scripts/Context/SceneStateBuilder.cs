using System.Collections.Generic;
using UnityEngine;

public class SceneStateBuilder : MonoBehaviour
{
    public static SceneStateBuilder Instance;

    private PipeSlot[] slots;

    private void Awake()
    {
        Instance = this;

        slots = slots = FindObjectsByType<PipeSlot>(FindObjectsSortMode.None);
    }

    public Dictionary<string, object> BuildState()
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning("[SceneStateBuilder] No pipe slots found.");
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