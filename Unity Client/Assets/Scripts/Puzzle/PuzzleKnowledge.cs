using System;
using System.Collections.Generic;

[Serializable]
public class PuzzleKnowledge
{
    public List<string> systems;
    public List<PipeTypeDefinition> pipe_types;
    public List<string> rules;
    public List<string> common_mistakes;
    public string goal;

    public PipeTypeDefinition GetPipeType(string type)
    {
        if (string.IsNullOrEmpty(type) || pipe_types == null)
            return null;

        return pipe_types.Find(p => p.type == type);
    }

    public bool IsPipeTypeEnabled(string type)
    {
        var pipe = GetPipeType(type);
        return pipe != null && pipe.enabled;
    }
}

[Serializable]
public class PipeTypeDefinition
{
    public string type;
    public int connections;
    public string description;
    public bool enabled;
}