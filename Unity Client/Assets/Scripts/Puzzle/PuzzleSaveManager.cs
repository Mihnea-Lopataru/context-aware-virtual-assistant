using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleSaveManager : MonoBehaviour
{
    public static PuzzleSaveManager Instance { get; private set; }

    private const string SaveKeyPrefix = "puzzle_save";

    private bool isRestoring;

    public bool IsRestoring => isRestoring;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSceneInstance()
    {
        if (Instance != null)
            return;

        if (FindAnyObjectByType<PipeSlot>() == null)
            return;

        var saveManagerObject = new GameObject("PuzzleSaveManager");
        saveManagerObject.AddComponent<PuzzleSaveManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PuzzleSave] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RestoreSavedPlacements();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SaveSlotPlacement(PipeSlot slot, Pipe pipe)
    {
        if (isRestoring || slot == null || pipe == null)
            return;

        var saveData = LoadSaveData();
        saveData.scene_name = SceneManager.GetActiveScene().name;
        saveData.updated_at_utc = DateTime.UtcNow.ToString("o");

        saveData.placements.RemoveAll(placement =>
            placement.slot_id == slot.name ||
            placement.pipe_id == GetPipeIdentifier(pipe));

        saveData.placements.Add(new SavedPipePlacement
        {
            slot_id = slot.name,
            pipe_id = GetPipeIdentifier(pipe),
            pipe_name = pipe.name,
            pipe_color = pipe.Color.ToString().ToLower(),
            pipe_type = pipe.Type.ToString().ToLower()
        });

        SaveData(saveData);
        Debug.Log($"[PuzzleSave] Saved placement. Slot={slot.name}, Pipe={pipe.name}, Count={saveData.placements.Count}");
    }

    public void ClearSlotPlacement(PipeSlot slot)
    {
        if (isRestoring || slot == null)
            return;

        var saveData = LoadSaveData();
        int removedCount = saveData.placements.RemoveAll(placement => placement.slot_id == slot.name);

        if (removedCount == 0)
            return;

        saveData.updated_at_utc = DateTime.UtcNow.ToString("o");
        SaveData(saveData);
        Debug.Log($"[PuzzleSave] Cleared placement for slot {slot.name}. Remaining={saveData.placements.Count}");
    }

    public void ClearSave()
    {
        string key = GetSaveKey();

        if (!PlayerPrefs.HasKey(key))
            return;

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log($"[PuzzleSave] Deleted puzzle save. Key={key}");
    }

    private void RestoreSavedPlacements()
    {
        var saveData = LoadSaveData();

        if (saveData.placements == null || saveData.placements.Count == 0)
        {
            Debug.Log("[PuzzleSave] No saved placements found.");
            return;
        }

        PipeSlot[] slots = FindObjectsByType<PipeSlot>(FindObjectsSortMode.None);
        Pipe[] pipes = FindObjectsByType<Pipe>(FindObjectsSortMode.None);

        var slotsById = slots.ToDictionary(slot => slot.name, slot => slot);
        var pipesById = pipes
            .GroupBy(GetPipeIdentifier)
            .ToDictionary(group => group.Key, group => group.First());

        isRestoring = true;
        int restoredCount = 0;

        try
        {
            foreach (var placement in saveData.placements)
            {
                if (!slotsById.TryGetValue(placement.slot_id, out PipeSlot slot))
                {
                    Debug.LogWarning($"[PuzzleSave] Saved slot not found: {placement.slot_id}");
                    continue;
                }

                if (!pipesById.TryGetValue(placement.pipe_id, out Pipe pipe))
                {
                    Debug.LogWarning($"[PuzzleSave] Saved pipe not found: {placement.pipe_id}");
                    continue;
                }

                pipe.DetachFromCurrentSlot();
                slot.RestorePipe(pipe);
                restoredCount++;
            }
        }
        finally
        {
            isRestoring = false;
        }

        Debug.Log($"[PuzzleSave] Restored {restoredCount}/{saveData.placements.Count} placement(s).");
    }

    private PuzzleSaveData LoadSaveData()
    {
        string key = GetSaveKey();

        if (!PlayerPrefs.HasKey(key))
        {
            return new PuzzleSaveData
            {
                scene_name = SceneManager.GetActiveScene().name,
                placements = new List<SavedPipePlacement>()
            };
        }

        string json = PlayerPrefs.GetString(key);

        try
        {
            var saveData = JsonConvert.DeserializeObject<PuzzleSaveData>(json);
            saveData ??= new PuzzleSaveData();
            saveData.placements ??= new List<SavedPipePlacement>();
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PuzzleSave] Failed to parse save data. Clearing corrupted save: {e.Message}");
            Debug.LogException(e);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();

            return new PuzzleSaveData
            {
                scene_name = SceneManager.GetActiveScene().name,
                placements = new List<SavedPipePlacement>()
            };
        }
    }

    private void SaveData(PuzzleSaveData saveData)
    {
        string json = JsonConvert.SerializeObject(saveData);
        PlayerPrefs.SetString(GetSaveKey(), json);
        PlayerPrefs.Save();
    }

    private string GetSaveKey()
    {
        int userId = UserManager.Instance?.CurrentUser?.Id ?? 0;
        string sceneName = SceneManager.GetActiveScene().name;
        return $"{SaveKeyPrefix}_{userId}_{sceneName}";
    }

    private static string GetPipeIdentifier(Pipe pipe)
    {
        if (pipe == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(pipe.Id) ? pipe.name : pipe.Id;
    }

    [Serializable]
    private class PuzzleSaveData
    {
        public string scene_name;
        public string updated_at_utc;
        public List<SavedPipePlacement> placements = new();
    }

    [Serializable]
    private class SavedPipePlacement
    {
        public string slot_id;
        public string pipe_id;
        public string pipe_name;
        public string pipe_color;
        public string pipe_type;
    }
}
