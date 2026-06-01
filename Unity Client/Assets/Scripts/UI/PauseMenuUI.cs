using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    private const int LocalProviderIndex = 0;
    private const int CloudProviderIndex = 1;

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private TMP_Dropdown providerDropdown;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public bool InputEnabled { get; set; } = true;

    private bool isPaused = false;
    private bool isQuitting = false;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PauseMenuUI] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        else
            Debug.LogError("[PauseMenuUI] Pause menu root is not assigned.");

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        else
            Debug.LogError("[PauseMenuUI] Resume button is not assigned.");

        if (menuButton != null)
            menuButton.onClick.AddListener(() => _ = QuitAsync());
        else
            Debug.LogError("[PauseMenuUI] Menu button is not assigned.");

        ConfigureProviderDropdown();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        resumeButton?.onClick.RemoveListener(Resume);
        menuButton?.onClick.RemoveAllListeners();

        if (providerDropdown != null)
            providerDropdown.onValueChanged.RemoveListener(HandleProviderDropdownChanged);
    }

    private void Update()
    {
        if (!InputEnabled || isQuitting)
            return;

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;

        FindAnyObjectByType<ChatInputUI>()?.ForceClose();
        VoiceInputManager.Instance?.CancelVoiceInput();

        pauseMenuRoot?.SetActive(true);

        SetGameplayInput(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SpeechManager.Instance?.Stop();

        SyncProviderDropdownSelection();
    }

    public void Resume()
    {
        if (isQuitting)
            return;

        isPaused = false;

        pauseMenuRoot?.SetActive(false);

        SetGameplayInput(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        VoiceInputManager.Instance?.ResumeWakeListeningIfAvailable();
    }

    private async Task QuitAsync()
    {
        if (isQuitting)
            return;

        isQuitting = true;

        SetGameplayInput(false);

        if (resumeButton != null)
            resumeButton.interactable = false;

        if (menuButton != null)
            menuButton.interactable = false;

        try
        {
            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.EndSession();
            }
            else
            {
                Debug.LogWarning("SessionManager.Instance is null.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to end session from pause menu: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            Debug.Log($"[PauseMenuUI] Loading main menu scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void SetGameplayInput(bool enabled)
    {
        if (playerController != null)
            playerController.InputEnabled = enabled;

        if (playerInteraction != null)
            playerInteraction.InputEnabled = enabled;
    }

    private void ConfigureProviderDropdown()
    {
        if (providerDropdown == null && pauseMenuRoot != null)
            providerDropdown = pauseMenuRoot.GetComponentInChildren<TMP_Dropdown>(true);

        if (providerDropdown == null)
        {
            Debug.LogWarning("[PauseMenuUI] Provider dropdown is not assigned. Provider switching is disabled.");
            return;
        }

        providerDropdown.onValueChanged.RemoveListener(HandleProviderDropdownChanged);

        providerDropdown.ClearOptions();
        providerDropdown.AddOptions(new List<string> { "LOCAL", "CLOUD" });

        SyncProviderDropdownSelection();

        providerDropdown.onValueChanged.AddListener(HandleProviderDropdownChanged);
    }

    private void SyncProviderDropdownSelection()
    {
        if (providerDropdown == null)
            return;

        int selectedIndex = IsCloudProviderMode() ? CloudProviderIndex : LocalProviderIndex;
        providerDropdown.SetValueWithoutNotify(selectedIndex);
        providerDropdown.RefreshShownValue();
    }

    private bool IsCloudProviderMode()
    {
        bool aiIsCloud = AIConfig.Instance != null &&
                         AIConfig.Instance.CurrentProvider == LLMProvider.OpenAI;

        bool speechIsCloud = SpeechConfig.Instance != null &&
                             (SpeechConfig.Instance.CurrentSTTProvider == STTProvider.Google ||
                              SpeechConfig.Instance.CurrentTTSProvider == TTSProvider.Google);

        return aiIsCloud || speechIsCloud;
    }

    private void HandleProviderDropdownChanged(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case LocalProviderIndex:
                ApplyLocalProviders();
                break;
            case CloudProviderIndex:
                ApplyCloudProviders();
                break;
            default:
                Debug.LogWarning($"[PauseMenuUI] Unknown provider dropdown index: {selectedIndex}");
                SyncProviderDropdownSelection();
                break;
        }
    }

    private void ApplyLocalProviders()
    {
        ApplyProviderMode(
            "LOCAL",
            LLMProvider.Ollama,
            STTProvider.Vosk,
            TTSProvider.Piper
        );
    }

    private void ApplyCloudProviders()
    {
        ApplyProviderMode(
            "CLOUD",
            LLMProvider.OpenAI,
            STTProvider.Google,
            TTSProvider.Google
        );
    }

    private void ApplyProviderMode(
        string modeName,
        LLMProvider llmProvider,
        STTProvider sttProvider,
        TTSProvider ttsProvider)
    {
        if (ChatManager.Instance != null && ChatManager.Instance.IsProcessing)
        {
            Debug.LogWarning("[PauseMenuUI] Provider mode changed while a chat request is processing. The current request may finish with the previous provider.");
        }

        if (AIConfig.Instance != null)
            AIConfig.Instance.SetProvider(llmProvider);
        else
            Debug.LogWarning("[PauseMenuUI] AIConfig is not available. LLM provider was not changed.");

        if (SpeechConfig.Instance != null)
        {
            SpeechConfig.Instance.SetSTTProvider(sttProvider);
            SpeechConfig.Instance.SetTTSProvider(ttsProvider);
            SpeechConfig.Instance.SaveSettings();
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] SpeechConfig is not available. Speech providers were not changed.");
        }

        Debug.Log($"[PauseMenuUI] Provider mode set to {modeName}. LLM={llmProvider}, STT={sttProvider}, TTS={ttsProvider}");
    }
}
