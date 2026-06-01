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
    private bool missingSessionWarningShown = false;
    private bool missingPlayerWarningShown = false;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ContextLogger instances detected!");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        await WaitForApiClient();

        if (Instance != this)
            return;

        if (ApiClient.Instance == null)
        {
            Debug.LogError("[ContextLogger] ApiClient not initialized!");
            return;
        }

        eventsApi = new EventsApi(ApiClient.Instance);
        Debug.Log($"[ContextLogger] Initialized. SendInterval={sendInterval:0.##}s, MaxBatchSize={maxBatchSize}");
    }

    private void Start()
    {
        if (sendInterval <= 0f)
        {
            Debug.LogWarning($"[ContextLogger] Invalid send interval {sendInterval}. Using 5 seconds.");
            sendInterval = 5f;
        }

        if (maxBatchSize <= 0)
        {
            Debug.LogWarning($"[ContextLogger] Invalid max batch size {maxBatchSize}. Using 20.");
            maxBatchSize = 20;
        }

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
        if (string.IsNullOrWhiteSpace(eventType))
        {
            Debug.LogWarning("[ContextLogger] Event ignored because event type is empty.");
            return;
        }

        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
        {
            if (!missingSessionWarningShown)
            {
                Debug.LogWarning($"[ContextLogger] No active session. Event ignored: {eventType}");
                missingSessionWarningShown = true;
            }

            return;
        }

        missingSessionWarningShown = false;

        if (playerTransform == null)
        {
            if (!missingPlayerWarningShown)
            {
                Debug.LogWarning("[ContextLogger] PlayerTransform not assigned. Events cannot include player state.");
                missingPlayerWarningShown = true;
            }

            return;
        }

        missingPlayerWarningShown = false;

        if (context == null)
            context = new Dictionary<string, object>();

        if (SceneStateBuilder.Instance != null)
        {
            context["scene_state"] = SceneStateBuilder.Instance.BuildState();
        }

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

        if (eventType != EventType.LOOK_AT.ToApiString())
        {
            Debug.Log($"[ContextLogger] Queued event '{eventType}'. BufferSize={eventBuffer.Count}");
        }

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

    public async Task FlushEventsNow()
    {
        await FlushEvents();
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

            Debug.Log($"[ContextLogger] Flushing {batch.Count} event(s) for session {sessionId}.");
            await eventsApi.SendEvents(sessionId, batch);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContextLogger] Failed to send events. Re-queueing {batch?.Count ?? 0} event(s): {ex.Message}");
            Debug.LogException(ex);

            if (batch != null)
            {
                batch.AddRange(eventBuffer);
                eventBuffer.Clear();
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
        Debug.Log($"[ContextLogger] Application quitting. Flushing remaining events: {eventBuffer.Count}");
        await FlushEvents();
    }

    private void OnDestroy()
    {
        if (sendCoroutine != null)
        {
            StopCoroutine(sendCoroutine);
            sendCoroutine = null;
        }

        if (Instance == this)
            Instance = null;
    }

    public void Clear()
    {
        Debug.Log($"[ContextLogger] Clearing buffered events. Count={eventBuffer.Count}");
        eventBuffer.Clear();
        currentHeldPipe = null;
    }
}
