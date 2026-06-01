using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HintApi
{
    private readonly ApiClient client;

    public HintApi(ApiClient client)
    {
        this.client = client;
    }

    public async Task<HintResponse> GenerateHint(int sessionId, string message, object knowledge)
    {
        string provider = AIConfig.Instance != null
            ? AIConfig.Instance.GetProviderString()
            : "ollama";

        var request = new HintRequest
        {
            session_id = sessionId,
            message = message,
            knowledge = knowledge,
            provider = provider
        };

        try
        {
            Debug.Log($"[HintApi] Requesting hint. Provider={provider}, SessionId={sessionId}, MessageLength={message?.Length ?? 0}, HasKnowledge={knowledge != null}");

            var response = await client.Post<HintResponse>(
                "/hints",
                request
            );

            Debug.Log($"[HintApi] Hint received. Provider={provider}, HintPreview={Preview(response?.hint)}");

            return response;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HintApi] Failed to generate hint. Provider={provider}, SessionId={sessionId}: {ex.Message}");
            Debug.LogException(ex);
            throw;
        }
    }

    private static string Preview(string value, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "<empty>";

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}
