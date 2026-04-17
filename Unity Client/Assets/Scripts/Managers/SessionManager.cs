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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await WaitForApiClient();

        sessionApi = new SessionApi(ApiClient.Instance);
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
            Debug.LogError("No user selected. Cannot start session.");
            return null;
        }

        this.currentScene = currentScene;
        this.currentObjective = currentObjective;

        CurrentSession = await sessionApi.StartSession(
            user.Id,
            currentScene,
            currentObjective
        );

        StartHeartbeat();

        return CurrentSession;
    }

    public async Task EndSession()
    {
        if (CurrentSession == null)
            return;

        try
        {
            await sessionApi.EndSession(CurrentSession.id);
            ContextLogger.Instance?.Clear();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Session] End failed: {e.Message}");
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
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Session] Heartbeat failed: {ex.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        if (CurrentSession != null)
        {
            try
            {
                sessionApi.EndSession(CurrentSession.id).Wait(2000);
            }
            catch { }
        }
    }
}