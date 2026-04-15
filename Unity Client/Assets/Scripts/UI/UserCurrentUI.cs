using UnityEngine;
using TMPro;

public class UserCurrentUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentUserText;

    private void OnEnable()
    {
        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged += HandleUserChanged;
        }

        ForceUpdate();
    }

    private void OnDisable()
    {
        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged -= HandleUserChanged;
        }
    }

    private void HandleUserChanged(UserResponse user)
    {
        ForceUpdate();
    }

    private void ForceUpdate()
    {
        var user = UserManager.Instance?.CurrentUser;

        if (currentUserText == null)
            return;

        currentUserText.text = user == null
            ? "No user selected"
            : $"Current User: {user.Username}";
    }
}