using UnityEngine;
using TMPro;

public class UserCreateUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private UserListUI userListUI;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI messageText;

    private Color errorColor = new Color32(239, 68, 68, 255);
    private Color successColor = new Color32(34, 197, 94, 255);
    
    private void Start()
    {
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    // =========================
    // BUTTON CLICK
    // =========================
    public async void OnCreateUserClicked()
    {
        string username = usernameInput.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Username cannot be empty!", true);
            return;
        }

        try
        {
            var user = await UserManager.Instance.CreateUser(username);

            // 🔥 auto-select (deja făcut în UserManager, dar safe)
            UserManager.Instance.SelectUser(user);

            usernameInput.text = "";

            ShowMessage("User created successfully!", false);

            // 🔥 refresh list
            await userListUI.LoadUsers();
        }
        catch (System.Exception e)
        {
            ShowMessage(ParseError(e.Message), true);
        }
    }

    // =========================
    // MESSAGE HANDLING
    // =========================
    private void ShowMessage(string message, bool isError)
    {
        if (messageText == null) return;

        messageText.gameObject.SetActive(true);
        messageText.text = message;
        messageText.color = isError ? errorColor : successColor;
    }

    // =========================
    // CLEAN ERROR MESSAGE
    // =========================
    private string ParseError(string rawError)
    {
        if (rawError.Contains("400"))
            return "Username already exists.";

        return "Something went wrong.";
    }
}