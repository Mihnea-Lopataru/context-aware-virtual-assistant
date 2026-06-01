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
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
        {
            Debug.LogError("[HintService] No active session. Cannot request hint.");
            return null;
        }

        int sessionId = SessionManager.Instance.CurrentSessionId;

        var knowledge = PuzzleKnowledgeLoader.Instance;
        if (knowledge == null)
        {
            Debug.LogWarning("[HintService] Puzzle knowledge is not loaded. Hint request will continue without knowledge.");
        }

        return await hintApi.GenerateHint(sessionId, message, knowledge);
    }
}
