using UnityEngine;
using TMPro;
using System.Collections;

namespace GuardSimulator.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Typewriter Settings")]
        [Tooltip("Karakter başına gecikme süresi (saniye)")]
        [SerializeField] private float characterDelay = 0.05f;
        
        [Tooltip("Başlangıç gecikmesi (saniye)")]
        [SerializeField] private float startDelay = 0f;
        
        [Tooltip("Her karakterde ses çal")]
        [SerializeField] private bool playSound = true;
        
        [Tooltip("Typewriter sesi")]
        [SerializeField] private AudioClip typeSound;
        
        [Tooltip("Ses volume")]
        [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.3f;
        
        [Tooltip("Noktalama işaretlerinde ekstra bekleme")]
        [SerializeField] private bool pauseAtPunctuation = true;
        
        [Tooltip("Noktalama gecikmesi (saniye)")]
        [SerializeField] private float punctuationDelay = 0.3f;
        
        [Tooltip("Noktalama karakterleri")]
        [SerializeField] private string punctuationMarks = ".,!?;:";
        
        [Tooltip("Canvas enable olunca otomatik başlat")]
        [SerializeField] private bool autoStart = true;
        
        [Header("Advanced Settings")]
        [Tooltip("Rich text tag'lerini atla")]
        [SerializeField] private bool skipRichTextTags = true;
        
        [Tooltip("Boşluklarda ses çalma")]
        [SerializeField] private bool skipSoundOnSpace = true;
        
        private TextMeshProUGUI textComponent;
        private string fullText;
        private AudioSource audioSource;
        private bool isTyping = false;
        
        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            
            if (playSound && typeSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.clip = typeSound;
                audioSource.volume = soundVolume;
            }
        }
        
        private void OnEnable()
        {
            if (autoStart && textComponent != null)
            {
                fullText = textComponent.text;
                textComponent.text = "";
                StartCoroutine(TypeText());
            }
        }
        
        private void OnDisable()
        {
            StopAllCoroutines();
            isTyping = false;
        }
        
        public void StartTyping()
        {
            if (!isTyping && textComponent != null)
            {
                fullText = textComponent.text;
                textComponent.text = "";
                StartCoroutine(TypeText());
            }
        }
        
        public void SkipToEnd()
        {
            if (isTyping)
            {
                StopAllCoroutines();
                textComponent.text = fullText;
                isTyping = false;
            }
        }
        
        private IEnumerator TypeText()
        {
            isTyping = true;
            
            if (startDelay > 0)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }
            
            int visibleCharCount = 0;
            
            while (visibleCharCount <= fullText.Length)
            {
                textComponent.text = fullText.Substring(0, visibleCharCount);
                
                if (visibleCharCount > 0 && visibleCharCount <= fullText.Length)
                {
                    char currentChar = fullText[visibleCharCount - 1];
                    
                    if (playSound && audioSource != null)
                    {
                        if (!(skipSoundOnSpace && char.IsWhiteSpace(currentChar)))
                        {
                            audioSource.pitch = Random.Range(0.95f, 1.05f);
                            audioSource.PlayOneShot(typeSound, soundVolume);
                        }
                    }
                    
                    if (pauseAtPunctuation && punctuationMarks.Contains(currentChar.ToString()))
                    {
                        yield return new WaitForSecondsRealtime(punctuationDelay);
                    }
                }
                
                visibleCharCount++;
                yield return new WaitForSecondsRealtime(characterDelay);
            }
            
            isTyping = false;
        }
    }
}
