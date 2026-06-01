using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject endScreenRoot;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MenuScene";

    [Header("Completion Check")]
    [SerializeField] private float checkInterval = 0.25f;

    private PipeSlot[] slots;
    private float nextCheckTime;
    private bool isCompleted;
    private bool isReturningToMenu;
    private bool isQuitting;
    private bool warnedNoSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EndGameUI] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (endScreenRoot != null)
            endScreenRoot.SetActive(false);
        else
            Debug.LogError("[EndGameUI] End screen root is not assigned.");

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        else
            Debug.LogError("[EndGameUI] Menu button is not assigned.");

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitApplication);
        else
            Debug.LogError("[EndGameUI] Quit button is not assigned.");

        slots = FindObjectsByType<PipeSlot>(FindObjectsSortMode.None);
        Debug.Log($"[EndGameUI] Tracking puzzle completion. SlotCount={slots?.Length ?? 0}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        menuButton?.onClick.RemoveListener(OnMenuButtonClicked);
        quitButton?.onClick.RemoveListener(QuitApplication);
    }

    private void Update()
    {
        if (isCompleted || isReturningToMenu || isQuitting)
            return;

        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + Mathf.Max(0.05f, checkInterval);
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (slots == null || slots.Length == 0)
        {
            slots = FindObjectsByType<PipeSlot>(FindObjectsSortMode.None);
        }

        if (slots == null || slots.Length == 0)
        {
            if (!warnedNoSlots)
            {
                Debug.LogWarning("[EndGameUI] No pipe slots found. Completion cannot be checked.");
                warnedNoSlots = true;
            }

            return;
        }

        warnedNoSlots = false;

        int filledSlots = 0;
        int correctSlots = 0;

        foreach (var slot in slots.Where(slot => slot != null))
        {
            if (!slot.HasPipe)
                continue;

            filledSlots++;

            if (slot.CurrentPipe != null && slot.CurrentPipe.IsPlacedCorrectly)
                correctSlots++;
        }

        if (correctSlots == slots.Length && filledSlots == slots.Length)
        {
            CompletePuzzle();
        }
    }

    private void CompletePuzzle()
    {
        if (isCompleted)
            return;

        isCompleted = true;

        Debug.Log("[EndGameUI] Puzzle completed. Showing end screen.");

        ContextLogger.Instance?.LogEvent(EventType.PUZZLE_COMPLETED, new Dictionary<string, object>
        {
            { "total_slots", slots.Length },
            { "correct_slots", slots.Length }
        });

        PuzzleSaveManager.Instance?.ClearSave();

        StopGameplay();

        if (endScreenRoot != null)
            endScreenRoot.SetActive(true);
    }

    private void StopGameplay()
    {
        FindAnyObjectByType<ChatInputUI>()?.ForceClose();
        VoiceInputManager.Instance?.CancelVoiceInput();
        WakeWordListener.Instance?.StopListening();
        SpeechManager.Instance?.Stop();

        if (VoiceInputManager.Instance != null)
            VoiceInputManager.Instance.enabled = false;

        if (ChatInputUI.Instance != null)
            ChatInputUI.Instance.enabled = false;

        if (PauseMenuUI.Instance != null)
            PauseMenuUI.Instance.InputEnabled = false;

        SetGameplayInput(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnMenuButtonClicked()
    {
        _ = ReturnToMenuAsync();
    }

    private async Task ReturnToMenuAsync()
    {
        if (isReturningToMenu || isQuitting)
            return;

        isReturningToMenu = true;

        SetButtonsInteractable(false);
        SetGameplayInput(false);

        try
        {
            if (SessionManager.Instance != null)
                await SessionManager.Instance.EndSession();
            else
                Debug.LogWarning("[EndGameUI] SessionManager.Instance is null.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to end session from end screen: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            Debug.Log($"[EndGameUI] Loading main menu scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void QuitApplication()
    {
        if (isQuitting)
            return;

        isQuitting = true;

        SetButtonsInteractable(false);
        SetGameplayInput(false);
        VoiceInputManager.Instance?.CancelVoiceInput();
        SpeechManager.Instance?.Stop();

        Debug.Log("[EndGameUI] Quitting application.");
        Application.Quit();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (menuButton != null)
            menuButton.interactable = interactable;

        if (quitButton != null)
            quitButton.interactable = interactable;
    }

    private void SetGameplayInput(bool enabled)
    {
        if (playerController != null)
            playerController.InputEnabled = enabled;

        if (playerInteraction != null)
            playerInteraction.InputEnabled = enabled;
    }
}
