using UnityEngine;
using TMPro;
using System.Collections;

namespace GuardSimulator.Gameplay
{
    public class QuestObjectiveUI : MonoBehaviour
    {
        public static QuestObjectiveUI Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("Objective text (örn: 'Gizli uyuşturucuları bul')")]
        [SerializeField] private TextMeshProUGUI objectiveText;
        
        [Tooltip("Interaction hint text (örn: 'Toplamak için E'ye bas')")]
        [SerializeField] private TextMeshProUGUI interactionHintText;
        
        [Tooltip("Objective panel (görev metni için)")]
        [SerializeField] private GameObject objectivePanel;
        
        [Tooltip("Interaction panel (etkileşim metni için)")]
        [SerializeField] private GameObject interactionPanel;
        
        [Tooltip("Notification panel (bildirim metni için)")]
        [SerializeField] private GameObject notificationPanel;
        
        [Tooltip("Notification text (örn: 'Kanıt toplandı!')")]
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("Settings")]
        [Tooltip("Objective font boyutu")]
        [SerializeField] private float objectiveFontSize = 32f;
        
        [Tooltip("Interaction hint font boyutu")]
        [SerializeField] private float interactionHintFontSize = 24f;
        
        [Tooltip("Notification font boyutu")]
        [SerializeField] private float notificationFontSize = 28f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            HideObjective();
            HideInteractionHint();
            HideNotification();
            
            if (objectiveText != null)
            {
                objectiveText.fontSize = objectiveFontSize;
            }
            
            if (interactionHintText != null)
            {
                interactionHintText.fontSize = interactionHintFontSize;
            }
            
            if (notificationText != null)
            {
                notificationText.fontSize = notificationFontSize;
            }
        }

        public void ShowObjective(string text)
        {
            if (objectiveText != null)
            {
                objectiveText.text = text;
            }
            
            if (objectivePanel != null)
            {
                objectivePanel.SetActive(true);
            }
            
            Debug.Log($"[QuestObjectiveUI] Objective gösteriliyor: {text}");
        }

        public void HideObjective()
        {
            if (objectivePanel != null)
            {
                objectivePanel.SetActive(false);
            }
        }

        public void ShowInteractionHint(string text)
        {
            if (interactionHintText != null)
            {
                interactionHintText.text = text;
            }
            
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(true);
            }
        }

        public void HideInteractionHint()
        {
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
            }
        }

        public void ShowObjectiveWithDuration(string text, float duration)
        {
            ShowObjective(text);
            StartCoroutine(HideObjectiveAfterDelay(duration));
        }

        private IEnumerator HideObjectiveAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideObjective();
        }
        
        public void ShowNotification(string text, float duration = 2f)
        {
            if (notificationText != null)
            {
                notificationText.text = text;
            }
            
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(true);
            }
            
            Debug.Log($"[QuestObjectiveUI] Notification gösteriliyor: {text}");
            StartCoroutine(HideNotificationAfterDelay(duration));
        }
        
        public void HideNotification()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
        }
        
        private IEnumerator HideNotificationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideNotification();
        }
    }
}
