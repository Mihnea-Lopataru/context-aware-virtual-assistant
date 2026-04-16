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
            var response = await client.Post<HintResponse>(
                "/hints",
                request
            );

            Debug.Log($"[HintApi] ({provider}) Hint: {response?.hint}");

            return response;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HintApi] Failed to generate hint: {ex.Message}");
            throw;
        }
    }
}