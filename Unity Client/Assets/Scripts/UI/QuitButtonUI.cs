using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitButtonUI : MonoBehaviour
{
    public void QuitApplication()
    {
        Debug.Log("[MainMenuUI] Quitting application.");

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
