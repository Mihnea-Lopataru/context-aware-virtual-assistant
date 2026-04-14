using System.Threading.Tasks;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    private SessionApi sessionApi;

    public SessionResponse CurrentSession { get; private set; }

    public int CurrentSessionId => CurrentSession?.Id ?? -1;

    public bool HasActiveSession => CurrentSession != null;

    [Header("Settings")]
    [SerializeField] private float heartbeatInterval = 15f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        sessionApi = new SessionApi();

        // 🔥 start heartbeat loop
        InvokeRepeating(nameof(SendHeartbeat), heartbeatInterval, heartbeatInterval);
    }

    public async Task<SessionResponse> StartSession()
    {
        var user = UserManager.Instance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No user selected. Cannot start session.");
            return null;
        }

        var session = await sessionApi.StartSession(user.Id);

        CurrentSession = session;

        Debug.Log($"[Session] Started: {session.Id}");

        return session;
    }

    public async Task EndSession()
    {
        if (CurrentSession == null)
            return;

        await sessionApi.EndSession(CurrentSession.Id);

        Debug.Log($"[Session] Ended: {CurrentSession.Id}");

        CurrentSession = null;
    }

    private async void SendHeartbeat()
    {
        if (!HasActiveSession)
            return;

        try
        {
            await sessionApi.UpdateSession(CurrentSessionId);
            Debug.Log("[Session] Heartbeat sent.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Session] Heartbeat failed: {ex.Message}");
        }
    }

    private async void OnApplicationQuit()
    {
        if (CurrentSession != null)
        {
            await EndSession();
        }
    }
}