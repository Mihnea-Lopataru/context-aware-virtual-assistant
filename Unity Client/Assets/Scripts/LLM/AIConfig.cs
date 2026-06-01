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
            Debug.Log($"[AIConfig] LLM provider selected: {GetProviderString()}");
        }
        else
        {
            Debug.Log("[AIConfig] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public string GetProviderString()
    {
        switch (provider)
        {
            case LLMProvider.Ollama:
                return "ollama";
            case LLMProvider.OpenAI:
                return "openai";
            default:
                Debug.LogWarning($"[AIConfig] Unknown provider '{provider}'. Falling back to ollama.");
                return "ollama";
        }
    }
}
