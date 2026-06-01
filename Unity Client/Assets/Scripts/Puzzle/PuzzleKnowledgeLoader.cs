using UnityEngine;

public class PuzzleKnowledgeLoader : MonoBehaviour
{
    public static PuzzleKnowledge Instance { get; private set; }

    [SerializeField] private string resourcePath = "Knowledge/puzzle_knowledge";

    private void Awake()
    {
        if (Instance != null)
        {
            return;
        }

        LoadKnowledge();
    }

    private void LoadKnowledge()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);

        if (jsonFile == null)
        {
            Debug.LogError($"[PuzzleKnowledgeLoader] Puzzle knowledge JSON not found at Resources/{resourcePath}.");
            return;
        }

        try
        {
            Instance = JsonUtility.FromJson<PuzzleKnowledge>(jsonFile.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PuzzleKnowledgeLoader] Failed to parse puzzle knowledge JSON at Resources/{resourcePath}: {e.Message}");
            Debug.LogException(e);
            return;
        }

        if (Instance == null)
        {
            Debug.LogError($"[PuzzleKnowledgeLoader] Parsed puzzle knowledge is null. ResourcePath={resourcePath}");
            return;
        }

        Debug.Log(
            $"[PuzzleKnowledgeLoader] Loaded '{Instance.puzzle_name}'. PipeTypes={Instance.pipe_types?.Count ?? 0}, Rules={Instance.rules?.Count ?? 0}");
    }
}
