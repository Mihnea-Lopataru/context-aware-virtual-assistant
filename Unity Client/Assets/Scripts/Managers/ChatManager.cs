using System;
using System.Threading.Tasks;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    private HintServiceUnity hintService;
    private SpeechApi speechApi;

    public bool IsProcessing { get; private set; }
    public bool IsReady => hintService != null && speechApi != null;

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

    private async Task EnsureInitialized()
    {
        if (IsReady)
            return;

        await WaitForApiClient();

        if (hintService == null)
            hintService = new HintServiceUnity(ApiClient.Instance);

        if (speechApi == null)
            speechApi = new SpeechApi();
    }

    public async Task ProcessMessage(string message)
    {
        await EnsureInitialized();

        if (IsProcessing)
        {
            Debug.LogWarning("[ChatManager] Message ignored because a request is already running.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("[ChatManager] Empty message received.");
            return;
        }

        try
        {
            IsProcessing = true;

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
        finally
        {
            IsProcessing = false;
        }
    }
}
