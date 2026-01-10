using UnityEngine;
using System.Collections.Generic;
using GuardSimulator.Character;

namespace GuardSimulator.Gameplay
{
    /// <summary>
    /// Görev verisi - Tutuklama görevleri için
    /// </summary>
    public class Quest
    {
        [Header("Quest Info")]
        [Tooltip("Görev adı")]
        public string questName = "Yeni Görev";
        
        [Tooltip("Görev açıklaması")]
        [TextArea(2, 4)]
        public string questDescription = "";

        [Header("Quest Target")]
        [Tooltip("Tutuklanacak NPC (runtime'da atanır)")]
        [SerializeField] private NPC targetNPC;
        
        [Tooltip("Teslim edilecek araç (runtime'da atanır)")]
        [SerializeField] private VehicleEscortDelivery deliveryVehicle;
        
        [Tooltip("Öldürülecek botlar listesi (2. görev için)")]
        [SerializeField] private List<NPC> targetBots = new List<NPC>();
        
        [Tooltip("2. görev tamamlandığında konuşulacak NPC (quest giver)")]
        [SerializeField] private NPC completionNPC;
        
        [Tooltip("Toplanacak itemler listesi (collect quest için)")]
        [SerializeField] private List<CollectibleItem> targetItems = new List<CollectibleItem>();

        [Header("Quest State")]
        [Tooltip("Görev durumu")]
        [SerializeField] private QuestState questState = QuestState.NotStarted;

        // Quest Marker referansları
        private QuestMarker npcMarker;
        private QuestMarker vehicleMarker;
        private List<QuestMarker> botMarkers = new List<QuestMarker>();
        private Dictionary<NPC, QuestMarker> botMarkerMap = new Dictionary<NPC, QuestMarker>(); // Bot -> Marker mapping
        
        // 2. görev için: Öldürülen botlar listesi
        private List<NPC> killedBots = new List<NPC>();
        
        // Collect quest için: Toplanan itemler listesi
        private List<CollectibleItem> collectedItems = new List<CollectibleItem>();

        public QuestState State => questState;
        public NPC TargetNPC => targetNPC;
        public VehicleEscortDelivery DeliveryVehicle => deliveryVehicle;
        public List<NPC> TargetBots => targetBots;
        public NPC CompletionNPC => completionNPC;
        public List<CollectibleItem> TargetItems => targetItems;

        /// <summary>
        /// Tutuklama görevi için initialize et
        /// </summary>
        public void InitializeArrestQuest(NPC npc, VehicleEscortDelivery vehicle)
        {
            targetNPC = npc;
            deliveryVehicle = vehicle;
            questName = $"Tutukla: {npc.NPCName}";
            questDescription = $"{npc.NPCName} adlı şüpheliyi tutukla ve arabaya teslim et.";
            questState = QuestState.NotStarted;
            targetBots = null;
            completionNPC = null;
            killedBots = new List<NPC>();
        }
        
        /// <summary>
        /// Bot öldürme görevi için initialize et
        /// </summary>
        public void InitializeBotKillQuest(List<NPC> bots, NPC completionNPC)
        {
            targetNPC = null;
            deliveryVehicle = null;
            targetBots = bots != null ? new List<NPC>(bots) : new List<NPC>();
            this.completionNPC = completionNPC;
            questName = "Bot Öldür";
            questDescription = $"Tüm botları öldür ve görev veren NPC'ye geri dön.";
            questState = QuestState.NotStarted;
            killedBots = new List<NPC>();
        }
        
        /// <summary>
        /// Item toplama görevi için initialize et
        /// </summary>
        public void InitializeCollectQuest(List<CollectibleItem> items, string questName, string questDescription, NPC completionNPC = null)
        {
            targetNPC = null;
            deliveryVehicle = null;
            targetBots = null;
            this.completionNPC = completionNPC;
            targetItems = items != null ? new List<CollectibleItem>(items) : new List<CollectibleItem>();
            this.questName = questName;
            this.questDescription = questDescription;
            questState = QuestState.NotStarted;
            collectedItems = new List<CollectibleItem>();
        }

