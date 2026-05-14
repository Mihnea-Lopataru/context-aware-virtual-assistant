using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Collections;

public class ChatInputUI : MonoBehaviour
{
    public static ChatInputUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject chatInputArea;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private TMP_InputField inputField;

    [Header("Result UI")]
    [SerializeField] private ChatResultUI chatResultUI;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    private bool isChatActive = false;
    private bool isRequestInProgress = false;

    public bool IsChatActive => isChatActive;
    public bool BlocksVoiceInput => isChatActive || isRequestInProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private async void Start()
    {
        chatInputArea.SetActive(false);
        loadingIndicator?.SetActive(false);

        await WaitForChatManager();

        ChatManager.Instance.OnProcessingStarted += HandleProcessingStarted;
        ChatManager.Instance.OnResponseReady += HandleResponse;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnProcessingStarted -= HandleProcessingStarted;
            ChatManager.Instance.OnResponseReady -= HandleResponse;
        }
    }

    private async Task WaitForChatManager()
    {
        while (ChatManager.Instance == null || !ChatManager.Instance.IsReady)
            await Task.Yield();
    }

    private void Update()
    {
        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return;

        HandleOpenChat();
        HandleSubmit();
        HandleCloseWithEscape();
    }

    private void HandleOpenChat()
    {
        if (!Input.GetKeyDown(KeyCode.T) || isChatActive)
            return;

        if (VoiceInputManager.Instance != null && VoiceInputManager.Instance.BlocksChatInput)
            return;

        if (ChatManager.Instance == null ||
            !ChatManager.Instance.IsReady ||
            ChatManager.Instance.IsProcessing)
            return;

        OpenChat();
    }

    public bool CanOpenChat()
    {
        if (isChatActive || isRequestInProgress)
            return false;

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return false;

        if (VoiceInputManager.Instance != null && VoiceInputManager.Instance.BlocksChatInput)
            return false;

        if (ChatManager.Instance == null ||
            !ChatManager.Instance.IsReady ||
            ChatManager.Instance.IsProcessing)
            return false;

        return true;
    }

    public void TryOpenChat()
    {
        if (CanOpenChat())
            OpenChat();
    }

    private void HandleSubmit()
    {
        if (!isChatActive)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
            SubmitMessage();
    }

    private void HandleCloseWithEscape()
    {
        if (isChatActive && Input.GetKeyDown(KeyCode.Escape))
            CancelChat();
    }

    private void OpenChat()
    {
        isChatActive = true;

        chatInputArea.SetActive(true);

        chatResultUI?.Hide();
        loadingIndicator?.SetActive(false);

        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            playerController.InputEnabled = false;

        if (playerInteraction != null)
            playerInteraction.InputEnabled = false;

        if (PauseMenuUI.Instance != null)
            PauseMenuUI.Instance.InputEnabled = false;

        SpeechManager.Instance?.Stop();
        WakeWordListener.Instance?.StopListening();
    }

    private void CloseChat(bool resumeVoiceListening = true)
    {
        isChatActive = false;

        chatInputArea.SetActive(false);
        inputField.DeactivateInputField();

        if (PauseMenuUI.Instance != null)
            PauseMenuUI.Instance.InputEnabled = true;

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController != null)
            playerController.InputEnabled = true;

        if (playerInteraction != null)
            playerInteraction.InputEnabled = true;

        if (resumeVoiceListening)
            VoiceInputManager.Instance?.ResumeWakeListeningIfAvailable();
    }

    private void CancelChat()
    {
        inputField.text = "";
        CloseChat();
    }

    public void ForceClose()
    {
        if (!isChatActive)
            return;

        inputField.text = "";
        CloseChat(resumeVoiceListening: false);
    }

    private async void SubmitMessage()
    {
        if (isRequestInProgress)
            return;

        string message = inputField.text;

        if (string.IsNullOrWhiteSpace(message))
            return;

        isRequestInProgress = true;

        CloseChat(resumeVoiceListening: false);

        try
        {
            await ChatManager.Instance.ProcessMessage(message);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Chat error: " + e.Message);
            HandleResponse("Something went wrong.", null);
        }
        finally
        {
            isRequestInProgress = false;
            VoiceInputManager.Instance?.ResumeWakeListeningIfAvailable();
        }
    }

    public async Task SendMessageFromVoice(string message)
    {
        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return;

        if (isRequestInProgress)
            return;

        isRequestInProgress = true;

        try
        {
            await ChatManager.Instance.ProcessMessage(message);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Voice message error: " + e.Message);
            HandleResponse("Something went wrong.", null);
        }
        finally
        {
            isRequestInProgress = false;
            VoiceInputManager.Instance?.ResumeWakeListeningIfAvailable();
        }
    }

    private void HandleProcessingStarted()
    {
        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return;

        loadingIndicator?.SetActive(true);
    }

    private void HandleResponse(string message, AudioClip clip)
    {
        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return;

        StartCoroutine(ShowResultAndPlayAudio(message, clip));
    }

    private IEnumerator ShowResultAndPlayAudio(string message, AudioClip clip)
    {
        yield return null;

        loadingIndicator?.SetActive(false);

        chatResultUI?.ShowResult(message);

        if (clip != null)
        {
            SpeechManager.Instance?.Play(clip);
        }
    }
}
