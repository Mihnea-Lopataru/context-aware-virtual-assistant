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
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    public void SetTTSProvider(TTSProvider provider)
    {
        if (ttsProvider == provider)
            return;

        ttsProvider = provider;
        SaveTTSProvider();
    }

    public void SetSTTProvider(STTProvider provider)
    {
        if (sttProvider == provider)
            return;

        sttProvider = provider;
        SaveSTTProvider();
    }

    public string GetTTSProviderString()
    {
        return ttsProvider switch
        {
            TTSProvider.Piper => "piper",
            TTSProvider.Google => "google",
            _ => "piper"
        };
    }

    public string GetSTTProviderString()
    {
        return sttProvider switch
        {
            STTProvider.Vosk => "vosk",
            STTProvider.Google => "google",
            _ => "vosk"
        };
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