        /// <summary>
        /// Görevi başlat
        /// </summary>
        public void StartQuest()
        {
            if (questState != QuestState.NotStarted) return;

            questState = QuestState.InProgress;

            // Tutuklama görevi ise
            if (targetNPC != null)
            {
                // DialogueNPC'nin oluşturduğu ilk görev marker'ını kaldır (varsa)
                RemoveFirstQuestMarkerForNPC(targetNPC);
                
                // NPC üzerinde marker oluştur (ayrı GameObject olarak)
                GameObject markerObject = new GameObject($"QuestMarker_{targetNPC.NPCName}");
                npcMarker = markerObject.AddComponent<QuestMarker>();
                npcMarker.Initialize(QuestMarkerType.ArrestTarget, targetNPC.transform);
                // NPC'ye parent yapma - sadece görsel takip
            }
            // Bot öldürme görevi ise
            else if (targetBots != null && targetBots.Count > 0)
            {
                // Her bot üzerinde marker oluştur
                foreach (NPC bot in targetBots)
                {
                    if (bot != null)
                    {
                        GameObject markerObject = new GameObject($"QuestMarker_Bot_{bot.NPCName}");
                        QuestMarker marker = markerObject.AddComponent<QuestMarker>();
                        marker.Initialize(QuestMarkerType.ArrestTarget, bot.transform);
                        botMarkers.Add(marker);
                        botMarkerMap[bot] = marker; // Bot -> Marker mapping
                    }
                }
            }
            // Item toplama görevi ise
            else if (targetItems != null && targetItems.Count > 0)
            {
                // Her item için quest'i aktifleştir (marker otomatik oluşturulacak)
                foreach (CollectibleItem item in targetItems)
                {
                    if (item != null)
                    {
                        item.ActivateQuest();
                    }
                }
                
                // Objective UI'ı göster
                if (QuestObjectiveUI.Instance != null)
                {
                    QuestObjectiveUI.Instance.ShowObjective(questDescription);
                }
            }

            Debug.Log($"[Quest] Görev başlatıldı: {questName}");
        }

        /// <summary>
        /// Görevi güncelle (NPC tutuklandı mı veya botlar öldürüldü mü kontrol et)
        /// </summary>
        public void UpdateQuest()
        {
            if (questState != QuestState.InProgress) return;

            // Tutuklama görevi: NPC tutuklandı mı kontrol et
            if (targetNPC != null && targetNPC.IsArrested)
            {
                OnNPCArrested();
            }
            
            // Bot öldürme görevi: Tüm botlar öldürüldü mü kontrol et
            if (targetBots != null && targetBots.Count > 0)
            {
                bool allBotsKilled = true;
                foreach (NPC bot in targetBots)
                {
                    if (bot != null && bot.IsAlive && !killedBots.Contains(bot))
                    {
                        allBotsKilled = false;
                        break;
                    }
                }
                
                if (allBotsKilled && killedBots.Count == targetBots.Count)
                {
                    OnAllBotsKilled();
                }
            }
            
            // Item toplama görevi: Tüm itemler toplandı mı kontrol et
            if (targetItems != null && targetItems.Count > 0)
            {
                bool allItemsCollected = true;
                foreach (CollectibleItem item in targetItems)
                {
                    if (item != null && !item.IsCollected)
                    {
                        allItemsCollected = false;
                        break;
                    }
                }
                
                if (allItemsCollected && collectedItems.Count == targetItems.Count)
                {
                    OnAllItemsCollected();
                }
            }
        }

