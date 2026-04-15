using Newtonsoft.Json;

[System.Serializable]
public class UserResponse
{
    [JsonProperty("id")]
    public int Id;

    [JsonProperty("username")]
    public string Username;

    [JsonProperty("is_active")]
    public bool IsActive;

    [JsonProperty("created_at")]
    public string CreatedAt;

    [JsonProperty("updated_at")]
    public string UpdatedAt;
}