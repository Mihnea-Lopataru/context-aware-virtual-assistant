using UnityEngine;
using UnityEngine.UI;

public class VoiceUI : MonoBehaviour
{
    public static VoiceUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject microphoneUI;
    [SerializeField] private Image micImage;

    [Header("Alpha Settings")]
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 1.0f;
    [SerializeField] private float smoothSpeed = 10f;

    private float currentAlpha = 0f;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show()
    {
        microphoneUI.SetActive(true);
        currentAlpha = minAlpha;
        SetAlpha(currentAlpha);
    }

    public void Hide()
    {
        microphoneUI.SetActive(false);
    }

    public void UpdateVolume(float level)
    {
        float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.Clamp01(level));

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * smoothSpeed);

        SetAlpha(currentAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (micImage == null) return;

        Color c = micImage.color;
        c.a = alpha;
        micImage.color = c;
    }
}