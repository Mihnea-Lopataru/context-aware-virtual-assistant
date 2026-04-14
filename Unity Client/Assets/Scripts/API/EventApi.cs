using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EventApi
{
    public async Task SendEvents(int sessionId, List<PlayerEventDTO> events)
    {
        if (events == null || events.Count == 0)
            return;

        var request = new EventBatchRequest(sessionId, events);

        try
        {
            await ApiClient.Instance.Post<object>(
                "/events",
                request
            );

            Debug.Log($"[EventApi] Sent {events.Count} events.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EventApi] Failed to send events: {ex.Message}");
        }
    }
}