using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Akila.FPSFramework;
using System.Linq;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI npcDialogueText;
    [SerializeField] private GameObject choicesContainer;
    [SerializeField] private Button choiceButtonPrefab;
    
    [Header("Text Size Settings")]
    [SerializeField] private float npcNameFontSize = 36f;
    [SerializeField] private float npcDialogueFontSize = 32f; // Büyütüldü
    [SerializeField] private float choiceButtonFontSize = 24f;
    
    [Header("Button Size Settings")]
    [SerializeField] private float buttonMinWidth = 200f;
    [SerializeField] private float buttonMinHeight = 50f;
    [SerializeField] private float buttonPadding = 20f; // Buton içi padding
    [SerializeField] private float buttonSpacing = 10f; // Butonlar arası boşluk
    [SerializeField] private float buttonBottomPadding = 20f; // Butonların altındaki boşluk
    
    [Header("Dialogue Panel Settings")]
    [Tooltip("Additional padding for dynamic background sizing")]
    [SerializeField] private float backgroundPadding = 30f;
    [Tooltip("Vertical position offset from center (0 = center, 1 = bottom, -1 = top). Use negative for lower position.")]
    [SerializeField] private float panelVerticalOffset = -0.15f; // Responsive offset (-0.15 = %15 aşağı)
    [Tooltip("Max panel width as percentage of screen width (0-1). 0.9 = 90% of screen width.")]
    [SerializeField] private float maxPanelWidthPercentage = 0.9f;
    [Tooltip("Max panel height as percentage of screen height (0-1). 0.8 = 80% of screen height.")]
    [SerializeField] private float maxPanelHeightPercentage = 0.8f;

    [Header("Camera References")]
    [SerializeField] private CameraManager fpsCameraManager;
    
    [Header("Particle Text Effects")]
    [Tooltip("Enable particle-like text effects (! and ?)")]
    [SerializeField] private bool enableParticleTextEffects = true;
    [Tooltip("Text color for particle effects")]
    [SerializeField] private Color particleTextColor = Color.yellow;
    [Tooltip("Font size for particle effects")]
    [SerializeField] private float particleTextSize = 48f;
    [Tooltip("Speed of particle text movement")]
    [SerializeField] private float particleTextSpeed = 2f;
    [Tooltip("Lifetime of particle text")]
    [SerializeField] private float particleTextLifetime = 2f;
    [Tooltip("Spawn radius around NPC")]
    [SerializeField] private float spawnRadius = 1.5f;
    [Tooltip("Spawn height offset from NPC")]
    [SerializeField] private float spawnHeightOffset = 1.5f;
    
    private List<GameObject> activeParticleTexts = new List<GameObject>();

    private DialogueData currentDialogue;
    private DialogueNode currentDialogueNode;
    private Transform dialogueCameraTarget;
    private bool isDialogueActive;
    private bool isShowingChoices;
    
    // NPC referansı (teslim olma için)
    private GuardSimulator.Character.NPC currentNPC;
    
    // DialogueNPC referansı (quest ayarları için)
    private DialogueNPC currentDialogueNPC;
    
    // InteractionsManager referansı (interaction UI'ı kapatmak için)
    private InteractionsManager interactionsManager;
    private bool wasInteractionActive; // Diyalog başlamadan önceki durum
    
    // Quest trigger flag (choice'tan gelen)
    private bool shouldTriggerQuestFromChoice = false;
    
    // Collect quest trigger flag
    private bool shouldTriggerCollectQuestFromChoice = false;
    
    // Event: Diyalog bittiğinde çağrılır
    public static System.Action OnDialogueEnded;

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
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Find FPS CameraManager if not assigned
        if (fpsCameraManager == null)
        {
            fpsCameraManager = FindObjectOfType<CameraManager>();
        }
        
        // Find InteractionsManager (interaction UI'ı kapatmak için)
        interactionsManager = FindObjectOfType<InteractionsManager>();
        
        // Canvas'ı kontrol et ve ayarla
        SetupCanvas();
        
        // Font boyutlarını ayarla
        SetupTextSizes();
        
        // Dialogue panel boyutunu ayarla
        SetupDialoguePanelSize();
    }
    
    /// <summary>
    /// Canvas'ı responsive hale getir ve Screen Space - Overlay olduğundan emin ol
    /// </summary>
    private void SetupCanvas()
    {
        if (dialoguePanel == null) return;
        
        // Canvas'ı bul (panel'in parent'ı veya kendisi)
        Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas != null)
        {
            // Canvas'ı Screen Space - Overlay yap (responsive olması için)
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.LogWarning("[DialogueManager] Canvas render mode changed to Screen Space - Overlay for responsive UI.");
            }
            
            // Canvas Scaler'ı kontrol et ve responsive yap
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            // Canvas Scaler ayarları - responsive için
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // Referans çözünürlük
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Hem genişlik hem yüksekliği dikkate al
        }
    }
    
    private void Update()
    {
        // ESC tuşu ile diyalog kapatma (sadece diyalog aktifken)
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
        // Diyalog yoksa pause işlemini FPS Framework'ün kendi sistemi yapsın
        // Burada pause kontrolü yapmıyoruz, FPS Framework'ün PauseMenu'su hallediyor
    }
    
    private void SetupTextSizes()
    {
        try
        {
            if (npcNameText != null && npcNameFontSize > 0)
            {
                npcNameText.fontSize = npcNameFontSize;
            }
            
            if (npcDialogueText != null && npcDialogueFontSize > 0)
            {
                npcDialogueText.fontSize = npcDialogueFontSize;
                npcDialogueText.fontStyle = FontStyles.Bold; // Kalın yap
            }
        }
        catch (System.Exception e)
        {
            // Font size ayarlanırken hata oluştu
        }
    }
    
    /// <summary>
    /// Dialogue panel boyutunu ayarla (başlangıç - orijinal boyut)
    /// </summary>
    private void SetupDialoguePanelSize()
    {
        // Panel boyutunu değiştirme, orijinal boyutunu kullan
        // Sadece yüksekliği dinamik olarak ayarlanacak (AdjustDialoguePanelSize'da)
    }
    
    /// <summary>
    /// Dialogue panel background'ının boyutunu dinamik olarak ayarla (butonlara göre)
    /// </summary>
    private void AdjustDialoguePanelSize()
    {
        if (dialoguePanel == null || choicesContainer == null) return;
        
        RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
        RectTransform containerRect = choicesContainer.GetComponent<RectTransform>();
        
        if (panelRect == null || containerRect == null) return;
        
        // NPC name ve dialogue text'in yüksekliğini hesapla
        float textAreaHeight = 0f;
        if (npcNameText != null)
        {
            RectTransform nameRect = npcNameText.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                textAreaHeight += nameRect.sizeDelta.y;
            }
        }
        
        if (npcDialogueText != null)
        {
            RectTransform dialogueRect = npcDialogueText.GetComponent<RectTransform>();
            if (dialogueRect != null)
            {
                textAreaHeight += dialogueRect.sizeDelta.y;
                // Text'in preferred height'ını da kontrol et
                textAreaHeight = Mathf.Max(textAreaHeight, npcDialogueText.preferredHeight);
            }
        }
        
        // Butonların toplam yüksekliğini al
        float buttonsHeight = containerRect.sizeDelta.y;
        
        // Panel'in yeni yüksekliğini hesapla (text + butonlar + padding)
        float newHeight = textAreaHeight + buttonsHeight + backgroundPadding * 2;
        
        // Panel genişliğini kontrol et ve gerekirse büyüt (butonlar taşıyorsa)
        float originalWidth = panelRect.sizeDelta.x;
        float requiredWidth = originalWidth;
        
        // Butonların genişliğini kontrol et (eğer butonlar taşıyorsa diyalog büyüsün)
        if (choicesContainer != null)
        {
            float containerWidth = containerRect.sizeDelta.x;
            float requiredButtonWidth = containerWidth + backgroundPadding * 2;
            // Eğer butonlar mevcut genişlikten büyükse, genişliği artır
            if (requiredButtonWidth > originalWidth)
            {
                requiredWidth = requiredButtonWidth;
            }
        }
        
        // Yüksekliği kontrol et ve gerekirse artır (butonlar taşıyorsa)
        // Eğer hesaplanan yükseklik mevcut yükseklikten büyükse, yüksekliği artır
        Vector2 currentSize = panelRect.sizeDelta;
        float requiredHeight = Mathf.Max(newHeight, currentSize.y);
        
        // Panel boyutunu ayarla (butonlar taşıyorsa hem genişlik hem yükseklik dinamik)
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
        
        // Panel'i responsive olarak konumlandır (anchor-based)
        // Anchor'ları ortaya al ve pivot'u ortaya al
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Responsive pozisyon: ekran boyutuna göre offset hesapla
        Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        float screenHeight = canvasRect != null ? canvasRect.sizeDelta.y : Screen.height;
        float verticalOffset = screenHeight * panelVerticalOffset;
        panelRect.anchoredPosition = new Vector2(0, verticalOffset);
        
        // Panel genişliğini ekran genişliğine göre sınırla
        float screenWidth = canvasRect != null ? canvasRect.sizeDelta.x : Screen.width;
        float maxWidth = screenWidth * maxPanelWidthPercentage;
        if (requiredWidth > maxWidth)
        {
            requiredWidth = maxWidth;
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
        }
        
        // Panel yüksekliğini ekran yüksekliğine göre sınırla
        float maxHeight = screenHeight * maxPanelHeightPercentage;
        if (requiredHeight > maxHeight)
        {
            requiredHeight = maxHeight;
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
        }
        
        // Container genişliğini de güncelle (eğer panel genişliği değiştiyse)
        if (choicesContainer != null && requiredWidth != originalWidth)
        {
            float updatedContainerWidth = requiredWidth - backgroundPadding * 2;
            containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, updatedContainerWidth);
        }
    }

    public void StartDialogue(DialogueData dialogue, Transform cameraTarget, GuardSimulator.Character.NPC npc = null, DialogueNPC dialogueNPC = null)
    {
        if (isDialogueActive) return;

        currentDialogue = dialogue;
        dialogueCameraTarget = cameraTarget;
        currentNPC = npc; // NPC referansını sakla
        currentDialogueNPC = dialogueNPC; // DialogueNPC referansını sakla
        isDialogueActive = true;

        // Disable FPS Framework input
        FPSFrameworkCore.IsInputActive = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Interaction UI'ı kapat
        if (interactionsManager != null)
        {
            wasInteractionActive = interactionsManager.isActive;
            interactionsManager.isActive = false; // Interaction UI'ı kapat
        }

        dialoguePanel.SetActive(true);
        
        // Panel'i responsive olarak konumlandır
        if (dialoguePanel != null)
        {
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                // Anchor ve pivot ayarları
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                
                // Responsive pozisyon: ekran boyutuna göre offset hesapla
                Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
                RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
                float screenHeight = canvasRect != null ? canvasRect.sizeDelta.y : Screen.height;
                float verticalOffset = screenHeight * panelVerticalOffset;
                panelRect.anchoredPosition = new Vector2(0, verticalOffset);
            }
        }
        
        ShowInitialDialogue();
    }

    private void ShowInitialDialogue()
    {
        if (currentDialogue == null) return;

        DialogueNode startNode = currentDialogue.GetStartNode();
        if (startNode == null)
        {
            EndDialogue();
            return;
        }

        ShowDialogueNode(startNode);
    }

    private void ShowDialogueNode(DialogueNode node)
    {
        if (node == null) return;

        currentDialogueNode = node;

        // Text'leri güvenli bir şekilde ayarla
        try
        {
            if (npcNameText != null)
            {
                // NPC ismini GameObject adından al
                GameObject npcObject = dialogueCameraTarget != null ? dialogueCameraTarget.root.gameObject : null;
                if (npcObject != null)
                {
                    npcNameText.text = npcObject.name;
                }
                else
                {
                    npcNameText.text = "NPC";
                }
            }
            
            if (npcDialogueText != null && !string.IsNullOrEmpty(node.npcText))
            {
                npcDialogueText.text = node.npcText;
                // NPC dialogue text'i kalın ve belirgin yap
                npcDialogueText.fontStyle = FontStyles.Bold;
                if (npcDialogueFontSize > 0)
                {
                    npcDialogueText.fontSize = npcDialogueFontSize;
                }
                
                // Particle text efektlerini oluştur (! ve ? karakterleri için)
                if (enableParticleTextEffects)
                {
                    CreateParticleTextEffects(node.npcText);
                }
            }
        }
        catch (System.Exception e)
        {
            // Hata olsa bile basit text'i ayarla
            if (npcNameText != null) npcNameText.text = "NPC";
            if (npcDialogueText != null) npcDialogueText.text = "Merhaba!";
        }

        ClearChoices();
        
        // Eğer quitAfterText true ise, seçenekleri gösterme, sadece text göster ve kapat
        if (currentDialogueNode.quitAfterText)
        {
            float delay = currentDialogue != null ? currentDialogue.endDialogueDelay : 2f;
            Invoke(nameof(EndDialogue), delay);
            return;
        }
        
        Invoke(nameof(ShowChoices), 1f);
    }

    private void ShowChoices()
    {
        if (currentDialogueNode == null) return;
        
        // Choices yoksa veya boşsa, sadece mesajı göster ve kapanmasını bekle
        if (currentDialogueNode.choices == null || currentDialogueNode.choices.Length == 0)
        {
            Invoke(nameof(EndDialogue), 3f); // 3 saniye sonra kapat
            return;
        }

        isShowingChoices = true;
        
        // Butonları oluştur ve boyutlarını hesapla
        float totalButtonsHeight = 0f;
        float maxButtonWidth = 0f;
        List<RectTransform> buttonRects = new List<RectTransform>();

        foreach (DialogueChoice choice in currentDialogueNode.choices)
        {
            if (choice == null) continue;
            
            Button choiceButton = Instantiate(choiceButtonPrefab, choicesContainer.transform);
            TextMeshProUGUI buttonText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                try
                {
                    buttonText.text = choice.choiceText;
                    // Buton yazı boyutunu ayarla
                    if (choiceButtonFontSize > 0)
                    {
                        buttonText.fontSize = choiceButtonFontSize;
                    }
                    
                    // Text overflow ayarları (yazının aşağıya kayması için)
                    buttonText.overflowMode = TextOverflowModes.Truncate;
                    buttonText.enableWordWrapping = true; // Kelime kaydırma aktif
                    buttonText.autoSizeTextContainer = false;
                    
                    // Buton boyutunu yazıya göre dinamik olarak ayarla
                    RectTransform buttonRect = choiceButton.GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        // Önce text container'ı ayarla (word wrapping için genişlik sınırı gerekli)
                        RectTransform textRect = buttonText.GetComponent<RectTransform>();
                        if (textRect != null)
                        {
                            // Text container'ı butonun içine yerleştir (padding ile)
                            textRect.anchorMin = Vector2.zero;
                            textRect.anchorMax = Vector2.one;
                            textRect.offsetMin = new Vector2(buttonPadding, buttonPadding);
                            textRect.offsetMax = new Vector2(-buttonPadding, -buttonPadding);
                            textRect.anchoredPosition = Vector2.zero;
                            
                            // Önce diyalog panel genişliğini al (maksimum genişlik için)
                            float maxTextWidth = 0f;
                            if (dialoguePanel != null)
                            {
                                RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
                                if (panelRect != null)
                                {
                                    float panelWidth = panelRect.sizeDelta.x;
                                    maxTextWidth = panelWidth - backgroundPadding * 2 - buttonPadding * 2;
                                }
                            }
                            
                            // Eğer maksimum genişlik yoksa, minimum genişlik kullan
                            if (maxTextWidth <= 0)
                            {
                                maxTextWidth = buttonMinWidth - buttonPadding * 2;
                            }
                            
                            // Text container genişliğini maksimum genişlikle sınırla (word wrapping için)
                            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxTextWidth);
                            
                            // Text'i güncelle (word wrapping çalışsın)
                            buttonText.ForceMeshUpdate();
                            
                            // Text'in preferred width ve height'ını al (word wrapping sonrası)
                            float preferredWidth = buttonText.preferredWidth;
                            float preferredHeight = buttonText.preferredHeight;
                            
                            // Buton genişliğini ayarla (text genişliği + padding, maksimum genişlik sınırı içinde)
                            // Eğer text maksimum genişlikten küçükse, text genişliğini kullan
                            // Eğer text maksimum genişlikten büyükse, maksimum genişliği kullan (word wrapping ile aşağıya kayacak)
                            float buttonWidth = Mathf.Max(buttonMinWidth, Mathf.Min(preferredWidth + buttonPadding * 2, maxTextWidth + buttonPadding * 2));
                            // Buton yüksekliğini ayarla (text yüksekliği + padding)
                            float buttonHeight = Mathf.Max(buttonMinHeight, preferredHeight + buttonPadding * 2);
                            
                            // Buton boyutunu ayarla
                            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, buttonWidth);
                            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);
                            
                            // Text container genişliğini güncelle (buton genişliğine göre)
                            float textContainerWidth = buttonWidth - buttonPadding * 2;
                            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textContainerWidth);
                            
                            // Text'i tekrar güncelle (yeni genişlikle)
                            buttonText.ForceMeshUpdate();
                            // Yüksekliği tekrar kontrol et (word wrapping sonrası)
                            float finalHeight = buttonText.preferredHeight;
                            buttonHeight = Mathf.Max(buttonMinHeight, finalHeight + buttonPadding * 2);
                            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);
                        }
                        
                        // En büyük genişliği ve toplam yüksekliği hesapla
                        float finalButtonWidth = buttonRect.sizeDelta.x;
                        float finalButtonHeight = buttonRect.sizeDelta.y;
                        maxButtonWidth = Mathf.Max(maxButtonWidth, finalButtonWidth);
                        totalButtonsHeight += finalButtonHeight;
                        if (buttonRects.Count > 0)
                        {
                            totalButtonsHeight += buttonSpacing; // Butonlar arası boşluk
                        }
                        
                        buttonRects.Add(buttonRect);
                    }
                }
                catch (System.Exception e)
                {
                    // Hata olsa bile text'i ayarla
                    buttonText.text = choice.choiceText;
                }
            }
            
            DialogueChoice capturedChoice = choice;
            choiceButton.onClick.AddListener(() => OnChoiceSelected(capturedChoice));
        }
        
        // Dialogue panel genişliğini al (butonlar için referans)
        float dialoguePanelWidth = 0f;
        if (dialoguePanel != null)
        {
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                dialoguePanelWidth = panelRect.sizeDelta.x;
            }
        }
        
        // Choices container'ın boyutunu ayarla (butonlar dinamik genişlikte ama container tam genişlikte)
        if (choicesContainer != null && buttonRects.Count > 0)
        {
            RectTransform containerRect = choicesContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                // Container'ın genişliğini dialogue panel genişliğine göre ayarla (padding ile)
                float containerWidth = dialoguePanelWidth > 0 ? dialoguePanelWidth - backgroundPadding * 2 : maxButtonWidth;
                containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerWidth);
                // Container'ın yüksekliğini tüm butonların toplam yüksekliğine göre ayarla + alt boşluk
                containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalButtonsHeight + buttonBottomPadding);
            }
        }
        
        // Dialogue panel background'ının boyutunu ayarla (sabit genişlik)
        AdjustDialoguePanelSize();
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        if (!isShowingChoices) return;
        if (choice == null) return;

        isShowingChoices = false;
        ClearChoices();

        // Quest trigger kontrolü (diyalog bitince başlatılacak)
        if (choice.triggerQuest)
        {
            shouldTriggerQuestFromChoice = true;
        }
        
        // Collect quest trigger kontrolü
        if (choice.triggerCollectQuest)
        {
            shouldTriggerCollectQuestFromChoice = true;
        }

        // Tutuklama event'ini kontrol et (hem teslim olma hem de tutuklama içerir)
        if (choice.triggerArrest && currentNPC != null)
        {
            // Önce teslim ol, sonra tutuklan
            currentNPC.Surrender();
            currentNPC.Arrest();
            
            // Tutuklama sonrası diyalogdan çık
            float delay = currentDialogue != null ? currentDialogue.endDialogueDelay : 2f;
            StartCoroutine(EndDialogueAfterDelay(delay));
            return;
        }
        
        // Rage event'ini kontrol et
        if (choice.triggerRage && currentNPC != null)
        {
            // NPC'yi rage moduna geçir
            currentNPC.Rage();
            
            // Rage sonrası diyalogdan çık
            float delay = currentDialogue != null ? currentDialogue.endDialogueDelay : 2f;
            StartCoroutine(EndDialogueAfterDelay(delay));
            return;
        }

        // Otomatik diyalog bitirme kontrolü
        if (choice.autoEndDialogue)
        {
            // Belirli bir süre bekleyip diyalog bitir (delay DialogueData'dan alınır)
            float delay = currentDialogue != null ? currentDialogue.endDialogueDelay : 2f;
            StartCoroutine(EndDialogueAfterDelay(delay));
            return;
        }

        // Next node'a geç veya diyalog bitir
        if (choice.nextNodeID >= 0)
        {
            GoToNextNode(choice.nextNodeID);
        }
        else
        {
            EndDialogue();
        }
    }

    private void GoToNextNode(int nodeID)
    {
        if (currentDialogue == null)
        {
            EndDialogue();
            return;
        }

        DialogueNode nextNode = currentDialogue.GetNodeByID(nodeID);
        if (nextNode != null)
        {
            ShowDialogueNode(nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ClearChoices()
    {
        foreach (Transform child in choicesContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private IEnumerator EndDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        currentNPC = null; // NPC referansını temizle

        // Re-enable FPS Framework input (eğer oyun pause değilse)
        if (!FPSFrameworkCore.IsPaused)
        {
            FPSFrameworkCore.IsInputActive = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // Oyun pause durumundaysa, cursor'ı görünür bırak (pause menü için)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Interaction UI'ı tekrar aç (eğer oyun pause değilse)
        if (interactionsManager != null && !FPSFrameworkCore.IsPaused)
        {
            interactionsManager.isActive = wasInteractionActive; // Önceki duruma döndür
        }

        ClearChoices();
        
        // Particle text efektlerini temizle
        ClearParticleTextEffects();
        
        // Diyalog bittiğinde event'i tetikle (DialogueNPC kamerayı sıfırlasın)
        OnDialogueEnded?.Invoke();
        
        // Görev başlatma kontrolü - sadece triggerQuest ile
        if (shouldTriggerQuestFromChoice)
        {
            StartQuestFromDialogue();
        }
        
        // Collect quest başlatma kontrolü
        if (shouldTriggerCollectQuestFromChoice)
        {
            StartCollectQuestFromDialogue();
        }
        
        // Flag'leri temizle
        shouldTriggerQuestFromChoice = false;
        shouldTriggerCollectQuestFromChoice = false;
        currentDialogueNPC = null;
    }
    
    /// <summary>
    /// Diyalog tamamlandıktan sonra görev başlat
    /// </summary>
    private void StartQuestFromDialogue()
    {
        if (currentDialogueNPC == null) return;
        
        // QuestSystem'i bul
        GuardSimulator.Gameplay.QuestSystem questSystem = FindObjectOfType<GuardSimulator.Gameplay.QuestSystem>();
        if (questSystem == null)
        {
            Debug.LogError("[DialogueManager] QuestSystem bulunamadı! Scene'de bir QuestSystem GameObject'i olmalı.");
            return;
        }
        
        // 1. görev tamamlandı mı kontrol et
        bool isQuest1Completed = questSystem.IsQuest1Completed();
        
        // 2. görev için konuşma kontrolü (1. görev tamamlandıysa)
        if (isQuest1Completed && questSystem.quest2DialogueNPC != null)
        {
            // Bu NPC 2. görev için konuşulacak NPC mi?
            if (currentDialogueNPC.gameObject == questSystem.quest2DialogueNPC.gameObject)
            {
                // 2. görevi başlat
                questSystem.StartQuest2BotKill();
                Debug.Log("[DialogueManager] 2. görev başlatıldı: Bot öldürme görevi.");
                return;
            }
        }
        
        // 1. görev (arrest quest)
        GuardSimulator.Character.NPC targetNPC = currentDialogueNPC.QuestTargetNPC;
        GuardSimulator.Gameplay.VehicleEscortDelivery deliveryVehicle = currentDialogueNPC.QuestDeliveryVehicle;
        
        if (targetNPC == null)
        {
            Debug.LogWarning("[DialogueManager] Görev hedefi NPC atanmamış!");
            return;
        }
        
        if (deliveryVehicle == null)
        {
            Debug.LogWarning("[DialogueManager] Görev teslim noktası (araba) atanmamış!");
            return;
        }
        
        // Quest giver NPC'yi al (currentDialogueNPC'nin NPC component'i)
        GuardSimulator.Character.NPC questGiverNPC = null;
        if (currentNPC != null)
        {
            questGiverNPC = currentNPC;
        }
        else if (currentDialogueNPC != null)
        {
            questGiverNPC = currentDialogueNPC.GetComponent<GuardSimulator.Character.NPC>();
            if (questGiverNPC == null)
            {
                questGiverNPC = currentDialogueNPC.GetComponentInParent<GuardSimulator.Character.NPC>();
            }
        }
        
        questSystem.StartArrestQuest(targetNPC, deliveryVehicle, questGiverNPC);
        Debug.Log($"[DialogueManager] 1. görev başlatıldı: {targetNPC.NPCName} tutuklanacak. Quest giver: {questGiverNPC?.NPCName}");
    }
    
    /// <summary>
    /// Diyalog tamamlandıktan sonra item toplama görevi başlat
    /// </summary>
    private void StartCollectQuestFromDialogue()
    {
        GameObject[] collectibleObjects = GameObject.FindGameObjectsWithTag("Collectible");
        System.Collections.Generic.List<GuardSimulator.Gameplay.CollectibleItem> drugsItems = new System.Collections.Generic.List<GuardSimulator.Gameplay.CollectibleItem>();
        
        foreach (GameObject obj in collectibleObjects)
        {
            GuardSimulator.Gameplay.CollectibleItem item = obj.GetComponent<GuardSimulator.Gameplay.CollectibleItem>();
            if (item == null)
            {
                item = obj.AddComponent<GuardSimulator.Gameplay.CollectibleItem>();
            }
            drugsItems.Add(item);
            Debug.Log($"[DialogueManager] Toplanabilir obje bulundu: {obj.name}");
        }
        
        if (drugsItems.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] Scene'de 'Collectible' tag'ine sahip obje bulunamadı!");
            return;
        }
        
        // Torbaci NPC'sini bul (completion için)
        GuardSimulator.Character.NPC torbaci = null;
        GuardSimulator.Character.NPC[] allNPCs = FindObjectsByType<GuardSimulator.Character.NPC>(FindObjectsSortMode.None);
        foreach (GuardSimulator.Character.NPC npc in allNPCs)
        {
            if (npc != null && npc.NPCName != null && npc.NPCName.ToLower().Contains("torbaci"))
            {
                torbaci = npc;
                Debug.Log($"[DialogueManager] Torbaci NPC bulundu: {npc.NPCName}");
                break;
            }
        }
        
        GuardSimulator.Gameplay.QuestSystem questSystem = GuardSimulator.Gameplay.QuestSystem.Instance;
        if (questSystem != null)
        {
            questSystem.StartCollectQuest(drugsItems, "Gizli Uyuşturucuları Bul", "Gizli uyuşturucuları bul", torbaci);
            Debug.Log($"[DialogueManager] Item toplama görevi başlatıldı: {drugsItems.Count} adet item. Completion NPC: {torbaci?.NPCName ?? "null"}");
        }
    }
    
    /// <summary>
    /// Particle text efektlerini oluştur (! ve ? karakterleri için)
    /// </summary>
    private void CreateParticleTextEffects(string dialogueText)
    {
        if (string.IsNullOrEmpty(dialogueText) || dialogueCameraTarget == null) return;
        
        // Önceki efektleri temizle
        ClearParticleTextEffects();
        
        // NPC pozisyonunu al
        Vector3 npcPosition = dialogueCameraTarget.position;
        if (currentNPC != null && currentNPC.transform != null)
        {
            npcPosition = currentNPC.transform.position;
        }
        
        // Text'teki ! ve ? karakterlerini say
        int exclamationCount = 0;
        int questionCount = 0;
        
        foreach (char c in dialogueText)
        {
            if (c == '!') exclamationCount++;
            else if (c == '?') questionCount++;
        }
        
        // Toplam efekt sayısı (sertlik seviyesine göre)
        int totalEffects = exclamationCount + questionCount;
        
        // Her karakter için efekt oluştur
        for (int i = 0; i < totalEffects; i++)
        {
            // Rastgele karakter seç (! veya ?)
            string character = (i < exclamationCount) ? "!" : "?";
            
            // NPC'nin etrafında rastgele pozisyon
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(0.5f, spawnRadius);
            Vector3 spawnPosition = npcPosition + new Vector3(
                Mathf.Cos(angle) * radius,
                spawnHeightOffset + Random.Range(-0.3f, 0.3f),
                Mathf.Sin(angle) * radius
            );
            
            // TextMeshPro objesi oluştur
            GameObject textObj = new GameObject($"ParticleText_{character}_{i}");
            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            
            // Text ayarları
            textMesh.text = character;
            textMesh.fontSize = particleTextSize;
            textMesh.color = particleTextColor;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 100; // Önde görünsün
            
            // Pozisyon
            textObj.transform.position = spawnPosition;
            Camera mainCam = fpsCameraManager != null && fpsCameraManager.mainCamera != null 
                ? fpsCameraManager.mainCamera 
                : Camera.main;
            if (mainCam != null)
            {
                textObj.transform.LookAt(mainCam.transform);
                textObj.transform.Rotate(0, 180, 0); // Kameraya bakması için
            }
            
            // Animasyon başlat
            StartCoroutine(AnimateParticleText(textObj));
            
            activeParticleTexts.Add(textObj);
        }
    }
    
    /// <summary>
    /// Particle text animasyonu (yukarı doğru hareket)
    /// </summary>
    private IEnumerator AnimateParticleText(GameObject textObj)
    {
        if (textObj == null) yield break;
        
        TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
        if (textMesh == null) yield break;
        
        Vector3 startPosition = textObj.transform.position;
        float elapsed = 0f;
        
        // Fade out için alpha değeri
        Color startColor = particleTextColor;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsed < particleTextLifetime && textObj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / particleTextLifetime;
            
            // Yukarı doğru hareket
            textObj.transform.position = startPosition + Vector3.up * (particleTextSpeed * elapsed);
            
            // Fade out efekti
            if (textMesh != null)
            {
                textMesh.color = Color.Lerp(startColor, endColor, t);
                
                // Hafif scale efekti
                float scale = 1f + (t * 0.5f);
                textObj.transform.localScale = Vector3.one * scale;
            }
            
            // Kameraya bak
            Camera mainCam = fpsCameraManager != null && fpsCameraManager.mainCamera != null 
                ? fpsCameraManager.mainCamera 
                : Camera.main;
            if (mainCam != null)
            {
                textObj.transform.LookAt(mainCam.transform);
                textObj.transform.Rotate(0, 180, 0);
            }
            
            yield return null;
        }
        
        // Objeyi yok et
        if (textObj != null)
        {
            activeParticleTexts.Remove(textObj);
            Destroy(textObj);
        }
    }
    
    /// <summary>
    /// Tüm particle text efektlerini temizle
    /// </summary>
    private void ClearParticleTextEffects()
    {
        foreach (GameObject textObj in activeParticleTexts)
        {
            if (textObj != null)
            {
                Destroy(textObj);
            }
        }
        activeParticleTexts.Clear();
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}
