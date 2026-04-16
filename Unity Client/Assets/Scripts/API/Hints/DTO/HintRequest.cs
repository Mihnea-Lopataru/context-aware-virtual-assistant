using System;
using System.Collections.Generic;

[Serializable]
public class HintRequest
{
    public int session_id;

    public string message;

    public object knowledge;

    public string provider;
}