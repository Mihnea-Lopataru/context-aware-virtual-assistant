using UnityEngine;

public class AIConfig : MonoBehaviour
{
    public static AIConfig Instance;

    private const string ProviderKey = "ai_llm_provider";

    [Header("LLM Settings")]
    public LLMProvider provider = LLMProvider.Ollama;
    public LLMProvider CurrentProvider => provider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadSettings();
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

    public void SetProvider(LLMProvider newProvider)
    {
        if (provider == newProvider)
            return;

        provider = newProvider;
        SaveSettings();
        Debug.Log($"[AIConfig] LLM provider changed: {GetProviderString()}");
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

    private void LoadSettings()
    {
        if (!PlayerPrefs.HasKey(ProviderKey))
            return;

        int savedProvider = PlayerPrefs.GetInt(ProviderKey);
        if (System.Enum.IsDefined(typeof(LLMProvider), savedProvider))
            provider = (LLMProvider)savedProvider;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(ProviderKey, (int)provider);
        PlayerPrefs.Save();
    }
}
