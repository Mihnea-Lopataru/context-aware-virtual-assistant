using System;
using System.Collections.Generic;

[Serializable]
public class PlayerEvent
{
    public string event_id;
    public int? session_id;

    public string event_type;

    public long? timestamp;

    public PlayerState player_state;

    public Dictionary<string, object> context;
}