using System;
using System.Threading.Tasks;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    private HintServiceUnity hintService;
    private SpeechApi speechApi;

    public event Action OnProcessingStarted;
    public event Action<string, AudioClip> OnResponseReady;

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            await WaitForApiClient();

            hintService = new HintServiceUnity(ApiClient.Instance);
            speechApi = new SpeechApi();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async Task WaitForApiClient()
    {
        while (ApiClient.Instance == null)
            await Task.Yield();
    }

    public async Task ProcessMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("[ChatManager] Empty message received.");
            return;
        }

        try
        {
            OnProcessingStarted?.Invoke();

            var response = await hintService.RequestHint(message);

            if (response == null || string.IsNullOrWhiteSpace(response.hint))
                throw new Exception("Empty hint response");

            string hintText = response.hint;

            AudioClip clip = null;

            try
            {
                clip = await speechApi.TextToSpeech(hintText);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ChatManager] TTS failed: " + e.Message);
            }

            OnResponseReady?.Invoke(hintText, clip);
        }
        catch (Exception e)
        {
            Debug.LogError("[ChatManager] Error: " + e.Message);

            OnResponseReady?.Invoke("Something went wrong.", null);
        }
    }
}