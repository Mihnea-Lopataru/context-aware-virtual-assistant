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

            try
            {
                await WaitForApiClient();

                if (Instance != this)
                    return;

                hintService = new HintServiceUnity(ApiClient.Instance);
                speechApi = new SpeechApi();

                Debug.Log("[ChatManager] Initialized.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatManager] Initialization failed: {e.Message}");
                Debug.LogException(e);
            }
        }
        else
        {
            Debug.LogWarning("[ChatManager] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        OnProcessingStarted = null;
        OnResponseReady = null;
        Instance = null;
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

        if (Instance != this)
            return;

        if (hintService == null)
            hintService = new HintServiceUnity(ApiClient.Instance);

        if (speechApi == null)
            speechApi = new SpeechApi();
    }

    public async Task ProcessMessage(string message)
    {
        await EnsureInitialized();

        if (Instance != this)
            return;

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
            Debug.Log($"[ChatManager] Processing message. Length={message.Length}, SessionId={SessionManager.Instance?.CurrentSessionId ?? -1}");

            OnProcessingStarted?.Invoke();

            var response = await hintService.RequestHint(message);

            if (Instance != this)
                return;

            if (response == null || string.IsNullOrWhiteSpace(response.hint))
                throw new Exception("Empty hint response");

            string hintText = response.hint;

            AudioClip clip = null;

            try
            {
                clip = await speechApi.TextToSpeech(hintText);

                if (Instance != this)
                    return;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ChatManager] TTS failed. Returning text-only response: " + e.Message);
                Debug.LogException(e);
            }

            Debug.Log($"[ChatManager] Response ready. HintLength={hintText.Length}, HasAudio={clip != null}");
            OnResponseReady?.Invoke(hintText, clip);
        }
        catch (Exception e)
        {
            Debug.LogError("[ChatManager] Error: " + e.Message);
            Debug.LogException(e);

            OnResponseReady?.Invoke("Something went wrong.", null);
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
