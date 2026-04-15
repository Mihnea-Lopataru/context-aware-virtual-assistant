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

    private async void Start()
    {
        await System.Threading.Tasks.Task.Yield();

        UpdateButtonState();

        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged += HandleUserChanged;
        }

        continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void OnEnable()
    {
        if (UserManager.Instance != null)
        {
            UserManager.Instance.OnUserChanged += HandleUserChanged;
        }

        UpdateButtonState();
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
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        bool hasUser = UserManager.Instance != null &&
                       UserManager.Instance.CurrentUser != null;

        continueButton.interactable = hasUser;
    }

    public void OnContinueClicked()
    {
        if (!continueButton.interactable)
            return;

        StartCoroutine(ContinueFlow());
    }

    private IEnumerator ContinueFlow()
    {
        continueButton.interactable = false;

        ShowLoading(true);

        yield return new WaitForSeconds(0.3f);

        var user = UserManager.Instance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No user selected!");
            ShowLoading(false);
            yield break;
        }

        var task = SessionManager.Instance.StartSession(
            nextSceneName,
            initialObjective
        );

        while (!task.IsCompleted)
            yield return null;

        if (task.Exception != null)
        {
            Debug.LogError($"Failed to start session: {task.Exception}");
            ShowLoading(false);
            continueButton.interactable = true;
            yield break;
        }

        var loadOp = SceneManager.LoadSceneAsync(nextSceneName);

        while (!loadOp.isDone)
            yield return null;

        ShowLoading(false);
    }
    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(show);
    }
}