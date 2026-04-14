using System;
using UnityEngine;

[Serializable]
public class PlayerEvent
{
    public string eventId;
    public string sessionId;

    public EventType eventType;
    public long timestamp;

    public PlayerState player;
    public EventContext context;

    public PlayerEvent(EventType type, PlayerState playerState, EventContext ctx)
    {
        eventId = Guid.NewGuid().ToString();
        sessionId = "";

        eventType = type;
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        player = playerState;
        context = ctx;
    }

    public PlayerEvent() { }
}

[Serializable]
public class PlayerState
{
    public Vector3Serializable position;
    public Vector3Serializable rotation;
    public Vector3Serializable forward;

    public static PlayerState FromTransform(Transform t)
    {
        return new PlayerState
        {
            position = Vector3Serializable.FromVector3(t.position),
            rotation = Vector3Serializable.FromVector3(t.eulerAngles),
            forward = Vector3Serializable.FromVector3(t.forward)
        };
    }
}

[Serializable]
public class EventContext
{
    public string objectId;
    public string objectType;

    public string pipeColor;
    public string pipeType;

    public string requiredColor;
    public string requiredType;

    public bool success;
}

[Serializable]
public class Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public static Vector3Serializable FromVector3(Vector3 v)
    {
        return new Vector3Serializable
        {
            x = v.x,
            y = v.y,
            z = v.z
        };
    }
}