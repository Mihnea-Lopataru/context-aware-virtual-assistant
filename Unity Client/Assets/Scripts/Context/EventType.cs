public enum EventType
{
    INTERACT_ATTEMPT,
    PICK_OBJECT,
    PLACE_OBJECT,
    DROP_OBJECT,

    PUZZLE_STARTED,
    PUZZLE_COMPLETED,
    PUZZLE_PROGRESS,
    PUZZLE_ERROR
}

public static class EventTypeExtensions
{
    public static string ToApiString(this EventType type)
    {
        return type.ToString().ToLower();
    }
}