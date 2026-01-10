using UnityEngine;
using System.Collections;
using Akila.FPSFramework;

public class DialogueNPC : MonoBehaviour, IInteractable
{
    [Header("Dialogue Settings")]
    [SerializeField] private DialogueData dialogueData;
    [Tooltip("Quest tamamlandıktan sonra gösterilecek diyalog (boss için Quest1, enemyboss için Quest2 tamamlandıktan sonra)")]
    [SerializeField] private DialogueData questCompletionDialogueData;
    [SerializeField] private Transform dialogueCameraPosition;
    [SerializeField] private string interactionName = "Konuş";
    
    [Header("Surrender Settings")]
    [Tooltip("NPC teslim olduktan sonra gösterilecek mesaj")]
    [TextArea(2, 3)]
    [SerializeField] private string surrenderMessage = "Teslim oldum ya, daha ne istiyorsun?";
    
    [Header("Quest Settings")]
    [Tooltip("Bu diyalog tamamlandığında görev başlatılacak mı?")]
    [SerializeField] private bool startQuestOnComplete = false;
    
    [Tooltip("Görev hedefi NPC (tutuklanacak NPC - genellikle başka bir NPC)")]
    [SerializeField] private GuardSimulator.Character.NPC questTargetNPC;
    
    [Tooltip("Görev teslim noktası (araba - VehicleEscortDelivery component'i olan GameObject)")]
    [SerializeField] private GuardSimulator.Gameplay.VehicleEscortDelivery questDeliveryVehicle;
    
    // Public properties for DialogueManager access
    public bool StartQuestOnComplete => startQuestOnComplete;
    public GuardSimulator.Character.NPC QuestTargetNPC => questTargetNPC;
    public GuardSimulator.Gameplay.VehicleEscortDelivery QuestDeliveryVehicle => questDeliveryVehicle;
    
    [Header("Camera Settings")]
    [SerializeField] private float cameraRotationSpeed = 2f; // Smooth dönüş hızı
    
    private CameraManager fpsCameraManager;
    private Quaternion originalCameraRotation;
    private Coroutine cameraRotationCoroutine;
    private bool isDialogueActive = false; // Bu NPC'nin diyaloğu aktif mi?
    private bool hasSavedOriginalCamera = false; // Orijinal kamera rotasyonu kaydedildi mi?
    
        // NPC component referansı
        private GuardSimulator.Character.NPC npcComponent;
        
        // İlk görev marker referansı
        private GuardSimulator.Gameplay.QuestMarker firstQuestMarker;
        
        // Quest completion diyalogu gösterildi mi? (bir daha konuşamamak için)
        private bool questCompletionDialogueShown = false;

    private void Start()
    {
        // Find FPS CameraManager
        fpsCameraManager = FindObjectOfType<CameraManager>();
        
        // NPC component'ini bul
        npcComponent = GetComponent<GuardSimulator.Character.NPC>();
        if (npcComponent == null)
        {
            npcComponent = GetComponentInParent<GuardSimulator.Character.NPC>();
        }
        
        if (npcComponent == null)
        {
            Debug.LogWarning($"[DialogueNPC] {gameObject.name} üzerinde NPC component bulunamadı! IsAlive kontrolü çalışmayacak.");
        }
        
        // NPC name = "boss" olan NPC için marker oluştur (quest1 başlatılmamışsa)
        if (npcComponent != null && npcComponent.NPCName == "boss")
        {
            CreateFirstQuestMarker();
        }
    }
    
    /// <summary>
    /// İlk görev NPC'si için marker oluştur (quest henüz başlatılmamışsa)
    /// Boss NPC (bu DialogueNPC) üzerinde marker gösterilecek
    /// </summary>
    private void CreateFirstQuestMarker()
    {
        // Marker zaten varsa oluşturma
        if (firstQuestMarker != null) return;
        
        // Quest1 zaten tamamlanmış mı kontrol et
        if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
            GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest1Completed())
        {
            return; // Quest1 zaten tamamlanmış, marker gerekmez
        }
        
