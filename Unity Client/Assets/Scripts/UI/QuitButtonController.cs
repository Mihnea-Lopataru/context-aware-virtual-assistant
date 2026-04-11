using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuitButtonController : MonoBehaviour
{
    //TO DO - DELETE

    [SerializeField] private Button quitButton;

    [SerializeField] private string menuSceneName = "MenuScene";

    private void Start()
    {
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDestroy()
    {
        quitButton.onClick.RemoveListener(OnQuitClicked);
    }


    // =========================
    // CLICK HANDLER
    // =========================
    private async void OnQuitClicked()
    {
        quitButton.interactable = false;

        try
        {
            await SessionManager.Instance.EndSession();

            SceneManager.LoadScene(menuSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to end session: {e.Message}");
            quitButton.interactable = true;
        }
    }
}