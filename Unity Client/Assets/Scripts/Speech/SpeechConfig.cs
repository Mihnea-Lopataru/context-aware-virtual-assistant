using UnityEngine;

public class SpeechConfig : MonoBehaviour
{
    public static SpeechConfig Instance { get; private set; }

    private const string TTSProviderKey = "speech_tts_provider";
    private const string STTProviderKey = "speech_stt_provider";

    [Header("Speech Settings")]
    [SerializeField] private TTSProvider ttsProvider = TTSProvider.Piper;
    [SerializeField] private STTProvider sttProvider = STTProvider.Vosk;

    public TTSProvider CurrentTTSProvider => ttsProvider;
    public STTProvider CurrentSTTProvider => sttProvider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadSettings();
        Debug.Log($"[SpeechConfig] Loaded settings. STT={GetSTTProviderString()}, TTS={GetTTSProviderString()}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetTTSProvider(TTSProvider provider)
    {
        if (ttsProvider == provider)
            return;

        ttsProvider = provider;
        SaveTTSProvider();
        Debug.Log($"[SpeechConfig] TTS provider changed: {GetTTSProviderString()}");
    }

    public void SetSTTProvider(STTProvider provider)
    {
        if (sttProvider == provider)
            return;

        sttProvider = provider;
        SaveSTTProvider();
        Debug.Log($"[SpeechConfig] STT provider changed: {GetSTTProviderString()}");
    }

    public string GetTTSProviderString()
    {
        switch (ttsProvider)
        {
            case TTSProvider.Piper:
                return "piper";
            case TTSProvider.Google:
                return "google";
            default:
                Debug.LogWarning($"[SpeechConfig] Unknown TTS provider '{ttsProvider}'. Falling back to piper.");
                return "piper";
        }
    }

    public string GetSTTProviderString()
    {
        switch (sttProvider)
        {
            case STTProvider.Vosk:
                return "vosk";
            case STTProvider.Google:
                return "google";
            default:
                Debug.LogWarning($"[SpeechConfig] Unknown STT provider '{sttProvider}'. Falling back to vosk.");
                return "vosk";
        }
    }

    public void SaveSettings()
    {
        SaveTTSProvider();
        SaveSTTProvider();
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(TTSProviderKey))
        {
            int savedTTS = PlayerPrefs.GetInt(TTSProviderKey);
            if (System.Enum.IsDefined(typeof(TTSProvider), savedTTS))
            {
                ttsProvider = (TTSProvider)savedTTS;
            }
        }

        if (PlayerPrefs.HasKey(STTProviderKey))
        {
            int savedSTT = PlayerPrefs.GetInt(STTProviderKey);
            if (System.Enum.IsDefined(typeof(STTProvider), savedSTT))
            {
                sttProvider = (STTProvider)savedSTT;
            }
        }
    }

    private void SaveTTSProvider()
    {
        PlayerPrefs.SetInt(TTSProviderKey, (int)ttsProvider);
    }

    private void SaveSTTProvider()
    {
        PlayerPrefs.SetInt(STTProviderKey, (int)sttProvider);
    }
}
