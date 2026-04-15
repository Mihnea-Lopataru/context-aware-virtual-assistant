using System;

[Serializable]
public class SessionCreateRequest
{
    public int user_id;

    public string current_scene;
    public string current_objective;
}