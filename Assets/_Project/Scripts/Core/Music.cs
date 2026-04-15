using UnityEngine.Audio;
using UnityEngine;


public class Music : MonoBehaviour
{
    public static Music Instance { get; private set; }
    public AudioSource audioSource;

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
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.UnPause();
    }

    public bool isPlaying
    {
        get { return audioSource != null && audioSource.isPlaying; }
    }
}