        /// <summary>
        /// NPC tutuklandığında çağrılır
        /// </summary>
        private void OnNPCArrested()
        {
            questState = QuestState.NPCArrested;

            // NPC marker'ı kaldır
            if (npcMarker != null)
            {
                Object.Destroy(npcMarker);
                npcMarker = null;
            }

            // Araç üzerinde marker oluştur (ayrı GameObject olarak)
            if (deliveryVehicle != null)
            {
                GameObject markerObject = new GameObject($"QuestMarker_Vehicle");
                vehicleMarker = markerObject.AddComponent<QuestMarker>();
                vehicleMarker.Initialize(QuestMarkerType.DeliveryTarget, deliveryVehicle.transform);
                // Araç'a parent yapma - sadece görsel takip
            }

            Debug.Log($"[Quest] NPC tutuklandı: {targetNPC.NPCName}");
        }

        /// <summary>
        /// Belirli bir bot için marker'ı kaldır (rage moduna girdiğinde)
        /// </summary>
        public void RemoveMarkerForBot(NPC bot)
        {
            if (bot == null || targetBots == null) return;
            
            // Bu bot hedef listede mi?
            if (targetBots.Contains(bot))
            {
                // Bu bot'un marker'ını kaldır
                if (botMarkerMap.ContainsKey(bot))
                {
                    QuestMarker marker = botMarkerMap[bot];
                    if (marker != null && marker.gameObject != null)
                    {
                        Object.Destroy(marker.gameObject);
                        botMarkers.Remove(marker);
                        botMarkerMap.Remove(bot);
                        Debug.Log($"[Quest] Bot rage moduna girdi, marker kaldırıldı: {bot.NPCName}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Belirli bir bot öldürüldü mü kontrol et (2. görev için)
        /// </summary>
        public void CheckBotKilled(NPC bot)
        {
            if (bot == null || targetBots == null) return;
            
            // Bu bot hedef listede mi ve daha önce öldürülmedi mi?
            if (targetBots.Contains(bot) && !killedBots.Contains(bot) && !bot.IsAlive)
            {
                killedBots.Add(bot);
                Debug.Log($"[Quest] Bot öldürüldü: {bot.NPCName} ({killedBots.Count}/{targetBots.Count})");
                
                // Bu bot'un marker'ını kaldır
                if (botMarkerMap.ContainsKey(bot))
                {
                    QuestMarker marker = botMarkerMap[bot];
                    if (marker != null && marker.gameObject != null)
                    {
                        Object.Destroy(marker.gameObject);
                        botMarkers.Remove(marker);
                        botMarkerMap.Remove(bot);
                    }
                }
                
                // Tüm botlar öldürüldü mü kontrol et
                if (killedBots.Count == targetBots.Count && questState != QuestState.Completed)
                {
                    OnAllBotsKilled();
                }
            }
        }
        
        /// <summary>
        /// Belirli bir item toplandı mı kontrol et (collect quest için)
        /// </summary>
        public void CheckItemCollected(CollectibleItem item)
        {
            if (item == null || targetItems == null) return;
            
            // Bu item hedef listede mi ve daha önce toplanmadı mı?
            if (targetItems.Contains(item) && !collectedItems.Contains(item) && item.IsCollected)
            {
                collectedItems.Add(item);
                Debug.Log($"[Quest] Item toplandı: {item.ItemName} ({collectedItems.Count}/{targetItems.Count})");
                
                // "Kanıt toplandı" bildirimi göster
                if (QuestObjectiveUI.Instance != null)
                {
                    QuestObjectiveUI.Instance.ShowNotification("Kanıt toplandı!", 2f);
                }
                
                // Objective UI'ı güncelle
                if (QuestObjectiveUI.Instance != null)
                {
                    string progressText = $"{questDescription} ({collectedItems.Count}/{targetItems.Count})";
                    QuestObjectiveUI.Instance.ShowObjective(progressText);
                }
                
                // Tüm itemler toplandı mı kontrol et
                if (collectedItems.Count == targetItems.Count && questState != QuestState.Completed)
                {
                    OnAllItemsCollected();
                }
            }
        }
        
        /// <summary>
        /// Tüm botlar öldürüldüğünde çağrılır
        /// </summary>
        private void OnAllBotsKilled()
        {
            if (questState == QuestState.Completed) return;
            
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.CompleteQuest(this);
                QuestSystem.Instance.OnQuestCompleted(this);
            }
            
            Debug.Log($"[Quest] Tüm botlar öldürüldü! {completionNPC?.NPCName} ile konuşarak görevi tamamlayın.");
        }
        
        /// <summary>
        /// Tüm itemler toplandığında çağrılır
        /// </summary>
        private void OnAllItemsCollected()
        {
            if (questState == QuestState.Completed) return;
            
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.CompleteQuest(this);
            }
            
            // Objective UI'ı güncelle
            if (QuestObjectiveUI.Instance != null)
            {
                QuestObjectiveUI.Instance.HideObjective();
                QuestObjectiveUI.Instance.ShowNotification($"{questName} tamamlandı!", 3f);
            }
            
            // Torbaci'ye yeşil marker ekle - YENİ BİR GAMEOBJECT OLARAK
            if (completionNPC != null)
            {
                GameObject markerObject = new GameObject($"QuestMarker_CollectCompletion_{completionNPC.NPCName}");
                QuestMarker completionMarker = markerObject.AddComponent<QuestMarker>();
                completionMarker.Initialize(QuestMarkerType.DeliveryTarget, completionNPC.transform);
                Debug.Log($"[Quest] Tüm itemler toplandı! {completionNPC.NPCName} ile konuşarak görevi tamamlayın.");
            }
            else
            {
                Debug.Log($"[Quest] Tüm itemler toplandı! {questName} tamamlandı.");
            }
        }
        
        /// <summary>
        /// Görevi tamamla (NPC teslim edildi veya botlar öldürüldü)
        /// </summary>
        public void CompleteQuest()
        {
            if (questState == QuestState.Completed) return;

            questState = QuestState.Completed;

            // Marker'ları kaldır
            if (npcMarker != null)
            {
                Object.Destroy(npcMarker.gameObject);
                npcMarker = null;
            }

            if (vehicleMarker != null)
            {
                Object.Destroy(vehicleMarker.gameObject);
                vehicleMarker = null;
            }
            
            // Bot marker'larını kaldır
            foreach (QuestMarker marker in botMarkers)
            {
                if (marker != null)
                {
                    Object.Destroy(marker.gameObject);
                }
            }
            botMarkers.Clear();

            Debug.Log($"[Quest] Görev tamamlandı: {questName}");
        }

        /// <summary>
        /// Bu görev belirli bir NPC için mi?
        /// </summary>
        public bool IsTargetNPC(NPC npc)
        {
            return targetNPC == npc;
        }
        
        /// <summary>
        /// DialogueNPC'nin oluşturduğu ilk görev marker'ını kaldır
        /// </summary>
        private void RemoveFirstQuestMarkerForNPC(NPC npc)
        {
            if (npc == null) return;
            
            // Scene'deki tüm QuestMarker'ları bul
            QuestMarker[] allMarkers = Object.FindObjectsOfType<QuestMarker>();
            foreach (QuestMarker marker in allMarkers)
            {
                if (marker != null && marker.gameObject != null)
                {
                    // İlk görev marker'ı mı kontrol et (name "QuestMarker_FirstQuest_" ile başlıyorsa)
                    // Boss NPC üzerindeki marker'ı kaldır (npc.NPCName değil, "Boss_" ile başlayan)
                    if (marker.gameObject.name.Contains("QuestMarker_FirstQuest_Boss_"))
                    {
                        Object.Destroy(marker.gameObject);
                        Debug.Log($"[Quest] İlk görev boss NPC marker'ı kaldırıldı");
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Görev durumu
    /// </summary>
    public enum QuestState
    {
        NotStarted,      // Görev başlatılmadı
        InProgress,      // Görev devam ediyor (NPC'yi tutukla)
        NPCArrested,     // NPC tutuklandı (arabaya teslim et)
        Completed        // Görev tamamlandı
    }
}
