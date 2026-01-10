using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;
using System;
using System.Reflection;
using Akila.FPSFramework.Internal;

namespace Akila.FPSFramework
{
    [AddComponentMenu("Akila/FPS Framework/Player/Interactions Manager")]
    public class InteractionsManager : MonoBehaviour
    {
        [Tooltip("The allowed range for any interaction")]
        public float range = 5f;
        [Tooltip("If 1 player interaction angle is 360 if 0.5 interaction angle is 180")]
        public float fieldOfInteractions = 0.3f;
        [Tooltip("What layer to interact with")]
        public LayerMask interactableLayers = -1;
        [Tooltip("The UI Object which contains all the data about the interaction")]
        public GameObject HUDObject;
        [Tooltip("The display text for interact key")]
        public TextMeshProUGUI interactKeyText;
        [Tooltip("The interaction name which will show if in range EXAMPLES (Open, Pickup, etc..)")]
        public TextMeshProUGUI interactActionText;
        
        [Header("UI Size Settings")]
        [Tooltip("Font size multiplier for interaction UI (default: 1.5x)")]
        [SerializeField] private float fontSizeMultiplier = 1.5f;
        [Tooltip("UI scale multiplier for interaction panel (default: 1.3x)")]
        [SerializeField] private float uiScaleMultiplier = 1.3f;
        
        public AudioProfile defaultInteractAudio;

        public Audio interactAudio;
        public IInventory Inventory { get; private set; }

        public bool isActive { get; set; } = true;

        public CharacterInput CharacterInput { get; private set; }
        public AudioClip currentInteractAudioClip { get; set; }

        private void Start()
        {
            Inventory = GetComponent<IInventory>();

            CharacterInput = this.SearchFor<CharacterInput>();

            if (CharacterInput != null && CharacterInput.controls != null)
            {
                cachedBindingDisplayString = CharacterInput.controls.Player.Interact.GetBindingDisplayString();
            }
            else
            {
                cachedBindingDisplayString = "F";
            }
            
            // UI boyutlarını ayarla
            SetupUISizes();
        }
        
        /// <summary>
        /// Interaction UI font ve çerçeve boyutlarını ayarla
        /// </summary>
        private void SetupUISizes()
        {
            // Font boyutlarını büyüt
            if (interactKeyText != null)
            {
                interactKeyText.fontSize *= fontSizeMultiplier;
            }
            
            if (interactActionText != null)
            {
                interactActionText.fontSize *= fontSizeMultiplier;
            }
            
            // HUDObject'in scale'ini büyüt
            if (HUDObject != null)
            {
                RectTransform hudRect = HUDObject.GetComponent<RectTransform>();
                if (hudRect != null)
                {
                    hudRect.localScale = Vector3.one * uiScaleMultiplier;
                }
            }
        }

        private void OnEnable()
        {
            interactAudio = new Audio();

            if (defaultInteractAudio != null)
            {
                currentInteractAudioClip = defaultInteractAudio.audioClip;
                interactAudio.Setup(gameObject, defaultInteractAudio);
            }
        }


        private string cachedBindingDisplayString;
        IInteractable interactable;


