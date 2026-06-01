using System;
using System.Threading.Tasks;
using UnityEngine;

public class VoiceInputManager : MonoBehaviour
{
    public static VoiceInputManager Instance { get; private set; }

    private SpeechApi speechApi;
    private bool isProcessingVoice = false;
    private int voiceOperationVersion = 0;

    public bool BlocksChatInput =>
        isProcessingVoice ||
        (VoiceRecorder.Instance != null && VoiceRecorder.Instance.IsRecording);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        speechApi = new SpeechApi();

        if (WakeWordListener.Instance != null)
        {
            WakeWordListener.Instance.OnWakeWordDetected += HandleWakeWord;
        }
        else
        {
            Debug.LogWarning("[VoiceInput] WakeWordListener not found. Manual voice trigger will still work.");
        }

        if (VoiceRecorder.Instance != null)
        {
            VoiceRecorder.Instance.OnRecordingFinished += HandleRecordingFinished;
        }
        else
        {
            Debug.LogWarning("[VoiceInput] VoiceRecorder not found at startup.");
        }
    }

    private void OnDestroy()
    {
        voiceOperationVersion++;

        if (Instance == this)
            Instance = null;

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
        if (!CanStartVoiceRecording())
            return;

        StartVoiceRecording();
    }

    private void HandleManualTrigger()
    {
        if (!Input.GetKeyDown(KeyCode.V))
            return;

        if (!CanStartVoiceRecording())
            return;

        StartVoiceRecording();
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
        Debug.Log("[VoiceInput] Voice recording started.");

        VoiceUI.Instance?.Show();
    }

    private bool CanStartVoiceRecording()
    {
        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return false;

        if (isProcessingVoice)
            return false;

        if (ChatInputUI.Instance != null && ChatInputUI.Instance.BlocksVoiceInput)
            return false;

        if (ChatManager.Instance == null ||
            !ChatManager.Instance.IsReady ||
            ChatManager.Instance.IsProcessing)
            return false;

        return true;
    }

    private async void HandleRecordingFinished(AudioClip clip)
    {
        int operationVersion = ++voiceOperationVersion;

        VoiceUI.Instance?.Hide();

        if (clip == null)
        {
            Debug.LogWarning("[VoiceInput] Recording finished without an AudioClip.");
            ResumeWakeListening(operationVersion);
            return;
        }

        try
        {
            isProcessingVoice = true;

            Debug.Log($"[VoiceInput] Processing recorded audio. ClipLength={clip.length:0.00}s, Frequency={clip.frequency}, Channels={clip.channels}");
            byte[] wavData = WavUtility.FromAudioClip(clip);

            var result = await speechApi.SpeechToText(wavData);

            if (!IsCurrentVoiceOperation(operationVersion))
            {
                Debug.Log("[VoiceInput] Ignoring stale STT response because voice input was cancelled or disabled.");
                return;
            }

            if (result == null || string.IsNullOrWhiteSpace(result.transcription))
            {
                Debug.LogWarning("[VoiceInput] STT returned an empty transcription.");
                ResumeWakeListening(operationVersion);
                return;
            }

            string text = result.transcription;
            Debug.Log($"[VoiceInput] STT transcription received. Length={text.Length}");

            if (ChatManager.Instance != null && ChatManager.Instance.IsReady)
            {
                await ChatManager.Instance.ProcessMessage(text);

                if (!IsCurrentVoiceOperation(operationVersion))
                {
                    Debug.Log("[VoiceInput] Voice chat finished after cancellation. Wake listening will stay stopped.");
                    return;
                }
            }
            else
            {
                Debug.LogError("[VoiceInput] ChatManager not available!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[VoiceInput] STT failed: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            if (IsCurrentVoiceOperation(operationVersion))
            {
                isProcessingVoice = false;
                ResumeWakeListening(operationVersion);
            }
        }
    }

    private void ResumeWakeListening()
    {
        ResumeWakeListening(voiceOperationVersion);
    }

    private void ResumeWakeListening(int operationVersion)
    {
        if (!IsCurrentVoiceOperation(operationVersion))
            return;

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return;

        if (ChatInputUI.Instance != null && ChatInputUI.Instance.BlocksVoiceInput)
            return;

        WakeWordListener.Instance?.StartListening();
    }

    public void ResumeWakeListeningIfAvailable()
    {
        ResumeWakeListening();
    }

    public void CancelVoiceInput()
    {
        voiceOperationVersion++;

        if (VoiceRecorder.Instance != null && VoiceRecorder.Instance.IsRecording)
            VoiceRecorder.Instance.CancelRecording();

        isProcessingVoice = false;
        VoiceUI.Instance?.Hide();
        WakeWordListener.Instance?.StopListening();
    }

    private bool IsCurrentVoiceOperation(int operationVersion)
    {
        return Instance == this &&
               isActiveAndEnabled &&
               operationVersion == voiceOperationVersion;
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
