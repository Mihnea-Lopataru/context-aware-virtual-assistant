using System;
using Newtonsoft.Json;

[Serializable]
public class EventContextDTO
{
    [JsonProperty("objectId")]
    public string ObjectId;

    [JsonProperty("objectType")]
    public string ObjectType;

    [JsonProperty("pipeColor")]
    public string PipeColor;

    [JsonProperty("pipeType")]
    public string PipeType;

    [JsonProperty("requiredColor")]
    public string RequiredColor;

    [JsonProperty("requiredType")]
    public string RequiredType;

    [JsonProperty("success")]
    public bool? Success;
}