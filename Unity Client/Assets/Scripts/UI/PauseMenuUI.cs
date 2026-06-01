using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;

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
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        resumeButton?.onClick.RemoveListener(Resume);
        menuButton?.onClick.RemoveAllListeners();
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
}
