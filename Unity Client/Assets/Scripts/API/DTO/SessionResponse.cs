using System;
using Newtonsoft.Json;

[Serializable]
public class SessionResponse
{
    [JsonProperty("id")]
    public int Id;

    [JsonProperty("user_id")]
    public int UserId;

    [JsonProperty("status")]
    public string Status;

    [JsonProperty("started_at")]
    public DateTime StartedAt;

    [JsonProperty("ended_at")]
    public DateTime? EndedAt;

    [JsonProperty("last_activity_at")]
    public DateTime LastActivityAt;
}