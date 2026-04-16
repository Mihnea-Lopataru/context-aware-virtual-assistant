using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ContextLogger : MonoBehaviour
{
    public static ContextLogger Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Settings")]
    [SerializeField] private float sendInterval = 5f;
    [SerializeField] private int maxBatchSize = 20;

    private EventsApi eventsApi;
    private Coroutine sendCoroutine;

    private readonly List<PlayerEvent> eventBuffer = new();

    private bool isFlushing = false;

    private Pipe currentHeldPipe;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ContextLogger instances detected!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await WaitForApiClient();

        if (ApiClient.Instance == null)
        {
            Debug.LogError("[ContextLogger] ApiClient not initialized!");
            return;
        }

        eventsApi = new EventsApi(ApiClient.Instance);
    }

    private void Start()
    {
        if (sendCoroutine == null)
        {
            sendCoroutine = StartCoroutine(SendLoop());
        }
    }

    private async Task WaitForApiClient()
    {
        while (ApiClient.Instance == null)
            await Task.Yield();
    }

    public void SetHeldPipe(Pipe pipe)
    {
        currentHeldPipe = pipe;
    }

    public void ClearHeldPipe()
    {
        currentHeldPipe = null;
    }

    public void LogEvent(string eventType, Dictionary<string, object> context = null)
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
        {
            Debug.LogWarning("[ContextLogger] No active session. Event ignored.");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[ContextLogger] PlayerTransform not assigned.");
            return;
        }

        if (context == null)
            context = new Dictionary<string, object>();

        if (currentHeldPipe != null)
        {
            context["held_object_type"] = "pipe";
            context["held_pipe_type"] = currentHeldPipe.Type.ToString().ToLower();
            context["held_pipe_color"] = currentHeldPipe.Color.ToString().ToLower();
        }

        var playerState = new PlayerState
        {
            position = new Vector3Serializable(playerTransform.position),
            rotation = new Vector3Serializable(playerTransform.eulerAngles),
            forward = new Vector3Serializable(playerTransform.forward)
        };

        var playerEvent = new PlayerEvent
        {
            event_id = System.Guid.NewGuid().ToString(),
            session_id = SessionManager.Instance.CurrentSessionId,
            event_type = eventType,
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            player_state = playerState,
            context = context
        };

        eventBuffer.Add(playerEvent);

        if (eventType == EventType.PICK_OBJECT.ToApiString() ||
            eventType == EventType.PLACE_OBJECT.ToApiString() ||
            eventType == EventType.DROP_OBJECT.ToApiString())
        {
            _ = FlushEvents();
        }
        else if (eventBuffer.Count >= maxBatchSize && !isFlushing)
        {
            _ = FlushEvents();
        }

        if (eventType != EventType.INTERACT_ATTEMPT.ToApiString())
        {
            Debug.Log($"[Event] {eventType} | Buffer: {eventBuffer.Count}");
        }
    }

    public void LogEvent(EventType eventType, Dictionary<string, object> context = null)
    {
        LogEvent(eventType.ToApiString(), context);
    }

    private IEnumerator SendLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(sendInterval);

            if (eventBuffer.Count > 0 && !isFlushing)
            {
                _ = FlushEvents();
            }
        }
    }

    private async Task FlushEvents()
    {
        if (isFlushing)
            return;

        if (eventBuffer.Count == 0)
            return;

        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
            return;

        isFlushing = true;

        List<PlayerEvent> batch = null;

        try
        {
            var sessionId = SessionManager.Instance.CurrentSessionId;

            batch = new List<PlayerEvent>(eventBuffer);
            eventBuffer.Clear();

            await eventsApi.SendEvents(sessionId, batch);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContextLogger] Failed to send events: {ex.Message}");

            if (batch != null)
            {
                eventBuffer.AddRange(batch);
            }
        }
        finally
        {
            isFlushing = false;
        }
    }

    private async void OnApplicationQuit()
    {
        await FlushEvents();
    }

    public void Clear()
    {
        eventBuffer.Clear();
        currentHeldPipe = null;

        Debug.Log("[ContextLogger] Buffer cleared.");
    }
}