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

    private HintServiceUnity hintService;

    private async void Start()
    {
        chatInputArea.SetActive(false);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        await WaitForApiClient();
        hintService = new HintServiceUnity(ApiClient.Instance);
    }

    private async Task WaitForApiClient()
    {
        while (ApiClient.Instance == null)
            await Task.Yield();
    }

    private void Update()
    {
        HandleOpenChat();
        HandleSubmit();
        HandleCloseWithEscape();
    }

    private void HandleOpenChat()
    {
        if (Input.GetKeyDown(KeyCode.T) && !isChatActive)
        {
            OpenChat();
        }
    }

    private void HandleSubmit()
    {
        if (!isChatActive) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SubmitMessage();
        }
    }

    private void HandleCloseWithEscape()
    {
        if (isChatActive && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelChat();
        }
    }

    private void OpenChat()
    {
        isChatActive = true;

        chatInputArea.SetActive(true);

        if (chatResultUI != null)
            chatResultUI.Hide();

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();

        inputField.caretPosition = inputField.text.Length;
        inputField.MoveTextEnd(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
            playerController.InputEnabled = false;

        if (playerInteraction != null)
            playerInteraction.InputEnabled = false;
    }

    private void CloseChat()
    {
        isChatActive = false;

        chatInputArea.SetActive(false);

        inputField.DeactivateInputField();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController != null)
            playerController.InputEnabled = true;

        if (playerInteraction != null)
            playerInteraction.InputEnabled = true;
    }

    private void CancelChat()
    {
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

        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        try
        {
            var response = await hintService.RequestHint(message);

            if (response != null)
            {
                StartCoroutine(ShowResultCoroutine(response.hint));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Hint request failed: " + e.Message);

            StartCoroutine(ShowResultCoroutine("Something went wrong. Try again."));
        }
        finally
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            isRequestInProgress = false;
        }
    }

    private IEnumerator ShowResultCoroutine(string message)
    {
        yield return null;

        if (chatResultUI != null)
        {
            chatResultUI.ShowResult(message);
        }
        else
        {
            Debug.LogError("chatResultUI is NULL!");
        }
    }
}