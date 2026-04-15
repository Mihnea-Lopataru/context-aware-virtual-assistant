using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private Button button;
    [SerializeField] private Image background;

    private UserResponse user;
    private System.Action<UserResponse> onClick;

    public void Setup(UserResponse user, System.Action<UserResponse> onClick)
    {
        this.user = user;
        this.onClick = onClick;

        usernameText.text = user?.Username ?? "Unknown";

        button.onClick.RemoveAllListeners();

        var capturedUser = user;
        button.onClick.AddListener(() => onClick?.Invoke(capturedUser));
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            background.color = new Color32(99, 102, 241, 255);
            button.interactable = false;
        }
        else
        {
            background.color = new Color32(51, 65, 85, 255);
            button.interactable = true;
        }
    }

    public int GetUserId()
    {
        return user != null ? user.Id : -1;
    }
}