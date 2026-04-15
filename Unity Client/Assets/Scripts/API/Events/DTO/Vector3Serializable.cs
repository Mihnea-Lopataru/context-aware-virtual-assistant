using System;

[Serializable]
public class Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public Vector3Serializable() { }

    public Vector3Serializable(UnityEngine.Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
}