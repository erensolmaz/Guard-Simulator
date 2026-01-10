using UnityEngine;
using UnityEngine.InputSystem;
using Akila.FPSFramework;
using System.Linq;

namespace GuardSimulator.Character
{
    public class PlayerEscortController : MonoBehaviour
    {
        [Header("Escort Settings")]
        [Tooltip("Etkileşim mesafesi (NPC'yi almak için)")]
        [SerializeField] private float interactionDistance = 3f;
        
        [Tooltip("Etkileşim layer mask")]
        [SerializeField] private LayerMask npcLayerMask = -1;
        
        [Header("Movement Restriction")]
        [Tooltip("Escort sırasında maksimum hareket hızı (normal yürümenin yarısı)")]
        [SerializeField] private float escortMoveSpeedMultiplier = 0.5f;
        
        [Header("Weapon Restriction")]
        [Tooltip("Escort sırasında izin verilen silah isimleri (büyük/küçük harf duyarsız)")]
        [SerializeField] private string[] allowedWeaponNames = new string[] { "pistol", "handgun", "glock", "revolver" };
        
        [Header("Input Keys")]
        [Tooltip("Escort başlatma/durdurma tuşu")]
        [SerializeField] private KeyCode escortKey = KeyCode.E;
        
        [Tooltip("Serbest bırakma tuşu")]
        [SerializeField] private KeyCode releaseKey = KeyCode.X;
        
        [Header("UI Messages")]
        [SerializeField] private string escortStartMessage = "Tutukluyu Al (E)";
        [SerializeField] private string escortStopMessage = "Bırak (X)";
        
        private NPC currentEscortTarget;
        private bool isEscortingTarget = false;
        private Camera playerCamera;
        private Inventory playerInventory;
        private MonoBehaviour weaponManager;
        private FirstPersonController firstPersonController;
        
        private float originalWalkSpeed;
        private float originalSprintSpeed;
        private float originalTacticalSprintSpeed;
        
        public bool IsEscortingTarget => isEscortingTarget;
        public NPC CurrentEscortTarget => currentEscortTarget;
        
        private void Start()
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            playerInventory = GetComponentInChildren<Inventory>();
            if (playerInventory == null)
            {
                playerInventory = GetComponent<Inventory>();
            }
            
            firstPersonController = GetComponent<FirstPersonController>();
            if (firstPersonController != null)
            {
                originalWalkSpeed = firstPersonController.walkSpeed;
                originalSprintSpeed = firstPersonController.sprintSpeed;
                originalTacticalSprintSpeed = firstPersonController.tacticalSprintSpeed;
            }
            
            MonoBehaviour[] allComponents = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour comp in allComponents)
            {
                if (comp.GetType().Name == "WeaponManager")
                {
                    weaponManager = comp;
                    break;
                }
            }
        }
        
        private void Update()
        {
            if (isEscortingTarget)
            {
                HandleEscortMode();
            }
            else
            {
                HandleNormalMode();
            }
        }
        
        private void HandleNormalMode()
        {
            if (Input.GetKeyDown(escortKey))
            {
                TryStartEscort();
            }
        }
        
        private void HandleEscortMode()
        {
            // Silah kısıtlamasını kontrol et
            EnforceWeaponRestriction();
            
            // Serbest bırakma
            if (Input.GetKeyDown(releaseKey))
            {
                StopEscort();
            }
            
            // Hedef yoksa veya artık tutuklanmamışsa escort'u sonlandır
            if (currentEscortTarget == null || !currentEscortTarget.IsArrested)
            {
                StopEscort();
            }
        }
        
        private void TryStartEscort()
        {
            if (playerCamera == null) return;
            
            // Kamera önünden raycast yap
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, npcLayerMask))
            {
                NPC npc = hit.collider.GetComponent<NPC>();
                if (npc == null)
                {
                    npc = hit.collider.GetComponentInParent<NPC>();
                }
                
                if (npc != null && npc.IsArrested && !npc.IsBeingEscorted)
                {
                    StartEscort(npc);
                }
            }
        }
        
        private void StartEscort(NPC npc)
        {
            currentEscortTarget = npc;
            isEscortingTarget = true;
            
            // NPC'yi escort moduna al
            npc.StartEscort(transform);
            
            // Player hızını düşür
            if (firstPersonController != null)
            {
                float escortSpeed = originalWalkSpeed * escortMoveSpeedMultiplier;
                firstPersonController.walkSpeed = escortSpeed;
                firstPersonController.sprintSpeed = escortSpeed;
                firstPersonController.tacticalSprintSpeed = escortSpeed;
            }
            
            Debug.Log($"Escort başlatıldı: {npc.NPCName}");
        }
        
        public void StopEscort()
        {
            if (currentEscortTarget != null)
            {
                currentEscortTarget.StopEscort();
                Debug.Log($"Escort sonlandırıldı: {currentEscortTarget.NPCName}");
            }
            
            currentEscortTarget = null;
            isEscortingTarget = false;
            
            // Player hızını normale döndür
            if (firstPersonController != null)
            {
                firstPersonController.walkSpeed = originalWalkSpeed;
                firstPersonController.sprintSpeed = originalSprintSpeed;
                firstPersonController.tacticalSprintSpeed = originalTacticalSprintSpeed;
            }
        }
        
        private void EnforceWeaponRestriction()
        {
            if (playerInventory == null) return;
            
            InventoryItem currentItem = playerInventory.currentItem;
            if (currentItem == null) return;
            
            Firearm firearm = currentItem.GetComponent<Firearm>();
            if (firearm == null) return;
            
            // Eğer silah izin verilen kategorilerde değilse, tabancaya geç
            if (!IsWeaponAllowed(firearm))
            {
                SwitchToAllowedWeapon();
            }
        }
        
        private bool IsWeaponAllowed(Firearm firearm)
        {
            string weaponName = firearm.gameObject.name.ToLower();
            
            foreach (string allowedName in allowedWeaponNames)
            {
                if (weaponName.Contains(allowedName.ToLower()))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void SwitchToAllowedWeapon()
        {
            if (playerInventory == null) return;
            
            // İzin verilen silahları bul
            for (int i = 0; i < playerInventory.items.Count; i++)
            {
                InventoryItem item = playerInventory.items[i];
                Firearm firearm = item.GetComponent<Firearm>();
                
                if (firearm != null && IsWeaponAllowed(firearm))
                {
                    playerInventory.Switch(i, true);
                    return;
                }
            }
            
            Debug.LogWarning("Escort sırasında kullanılabilir silah bulunamadı!");
        }
        
        public string GetInteractionMessage()
        {
            if (isEscortingTarget)
            {
                return escortStopMessage;
            }
            
            if (playerCamera != null)
            {
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, npcLayerMask))
                {
                    NPC npc = hit.collider.GetComponent<NPC>();
                    if (npc == null)
                    {
                        npc = hit.collider.GetComponentInParent<NPC>();
                    }
                    
                    if (npc != null && npc.IsArrested && !npc.IsBeingEscorted)
                    {
                        return escortStartMessage;
                    }
                }
            }
            
            return string.Empty;
        }
        
        private void OnDestroy()
        {
            if (isEscortingTarget && currentEscortTarget != null)
            {
                currentEscortTarget.StopEscort();
            }
        }
    }
}
