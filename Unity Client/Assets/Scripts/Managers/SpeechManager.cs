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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
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