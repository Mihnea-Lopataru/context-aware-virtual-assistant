using System;
using System.Threading.Tasks;
using UnityEngine;

public class SessionApi
{
    // =========================
    // START SESSION
    // =========================
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

    // =========================
    // UPDATE SESSION (ACTIVITY)
    // =========================
    public async Task<SessionResponse> UpdateSession(int sessionId)
    {
        var request = new SessionUpdateRequest
        {
            LastActivityAt = DateTime.UtcNow.ToString("o")
        };

        return await ApiClient.Instance.Patch<SessionResponse>(
            $"/sessions/{sessionId}",
            request
        );
    }

    // =========================
    // END SESSION
    // =========================
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