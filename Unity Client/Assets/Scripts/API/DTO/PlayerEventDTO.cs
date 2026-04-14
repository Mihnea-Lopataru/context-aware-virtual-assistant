using System;
using Newtonsoft.Json;

[Serializable]
public class PlayerEventDTO
{
    [JsonProperty("event_id")]
    public string EventId;

    [JsonProperty("session_id")]
    public int SessionId;

    [JsonProperty("event_type")]
    public string EventType;

    [JsonProperty("timestamp")]
    public string Timestamp;

    [JsonProperty("player")]
    public PlayerStateDTO Player;

    [JsonProperty("context")]
    public EventContextDTO Context;

    public static PlayerEventDTO Create(
        int sessionId,
        string eventType,
        PlayerStateDTO player,
        EventContextDTO context
    )
    {
        return new PlayerEventDTO
        {
            EventId = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            EventType = eventType,
            Timestamp = DateTime.UtcNow.ToString("o"),
            Player = player,
            Context = context
        };
    }
}