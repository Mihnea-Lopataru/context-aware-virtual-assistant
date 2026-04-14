using System.Collections.Generic;
using UnityEngine;

public class ContextLogger : MonoBehaviour
{
    public static ContextLogger Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;

    private List<PlayerEventDTO> eventBuffer = new List<PlayerEventDTO>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ContextLogger instances detected!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void LogEvent(string eventType, EventContextDTO context)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("PlayerTransform not assigned in ContextLogger.");
            return;
        }

        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
        {
            Debug.LogWarning("No active session. Event ignored.");
            return;
        }

        int sessionId = SessionManager.Instance.CurrentSessionId;

        var playerState = PlayerStateDTO.FromTransform(playerTransform);

        var playerEvent = PlayerEventDTO.Create(
            sessionId,
            eventType,
            playerState,
            context
        );

        eventBuffer.Add(playerEvent);

        Debug.Log($"[Event] {eventType} | Success: {context?.Success}");
    }

    public List<PlayerEventDTO> ConsumeEvents()
    {
        if (eventBuffer.Count == 0)
            return null;

        var copy = new List<PlayerEventDTO>(eventBuffer);
        eventBuffer.Clear();

        return copy;
    }
}