using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class WakeWordListener : MonoBehaviour
{
    public static WakeWordListener Instance { get; private set; }

    [Header("Wake Word Settings")]
    [SerializeField] private string wakeWord = "hey nova";

    private KeywordRecognizer keywordRecognizer;

    public event Action OnWakeWordDetected;

    private bool isListening = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeRecognizer();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeRecognizer()
    {
        try
        {
            var keywords = new List<string> { wakeWord.ToLower() };

            keywordRecognizer = new KeywordRecognizer(keywords.ToArray());

            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
            keywordRecognizer.Start();

            isListening = true;
            Debug.Log($"[WakeWord] Listening for wake word: '{wakeWord}'");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WakeWord] Failed to initialize: {e.Message}");
            Debug.LogException(e);
        }
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (!isListening)
            return;

        string recognized = args.text.ToLower();

        if (recognized.Contains(wakeWord))
        {
            Debug.Log($"[WakeWord] Wake word detected. Confidence={args.confidence}, Text='{args.text}'");
            OnWakeWordDetected?.Invoke();
        }
    }

    public void StopListening()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            try
            {
                keywordRecognizer.Stop();
                isListening = false;
                Debug.Log("[WakeWord] Listening stopped.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WakeWord] Failed to stop listener: {e.Message}");
            }
        }
    }

    public void StartListening()
    {
        if (keywordRecognizer != null && !keywordRecognizer.IsRunning)
        {
            try
            {
                keywordRecognizer.Start();
                isListening = true;
                Debug.Log("[WakeWord] Listening started.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WakeWord] Failed to start listener: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        if (keywordRecognizer != null)
        {
            if (keywordRecognizer.IsRunning)
                keywordRecognizer.Stop();

            keywordRecognizer.OnPhraseRecognized -= OnPhraseRecognized;
            keywordRecognizer.Dispose();
        }
    }
}
