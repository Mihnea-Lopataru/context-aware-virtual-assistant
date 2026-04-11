using System;
using Newtonsoft.Json;

[Serializable]
public class SessionUpdateRequest
{
    [JsonProperty("last_activity_at")]
    public string LastActivityAt;

    // TO DO - EXTEND FOR CONTEXT
}