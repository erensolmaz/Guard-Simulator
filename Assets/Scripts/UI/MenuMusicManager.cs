using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField][Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loop = true;
    
    [Header("Fade Settings")]
    [SerializeField] private bool fadeIn = true;
    [SerializeField] private float fadeInDuration = 2f;

    private AudioSource audioSource;
    private float targetVolume;
    private float currentFadeTime = 0f;
    private bool isFading = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupAudioSource();
        
        if (playOnAwake)
        {
            PlayMusic();
        }
    }

    private void Update()
    {
        if (isFading)
        {
            HandleFadeIn();
        }
    }

    private void SetupAudioSource()
    {
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        targetVolume = volume;
        
        if (fadeIn)
        {
            audioSource.volume = 0f;
        }
        else
        {
            audioSource.volume = volume;
        }
    }

    private void PlayMusic()
    {
        if (menuMusic != null)
        {
            audioSource.clip = menuMusic;
            audioSource.Play();
            
            if (fadeIn)
            {
                isFading = true;
                currentFadeTime = 0f;
            }
            
            Debug.Log($"Menu music started: {menuMusic.name}");
        }
        else
        {
            Debug.LogWarning("No menu music assigned to MenuMusicManager!");
        }
    }

    private void HandleFadeIn()
    {
        currentFadeTime += Time.deltaTime;
        float fadeProgress = Mathf.Clamp01(currentFadeTime / fadeInDuration);
        audioSource.volume = Mathf.Lerp(0f, targetVolume, fadeProgress);
        
        if (fadeProgress >= 1f)
        {
            isFading = false;
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        targetVolume = volume;
        
        if (!isFading)
        {
            audioSource.volume = volume;
        }
    }

    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void Pause()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public void Resume()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }
}
