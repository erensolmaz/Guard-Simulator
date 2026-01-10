using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Background Music Settings")]
    [Tooltip("Oyunun genel müziği (loop olacak)")]
    [SerializeField] private AudioClip backgroundMusic;
    
    [Tooltip("Müzik ses seviyesi (0-1 arası)")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;
    
    [Tooltip("Oyun başlangıcında müziği otomatik oynat")]
    [SerializeField] private bool playOnStart = true;
    
    [Header("Rage Music Settings")]
    [Tooltip("NPC'ler saldırdığında çalacak müzik (loop olacak)")]
    [SerializeField] private AudioClip rageMusic;
    
    [Tooltip("Rage müzik ses seviyesi (0-1 arası)")]
    [Range(0f, 1f)]
    [SerializeField] private float rageMusicVolume = 0.5f;
    
    private AudioSource musicSource;
    private bool isRageMusicPlaying = false;
    private bool wasBackgroundMusicPlaying = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Music AudioSource oluştur
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (playOnStart && backgroundMusic != null)
        {
            PlayBackgroundMusic();
        }
    }
    
    /// <summary>
    /// Background müziği oynat
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null)
        {
            Debug.LogWarning("[SoundManager] Background music atanmamış!");
            return;
        }
        
        if (musicSource == null)
        {
            Debug.LogError("[SoundManager] Music AudioSource bulunamadı!");
            return;
        }
        
        // Eğer aynı müzik zaten çalıyorsa, tekrar başlatma
        if (musicSource.isPlaying && musicSource.clip == backgroundMusic)
        {
            return;
        }
        
        musicSource.clip = backgroundMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
        
        Debug.Log($"[SoundManager] Background music oynatılıyor: {backgroundMusic.name}");
    }
    
    /// <summary>
    /// Background müziği durdur
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("[SoundManager] Background music durduruldu.");
        }
    }
    
    /// <summary>
    /// Background müziği duraklat
    /// </summary>
    public void PauseBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
            Debug.Log("[SoundManager] Background music duraklatıldı.");
        }
    }
    
    /// <summary>
    /// Duraklatılmış müziği devam ettir
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
            Debug.Log("[SoundManager] Background music devam ettiriliyor.");
        }
    }
    
    /// <summary>
    /// Müzik ses seviyesini ayarla
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        
        Debug.Log($"[SoundManager] Music volume: {musicVolume}");
    }
    
    /// <summary>
    /// Müzik ses seviyesini al
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }
    
    /// <summary>
    /// Yeni bir background müzik ayarla ve oynat
    /// </summary>
    public void SetBackgroundMusic(AudioClip newMusic, bool playImmediately = true)
    {
        backgroundMusic = newMusic;
        
        if (playImmediately)
        {
            PlayBackgroundMusic();
        }
    }
    
    /// <summary>
    /// Müzik çalıyor mu kontrol et
    /// </summary>
    public bool IsMusicPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
    }
    
    /// <summary>
    /// Rage müziğini oynat (normal müziği durdurur)
    /// </summary>
    public void PlayRageMusic()
    {
        if (rageMusic == null)
        {
            Debug.LogWarning("[SoundManager] Rage music atanmamış!");
            return;
        }
        
        if (musicSource == null)
        {
            Debug.LogError("[SoundManager] Music AudioSource bulunamadı!");
            return;
        }
        
        // Eğer rage müziği zaten çalıyorsa, tekrar başlatma
        if (isRageMusicPlaying && musicSource.isPlaying && musicSource.clip == rageMusic)
        {
            return;
        }
        
        // Normal müziğin çalıp çalmadığını kaydet
        wasBackgroundMusicPlaying = musicSource.isPlaying && musicSource.clip == backgroundMusic;
        
        // Normal müziği durdur
        musicSource.Stop();
        
        // Rage müziğini oynat
        musicSource.clip = rageMusic;
        musicSource.volume = rageMusicVolume;
        musicSource.Play();
        isRageMusicPlaying = true;
        
        Debug.Log($"[SoundManager] Rage music oynatılıyor: {rageMusic.name}");
    }
    
    /// <summary>
    /// Rage müziğini durdur ve normal müziğe geri dön
    /// </summary>
    public void StopRageMusic()
    {
        if (!isRageMusicPlaying)
        {
            return;
        }
        
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        
        isRageMusicPlaying = false;
        
        // Eğer normal müzik çalıyorsa, tekrar başlat
        if (wasBackgroundMusicPlaying && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
            Debug.Log("[SoundManager] Normal background music'e geri dönüldü.");
        }
        else
        {
            Debug.Log("[SoundManager] Rage music durduruldu.");
        }
    }
    
    /// <summary>
    /// Rage müziği çalıyor mu kontrol et
    /// </summary>
    public bool IsRageMusicPlaying()
    {
        return isRageMusicPlaying;
    }
    
    /// <summary>
    /// Rage müzik ses seviyesini ayarla
    /// </summary>
    public void SetRageMusicVolume(float volume)
    {
        rageMusicVolume = Mathf.Clamp01(volume);
        
        if (musicSource != null && isRageMusicPlaying)
        {
            musicSource.volume = rageMusicVolume;
        }
        
        Debug.Log($"[SoundManager] Rage music volume: {rageMusicVolume}");
    }
}

