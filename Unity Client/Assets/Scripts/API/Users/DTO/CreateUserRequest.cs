using Newtonsoft.Json;

[System.Serializable]
public class CreateUserRequest
{
    [JsonProperty("username")]
    public string Username;
}