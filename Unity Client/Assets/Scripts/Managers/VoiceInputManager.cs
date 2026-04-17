using System;
using System.Threading.Tasks;
using UnityEngine;

public class VoiceInputManager : MonoBehaviour
{
    private SpeechApi speechApi;

    private void Start()
    {
        speechApi = new SpeechApi();

        if (WakeWordListener.Instance != null)
        {
            WakeWordListener.Instance.OnWakeWordDetected += HandleWakeWord;
        }

        if (VoiceRecorder.Instance != null)
        {
            VoiceRecorder.Instance.OnRecordingFinished += HandleRecordingFinished;
        }
    }

    private void OnDestroy()
    {
        if (WakeWordListener.Instance != null)
        {
            WakeWordListener.Instance.OnWakeWordDetected -= HandleWakeWord;
        }

        if (VoiceRecorder.Instance != null)
        {
            VoiceRecorder.Instance.OnRecordingFinished -= HandleRecordingFinished;
        }
    }

    private void Update()
    {
        HandleManualTrigger();
        UpdateMicVolumeUI();
    }

    private void HandleWakeWord()
    {
        StartVoiceRecording();
    }

    private void HandleManualTrigger()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            StartVoiceRecording();
        }
    }

    private void StartVoiceRecording()
    {
        if (VoiceRecorder.Instance == null)
        {
            Debug.LogError("[VoiceInput] VoiceRecorder not found!");
            return;
        }

        if (VoiceRecorder.Instance.IsRecording)
        {
            return;
        }

        WakeWordListener.Instance?.StopListening();

        VoiceRecorder.Instance.StartRecording();

        VoiceUI.Instance?.Show();
    }

    private async void HandleRecordingFinished(AudioClip clip)
    {
        VoiceUI.Instance?.Hide();

        if (clip == null)
        {
            ResumeWakeListening();
            return;
        }

        try
        {
            byte[] wavData = WavUtility.FromAudioClip(clip);

            var result = await speechApi.SpeechToText(wavData);

            if (result == null || string.IsNullOrWhiteSpace(result.transcription))
            {
                ResumeWakeListening();
                return;
            }

            string text = result.transcription;

            if (ChatManager.Instance != null)
            {
                await ChatManager.Instance.ProcessMessage(text);
            }
            else
            {
                Debug.LogError("[VoiceInput] ChatManager not available!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[VoiceInput] STT failed: " + e.Message);
        }
        finally
        {
            ResumeWakeListening();
        }
    }

    private void ResumeWakeListening()
    {
        WakeWordListener.Instance?.StartListening();
    }

    private void UpdateMicVolumeUI()
    {
        if (VoiceRecorder.Instance == null || !VoiceRecorder.Instance.IsRecording)
            return;

        float volume = VoiceRecorder.Instance.GetCurrentVolume();

        volume *= 10f;

        VoiceUI.Instance?.UpdateVolume(volume);
    }
}