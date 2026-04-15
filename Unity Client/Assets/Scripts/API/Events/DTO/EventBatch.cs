using System;
using System.Collections.Generic;

[Serializable]
public class EventBatch
{
    public int session_id;
    public List<PlayerEvent> events;
}