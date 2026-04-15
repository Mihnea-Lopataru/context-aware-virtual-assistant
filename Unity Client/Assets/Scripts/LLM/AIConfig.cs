using UnityEngine;

public class AIConfig : MonoBehaviour
{
    public static AIConfig Instance;

    [Header("LLM Settings")]
    public LLMProvider provider = LLMProvider.Ollama;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetProviderString()
    {
        return provider switch
        {
            LLMProvider.Ollama => "ollama",
            LLMProvider.OpenAI => "openai",
            _ => "ollama"
        };
    }
}