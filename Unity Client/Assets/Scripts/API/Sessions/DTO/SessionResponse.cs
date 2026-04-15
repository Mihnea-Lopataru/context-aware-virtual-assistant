using System;

[Serializable]
public class SessionResponse
{
    public int id;
    public int user_id;

    public string status;

    public string started_at;
    public string last_activity_at;
    public string ended_at;

    public string current_scene;
    public string current_objective;
}