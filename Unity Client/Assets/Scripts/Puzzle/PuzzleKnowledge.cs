using System;
using System.Collections.Generic;

[Serializable]
public class PuzzleKnowledge
{
    public string puzzle_name;
    public string puzzle_type;
    public string goal;

    public List<string> systems;
    public List<PipeTypeDefinition> pipe_types;
    public List<string> rules;
    public List<string> visible_clues;
    public List<string> common_mistakes;
    public List<string> success_conditions;
    public List<PipeExample> examples;

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
    public string visual_shape;
    public string matching_gap;
    public string color_rule;
    public string current_puzzle_usage;
    public bool enabled;
}

[Serializable]
public class PipeExample
{
    public string pipe_type;
    public string correct_match;
    public string incorrect_match;
    public string reason;
}
