using System.Threading.Tasks;

public class SessionApi
{
    private readonly ApiClient client;

    public SessionApi(ApiClient client)
    {
        this.client = client;
    }

    public async Task<SessionResponse> StartSession(
        int userId,
        string currentScene = null,
        string currentObjective = null
    )
    {
        var request = new SessionCreateRequest
        {
            user_id = userId,
            current_scene = currentScene,
            current_objective = currentObjective
        };

        return await client.Post<SessionResponse>(
            "/sessions/start",
            request
        );
    }

    public async Task<SessionResponse> GetActiveSession(int userId)
    {
        return await client.Get<SessionResponse>(
            $"/sessions/active/{userId}"
        );
    }

    public async Task<SessionResponse> UpdateSession(
        int sessionId,
        string currentScene = null,
        string currentObjective = null
    )
    {
        var request = new SessionUpdateRequest
        {
            current_scene = currentScene,
            current_objective = currentObjective
        };

        return await client.Patch<SessionResponse>(
            $"/sessions/{sessionId}",
            request
        );
    }

    public async Task<SessionResponse> EndSession(int sessionId)
    {
        return await client.Post<SessionResponse>(
            $"/sessions/{sessionId}/end",
            null
        );
    }
}