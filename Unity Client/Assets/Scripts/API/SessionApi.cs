using System;
using System.Threading.Tasks;
using UnityEngine;

public class SessionApi
{
    public async Task<SessionResponse> StartSession(int userId)
    {
        var request = new SessionStartRequest
        {
            UserId = userId
        };

        return await ApiClient.Instance.Post<SessionResponse>(
            "/sessions/start",
            request
        );
    }

    public async Task<SessionResponse> UpdateSession(int sessionId)
    {
        return await ApiClient.Instance.Patch<SessionResponse>(
            $"/sessions/{sessionId}",
            new SessionUpdateRequest()
        );
    }

    public async Task EndSession(int sessionId)
    {
        var request = new SessionEndRequest
        {
            EndedAt = DateTime.UtcNow
        };

        await ApiClient.Instance.Post<object>(
            $"/sessions/{sessionId}/end",
            request
        );
    }
}