using System.Collections.Generic;
using System.Linq;
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
        var slotStates = new List<Dictionary<string, object>>();
        var correctSlotIds = new List<string>();
        var incorrectSlotIds = new List<string>();
        var emptySlotIds = new List<string>();

        foreach (var slot in slots.OrderBy(slot => slot.name))
        {
            if (slot == null)
                continue;

            var slotState = BuildSlotState(slot);
            slotStates.Add(slotState);

            string slotId = (string)slotState["slot_id"];
            bool isFilled = (bool)slotState["is_filled"];
            bool isCorrect = (bool)slotState["is_correct"];

            if (!isFilled)
            {
                emptySlotIds.Add(slotId);
                continue;
            }

            filledSlots++;

            if (isCorrect)
            {
                correctSlots++;
                correctSlotIds.Add(slotId);
            }
            else
            {
                incorrectSlots++;
                incorrectSlotIds.Add(slotId);
            }
        }

        var state = new Dictionary<string, object>
        {
            { "total_slots", totalSlots },
            { "filled_slots", filledSlots },
            { "correct_slots", correctSlots },
            { "incorrect_slots", incorrectSlots },
            { "remaining_slots", totalSlots - filledSlots },
            { "correct_slot_ids", correctSlotIds },
            { "incorrect_slot_ids", incorrectSlotIds },
            { "empty_slot_ids", emptySlotIds },
            { "slots", slotStates }
        };

        return state;
    }

    private Dictionary<string, object> BuildSlotState(PipeSlot slot)
    {
        Pipe pipe = slot.CurrentPipe;
        bool isFilled = pipe != null;
        bool isCorrect = isFilled && IsCorrect(slot);

        var slotState = new Dictionary<string, object>
        {
            { "slot_id", slot.name },
            { "required_color", slot.RequiredColorString() },
            { "required_type", slot.RequiredTypeString() },
            { "is_filled", isFilled },
            { "is_correct", isCorrect }
        };

        if (!isFilled)
        {
            slotState["state"] = "empty";
            return slotState;
        }

        slotState["state"] = isCorrect ? "correct" : "incorrect";
        slotState["placed_pipe_id"] = string.IsNullOrWhiteSpace(pipe.Id) ? pipe.name : pipe.Id;
        slotState["placed_pipe_name"] = pipe.name;
        slotState["placed_pipe_color"] = pipe.Color.ToString().ToLower();
        slotState["placed_pipe_type"] = pipe.Type.ToString().ToLower();

        return slotState;
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
