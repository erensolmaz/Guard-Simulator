using UnityEngine;
using GuardSimulator.Gameplay;

namespace GuardSimulator.Gameplay
{
    public class CollectibleItem : MonoBehaviour
    {
        [Header("Item Settings")]
        [Tooltip("Eşya adı")]
        [SerializeField] private string itemName = "Item";
        
        [Tooltip("Eşya açıklaması")]
        [SerializeField] private string itemDescription = "";
        
        [Tooltip("Etkileşim mesafesi")]
        [SerializeField] private float interactionDistance = 3f;
        
        [Tooltip("Etkileşim mesajı")]
        [SerializeField] private string interactionPrompt = "Toplamak için E'ye bas";
        
        [Header("References")]
        [Tooltip("Oyuncunun transform'u (runtime'da bulunur)")]
        private Transform playerTransform;
        
        [Tooltip("Bu item için quest marker")]
        private QuestMarker questMarker;
        
        private bool isCollected = false;
        private bool isQuestActive = false;

        public string ItemName => itemName;
        public bool IsCollected => isCollected;
        public bool IsQuestActive => isQuestActive;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (isCollected || !isQuestActive || playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance <= interactionDistance)
            {
                ShowInteractionPrompt();
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CollectItem();
                }
            }
            else
            {
                HideInteractionPrompt();
            }
        }

        public void ActivateQuest()
        {
            if (isQuestActive) return;
            
            isQuestActive = true;
            
            CreateQuestMarker();
            
            Debug.Log($"[CollectibleItem] Quest aktif: {itemName}");
        }

        private void CreateQuestMarker()
        {
            if (questMarker != null) return;
            
            GameObject markerObject = new GameObject($"QuestMarker_{itemName}");
            questMarker = markerObject.AddComponent<QuestMarker>();
            questMarker.Initialize(QuestMarkerType.ArrestTarget, transform);
            
            Debug.Log($"[CollectibleItem] Marker oluşturuldu: {itemName}");
        }

        private void ShowInteractionPrompt()
        {
            if (QuestObjectiveUI.Instance != null)
            {
                QuestObjectiveUI.Instance.ShowInteractionHint(interactionPrompt);
            }
        }
        
        private void HideInteractionPrompt()
        {
            if (QuestObjectiveUI.Instance != null)
            {
                QuestObjectiveUI.Instance.HideInteractionHint();
            }
        }

        private void CollectItem()
        {
            if (isCollected) return;
            
            isCollected = true;
            
            HideInteractionPrompt();
            RemoveQuestMarker();
            
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.OnItemCollected(this);
            }
            
            gameObject.SetActive(false);
            
            Debug.Log($"[CollectibleItem] Item toplandı: {itemName}");
        }

        private void RemoveQuestMarker()
        {
            if (questMarker != null)
            {
                if (questMarker.gameObject != null)
                {
                    Destroy(questMarker.gameObject);
                }
                questMarker = null;
            }
        }

        private void OnDestroy()
        {
            RemoveQuestMarker();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
