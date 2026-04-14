using UnityEngine;

public class PuzzleKnowledgeLoader : MonoBehaviour
{
    public static PuzzleKnowledge Instance { get; private set; }

    [SerializeField] private string resourcePath = "Knowledge/puzzle_knowledge";

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Multiple PuzzleKnowledgeLoader instances detected!");
            return;
        }

        LoadKnowledge();
    }

    private void LoadKnowledge()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);

        if (jsonFile == null)
        {
            Debug.LogError("Puzzle knowledge JSON not found!");
            return;
        }

        Instance = JsonUtility.FromJson<PuzzleKnowledge>(jsonFile.text);

        if (Instance == null)
        {
            Debug.LogError("Failed to parse puzzle knowledge JSON!");
            return;
        }

        Debug.Log("Puzzle knowledge loaded successfully.");
    }
}