using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Collections;

public class ChatInputUI : MonoBehaviour
{
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
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnProcessingStarted -= HandleProcessingStarted;
            ChatManager.Instance.OnResponseReady -= HandleResponse;
        }
    }

    private async Task WaitForChatManager()
    {
        while (ChatManager.Instance == null)
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
        if (Input.GetKeyDown(KeyCode.T) && !isChatActive)
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

        playerController.InputEnabled = false;
        playerInteraction.InputEnabled = false;

        if (PauseMenuUI.Instance != null)
            PauseMenuUI.Instance.InputEnabled = false;

        SpeechManager.Instance?.Stop();
    }

    private void CloseChat()
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

        playerController.InputEnabled = true;
        playerInteraction.InputEnabled = true;
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
        CloseChat();
    }

    private async void SubmitMessage()
    {
        if (isRequestInProgress)
            return;

        string message = inputField.text;

        if (string.IsNullOrWhiteSpace(message))
            return;

        isRequestInProgress = true;

        CloseChat();

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