using System;
using System.Threading.Tasks;
using UnityEngine;

public class VoiceInputManager : MonoBehaviour
{
    public static VoiceInputManager Instance { get; private set; }

    private SpeechApi speechApi;
    private bool isProcessingVoice = false;

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

        if (VoiceRecorder.Instance != null)
        {
            VoiceRecorder.Instance.OnRecordingFinished += HandleRecordingFinished;
        }
    }

    private void OnDestroy()
    {
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
        VoiceUI.Instance?.Hide();

        if (clip == null)
        {
            ResumeWakeListening();
            return;
        }

        try
        {
            isProcessingVoice = true;

            byte[] wavData = WavUtility.FromAudioClip(clip);

            var result = await speechApi.SpeechToText(wavData);

            if (result == null || string.IsNullOrWhiteSpace(result.transcription))
            {
                ResumeWakeListening();
                return;
            }

            string text = result.transcription;

            if (ChatManager.Instance != null && ChatManager.Instance.IsReady)
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
            isProcessingVoice = false;
            ResumeWakeListening();
        }
    }

    private void ResumeWakeListening()
    {
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
        if (VoiceRecorder.Instance != null && VoiceRecorder.Instance.IsRecording)
            VoiceRecorder.Instance.CancelRecording();

        isProcessingVoice = false;
        VoiceUI.Instance?.Hide();
        WakeWordListener.Instance?.StopListening();
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
