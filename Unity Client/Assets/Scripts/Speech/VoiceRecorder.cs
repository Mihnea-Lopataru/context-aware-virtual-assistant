using System;
using UnityEngine;

public class VoiceRecorder : MonoBehaviour
{
    public static VoiceRecorder Instance { get; private set; }

    [Header("Recording Settings")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxRecordingLength = 10;

    [Header("Silence Detection")]
    [SerializeField] private float silenceThreshold = 0.01f;
    [SerializeField] private float silenceDuration = 2.0f;
    [SerializeField] private float minRecordingTime = 0.5f;

    private AudioClip recordingClip;
    private string microphoneDevice;

    private bool isRecording = false;
    private float silenceTimer = 0f;
    private float recordingTimer = 0f;

    private float currentVolume = 0f;

    public bool IsRecording => isRecording;

    public event Action<AudioClip> OnRecordingFinished;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
                Debug.Log($"[VoiceRecorder] Using mic: {microphoneDevice}");
            }
            else
            {
                Debug.LogError("[VoiceRecorder] No microphone detected!");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isRecording) return;

        recordingTimer += Time.deltaTime;

        UpdateVolume();
        DetectSilence();
    }

    public void StartRecording()
    {
        if (isRecording)
            return;

        if (string.IsNullOrEmpty(microphoneDevice))
        {
            Debug.LogError("[VoiceRecorder] No microphone available.");
            return;
        }

        recordingClip = Microphone.Start(
            microphoneDevice,
            false,
            maxRecordingLength,
            sampleRate
        );

        isRecording = true;
        silenceTimer = 0f;
        recordingTimer = 0f;
    }

    public void StopRecording()
    {
        if (!isRecording)
            return;

        int position = Microphone.GetPosition(microphoneDevice);

        Microphone.End(microphoneDevice);

        isRecording = false;

        if (position <= 0)
        {
            return;
        }

        float[] samples = new float[position * recordingClip.channels];
        recordingClip.GetData(samples, 0);

        AudioClip finalClip = AudioClip.Create(
            "VoiceRecording",
            position,
            recordingClip.channels,
            sampleRate,
            false
        );

        finalClip.SetData(samples, 0);

        OnRecordingFinished?.Invoke(finalClip);
    }

    private void UpdateVolume()
    {
        int micPosition = Microphone.GetPosition(microphoneDevice);

        if (micPosition < 256 || recordingClip == null)
            return;

        float[] samples = new float[256];

        int start = micPosition - 256;
        if (start < 0) return;

        recordingClip.GetData(samples, start);

        float volume = CalculateRMS(samples);

        currentVolume = Mathf.Lerp(currentVolume, volume, Time.deltaTime * 15f);
    }

    private void DetectSilence()
    {
        if (recordingTimer < minRecordingTime)
            return;

        if (currentVolume < silenceThreshold)
        {
            silenceTimer += Time.deltaTime;

            if (silenceTimer >= silenceDuration)
            {
                StopRecording();
            }
        }
        else
        {
            silenceTimer = 0f;
        }
    }

    private float CalculateRMS(float[] samples)
    {
        float sum = 0f;

        foreach (var s in samples)
        {
            sum += s * s;
        }

        return Mathf.Sqrt(sum / samples.Length);
    }

    public float GetCurrentVolume()
    {
        return currentVolume;
    }
}