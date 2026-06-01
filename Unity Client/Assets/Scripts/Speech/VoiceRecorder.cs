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

        if (recordingTimer >= maxRecordingLength)
        {
            Debug.Log($"[VoiceRecorder] Max recording length reached ({maxRecordingLength}s). Stopping recording.");
            StopRecording();
            return;
        }

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

        try
        {
            recordingClip = Microphone.Start(
                microphoneDevice,
                false,
                maxRecordingLength,
                sampleRate
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceRecorder] Failed to start microphone '{microphoneDevice}': {e.Message}");
            Debug.LogException(e);
            recordingClip = null;
            return;
        }

        if (recordingClip == null)
        {
            Debug.LogError($"[VoiceRecorder] Microphone.Start returned null for device '{microphoneDevice}'.");
            return;
        }

        isRecording = true;
        silenceTimer = 0f;
        recordingTimer = 0f;
        currentVolume = 0f;

        Debug.Log($"[VoiceRecorder] Recording started. Device={microphoneDevice}, SampleRate={sampleRate}, MaxLength={maxRecordingLength}s");
    }

    public void StopRecording()
    {
        if (!isRecording)
            return;

        int position = Microphone.GetPosition(microphoneDevice);

        Microphone.End(microphoneDevice);

        isRecording = false;
        currentVolume = 0f;

        if (recordingClip == null)
        {
            Debug.LogWarning("[VoiceRecorder] Recording clip was null when stopping.");
            OnRecordingFinished?.Invoke(null);
            return;
        }

        if (position <= 0)
        {
            Debug.LogWarning($"[VoiceRecorder] Recording stopped with no captured samples. Position={position}");
            recordingClip = null;
            OnRecordingFinished?.Invoke(null);
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

        recordingClip = null;

        Debug.Log($"[VoiceRecorder] Recording stopped. Samples={position}, Duration={(float)position / sampleRate:0.00}s, Channels={finalClip.channels}");
        OnRecordingFinished?.Invoke(finalClip);
    }

    public void CancelRecording()
    {
        if (!isRecording)
            return;

        Microphone.End(microphoneDevice);

        isRecording = false;
        silenceTimer = 0f;
        recordingTimer = 0f;
        currentVolume = 0f;
        recordingClip = null;

        Debug.Log("[VoiceRecorder] Recording cancelled.");
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
                Debug.Log($"[VoiceRecorder] Silence detected for {silenceDuration:0.##}s. Stopping recording.");
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
