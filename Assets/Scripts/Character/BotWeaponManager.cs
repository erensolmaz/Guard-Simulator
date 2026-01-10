using UnityEngine;
using Akila.FPSFramework;
using System.Collections;
using System.Reflection;
using UnityEngine.Animations.Rigging;
using GuardSimulator.Character;

namespace GuardSimulator.Character
{
    /// <summary>
    /// Botlara silah ekleme sistemi - Rage modunda items[0] prefab'ından silah ekler
    /// </summary>
    [AddComponentMenu("Guard Simulator/Bot/Bot Weapon Manager")]
    public class BotWeaponManager : MonoBehaviour
    {
        [Header("IK Ayarları")]
        [Tooltip("IK kullanarak ellerin silaha gitmesini sağla (Rage modunda otomatik açılır)")]
        [SerializeField] private bool useIK = false;
        
        [Tooltip("Rage modunda otomatik IK açılsın mı?")]
        [SerializeField] private bool enableIKOnRage = true;
        
        [Tooltip("OnAnimatorIK mi yoksa Animation Rigging mi kullanılacak?")]
        [SerializeField] private bool useAnimationRigging = false;
        
        [Tooltip("IK weight (0-1 arası)")]
        [Range(0f, 1f)]
        [SerializeField] private float ikWeight = 1f;
        
        [Tooltip("IK'a geçiş hızı")]
        [SerializeField] private float ikTransitionSpeed = 10f;
        
        [Tooltip("Sol el için IK target pozisyonu (silahın local space'inde)")]
        [SerializeField] private Vector3 leftHandIKPosition = new Vector3(-0.1f, 0f, 0f);
        
        [Tooltip("Sağ el için IK target pozisyonu (silahın local space'inde)")]
        [SerializeField] private Vector3 rightHandIKPosition = new Vector3(0.1f, 0f, 0f);
        
        [Tooltip("Sol el için IK target rotasyonu (Euler angles)")]
        [SerializeField] private Vector3 leftHandIKRotation = new Vector3(0, 0, -90f);
        
        [Tooltip("Sağ el için IK target rotasyonu (Euler angles)")]
        [SerializeField] private Vector3 rightHandIKRotation = new Vector3(0, 0, -90f);
        
        [Header("Silah Pozisyon Ayarları")]
        [Tooltip("Silahın yerleştirileceği transform (boş bırakılırsa inventory.transform kullanılır)")]
        [SerializeField] private Transform handTransform;
        
        [Header("Recoil Ayarları")]
        [Tooltip("Recoil'de ellerin ne kadar tepki vereceği (1 = tam recoil, 0 = tepki yok)")]
        [Range(0f, 2f)]
        [SerializeField] private float recoilSensitivity = 1f;
        
        [Tooltip("Recoil'in geri dönme hızı")]
        [SerializeField] private float recoilRecoverySpeed = 10f;
        
        // IK için gerekli referanslar
        private Animator animator;
        private Inventory inventory;
        private NPC npc;
        private Transform leftHandIKTarget;
        private Transform rightHandIKTarget;
        private Transform weaponTransform;
        private float currentIKWeight = 0f;
        private InventoryItem currentWeaponItem;
        private Firearm currentFirearm;
        
        // Recoil için
        private Vector3 leftHandRecoilOffset = Vector3.zero;
        private Vector3 rightHandRecoilOffset = Vector3.zero;
        private Vector3 baseLeftHandPosition;
        private Vector3 baseRightHandPosition;
        
        // Animation Rigging için
        private RigBuilder rigBuilder;
        private Rig weaponRig;
        private TwoBoneIKConstraint leftHandIKConstraint;
        private TwoBoneIKConstraint rightHandIKConstraint;
        
