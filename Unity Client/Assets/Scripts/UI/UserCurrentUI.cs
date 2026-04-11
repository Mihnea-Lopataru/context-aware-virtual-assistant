using UnityEngine;
using TMPro;

public class UserCurrentUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentUserText;

    private void Start()
    {
        // subscribe ONCE (safe)
        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged += HandleUserChanged;
        }

        ForceUpdate();
    }

    private void OnDestroy()
    {
        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged -= HandleUserChanged;
        }
    }

    // =========================
    // EVENT HANDLER
    // =========================
    private void HandleUserChanged(UserResponse user)
    {
        ForceUpdate();
    }

    // =========================
    // UI UPDATE
    // =========================
    private void ForceUpdate()
    {
        var user = UserManager.Instance?.CurrentUser;

        currentUserText.text = user == null
            ? "No user selected"
            : $"Current User: {user.Username}";
    }
}