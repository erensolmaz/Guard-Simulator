using UnityEngine;
using System.Collections;

namespace GuardSimulator.Gameplay
{
    /// <summary>
    /// Görev işaretçisi - NPC ve araç üzerinde görsel işaretler gösterir (Particle Effect)
    /// </summary>
    public class QuestMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        [Tooltip("Marker tipi")]
        [SerializeField] private QuestMarkerType markerType = QuestMarkerType.ArrestTarget;

        [Header("Visual Settings")]
        [Tooltip("Marker rengi (NPC için - sarı)")]
        [SerializeField] private Color markerColor = new Color(1f, 0.9f, 0.2f, 1f); // Sarı

        [Tooltip("Marker rengi (Araç için - yeşil)")]
        [SerializeField] private Color deliveryMarkerColor = new Color(0f, 1f, 0f, 1f); // Yeşil

        [Tooltip("Ok yüksekliği (karakterin üstünde)")]
        [SerializeField] private float arrowHeight = 2.2f; // Karakterin üstünde daha yukarıda görünür konum

        [Tooltip("Kıpırdama hızı")]
        [SerializeField] private float floatSpeed = 2f;

        [Tooltip("Kıpırdama mesafesi")]
        [SerializeField] private float floatDistance = 0.3f;

        [Tooltip("Yanıp sönme hızı (araç için)")]
        [SerializeField] private float blinkSpeed = 2f;

        private GameObject arrowObject;
        private TMPro.TextMeshPro arrowText;
        private Transform targetTransform;
        private float floatOffset = 0f;
        private bool isBlinking = false;
        private float blinkTimer = 0f;

        /// <summary>
        /// Marker'ı initialize et
        /// </summary>
        public void Initialize(QuestMarkerType type, Transform target)
        {
            markerType = type;
            targetTransform = target;

            // Dikkat çekici marker sistemi oluştur
            CreateMarkerSystem();
        }

        private void CreateMarkerSystem()
        {
            // Marker GameObject'inin fiziksel etkisi olmaması için ayarlar
            gameObject.name = $"QuestMarker_{markerType}";
            
            Color targetColor = markerType == QuestMarkerType.ArrestTarget ? markerColor : deliveryMarkerColor;
            
            // Basit, parlak ok işareti oluştur
            CreateArrowMarker(targetColor);
            
            // Delivery tipi için yanıp sönme aktif
            if (markerType == QuestMarkerType.DeliveryTarget)
            {
                isBlinking = true;
            }
        }
        
        private void CreateArrowMarker(Color color)
        {
            // Ok işareti GameObject'i oluştur
            arrowObject = new GameObject("ArrowMarker");
            arrowObject.transform.SetParent(transform);
            arrowObject.transform.localPosition = Vector3.zero;
            
            // TextMeshPro ile küçük, parlak ok işareti (ters - aşağı bakıyor)
            arrowText = arrowObject.AddComponent<TMPro.TextMeshPro>();
            arrowText.text = "↓"; // Aşağı ok (ters)
            arrowText.fontSize = 32f; // Daha küçük
            arrowText.color = color;
            arrowText.alignment = TMPro.TextAlignmentOptions.Center;
            arrowText.fontStyle = TMPro.FontStyles.Bold;
            arrowText.sortingOrder = 100;
            arrowText.rectTransform.sizeDelta = new Vector2(1.0f, 1.0f); // Daha küçük boyut
            
            // Daha parıltılı glow efekti için outline ekle
            arrowText.outlineWidth = 0.5f;
            arrowText.outlineColor = color; // Aynı renk, daha parlak
        }
        

        private void Update()
        {
            if (targetTransform == null || arrowObject == null) return;

            // Karakterin üstüne konumlandır - daha doğru yükseklik hesaplama
            float baseHeight = targetTransform.position.y;
            
            // Collider varsa bounds kullan, yoksa sabit yükseklik ekle
            Collider targetCollider = targetTransform.GetComponent<Collider>();
            if (targetCollider != null)
            {
                Bounds bounds = targetCollider.bounds;
                // Collider'ın üst noktası ile transform pozisyonu arasındaki farkı hesapla
                float heightOffset = bounds.max.y - targetTransform.position.y;
                // Eğer heightOffset mantıklıysa (0 ile 3 arasında), kullan
                if (heightOffset > 0 && heightOffset < 3f)
                {
                    baseHeight = bounds.max.y;
                }
                else
                {
                    // Bounds garip ise, varsayılan yükseklik kullan (karakterin üstü)
                    baseHeight = targetTransform.position.y + 1.8f;
                }
            }
            else
            {
                // Collider yoksa, karakterin başının üstüne konumlandır (yaklaşık 1.8m yüksekliğe göre)
                baseHeight = targetTransform.position.y + 1.8f;
            }
            
            // Kıpırdama animasyonu
            floatOffset += Time.deltaTime * floatSpeed;
            float verticalOffset = Mathf.Sin(floatOffset) * floatDistance;
            
            // Final pozisyon: karakterin üstü + küçük offset + kıpırdama
            Vector3 finalPosition = new Vector3(targetTransform.position.x, baseHeight + arrowHeight + verticalOffset, targetTransform.position.z);
            
            transform.position = finalPosition;

            // Ok işaretini kameraya bakacak şekilde ayarla
            Camera mainCam = Camera.main;
            if (mainCam != null && arrowObject != null)
            {
                arrowObject.transform.LookAt(mainCam.transform);
                arrowObject.transform.Rotate(0, 180, 0);
            }
            
            // Yanıp sönme animasyonu (araç için)
            if (isBlinking && arrowText != null)
            {
                blinkTimer += Time.deltaTime * blinkSpeed;
                float alpha = (Mathf.Sin(blinkTimer) + 1f) * 0.5f;
                alpha = Mathf.Lerp(0.4f, 1f, alpha);
                
                Color currentColor = arrowText.color;
                currentColor.a = alpha;
                arrowText.color = currentColor;
            }
        }

        private void OnDestroy()
        {
            // Ok objesini temizle
            if (arrowObject != null)
            {
                Destroy(arrowObject);
            }
        }
    }

    /// <summary>
    /// Marker tipi
    /// </summary>
    public enum QuestMarkerType
    {
        ArrestTarget,    // Tutuklanacak NPC
        DeliveryTarget   // Teslim edilecek araç
    }
}