        private void Awake()
        {
            inventory = GetComponentInChildren<Inventory>();
            animator = GetComponentInChildren<Animator>();
            npc = GetComponent<NPC>();
            if (npc == null) npc = GetComponentInParent<NPC>();
            
            // Inventory.isActive'yi false yap ki başlangıçta silah instantiate edilmesin
            // Rage modunda items[0] prefab'ından silah instantiate edeceğiz
            if (inventory != null)
            {
                var isActiveProperty = typeof(Inventory).GetProperty("isActive",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (isActiveProperty != null && isActiveProperty.CanWrite)
                {
                    isActiveProperty.SetValue(inventory, false);
                }
                
                // Items listesini temizle (Inventory.Start() uyarısını önlemek için)
                var itemsField = typeof(Inventory).GetField("items",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (itemsField != null)
                {
                    var itemsList = itemsField.GetValue(inventory) as System.Collections.Generic.List<InventoryItem>;
                    if (itemsList != null)
                    {
                        itemsList.Clear();
                    }
                }
            }
            
            EnsureRequiredComponents();
        }
        
        private void Start()
        {
            if (inventory == null || npc == null) return;
            
            StartCoroutine(SetupRageModeListener());
        }
        
        private void OnAnimatorIK(int layerIndex)
        {
            if (useAnimationRigging || !useIK) return;
            
            if (animator == null || animator.avatar == null || !animator.avatar.isValid) return;
            
            if (weaponTransform == null || leftHandIKTarget == null || rightHandIKTarget == null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                return;
            }
            
            currentIKWeight = Mathf.Lerp(currentIKWeight, ikWeight, Time.deltaTime * ikTransitionSpeed);
            
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentIKWeight);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentIKWeight);
            
            // Recoil offset'lerini uygula (world space'de offset ekle)
            // Recoil offset'leri silahın local space'inde, bu yüzden world space'e çevir
            Vector3 leftHandRecoilWorld = weaponTransform != null ? weaponTransform.TransformDirection(leftHandRecoilOffset) : leftHandRecoilOffset;
            Vector3 rightHandRecoilWorld = weaponTransform != null ? weaponTransform.TransformDirection(rightHandRecoilOffset) : rightHandRecoilOffset;
            
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position + leftHandRecoilWorld);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position + rightHandRecoilWorld);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
        }
        
        private void Update()
        {
            // CharacterManager için SetValues çağrısı (Akila Framework gereksinimi)
            CharacterManager characterManager = GetComponent<CharacterManager>();
            if (characterManager != null)
            {
                // Bot animasyon tabanlı hareket ediyor, basit default değerler kullan
                characterManager.SetValues(Vector3.zero, true, 0f, 0f);
            }
        }
        
        private void LateUpdate()
        {
            // Recoil offset'lerini geri döndür
            leftHandRecoilOffset = Vector3.Lerp(leftHandRecoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
            rightHandRecoilOffset = Vector3.Lerp(rightHandRecoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
            
            if (!useAnimationRigging || !useIK) return;
            
            currentIKWeight = Mathf.Lerp(currentIKWeight, ikWeight, Time.deltaTime * ikTransitionSpeed);
            
            if (leftHandIKConstraint != null)
            {
                leftHandIKConstraint.weight = currentIKWeight;
                // Animation Rigging için recoil offset'i constraint target'a uygula
                if (leftHandIKConstraint.data.target != null)
                {
                    leftHandIKConstraint.data.target.localPosition = baseLeftHandPosition + leftHandRecoilOffset;
                }
            }
            
            if (rightHandIKConstraint != null)
            {
                rightHandIKConstraint.weight = currentIKWeight;
                if (rightHandIKConstraint.data.target != null)
                {
                    rightHandIKConstraint.data.target.localPosition = baseRightHandPosition + rightHandRecoilOffset;
                }
            }
        }
        
        private IEnumerator SetupRageModeListener()
        {
            yield return null;
            yield return null;
            
            if (npc == null) yield break;
            
            bool lastRageState = npc.IsRage;
            
            while (npc != null)
            {
                bool currentRageState = npc.IsRage;
                
                if (currentRageState != lastRageState)
                {
                    if (currentRageState)
                    {
                        OnRageModeActivated();
                    }
                    
                    lastRageState = currentRageState;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private void OnRageModeActivated()
        {
            if (!enableIKOnRage || inventory == null) return;
            
            useIK = true;
            
            // items[0] prefab'ından silah instantiate et
            if (inventory.startItems == null || inventory.startItems.Count == 0) return;
            
            InventoryItem weaponPrefab = null;
            foreach (InventoryItem item in inventory.startItems)
            {
                if (item != null && item.GetComponent<Firearm>() != null)
                {
                    weaponPrefab = item;
                    break;
                }
            }
            
            if (weaponPrefab == null) return;
            
            // Zaten instantiate edilmiş silah var mı kontrol et
            InventoryItem[] allItems = inventory.transform.GetComponentsInChildren<InventoryItem>(true);
            InventoryItem existingWeapon = null;
            foreach (InventoryItem item in allItems)
            {
                if (item != null && item.GetComponent<Firearm>() != null && 
                    item.name.Contains(weaponPrefab.name.Replace("(Clone)", "")))
                {
                    existingWeapon = item;
                    break;
                }
            }
            
            InventoryItem weapon = existingWeapon;
            
            // Yoksa instantiate et
            if (weapon == null)
            {
                Transform weaponParent = inventory.transform;
                if (handTransform != null && handTransform.IsChildOf(inventory.transform))
                {
                    weaponParent = handTransform;
                }
                
                weapon = Instantiate(weaponPrefab, weaponParent);
                weapon.name = weaponPrefab.name;
            }
            
            // Pozisyonu sıfırla
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            
            // Aktif et
            weapon.gameObject.SetActive(true);
            
            // Firearm setup
            SetupFirearmForBot(weapon);
            DisableFirearmHUD(weapon);
            
            // IK target'ları oluştur
            weaponTransform = weapon.transform;
            currentWeaponItem = weapon;
            currentFirearm = weapon.GetComponent<Firearm>();
            SetupIKTargets(weaponTransform);
            
            // Firearm event'lerini dinle
            SetupFirearmEvents();
            
            // Inventory'e ekle ve aktif et
            UpdateInventoryItemsList();
            int index = inventory.items.IndexOf(weapon);
            if (index >= 0)
            {
                inventory.Switch(index);
            }
            
            // BotAI'ya bildir
            StartCoroutine(NotifyBotAIAfterWeaponReady());
        }
        
        private IEnumerator NotifyBotAIAfterWeaponReady()
        {
            yield return null;
            
            BotAI botAI = GetComponent<BotAI>();
            if (botAI == null) botAI = GetComponentInParent<BotAI>();
            
            if (botAI != null)
            {
                var refreshMethod = typeof(BotAI).GetMethod("RefreshWeapon", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(botAI, null);
                }
            }
        }
        
        private void SetupIKTargets(Transform weapon)
        {
            if (leftHandIKTarget != null) Destroy(leftHandIKTarget.gameObject);
            if (rightHandIKTarget != null) Destroy(rightHandIKTarget.gameObject);
            
            leftHandIKTarget = new GameObject("LeftHandIKTarget").transform;
            rightHandIKTarget = new GameObject("RightHandIKTarget").transform;
            
            leftHandIKTarget.SetParent(weapon);
            rightHandIKTarget.SetParent(weapon);
            
            baseLeftHandPosition = leftHandIKPosition;
            baseRightHandPosition = rightHandIKPosition;
            
            leftHandIKTarget.localPosition = baseLeftHandPosition;
            leftHandIKTarget.localRotation = Quaternion.Euler(leftHandIKRotation);
            rightHandIKTarget.localPosition = baseRightHandPosition;
            rightHandIKTarget.localRotation = Quaternion.Euler(rightHandIKRotation);
            
            // Recoil offset'leri sıfırla
            leftHandRecoilOffset = Vector3.zero;
            rightHandRecoilOffset = Vector3.zero;
            
            if (useAnimationRigging)
            {
                SetupAnimationRigging();
                
                // Animation Rigging için base position'ları constraint target'lardan al
                if (leftHandIKConstraint != null && leftHandIKConstraint.data.target != null)
                {
                    baseLeftHandPosition = leftHandIKConstraint.data.target.localPosition;
                }
                if (rightHandIKConstraint != null && rightHandIKConstraint.data.target != null)
                {
                    baseRightHandPosition = rightHandIKConstraint.data.target.localPosition;
                }
            }
        }
        
        private void SetupAnimationRigging()
        {
            if (animator == null) return;
            
            rigBuilder = animator.GetComponent<RigBuilder>();
            if (rigBuilder == null) rigBuilder = animator.gameObject.AddComponent<RigBuilder>();
            
            weaponRig = animator.transform.Find("WeaponRig")?.GetComponent<Rig>();
            if (weaponRig == null)
            {
                GameObject rigGO = new GameObject("WeaponRig");
                rigGO.transform.SetParent(animator.transform);
                weaponRig = rigGO.AddComponent<Rig>();
                weaponRig.weight = 1f;
            }
            
            rigBuilder.layers.Clear();
            rigBuilder.layers.Add(new RigLayer(weaponRig, true));
            
            if (leftHandIKConstraint == null)
            {
                GameObject leftGO = new GameObject("LeftHandIK");
                leftGO.transform.SetParent(weaponRig.transform);
                leftHandIKConstraint = leftGO.AddComponent<TwoBoneIKConstraint>();
                SetupHandIKConstraint(leftHandIKConstraint, AvatarIKGoal.LeftHand);
            }
            
            if (rightHandIKConstraint == null)
            {
                GameObject rightGO = new GameObject("RightHandIK");
                rightGO.transform.SetParent(weaponRig.transform);
                rightHandIKConstraint = rightGO.AddComponent<TwoBoneIKConstraint>();
                SetupHandIKConstraint(rightHandIKConstraint, AvatarIKGoal.RightHand);
            }
        }
        
        private void SetupHandIKConstraint(TwoBoneIKConstraint constraint, AvatarIKGoal goal)
        {
            if (animator == null || !animator.isHuman) return;
            
            HumanBodyBones rootBone = goal == AvatarIKGoal.LeftHand ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
            HumanBodyBones midBone = goal == AvatarIKGoal.LeftHand ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
            HumanBodyBones tipBone = goal == AvatarIKGoal.LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            
            constraint.data.root = animator.GetBoneTransform(rootBone);
            constraint.data.mid = animator.GetBoneTransform(midBone);
            constraint.data.tip = animator.GetBoneTransform(tipBone);
            constraint.data.target = goal == AvatarIKGoal.LeftHand ? leftHandIKTarget : rightHandIKTarget;
        }
        
        private void SetupFirearmForBot(InventoryItem weapon)
        {
            Firearm firearm = weapon.GetComponent<Firearm>();
            if (firearm == null || firearm.preset == null) return;
            
            var characterInputField = typeof(Inventory).GetField("characterInput", 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (characterInputField != null && inventory != null)
            {
                var characterInput = characterInputField.GetValue(inventory);
                if (characterInput != null)
                {
                    var itemInputType = typeof(ItemInput);
                    var inventoryField = itemInputType.GetField("Inventory", 
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (inventoryField != null)
                    {
                        ItemInput itemInput = weapon.GetComponent<ItemInput>();
                        if (itemInput == null) itemInput = weapon.gameObject.AddComponent<ItemInput>();
                        inventoryField.SetValue(itemInput, inventory);
                    }
                }
            }
            
            int playerLayer = 8;
            LayerMask currentLayers = firearm.preset.hittableLayers;
            if ((currentLayers.value & (1 << playerLayer)) == 0)
            {
                firearm.preset.hittableLayers = currentLayers | (1 << playerLayer);
            }
        }
        
        private void DisableFirearmHUD(InventoryItem weapon)
        {
            Firearm firearm = weapon.GetComponent<Firearm>();
            if (firearm == null) return;
            
            var hudField = typeof(Firearm).GetField("firearmHUD", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (hudField != null)
            {
                FirearmHUD hud = hudField.GetValue(firearm) as FirearmHUD;
                if (hud != null && hud.gameObject != null)
                {
                    hud.gameObject.SetActive(false);
                }
                hudField.SetValue(firearm, null);
            }
        }
        
        private void UpdateInventoryItemsList()
        {
            if (inventory == null) return;
            
            var itemsField = typeof(Inventory).GetField("items", 
                BindingFlags.Public | BindingFlags.Instance);
            if (itemsField != null)
            {
                var itemsList = itemsField.GetValue(inventory) as System.Collections.Generic.List<InventoryItem>;
                if (itemsList != null)
                {
                    itemsList.Clear();
                    InventoryItem[] allItems = inventory.transform.GetComponentsInChildren<InventoryItem>(true);
                    foreach (InventoryItem item in allItems)
                    {
                        if (item != null && item.gameObject.activeInHierarchy)
                        {
                            itemsList.Add(item);
                        }
                    }
                }
            }
        }
        
        private void SetupFirearmEvents()
        {
            if (currentFirearm == null) return;
            
            // Firearm events field'ını bul (reflection ile)
            var eventsField = typeof(Firearm).GetField("events",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (eventsField != null)
            {
                var events = eventsField.GetValue(currentFirearm) as FirearmEvents;
                if (events != null && events.OnFireDone != null)
                {
                    // Mevcut listener'ları kaldır ve yenisini ekle
                    events.OnFireDone.RemoveListener(OnFirearmFired);
                    events.OnFireDone.AddListener(OnFirearmFired);
                }
            }
        }
        
        private void OnFirearmFired(Vector3 position, Quaternion rotation, Vector3 direction)
        {
            if (currentFirearm == null || currentFirearm.preset == null) return;
            if (!useIK) return;
            
            // Recoil değerlerini al
            float verticalRecoil = currentFirearm.preset.verticalRecoil;
            float horizontalRecoil = currentFirearm.preset.horizontalRecoil;
            
            // Attachment modifier'larını al
            var attachmentsManagerField = typeof(Firearm).GetField("firearmAttachmentsManager",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (attachmentsManagerField != null)
            {
                var attachmentsManager = attachmentsManagerField.GetValue(currentFirearm);
                if (attachmentsManager != null)
                {
                    var recoilProperty = attachmentsManager.GetType().GetProperty("recoil",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (recoilProperty != null)
                    {
                        float recoilModifier = (float)recoilProperty.GetValue(attachmentsManager);
                        verticalRecoil *= recoilModifier;
                        horizontalRecoil *= recoilModifier;
                    }
                }
            }
            
            // Recoil offset'lerini hesapla (silahın local space'inde)
            // Vertical recoil: geriye doğru (-Z) ve yukarı (+Y)
            // Horizontal recoil: sağa/sola (random X ekseninde)
            // Recoil değerleri genellikle 0.1-1.0 arası, bu yüzden daha büyük multiplier kullanıyoruz
            float horizontalOffset = Random.Range(-horizontalRecoil, horizontalRecoil) * recoilSensitivity * 0.02f;
            float verticalUp = verticalRecoil * recoilSensitivity * 0.03f; // Yukarı
            float verticalBack = -verticalRecoil * recoilSensitivity * 0.08f; // Geriye doğru
            
            Vector3 recoilOffset = new Vector3(horizontalOffset, verticalUp, verticalBack);
            
            // Her iki el için de recoil offset uygula (her ateş ettiğinde yeni impulse)
            // += kullanmak yerine direkt set ediyoruz çünkü recoil recovery zaten smooth bir şekilde geri döndürüyor
            leftHandRecoilOffset = recoilOffset;
            rightHandRecoilOffset = recoilOffset;
        }
        
        private void OnDestroy()
        {
            // Event listener'ları temizle
            if (currentFirearm != null)
            {
                var eventsField = typeof(Firearm).GetField("events",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (eventsField != null)
                {
                    var events = eventsField.GetValue(currentFirearm) as FirearmEvents;
                    if (events != null && events.OnFireDone != null)
                    {
                        events.OnFireDone.RemoveListener(OnFirearmFired);
                    }
                }
            }
        }
        
        private void EnsureRequiredComponents()
        {
            if (GetComponent<CharacterManager>() == null)
            {
                gameObject.AddComponent<CharacterManager>();
            }
            
            if (GetComponent<CharacterInput>() == null)
            {
                gameObject.AddComponent<CharacterInput>();
            }
            
            if (GetComponent<CameraManager>() == null)
            {
                gameObject.AddComponent<CameraManager>();
            }
        }
    }
}
