using System.Threading.Tasks;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    private SessionApi sessionApi;

    public SessionResponse CurrentSession { get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        sessionApi = new SessionApi();
    }

    // =========================
    // START SESSION
    // =========================
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

        Debug.Log($"Session started: {session.Id}");

        return session;
    }

    // =========================
    // END SESSION
    // =========================
    public async Task EndSession()
    {
        if (CurrentSession == null)
            return;

        await sessionApi.EndSession(CurrentSession.Id);

        Debug.Log($"Session ended: {CurrentSession.Id}");

        CurrentSession = null;
    }

    private async void OnApplicationQuit()
    {
        if (CurrentSession != null)
        {
            await EndSession();
        }
    }
}