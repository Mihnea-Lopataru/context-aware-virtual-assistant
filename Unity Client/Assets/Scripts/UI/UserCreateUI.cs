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

        isLoading = true;

        try
        {
            var user = await UserManager.Instance.CreateUser(username);

            usernameInput.text = "";

            ShowMessage("User created successfully!", false);

            await userListUI.LoadUsers();
        }
        catch (Exception e)
        {
            Debug.LogError($"Create user failed: {e.Message}");
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