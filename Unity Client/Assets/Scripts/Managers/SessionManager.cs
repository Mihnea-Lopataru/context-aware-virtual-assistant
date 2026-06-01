using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    private SessionApi sessionApi;

    public SessionResponse CurrentSession { get; private set; }

    public int CurrentSessionId => CurrentSession?.id ?? -1;
    public bool HasActiveSession => CurrentSession != null;

    [Header("Settings")]
    [SerializeField] private float heartbeatInterval = 15f;

    private Coroutine heartbeatCoroutine;
    private string currentScene;
    private string currentObjective;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SessionManager] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await WaitForApiClient();

        sessionApi = new SessionApi(ApiClient.Instance);
        Debug.Log("[SessionManager] Initialized.");
    }

    private async Task WaitForApiClient()
    {
        while (ApiClient.Instance == null)
            await Task.Yield();
    }

    public async Task<SessionResponse> StartSession(
        string currentScene = null,
        string currentObjective = null
    )
    {
        var user = UserManager.Instance?.CurrentUser;

        if (user == null)
        {
            Debug.LogError("[SessionManager] No user selected. Cannot start session.");
            return null;
        }

        this.currentScene = currentScene;
        this.currentObjective = currentObjective;

        try
        {
            Debug.Log($"[SessionManager] Starting session. UserId={user.Id}, Scene={currentScene ?? "<none>"}, Objective={currentObjective ?? "<none>"}");

            CurrentSession = await sessionApi.StartSession(
                user.Id,
                currentScene,
                currentObjective
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Failed to start session for user {user.Id}: {e.Message}");
            Debug.LogException(e);
            throw;
        }

        if (CurrentSession == null)
        {
            Debug.LogError("[SessionManager] Backend returned an empty session response.");
            return null;
        }

        StartHeartbeat();
        Debug.Log($"[SessionManager] Session started. SessionId={CurrentSessionId}");

        return CurrentSession;
    }

    public async Task EndSession()
    {
        if (CurrentSession == null)
            return;

        try
        {
            Debug.Log($"[SessionManager] Ending session {CurrentSession.id}.");
            await sessionApi.EndSession(CurrentSession.id);
            ContextLogger.Instance?.Clear();
            Debug.Log($"[SessionManager] Session ended: {CurrentSession.id}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Session] End failed: {e.Message}");
            Debug.LogException(e);
        }

        StopHeartbeat();

        CurrentSession = null;
    }

    private void StartHeartbeat()
    {
        if (heartbeatCoroutine != null)
            StopCoroutine(heartbeatCoroutine);

        heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
    }

    private void StopHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (HasActiveSession)
        {
            yield return new WaitForSeconds(heartbeatInterval);

            _ = SendHeartbeat();
        }
    }

    private async Task SendHeartbeat()
    {
        if (!HasActiveSession)
            return;

        try
        {
            await sessionApi.UpdateSession(
                CurrentSessionId,
                currentScene,
                currentObjective
            );

            Debug.Log($"[SessionManager] Heartbeat sent. SessionId={CurrentSessionId}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Session] Heartbeat failed: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    private void OnApplicationQuit()
    {
        if (CurrentSession != null)
        {
            try
            {
                Debug.Log($"[SessionManager] Application quit. Ending session {CurrentSession.id}.");
                sessionApi.EndSession(CurrentSession.id).Wait(2000);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SessionManager] Failed to end session during quit: {e.Message}");
            }
        }
    }
}
