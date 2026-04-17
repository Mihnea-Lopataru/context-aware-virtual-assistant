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
        }
        catch (Exception e)
        {
            Debug.LogError($"[WakeWord] Failed to initialize: {e.Message}");
        }
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (!isListening)
            return;

        string recognized = args.text.ToLower();

        if (recognized.Contains(wakeWord))
        {
            OnWakeWordDetected?.Invoke();
        }
    }

    public void StopListening()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
            isListening = false;
        }
    }

    public void StartListening()
    {
        if (keywordRecognizer != null && !keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Start();
            isListening = true;
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