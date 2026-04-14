using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class EventBatchRequest
{
    [JsonProperty("session_id")]
    public int SessionId;

    [JsonProperty("events")]
    public List<PlayerEventDTO> Events;

    public EventBatchRequest(int sessionId, List<PlayerEventDTO> events)
    {
        SessionId = sessionId;
        Events = events;
    }
}