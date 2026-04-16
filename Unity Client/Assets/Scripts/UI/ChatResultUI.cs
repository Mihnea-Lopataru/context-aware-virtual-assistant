using System.Collections;
using TMPro;
using UnityEngine;

public class ChatResultUI : MonoBehaviour
{
    [SerializeField] private GameObject chatResultArea;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private float typingSpeed = 0.02f;

    private Coroutine typingCoroutine;

    private void Start()
    {
        chatResultArea.SetActive(false);
    }

    public void ShowResult(string message)
    {
        chatResultArea.SetActive(true);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(message));
    }

    private IEnumerator TypeText(string message)
    {
        resultText.text = "";

        foreach (char c in message)
        {
            resultText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void Hide()
    {
        chatResultArea.SetActive(false);
    }
}