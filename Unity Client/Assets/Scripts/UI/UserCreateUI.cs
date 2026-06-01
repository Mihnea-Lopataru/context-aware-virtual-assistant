using UnityEngine;
using TMPro;
using System;

public class UserCreateUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private UserListUI userListUI;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI messageText;

    private Color errorColor = new Color32(239, 68, 68, 255);
    private Color successColor = new Color32(34, 197, 94, 255);

    private bool isLoading = false;

    private void Start()
    {
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    public async void OnCreateUserClicked()
    {
        if (isLoading) return;

        if (usernameInput == null)
        {
            Debug.LogError("Username input not assigned!");
            return;
        }

        string username = usernameInput.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Username cannot be empty!", true);
            return;
        }

        if (UserManager.Instance == null)
        {
            Debug.LogError("[UserCreateUI] UserManager is not available.");
            ShowMessage("User service is not ready.", true);
            return;
        }

        isLoading = true;

        try
        {
            Debug.Log($"[UserCreateUI] Creating user. UsernameLength={username.Length}");
            var user = await UserManager.Instance.CreateUser(username);
            Debug.Log($"[UserCreateUI] User created. UserId={user?.Id.ToString() ?? "<unknown>"}");

            usernameInput.text = "";

            ShowMessage("User created successfully!", false);

            if (userListUI != null)
                await userListUI.LoadUsers();
            else
                Debug.LogWarning("[UserCreateUI] UserListUI is not assigned. Skipping list refresh.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Create user failed: {e.Message}");
            Debug.LogException(e);
            ShowMessage(ParseError(e.Message), true);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ShowMessage(string message, bool isError)
    {
        if (messageText == null) return;

        messageText.gameObject.SetActive(true);
        messageText.text = message;
        messageText.color = isError ? errorColor : successColor;
    }

    private string ParseError(string rawError)
    {
        string lower = rawError.ToLower();

        if (lower.Contains("exists") || lower.Contains("400"))
            return "Username already exists.";

        return "Something went wrong.";
    }
}
