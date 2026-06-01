using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SpeechManager : MonoBehaviour
{
    public static SpeechManager Instance { get; private set; }

    private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] private bool playOnAwake = false;
    [SerializeField] private float volume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = playOnAwake;
            audioSource.volume = volume;
            Debug.Log($"[SpeechManager] Initialized. Volume={volume:0.##}");
        }
        else
        {
            Debug.LogWarning("[SpeechManager] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SpeechManager] Play called with null clip.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        Debug.Log($"[SpeechManager] Playing clip. Length={clip.length:0.00}s");
    }

    public void Stop()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }
}
