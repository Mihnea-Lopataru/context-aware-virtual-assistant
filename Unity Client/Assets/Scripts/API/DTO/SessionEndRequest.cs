using System;
using Newtonsoft.Json;

[Serializable]
public class SessionEndRequest
{
    [JsonProperty("ended_at")]
    public DateTime EndedAt;
}