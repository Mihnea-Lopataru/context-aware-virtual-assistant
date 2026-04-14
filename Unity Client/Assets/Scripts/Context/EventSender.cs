using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventSender : MonoBehaviour
{
    public static EventSender Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float sendInterval = 3f;

    private EventApi eventApi = new EventApi();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(SendLoop());
    }

    private IEnumerator SendLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(sendInterval);

            TrySendEvents();
        }
    }

    private async void TrySendEvents()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
            return;

        int sessionId = SessionManager.Instance.CurrentSessionId;

        List<PlayerEventDTO> events = ContextLogger.Instance.ConsumeEvents();

        if (events == null || events.Count == 0)
            return;

        await eventApi.SendEvents(sessionId, events);
    }
}