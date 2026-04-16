using System.Threading.Tasks;
using UnityEngine;

public class HintServiceUnity
{
    private readonly HintApi hintApi;

    public HintServiceUnity(ApiClient client)
    {
        hintApi = new HintApi(client);
    }

    public async Task<HintResponse> RequestHint(string message)
    {
        if (!SessionManager.Instance.HasActiveSession)
        {
            Debug.LogError("No active session.");
            return null;
        }

        int sessionId = SessionManager.Instance.CurrentSessionId;

        var knowledge = PuzzleKnowledgeLoader.Instance;

        return await hintApi.GenerateHint(sessionId, message, knowledge);
    }
}