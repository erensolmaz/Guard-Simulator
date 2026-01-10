using UnityEngine;

namespace GuardSimulator.UI
{
    public class FloatingImageEffect : MonoBehaviour
    {
        [Header("Float Settings")]
        [Tooltip("Yukarı-aşağı hareket mesafesi")]
        [SerializeField] private float floatAmplitude = 10f;
        
        [Tooltip("Hareket hızı (yüksek = daha hızlı)")]
        [SerializeField] private float floatSpeed = 1f;
        
        [Tooltip("Başlangıç offset (farklı objeler için farklı fazlar)")]
        [SerializeField] private float phaseOffset = 0f;
        
        [Header("Rotation Settings")]
        [Tooltip("Rotasyon efekti aktif")]
        [SerializeField] private bool enableRotation = false;
        
        [Tooltip("Rotasyon genliği (derece)")]
        [SerializeField] private float rotationAmplitude = 5f;
        
        [Tooltip("Rotasyon hızı")]
        [SerializeField] private float rotationSpeed = 1.5f;
        
        [Header("Scale Settings")]
        [Tooltip("Ölçekleme efekti aktif")]
        [SerializeField] private bool enableScale = false;
        
        [Tooltip("Ölçekleme genliği (0.1 = %10 değişim)")]
        [SerializeField] private float scaleAmplitude = 0.05f;
        
        [Tooltip("Ölçekleme hızı")]
        [SerializeField] private float scaleSpeed = 2f;
        
        [Header("Smoothness")]
        [Tooltip("Hareket eğrisi tipi")]
        [SerializeField] private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Tooltip("Canvas enable olunca otomatik başlat")]
        [SerializeField] private bool autoStart = true;
        
        [Tooltip("Başlangıç gecikmesi (saniye)")]
        [SerializeField] private float startDelay = 0f;
        
        private RectTransform rectTransform;
        private Vector2 startPosition;
        private Vector3 startRotation;
        private Vector3 startScale;
        private float elapsedTime = 0f;
        private bool isAnimating = false;
        private bool isInitialized = false;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            if (rectTransform == null)
            {
                Debug.LogError("FloatingImageEffect: RectTransform bulunamadı!");
                enabled = false;
                return;
            }
        }
        
        private void OnEnable()
        {
            if (autoStart)
            {
                if (!isInitialized)
                {
                    InitializePositions();
                }
                
                if (startDelay > 0)
                {
                    Invoke(nameof(StartAnimation), startDelay);
                }
                else
                {
                    StartAnimation();
                }
            }
        }
        
        private void OnDisable()
        {
            isAnimating = false;
            CancelInvoke();
        }
        
        private void InitializePositions()
        {
            if (rectTransform != null)
            {
                startPosition = rectTransform.anchoredPosition;
                startRotation = rectTransform.localEulerAngles;
                startScale = rectTransform.localScale;
                isInitialized = true;
            }
        }
        
        public void StartAnimation()
        {
            if (!isInitialized)
            {
                InitializePositions();
            }
            
            isAnimating = true;
            elapsedTime = phaseOffset;
        }
        
        public void StopAnimation()
        {
            isAnimating = false;
        }
        
        public void ResetToStart()
        {
            if (rectTransform != null && isInitialized)
            {
                rectTransform.anchoredPosition = startPosition;
                rectTransform.localEulerAngles = startRotation;
                rectTransform.localScale = startScale;
            }
        }
        
        private void Update()
        {
            if (!isAnimating || rectTransform == null)
            {
                return;
            }
            
            elapsedTime += Time.unscaledDeltaTime;
            
            Vector2 newPosition = startPosition;
            newPosition.y += Mathf.Sin(elapsedTime * floatSpeed) * floatAmplitude * floatCurve.Evaluate((Mathf.Sin(elapsedTime * floatSpeed) + 1f) / 2f);
            rectTransform.anchoredPosition = newPosition;
            
            if (enableRotation)
            {
                Vector3 newRotation = startRotation;
                newRotation.z = startRotation.z + Mathf.Sin(elapsedTime * rotationSpeed) * rotationAmplitude;
                rectTransform.localEulerAngles = newRotation;
            }
            
            if (enableScale)
            {
                Vector3 newScale = startScale;
                float scaleFactor = 1f + Mathf.Sin(elapsedTime * scaleSpeed) * scaleAmplitude;
                newScale = startScale * scaleFactor;
                rectTransform.localScale = newScale;
            }
        }
    }
}
