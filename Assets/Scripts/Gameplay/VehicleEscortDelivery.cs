using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using GuardSimulator.Character;
using Akila.FPSFramework.Animation;

namespace GuardSimulator.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class VehicleEscortDelivery : MonoBehaviour
    {
        [Header("Delivery Settings")]
        [Tooltip("Teslim etmek için basılacak tuş")]
        [SerializeField] private KeyCode deliveryKey = KeyCode.F;
        
        [Tooltip("Player layer mask")]
        [SerializeField] private LayerMask playerLayer = -1;
        
        [Header("Success Screen")]
        [Tooltip("Başarı ekranı Canvas (Scene'de mevcut olan)")]
        [SerializeField] private GameObject arrestedScreenCanvas;
        
        [Tooltip("Siyah ekran süresi (saniye)")]
        [SerializeField] private float blackScreenDuration = 10f;
        
        [Tooltip("Başarı sesi")]
        [SerializeField] private AudioClip successSound;
        
        [Header("Scene Transition")]
        [Tooltip("Geçilecek scene adı (boş bırakılırsa scene değişmez)")]
        [SerializeField] private string nextSceneName = "";
        
        private bool playerInZone = false;
        private PlayerEscortController escortController;
        private AudioSource audioSource;
        private bool deliveryInProgress = false;
        
        private void Start()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                Debug.LogWarning("VehicleEscortDelivery: Collider bulunamadı! Lütfen bir Collider ekleyin.");
            }
            
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            
            if (arrestedScreenCanvas != null)
            {
                arrestedScreenCanvas.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (playerInZone && !deliveryInProgress && escortController != null)
            {
                if (Input.GetKeyDown(deliveryKey))
                {
                    if (escortController.IsEscortingTarget && escortController.CurrentEscortTarget != null)
                    {
                        if (escortController.CurrentEscortTarget.IsArrested)
                        {
                            StartCoroutine(DeliverSuspect());
                        }
                    }
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                PlayerEscortController controller = other.GetComponent<PlayerEscortController>();
                if (controller != null)
                {
                    playerInZone = true;
                    escortController = controller;
                    Debug.Log($"VehicleEscortDelivery: Player alana girdi. Escort durumu: {controller.IsEscortingTarget}");
                }
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                playerInZone = false;
                escortController = null;
                Debug.Log("VehicleEscortDelivery: Player alandan çıktı.");
            }
        }
        
        private IEnumerator DeliverSuspect()
        {
            deliveryInProgress = true;
            
            NPC deliveredNPC = escortController.CurrentEscortTarget;
            
            Debug.Log("VehicleEscortDelivery: Şüpheli teslim ediliyor...");
            
            // Escort'u durdur
            if (escortController != null)
            {
                escortController.StopEscort();
            }
            
            // Görev tamamlama kontrolü
            Quest completedQuest = null;
            if (QuestSystem.Instance != null && deliveredNPC != null)
            {
                Quest quest = QuestSystem.Instance.GetQuestForNPC(deliveredNPC);
                if (quest != null)
                {
                    completedQuest = quest;
                    QuestSystem.Instance.CompleteQuest(quest);
                }
            }
            
            if (arrestedScreenCanvas != null)
            {
                arrestedScreenCanvas.SetActive(true);
            }
            else
            {
                Debug.LogWarning("VehicleEscortDelivery: Arrested Screen Canvas atanmamış!");
            }
            
            if (successSound != null && audioSource != null)
            {
                audioSource.clip = successSound;
                audioSource.Play();
            }
            
            Time.timeScale = 0f;
            
            yield return new WaitForSecondsRealtime(blackScreenDuration);
            
            Time.timeScale = 1f;
            
            if (arrestedScreenCanvas != null)
            {
                arrestedScreenCanvas.SetActive(false);
            }
            
            // Teslim edilen NPC'yi sahneden kaldır
            if (deliveredNPC != null)
            {
                Debug.Log($"VehicleEscortDelivery: NPC sahneden kaldırılıyor: {deliveredNPC.NPCName}");
                
                // ProceduralAnimator'ı durdur (NaN rotation hatasını önlemek için)
                ProceduralAnimator proceduralAnimator = deliveredNPC.GetComponent<ProceduralAnimator>();
                if (proceduralAnimator != null)
                {
                    proceduralAnimator.isActive = false;
                    proceduralAnimator.enabled = false;
                }
                
                // Tüm ProceduralAnimator component'lerini child'larda da durdur
                ProceduralAnimator[] allProceduralAnimators = deliveredNPC.GetComponentsInChildren<ProceduralAnimator>();
                foreach (ProceduralAnimator animator in allProceduralAnimators)
                {
                    if (animator != null)
                    {
                        animator.isActive = false;
                        animator.enabled = false;
                    }
                }
                
                Destroy(deliveredNPC.gameObject);
            }
            
            // Araç'ı sahneden kaldır
            if (gameObject != null)
            {
                Debug.Log("VehicleEscortDelivery: Araç sahneden kaldırılıyor.");
                Destroy(gameObject);
            }
            
            // Görev tamamlandıktan sonra 2. göreve geçiş kontrolü
            if (completedQuest != null && QuestSystem.Instance != null)
            {
                QuestSystem.Instance.OnQuestCompleted(completedQuest);
            }
            
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"VehicleEscortDelivery: Scene yükleniyor: {nextSceneName}");
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                deliveryInProgress = false;
                Debug.Log("VehicleEscortDelivery: Teslim tamamlandı.");
            }
        }
        
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = playerInZone ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(transform.position + col.bounds.center - transform.position, col.bounds.size);
            }
        }
    }
}
