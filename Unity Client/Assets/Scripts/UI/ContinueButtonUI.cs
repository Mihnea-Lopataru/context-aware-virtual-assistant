using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ContinueButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button continueButton;

    [Header("Scene Config")]
    [SerializeField] private string nextSceneName = "GameScene";
    [SerializeField] private string initialObjective = "Solve the puzzle";

    [Header("Loading")]
    [SerializeField] private GameObject loadingPanel;

    private bool isSubscribedToUserManager;

    private async void Start()
    {
        await System.Threading.Tasks.Task.Yield();

        UpdateButtonState();

        SubscribeToUserManager();

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        else
            Debug.LogError("[ContinueButtonUI] Continue button is not assigned.");
    }

    private void OnEnable()
    {
        SubscribeToUserManager();

        UpdateButtonState();
    }

    private void OnDisable()
    {
        if (UserManager.Instance != null && isSubscribedToUserManager)
        {
            UserManager.Instance.OnUserChanged -= HandleUserChanged;
            isSubscribedToUserManager = false;
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    private void SubscribeToUserManager()
    {
        if (UserManager.Instance == null || isSubscribedToUserManager)
            return;

        UserManager.Instance.OnUserChanged += HandleUserChanged;
        isSubscribedToUserManager = true;
    }

    private void HandleUserChanged(UserResponse user)
    {
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        bool hasUser = UserManager.Instance != null &&
                       UserManager.Instance.CurrentUser != null;

        if (continueButton != null)
            continueButton.interactable = hasUser;
    }

    public void OnContinueClicked()
    {
        if (continueButton == null || !continueButton.interactable)
            return;

        StartCoroutine(ContinueFlow());
    }

    private IEnumerator ContinueFlow()
    {
        continueButton.interactable = false;

        ShowLoading(true);

        yield return new WaitForSeconds(0.3f);

        if (UserManager.Instance == null)
        {
            Debug.LogError("[ContinueButtonUI] UserManager is not available.");
            ShowLoading(false);
            continueButton.interactable = true;
            yield break;
        }

        var user = UserManager.Instance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("[ContinueButtonUI] No user selected.");
            ShowLoading(false);
            continueButton.interactable = true;
            yield break;
        }

        if (SessionManager.Instance == null)
        {
            Debug.LogError("[ContinueButtonUI] SessionManager is not available.");
            ShowLoading(false);
            continueButton.interactable = true;
            yield break;
        }

        Debug.Log($"[ContinueButtonUI] Continue clicked. UserId={user.Id}, NextScene={nextSceneName}");

        var task = SessionManager.Instance.StartSession(
            nextSceneName,
            initialObjective
        );

        while (!task.IsCompleted)
            yield return null;

        if (task.Exception != null)
        {
            Debug.LogError($"Failed to start session: {task.Exception}");
            Debug.LogException(task.Exception);
            ShowLoading(false);
            continueButton.interactable = true;
            yield break;
        }

        var loadOp = SceneManager.LoadSceneAsync(nextSceneName);
        if (loadOp == null)
        {
            Debug.LogError($"[ContinueButtonUI] Failed to start loading scene '{nextSceneName}'.");
            ShowLoading(false);
            continueButton.interactable = true;
            yield break;
        }

        while (!loadOp.isDone)
            yield return null;

        ShowLoading(false);
        Debug.Log($"[ContinueButtonUI] Scene loaded: {nextSceneName}");
    }
    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(show);
    }
}
