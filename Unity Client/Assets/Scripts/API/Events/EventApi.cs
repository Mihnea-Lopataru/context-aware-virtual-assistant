using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EventsApi
{
    private readonly ApiClient client;

    public EventsApi(ApiClient client)
    {
        this.client = client;
    }

    public async Task SendEvents(int sessionId, List<PlayerEvent> events)
    {
        if (events == null || events.Count == 0)
            return;

        var request = new EventBatch
        {
            session_id = sessionId,
            events = events
        };

        try
        {
            await client.Post<EventResponse>(
                "/events",
                request
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EventsApi] Failed to send events: {ex.Message}");
        }
    }
}