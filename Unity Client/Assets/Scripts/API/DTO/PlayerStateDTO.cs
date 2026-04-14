using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class PlayerStateDTO
{
    [JsonProperty("position")]
    public Vector3DTO Position;

    [JsonProperty("rotation")]
    public Vector3DTO Rotation;

    [JsonProperty("forward")]
    public Vector3DTO Forward;

    public static PlayerStateDTO FromTransform(Transform t)
    {
        return new PlayerStateDTO
        {
            Position = Vector3DTO.FromVector3(t.position),
            Rotation = Vector3DTO.FromVector3(t.eulerAngles),
            Forward = Vector3DTO.FromVector3(t.forward)
        };
    }
}

[Serializable]
public class Vector3DTO
{
    [JsonProperty("x")]
    public float X;

    [JsonProperty("y")]
    public float Y;

    [JsonProperty("z")]
    public float Z;

    public static Vector3DTO FromVector3(Vector3 v)
    {
        return new Vector3DTO
        {
            X = v.x,
            Y = v.y,
            Z = v.z
        };
    }
}