        private void Update()
        {
            interactable = GetInteractable();

            if (HUDObject)
            HUDObject.SetActive(isActive && interactable != null);

            if(interactable != null && isActive)
            {
                string interactionName = interactable.GetInteractionName();
                
                // DialogueNPC olup olmadığını ve tutuklu olup olmadığını kontrol et
                bool isDialogueNPC = interactable != null && interactable.GetType().Name == "DialogueNPC";
                bool isArrestedNPC = false;
                
                if (isDialogueNPC)
                {
                    // Reflection ile NPC component'ini kontrol et
                    var npcComponentField = interactable.GetType().GetField("npcComponent", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (npcComponentField != null)
                    {
                        var npcComponent = npcComponentField.GetValue(interactable);
                        if (npcComponent != null)
                        {
                            var isArrestedProperty = npcComponent.GetType().GetProperty("IsArrested");
                            if (isArrestedProperty != null)
                            {
                                isArrestedNPC = (bool)isArrestedProperty.GetValue(npcComponent);
                            }
                        }
                    }
                }
                
                // Eğer "Serbest Bırak" içeriyorsa X tuşu ile çalışmalı
                bool isReleaseAction = interactionName.Contains("Serbest Bırak", System.StringComparison.OrdinalIgnoreCase) ||
                                      interactionName.Contains("X", System.StringComparison.OrdinalIgnoreCase);
                
                // Tutuklu NPC için hem X hem de K tuşunu göster
                if (isArrestedNPC)
                {
                    if (interactKeyText)
                    {
                        // Tutuklu NPC için hem X hem de K tuşunu göster
                        string keyText = cachedBindingDisplayString;
                        if (keyText.Contains("|"))
                        {
                            keyText = keyText.Split('|')[0].Trim();
                        }
                        keyText = keyText.Replace("X", "").Trim();
                        // "X / K" formatında göster
                        interactKeyText.text = "X / " + keyText;
                    }
                    
                    if(interactActionText)
                    {
                        // GetInteractionName() "X Serbest Bırak | Konuş" formatında döner
                        // Parse et: "|" ile ayır
                        string releaseText = "Serbest Bırak";
                        string dialogueText = "Konuş";
                        
                        if (interactionName.Contains("|"))
                        {
                            string[] parts = interactionName.Split('|');
                            if (parts.Length >= 1)
                            {
                                // İlk kısım: "X Serbest Bırak"
                                releaseText = parts[0].Trim();
                                releaseText = releaseText.Replace("X ", "").Replace("X", "").Trim();
                                if (string.IsNullOrEmpty(releaseText))
                                {
                                    releaseText = "Serbest Bırak";
                                }
                            }
                            if (parts.Length >= 2)
                            {
                                // İkinci kısım: "Konuş"
                                dialogueText = parts[1].Trim();
                                dialogueText = dialogueText.Replace("X ", "").Replace("X", "").Trim();
                                if (string.IsNullOrEmpty(dialogueText))
                                {
                                    dialogueText = "Konuş";
                                }
                            }
                        }
                        else
                        {
                            // Fallback: Eğer "|" yoksa sadece serbest bırakma adını göster
                            releaseText = interactionName.Replace("X ", "").Replace("X", "").Trim();
                            if (string.IsNullOrEmpty(releaseText))
                            {
                                releaseText = "Serbest Bırak";
                            }
                        }
                        
                        interactActionText.text = releaseText + " / " + dialogueText;
                    }
                }
                else
                {
                    // Normal NPC için mevcut mantık
                    if (interactKeyText)
                    {
                        if (isReleaseAction)
                        {
                            // Serbest bırakma için X tuşunu göster
                            interactKeyText.text = "X";
                        }
                        else
                        {
                            // Normal etkileşim için mevcut tuşu göster
                            string keyText = cachedBindingDisplayString;
                            if (keyText.Contains("|"))
                            {
                                keyText = keyText.Split('|')[0].Trim();
                            }
                            // "X" karakterini kaldır
                            keyText = keyText.Replace("X", "").Trim();
                            interactKeyText.text = keyText;
                        }
                    }
                    
                    if(interactActionText)
                    {
                        // "|" karakterinden sonraki her şeyi kaldır (örn: "K | X Konuş" -> "K")
                        if (interactionName.Contains("|"))
                        {
                            interactionName = interactionName.Split('|')[0].Trim();
                        }
                        
                        // Eğer serbest bırakma aksiyonu değilse "X" karakterini kaldır
                        if (!isReleaseAction)
                        {
                            interactionName = interactionName.Replace("X ", "").Replace("X", "").Trim();
                        }
                        else
                        {
                            // Serbest bırakma aksiyonu ise "X " kısmını kaldır, sadece "Serbest Bırak" göster
                            interactionName = interactionName.Replace("X ", "").Replace("X", "").Trim();
                        }
                        
                        interactActionText.text = interactionName;
                    }
                }

                // X tuşu kontrolü (serbest bırakma için)
                bool xKeyPressed = false;
                if (isReleaseAction || isArrestedNPC)
                {
                    // Unity Input System ile X tuşu kontrolü
                    if (UnityEngine.InputSystem.Keyboard.current != null)
                    {
                        xKeyPressed = UnityEngine.InputSystem.Keyboard.current.xKey.wasPressedThisFrame;
                    }
                    // Fallback: Eski Input sistemi
                    if (!xKeyPressed)
                    {
                        xKeyPressed = Input.GetKeyDown(KeyCode.X);
                    }
                }

                // Normal etkileşim tuşu kontrolü (F/K veya mevcut tuş)
                bool interactKeyPressed = false;
                if (!isReleaseAction || isArrestedNPC)
                {
                    if (CharacterInput != null && CharacterInput.controls != null)
                    {
                        interactKeyPressed = CharacterInput.controls.Player.Interact.triggered;
                    }
                }

                // Etkileşimi tetikle
                if (xKeyPressed || interactKeyPressed)
                {
                    // Tutuklu NPC ve K tuşu basıldıysa InteractDialogue metodunu çağır
                    if (isArrestedNPC && interactKeyPressed && !xKeyPressed)
                    {
                        // Reflection ile InteractDialogue metodunu çağır
                        var interactDialogueMethod = interactable.GetType().GetMethod("InteractDialogue",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (interactDialogueMethod != null)
                        {
                            interactDialogueMethod.Invoke(interactable, new object[] { this });
                        }
                        else
                        {
                            // Fallback: Normal Interact çağır
                            interactable.Interact(this);
                        }
                    }
                    else
                    {
                        // X tuşu veya normal etkileşim
                        interactable.Interact(this);
                    }
                }
            }
        }

        List<IInteractable> interactablesList = new List<IInteractable>();
         IInteractable closestInteractable = null;

        public IInteractable GetInteractable()
        {
            interactablesList.Clear();

            Collider[] colliders = Physics.OverlapSphere(transform.position, range, interactableLayers);

            foreach (Collider collider in colliders)
            {
                // Aynı GameObject'te ve child'larda birden fazla IInteractable olabilir, hepsini bul
                IInteractable[] allInteractables = collider.GetComponentsInParent<IInteractable>();
                foreach (IInteractable interactable in allInteractables)
                {
                    // Zaten eklenmişse tekrar ekleme
                    if (!interactablesList.Contains(interactable))
                    {
                        // GetInteractionName boş değilse ekle (aktif olanları)
                        string interactionName = interactable.GetInteractionName();
                        if (!string.IsNullOrEmpty(interactionName))
                        {
                            interactablesList.Add(interactable);
                        }
                    }
                }
            }

            // DialogueNPC'leri öncelikli yap (reflection ile kontrol et)
            List<IInteractable> dialogueNPCs = new List<IInteractable>();
            List<IInteractable> otherInteractables = new List<IInteractable>();

            foreach (IInteractable interactable in interactablesList)
            {
                // DialogueNPC'yi reflection ile kontrol et (namespace sorunu olmaması için)
                if (interactable != null && interactable.GetType().Name == "DialogueNPC")
                {
                    dialogueNPCs.Add(interactable);
                }
                else
                {
                    otherInteractables.Add(interactable);
                }
            }

            // Önce DialogueNPC'leri kontrol et
            if (dialogueNPCs.Count > 0)
            {
                closestInteractable = null;
                float closestDistance = float.MaxValue;

                foreach (IInteractable interactable in dialogueNPCs)
                {
                    Vector3 directionToInteractable = (interactable.transform.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(transform.forward, directionToInteractable);
                    
                    if (dotProduct > fieldOfInteractions)
                    {
                        float distance = Vector3.Distance(transform.position, interactable.transform.position);
                        
                        if (closestInteractable == null || distance < closestDistance)
                        {
                            closestInteractable = interactable;
                            closestDistance = distance;
                        }
                    }
                }

                if (closestInteractable != null)
                    return closestInteractable;
            }

            // DialogueNPC yoksa diğer IInteractable'ları kontrol et
            closestInteractable = null;
            float closestDistance2 = float.MaxValue;

            foreach (IInteractable interactable in otherInteractables)
            {
                Vector3 directionToInteractable = (interactable.transform.position - transform.position).normalized;
                float dotProduct = Vector3.Dot(transform.forward, directionToInteractable);
                
                // Check if interactable is in front of player (dot product should be > fieldOfInteractions)
                // fieldOfInteractions: 0.5 = 180 degrees, 1.0 = 360 degrees (all directions)
                if (dotProduct > fieldOfInteractions)
                {
                    float distance = Vector3.Distance(transform.position, interactable.transform.position);
                    
                    if (closestInteractable == null || distance < closestDistance2)
                    {
                        closestInteractable = interactable;
                        closestDistance2 = distance;
                    }
                }
            }

            return closestInteractable;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
        }

        /// <summary>
        /// Editor-only context menu option to set up network components.
        /// </summary>
        [ContextMenu("Setup/Network Components")]
        private void SetupNetworkComponents()
        {
            FPSFrameworkCore.InvokeConvertMethod("ConvertInteractionsManager", this, new object[] { this });
        }
    }
}