using System;
using Newtonsoft.Json;

[Serializable]
public class UserCreateRequest
{
    [JsonProperty("username")]
    public string Username;
}