        // Quest1 aktif mi kontrol et
        if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
            GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest1Started())
        {
            return; // Quest1 zaten başlatılmış, marker gerekmez
        }
        
        // Boss NPC (bu DialogueNPC) üzerinde marker oluştur
        Transform bossTransform = npcComponent != null ? npcComponent.transform : transform;
        if (bossTransform != null)
        {
            GameObject markerObject = new GameObject($"QuestMarker_FirstQuest_Boss_{bossTransform.name}");
            firstQuestMarker = markerObject.AddComponent<GuardSimulator.Gameplay.QuestMarker>();
            firstQuestMarker.Initialize(GuardSimulator.Gameplay.QuestMarkerType.ArrestTarget, bossTransform);
            Debug.Log($"[DialogueNPC] İlk görev için boss NPC üzerinde marker oluşturuldu: {bossTransform.name}");
        }
    }
    
    /// <summary>
    /// İlk görev marker'ını kaldır
    /// </summary>
    private void RemoveFirstQuestMarker()
    {
        if (firstQuestMarker != null)
        {
            if (firstQuestMarker.gameObject != null)
            {
                Destroy(firstQuestMarker.gameObject);
            }
            firstQuestMarker = null;
            Debug.Log("[DialogueNPC] İlk görev marker'ı kaldırıldı.");
        }
    }
    
    /// <summary>
    /// NPC'nin canlı olup olmadığını kontrol et (NPC.cs'den alır)
    /// </summary>
    private bool IsAlive
    {
        get
        {
            if (npcComponent == null) return true; // NPC component yoksa varsayılan olarak canlı kabul et
            
            return npcComponent.IsAlive;
        }
    }

    // IInteractable implementation
    public void Interact(InteractionsManager source)
    {
        // NPC ölüyse diyalog başlatma
        if (!IsAlive)
        {
            return;
        }
        
        // Saldırı modu açıksa diyalog başlatma
        if (PlayerMain.Instance != null && PlayerMain.Instance.IsCombatModeActive)
        {
            Debug.Log("[DialogueNPC] Saldırı modu açıkken NPC'lerle konuşulamaz!");
            return;
        }
        
        if (DialogueManager.Instance == null) return;
        if (DialogueManager.Instance.IsDialogueActive()) return;

        // NPC tutuklanmışsa özel mesaj göster (teslim olma mesajı)
        if (npcComponent != null && npcComponent.IsArrested)
        {
            ShowSurrenderDialogue();
            return;
        }
        
        // Boss için Quest1 tamamlanmışsa questCompletionDialogueData kullan (ve daha önce gösterilmediyse)
        DialogueData dialogueToUse = dialogueData;
        bool isCompletionDialogue = false;
        
        if (npcComponent != null && npcComponent.NPCName == "boss")
        {
            if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
                GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest1Completed() &&
                !questCompletionDialogueShown)
            {
                // Quest1 tamamlanmış, quest completion diyalogunu kullan
                if (questCompletionDialogueData != null)
                {
                    dialogueToUse = questCompletionDialogueData;
                    isCompletionDialogue = true;
                }
            }
            // Quest completion diyalogu daha önce gösterildiyse, diyalog açılamaz
            else if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
                     GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest1Completed() &&
                     questCompletionDialogueShown)
            {
                Debug.Log("[DialogueNPC] Quest completion diyalogu daha önce gösterildi, diyalog açılamaz.");
                return;
            }
        }
        
        // Enemyboss için Quest2 tamamlanmışsa questCompletionDialogueData kullan (ve daha önce gösterilmediyse)
        if (npcComponent != null && npcComponent.NPCName == "enemyboss")
        {
            if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
                GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest2Completed() &&
                !questCompletionDialogueShown)
            {
                // Quest2 tamamlanmış, quest completion diyalogunu kullan (yeşil marker çıktığında)
                if (questCompletionDialogueData != null)
                {
                    dialogueToUse = questCompletionDialogueData;
                    isCompletionDialogue = true;
                    Debug.Log("[DialogueNPC] Enemyboss için quest completion diyalogu etkinleştirildi (yeşil marker mevcut).");
                }
                else
                {
                    Debug.LogWarning("[DialogueNPC] Enemyboss için questCompletionDialogueData atanmamış! Completion diyalogu gösterilemeyecek.");
                    // QuestCompletionDialogueData yoksa normal diyalogu göster
                }
            }
            // Quest completion diyalogu daha önce gösterildiyse, diyalog açılamaz
            else if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
                     GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest2Completed() &&
                     questCompletionDialogueShown)
            {
                Debug.Log("[DialogueNPC] Quest completion diyalogu daha önce gösterildi, diyalog açılamaz.");
                return;
            }
        }
        
        if (dialogueToUse == null) return;

        // Boss NPC için marker'ı kaldır (diyalog başladığında)
        if (npcComponent != null && npcComponent.NPCName == "boss")
        {
            RemoveFirstQuestMarker();
            
            // Quest1 completion marker'ı kaldır (ödül almak için)
            if (GuardSimulator.Gameplay.QuestSystem.Instance != null)
            {
                GuardSimulator.Gameplay.QuestSystem.Instance.OnQuest1CompletionDialogueStarted();
            }
        }
        
        // Enemyboss NPC için marker'ı kaldır (diyalog başladığında - Quest2 tamamlandıktan sonra)
        if (npcComponent != null && npcComponent.NPCName == "enemyboss")
        {
            if (GuardSimulator.Gameplay.QuestSystem.Instance != null)
            {
                GuardSimulator.Gameplay.QuestSystem.Instance.OnEnemyBossDialogueStarted();
            }
        }

        // Bu NPC'nin diyaloğu aktif
        isDialogueActive = true;

        // Kamerayı NPC'nin belirlediği noktaya smooth döndür
        if (dialogueCameraPosition != null)
        {
            StartCameraRotation();
        }

        // Quest completion diyalogu ise flag'i set et
        if (isCompletionDialogue)
        {
            questCompletionDialogueShown = true;
        }

        // NPC referansını DialogueManager'a ilet (quest ayarları için DialogueNPC referansını da gönder)
        DialogueManager.Instance.StartDialogue(dialogueToUse, dialogueCameraPosition, npcComponent, this);
    }
    
    /// <summary>
    /// Teslim olmuş NPC için özel diyalog göster
    /// </summary>
    private void ShowSurrenderDialogue()
    {
        // Runtime'da teslim olma mesajı için DialogueData oluştur
        DialogueData surrenderDialogue = ScriptableObject.CreateInstance<DialogueData>();
        surrenderDialogue.endDialogueDelay = 2f;
        
        DialogueNode surrenderNode = new DialogueNode
        {
            nodeNumber = 0,
            npcText = surrenderMessage,
            quitAfterText = true,
            choices = new DialogueChoice[0] // Seçenek yok
        };
        
        surrenderDialogue.dialogueNodes = new DialogueNode[] { surrenderNode };
        
        // Bu NPC'nin diyaloğu aktif
        isDialogueActive = true;

        // Kamerayı NPC'nin belirlediği noktaya smooth döndür
        if (dialogueCameraPosition != null)
        {
            StartCameraRotation();
        }

        // Teslim olma diyaloğunu başlat (quest ayarları olmayacak çünkü teslim olma diyaloğu)
        DialogueManager.Instance.StartDialogue(surrenderDialogue, dialogueCameraPosition, npcComponent, null);
    }
    
    private void StartCameraRotation()
    {
        if (fpsCameraManager == null || fpsCameraManager.mainCamera == null || dialogueCameraPosition == null)
            return;
        
        Camera mainCam = fpsCameraManager.mainCamera;
        
        // Orijinal rotasyonu sadece ilk kez sakla (diyalog başladığında)
        if (!hasSavedOriginalCamera)
        {
            originalCameraRotation = mainCam.transform.rotation;
            hasSavedOriginalCamera = true;
        }
        
        // Kamerayı hedefe smooth döndür (pozisyon değişmez, sadece rotasyon)
        if (cameraRotationCoroutine != null)
            StopCoroutine(cameraRotationCoroutine);
        cameraRotationCoroutine = StartCoroutine(RotateCameraToTarget());
    }
    
    private IEnumerator RotateCameraToTarget()
    {
        if (fpsCameraManager == null || fpsCameraManager.mainCamera == null || dialogueCameraPosition == null)
            yield break;
        
        Camera mainCam = fpsCameraManager.mainCamera;
        float elapsed = 0f;
        
        Quaternion startRotation = mainCam.transform.rotation;
        
        // Kameranın pozisyonu değişmez, sadece hedef noktaya bakacak şekilde rotasyon hesapla
        Vector3 directionToTarget = (dialogueCameraPosition.position - mainCam.transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * cameraRotationSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            // Smooth interpolation (ease in-out)
            float smoothT = t * t * (3f - 2f * t);
            
            // Sadece rotasyonu değiştir, pozisyon aynı kalır
            mainCam.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            
            yield return null;
        }
        
        // Son rotasyonu garantile
        mainCam.transform.rotation = targetRotation;
    }
    
    private void ResetCameraToOriginal()
    {
        if (fpsCameraManager == null || fpsCameraManager.mainCamera == null)
            return;
        
        // Eğer orijinal kamera pozisyonu kaydedilmemişse, resetleme
        if (!hasSavedOriginalCamera)
            return;
        
        if (cameraRotationCoroutine != null)
            StopCoroutine(cameraRotationCoroutine);
        
        cameraRotationCoroutine = StartCoroutine(ResetCameraCoroutine());
    }
    
    private IEnumerator ResetCameraCoroutine()
    {
        if (fpsCameraManager == null || fpsCameraManager.mainCamera == null)
            yield break;
        
        Camera mainCam = fpsCameraManager.mainCamera;
        float elapsed = 0f;
        
        Quaternion startRotation = mainCam.transform.rotation;
        
        // FPS Framework input'u geçici olarak kapat (kamera resetlenirken)
        bool wasInputActive = FPSFrameworkCore.IsInputActive;
        FPSFrameworkCore.IsInputActive = false;
        
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * cameraRotationSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            // Smooth interpolation (ease in-out)
            float smoothT = t * t * (3f - 2f * t);
            
            // Sadece rotasyonu orijinal haline döndür, pozisyon aynı kalır
            mainCam.transform.rotation = Quaternion.Slerp(startRotation, originalCameraRotation, smoothT);
            
            yield return null;
        }
        
        // Son rotasyonu garantile
        mainCam.transform.rotation = originalCameraRotation;
        
        // Kısa bir süre bekleyip FPS Framework input'u tekrar aç
        yield return new WaitForSeconds(0.1f);
        FPSFrameworkCore.IsInputActive = wasInputActive;
    }
    
    private void OnEnable()
    {
        // Diyalog bittiğinde kamerayı sıfırla
        DialogueManager.OnDialogueEnded += OnDialogueEnded;
    }
    
    private void OnDisable()
    {
        // Event subscription'ı temizle
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
    }
    
    private void OnDialogueEnded()
    {
        // Sadece bu NPC'nin diyaloğu aktifse kamerayı sıfırla
        if (isDialogueActive && hasSavedOriginalCamera)
        {
            isDialogueActive = false;
            ResetCameraToOriginal();
            hasSavedOriginalCamera = false; // Reset flag, bir sonraki diyalog için hazır
            
            // Boss ile quest completion diyalogu gösterildiyse, 2. göreve geçişi başlat
            if (npcComponent != null && npcComponent.NPCName == "boss" && questCompletionDialogueShown)
            {
                if (GuardSimulator.Gameplay.QuestSystem.Instance != null)
                {
                    GuardSimulator.Gameplay.QuestSystem.Instance.OnQuest1CompletionDialogueShown();
                }
            }
        }
    }

    public string GetInteractionName()
    {
        // NPC ölüyse etkileşim gösterilmesin
        if (!IsAlive)
        {
            return "";
        }
        
        // Saldırı modu açıksa etkileşim gösterilmesin
        if (PlayerMain.Instance != null && PlayerMain.Instance.IsCombatModeActive)
        {
            return "";
        }
        
        // Boss için Quest1 tamamlanmış ve completion diyalogu gösterildiyse diyalog açılamaz
        if (npcComponent != null && npcComponent.NPCName == "boss")
        {
            if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
                GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest1Completed() &&
                questCompletionDialogueShown)
            {
                return "";
            }
        }
        
        // Enemyboss için Quest2 tamamlanmış ve completion diyalogu gösterildiyse diyalog açılamaz
        if (npcComponent != null && npcComponent.NPCName == "enemyboss")
        {
            if (GuardSimulator.Gameplay.QuestSystem.Instance != null && 
                GuardSimulator.Gameplay.QuestSystem.Instance.IsQuest2Completed() &&
                questCompletionDialogueShown)
            {
                return "";
            }
        }
        
        // DialogueData yoksa boş string döndür (etkileşim gösterilmesin)
        if (dialogueData == null)
        {
            return "";
        }
        
        // DialogueNPC her zaman öncelikli olsun (EnemyCarryable'dan önce)
        // EnemyCarryable sadece bilinçsiz düşmanlarda çalışır, normal düşmanlarda diyalog çalışmalı
        
        // "|" karakterinden sonraki her şeyi kaldır (örn: "K | X Konuş" -> "K")
        string cleanedName = interactionName;
        if (cleanedName.Contains("|"))
        {
            cleanedName = cleanedName.Split('|')[0].Trim();
        }
        
        // "X" karakterini kaldır (örn: "X Konuş" -> "Konuş")
        cleanedName = cleanedName.Replace("X ", "").Replace("X", "").Trim();
        
        return cleanedName;
    }
}
