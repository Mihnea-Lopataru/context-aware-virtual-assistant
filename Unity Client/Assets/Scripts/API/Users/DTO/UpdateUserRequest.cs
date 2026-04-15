using Newtonsoft.Json;

[System.Serializable]
public class UpdateUserRequest
{
    [JsonProperty("username")]
    public string Username;

    [JsonProperty("is_active")]
    public bool? IsActive;
}