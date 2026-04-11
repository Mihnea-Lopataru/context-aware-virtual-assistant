using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ContinueButtonController : MonoBehaviour
{
    [SerializeField] private Button continueButton;

    [SerializeField] private string nextSceneName = "GameScene";

    private void Start()
    {
        UpdateButtonState();

        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged += HandleUserChanged;
        }

        continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void OnDestroy()
    {
        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged -= HandleUserChanged;
        }

        continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    // =========================
    // EVENT HANDLER
    // =========================
    private void HandleUserChanged(UserResponse user)
    {
        UpdateButtonState();
    }

    // =========================
    // BUTTON STATE
    // =========================
    private void UpdateButtonState()
    {
        bool hasUser = UserManager.Instance != null &&
                       UserManager.Instance.CurrentUser != null;

        continueButton.interactable = hasUser;
    }

    // =========================
    // CLICK HANDLER
    // =========================
    private async void OnContinueClicked()
    {
        var user = UserManager.Instance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No user selected!");
            return;
        }

        continueButton.interactable = false;

        try
        {
            await SessionManager.Instance.StartSession();

            SceneManager.LoadScene(nextSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start session: {e.Message}");
            continueButton.interactable = true;
        }
    }
}