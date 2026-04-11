using System;
using Newtonsoft.Json;

[Serializable]
public class SessionStartRequest
{
    [JsonProperty("user_id")]
    public int UserId;
}