using UnityEngine;
using System.Collections.Generic;
using GuardSimulator.Character;

namespace GuardSimulator.Gameplay
{
    /// <summary>
    /// Görev sistemi - Diyalog tamamlandıktan sonra görev başlatma ve takip
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("Quest Settings")]
        [Tooltip("Aktif görevler listesi")]
        [SerializeField] private List<Quest> activeQuests = new List<Quest>();
        
        [Tooltip("Tamamlanan görevler listesi")]
        [SerializeField] private List<Quest> completedQuests = new List<Quest>();
        
        [Header("Quest Chain Settings")]
        [Tooltip("1. görev: Görev veren NPC (boss) - Quest1 tamamlandıktan sonra ödül almak için")]
        [SerializeField] private NPC quest1CompletionNPC;
        
        [Tooltip("Görev sırası (1. görev tamamlandıktan sonra 2. görev için NPC)")]
        public NPC quest2DialogueNPC;
        
        [Tooltip("2. görev: Bot öldürme görevi - öldürülecek botlar listesi")]
        [SerializeField] private List<NPC> quest2TargetBots = new List<NPC>();
        
        [Tooltip("2. görev: Bot öldürme görevi tamamlandığında konuşulacak NPC (quest giver)")]
        [SerializeField] private NPC quest2CompletionNPC;
        
        // Quest1 completion NPC (boss) için marker
        private QuestMarker quest1CompletionMarker;
        
        // Quest2 dialogue NPC için marker
        private QuestMarker quest2DialogueMarker;
        
        // Quest2 completion NPC (enemyboss) için marker
        private QuestMarker quest2CompletionMarker;

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
            // Quest2 marker'ı sadece quest1 tamamlandığında oluşturulacak (OnQuestCompleted içinde)
        }

        /// <summary>
        /// Yeni bir görev başlat
        /// </summary>
        public void StartQuest(Quest quest)
        {
            if (quest == null) return;

            // Aynı görev zaten aktifse başlatma
            if (activeQuests.Contains(quest))
            {
                Debug.LogWarning($"[QuestSystem] Görev zaten aktif: {quest.questName}");
                return;
            }

            activeQuests.Add(quest);
            quest.StartQuest();
            Debug.Log($"[QuestSystem] Görev başlatıldı: {quest.questName}");
        }

        /// <summary>
        /// Görevi tamamla
        /// </summary>
        public void CompleteQuest(Quest quest)
        {
            if (quest == null) return;

            if (activeQuests.Contains(quest))
            {
                quest.CompleteQuest();
                activeQuests.Remove(quest);
                completedQuests.Add(quest);
                Debug.Log($"[QuestSystem] Görev tamamlandı: {quest.questName}");
            }
        }
        
        /// <summary>
        /// Görev tamamlandığında çağrılır (2. göreve geçiş için)
        /// </summary>
        public void OnQuestCompleted(Quest quest)
        {
            if (quest == null) return;
            
            Debug.Log($"[QuestSystem] Görev tamamlandı event: {quest.questName}");
            
            // 1. görev tamamlandı mı kontrol et (arrest quest ise)
            if (quest.questName.Contains("Tutukla:"))
            {
                // 1. görev tamamlandı, boss üzerinde yeşil marker oluştur (ödül almak için)
                if (quest1CompletionNPC != null)
                {
                    CreateQuest1CompletionMarker();
                    Debug.Log($"[QuestSystem] 1. görev tamamlandı! {quest1CompletionNPC.NPCName} üzerinde yeşil marker oluşturuldu (ödül almak için).");
                }
                else
                {
                    Debug.LogWarning("[QuestSystem] Quest1CompletionNPC atanmamış! Boss üzerinde yeşil marker oluşturulamadı. Lütfen QuestSystem component'inde Quest 1 Completion NPC alanını doldurun.");
                }
            }
            
            // 2. görev tamamlandı mı kontrol et (bot kill quest ise)
            if (quest.questName.Contains("Bot Öldür"))
            {
                // Quest2 dialogue marker'ı kaldır (artık gerekli değil)
                RemoveQuest2DialogueMarker();
                
                // 2. görev tamamlandı, enemyboss üzerinde yeşil marker oluştur (yanıp sönecek)
                if (quest2CompletionNPC != null)
                {
                    CreateQuest2CompletionMarker();
                    Debug.Log($"[QuestSystem] 2. görev tamamlandı! {quest2CompletionNPC.NPCName} üzerinde yeşil marker oluşturuldu.");
                }
            }
        }
        
        /// <summary>
        /// Quest1 completion NPC (boss) üzerinde yeşil marker oluştur (ödül almak için)
        /// </summary>
        private void CreateQuest1CompletionMarker()
        {
            // Marker zaten varsa oluşturma
            if (quest1CompletionMarker != null) return;
            
            if (quest1CompletionNPC == null) return;
            
            GameObject markerObject = new GameObject($"QuestMarker_Quest1Completion_{quest1CompletionNPC.NPCName}");
            quest1CompletionMarker = markerObject.AddComponent<QuestMarker>();
            // DeliveryTarget tipi kullan (yeşil renk ve yanıp sönme aktif)
            quest1CompletionMarker.Initialize(QuestMarkerType.DeliveryTarget, quest1CompletionNPC.transform);
            
            Debug.Log($"[QuestSystem] Quest1 completion marker (yeşil, yanıp sönen) oluşturuldu: {quest1CompletionNPC.NPCName}");
        }
        
        /// <summary>
        /// Quest1 completion marker'ı kaldır
        /// </summary>
        private void RemoveQuest1CompletionMarker()
        {
            if (quest1CompletionMarker != null)
            {
                if (quest1CompletionMarker.gameObject != null)
                {
                    Destroy(quest1CompletionMarker.gameObject);
                }
                quest1CompletionMarker = null;
                Debug.Log("[QuestSystem] Quest1 completion marker kaldırıldı.");
            }
        }
        
        /// <summary>
        /// Boss ile completion diyalogu başladığında çağrılır (marker'ı kaldırmak için)
        /// </summary>
        public void OnQuest1CompletionDialogueStarted()
        {
            // Quest1 completion marker'ı kaldır
            RemoveQuest1CompletionMarker();
        }
        
        /// <summary>
        /// Boss ile completion diyalogu gösterildikten sonra çağrılır (2. göreve geçiş için)
        /// </summary>
        public void OnQuest1CompletionDialogueShown()
        {
            // 2. göreve geçiş için NPC'yi hazırla
            if (quest2DialogueNPC != null)
            {
                CreateQuest2DialogueMarker();
                Debug.Log($"[QuestSystem] 1. görev ödülü alındı! 2. görev için {quest2DialogueNPC.NPCName} ile konuşun.");
            }
        }
        
        /// <summary>
        /// Quest2 dialogue NPC üzerinde marker oluştur
        /// </summary>
        private void CreateQuest2DialogueMarker()
        {
            // Eğer marker zaten varsa, önce kaldır
            RemoveQuest2DialogueMarker();
            
            if (quest2DialogueNPC == null) return;
            
            GameObject markerObject = new GameObject($"QuestMarker_Quest2Dialogue_{quest2DialogueNPC.NPCName}");
            quest2DialogueMarker = markerObject.AddComponent<QuestMarker>();
            quest2DialogueMarker.Initialize(QuestMarkerType.ArrestTarget, quest2DialogueNPC.transform);
            
            Debug.Log($"[QuestSystem] Quest2 dialogue marker oluşturuldu: {quest2DialogueNPC.NPCName}");
        }
        
        /// <summary>
        /// Quest2 dialogue marker'ı kaldır
        /// </summary>
        private void RemoveQuest2DialogueMarker()
        {
            if (quest2DialogueMarker != null)
            {
                if (quest2DialogueMarker.gameObject != null)
                {
                    Destroy(quest2DialogueMarker.gameObject);
                }
                quest2DialogueMarker = null;
            }
        }
        
        /// <summary>
        /// Quest2 completion NPC (enemyboss) üzerinde yeşil marker oluştur (yanıp sönecek)
        /// </summary>
        private void CreateQuest2CompletionMarker()
        {
            // Marker zaten varsa oluşturma
            if (quest2CompletionMarker != null) return;
            
            if (quest2CompletionNPC == null) return;
            
            // NPC name'i kontrol et (enemyboss olmalı)
            if (quest2CompletionNPC.NPCName != "enemyboss")
            {
                Debug.LogWarning($"[QuestSystem] Quest2 completion NPC ismi 'enemyboss' değil: {quest2CompletionNPC.NPCName}");
            }
            
            GameObject markerObject = new GameObject($"QuestMarker_Quest2Completion_{quest2CompletionNPC.NPCName}");
            quest2CompletionMarker = markerObject.AddComponent<QuestMarker>();
            // DeliveryTarget tipi kullan (yeşil renk ve yanıp sönme aktif)
            quest2CompletionMarker.Initialize(QuestMarkerType.DeliveryTarget, quest2CompletionNPC.transform);
            
            Debug.Log($"[QuestSystem] Quest2 completion marker (yeşil, yanıp sönen) oluşturuldu: {quest2CompletionNPC.NPCName}");
        }
        
        /// <summary>
        /// Quest2 completion marker'ı kaldır
        /// </summary>
        private void RemoveQuest2CompletionMarker()
        {
            if (quest2CompletionMarker != null)
            {
                if (quest2CompletionMarker.gameObject != null)
                {
                    Destroy(quest2CompletionMarker.gameObject);
                }
                quest2CompletionMarker = null;
                Debug.Log("[QuestSystem] Quest2 completion marker kaldırıldı.");
            }
        }

        /// <summary>
        /// Belirli bir NPC'yi tutuklama görevi başlat
        /// </summary>
        public void StartArrestQuest(NPC targetNPC, VehicleEscortDelivery deliveryVehicle, NPC questGiverNPC = null)
        {
            if (targetNPC == null)
            {
                Debug.LogError("[QuestSystem] Hedef NPC null!");
                return;
            }

            // Quest giver NPC'yi kaydet (quest tamamlandığında yeşil marker için)
            if (questGiverNPC != null)
            {
                quest1CompletionNPC = questGiverNPC;
                Debug.Log($"[QuestSystem] Quest giver NPC kaydedildi: {questGiverNPC.NPCName}");
            }

            // Yeni görev oluştur
            Quest arrestQuest = new Quest();
            arrestQuest.InitializeArrestQuest(targetNPC, deliveryVehicle);
            
            StartQuest(arrestQuest);
        }

        /// <summary>
        /// Aktif görevleri kontrol et (NPC tutuklandı mı, teslim edildi mi?)
        /// </summary>
        private void Update()
        {
            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                Quest quest = activeQuests[i];
                if (quest != null)
                {
                    quest.UpdateQuest();
                }
            }
        }

        /// <summary>
        /// Belirli bir NPC için aktif görev var mı?
        /// </summary>
        public bool HasActiveQuestForNPC(NPC npc)
        {
            foreach (Quest quest in activeQuests)
            {
                if (quest != null && quest.IsTargetNPC(npc))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Belirli bir NPC için görev al
        /// </summary>
        public Quest GetQuestForNPC(NPC npc)
        {
            foreach (Quest quest in activeQuests)
            {
                if (quest != null && quest.IsTargetNPC(npc))
                {
                    return quest;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 2. görevi başlat (bot öldürme görevi)
        /// </summary>
        public void StartQuest2BotKill()
        {
            if (quest2TargetBots == null || quest2TargetBots.Count == 0)
            {
                Debug.LogWarning("[QuestSystem] 2. görev için bot listesi boş!");
                return;
            }
            
            // Aktif bot öldürme görevi var mı kontrol et
            foreach (Quest quest in activeQuests)
            {
                if (quest != null && quest.questName.Contains("Bot Öldür"))
                {
                    Debug.LogWarning("[QuestSystem] Bot öldürme görevi zaten aktif!");
                    return;
                }
            }
            
            // Quest2 dialogue marker'ı kaldır (görev başlatıldı, artık gerekli değil)
            RemoveQuest2DialogueMarker();
            
            // Yeni bot öldürme görevi oluştur
            Quest botKillQuest = new Quest();
            botKillQuest.InitializeBotKillQuest(quest2TargetBots, quest2CompletionNPC);
            
            StartQuest(botKillQuest);
            Debug.Log($"[QuestSystem] 2. görev başlatıldı: {botKillQuest.questName}");
        }
        
        /// <summary>
        /// 1. görev başlatıldı mı?
        /// </summary>
        public bool IsQuest1Started()
        {
            foreach (Quest quest in activeQuests)
            {
                if (quest != null && quest.questName.Contains("Tutukla:"))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 1. görev tamamlandı mı?
        /// </summary>
        public bool IsQuest1Completed()
        {
            foreach (Quest quest in completedQuests)
            {
                if (quest != null && quest.questName.Contains("Tutukla:"))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 2. görev tamamlandı mı?
        /// </summary>
        public bool IsQuest2Completed()
        {
            foreach (Quest quest in completedQuests)
            {
                if (quest != null && quest.questName.Contains("Bot Öldür"))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Belirli bir bot öldürüldü mü kontrol et (2. görev için)
        /// </summary>
        public void CheckBotKilled(NPC bot)
        {
            if (bot == null) return;
            
            foreach (Quest quest in activeQuests)
            {
                if (quest != null && quest.questName.Contains("Bot Öldür"))
                {
                    quest.CheckBotKilled(bot);
                }
            }
        }
        
        /// <summary>
        /// Belirli bir NPC için marker'ı kaldır (rage moduna girdiğinde bot marker'larını kaldırmak için)
        /// </summary>
        public void RemoveMarkerForNPC(NPC npc)
        {
            if (npc == null) return;
            
            foreach (Quest quest in activeQuests)
            {
                if (quest != null && quest.questName.Contains("Bot Öldür"))
                {
                    quest.RemoveMarkerForBot(npc);
                }
            }
        }
        
        /// <summary>
        /// Item toplandığında çağrılır (collect quest için)
        /// </summary>
        public void OnItemCollected(CollectibleItem item)
        {
            if (item == null) return;
            
            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                Quest quest = activeQuests[i];
                if (quest != null && quest.TargetItems != null && quest.TargetItems.Contains(item))
                {
                    quest.CheckItemCollected(item);
                }
            }
        }
        
        /// <summary>
        /// Item toplama görevi başlat
        /// </summary>
        public void StartCollectQuest(List<CollectibleItem> items, string questName, string questDescription, NPC completionNPC = null)
        {
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("[QuestSystem] Item listesi boş!");
                return;
            }
            
            Quest collectQuest = new Quest();
            collectQuest.InitializeCollectQuest(items, questName, questDescription, completionNPC);
            
            StartQuest(collectQuest);
            Debug.Log($"[QuestSystem] Item toplama görevi başlatıldı: {questName}. Completion NPC: {completionNPC?.NPCName ?? "null"}");
        }
        
        /// <summary>
        /// Enemyboss ile diyalog başladığında marker'ı kaldır
        /// </summary>
        public void OnEnemyBossDialogueStarted()
        {
            RemoveQuest2CompletionMarker();
        }
        
        private void OnDestroy()
        {
            // Quest1 completion marker'ı temizle
            RemoveQuest1CompletionMarker();
            
            // Quest2 dialogue marker'ı temizle
            RemoveQuest2DialogueMarker();
            
            // Quest2 completion marker'ı temizle
            RemoveQuest2CompletionMarker();
        }
    }
}
