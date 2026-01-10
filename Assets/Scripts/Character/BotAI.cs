using UnityEngine;
using Akila.FPSFramework;
using GuardSimulator.Character;

namespace GuardSimulator.Character
{
    /// <summary>
    /// Botların player'ı bulup saldırması için AI sistemi
    /// </summary>
    [AddComponentMenu("Guard Simulator/Bot/Bot AI")]
    [RequireComponent(typeof(NPC))]
    public class BotAI : MonoBehaviour
    {
        [Header("AI Ayarları")]
        [Tooltip("Bot saldırı yapabilir mi?")]
        [SerializeField] private bool canAttack = true;
        
        [Tooltip("Saldırı mesafesi (metre)")]
        [SerializeField] private float attackRange = 50f;
        
        [Tooltip("Player'ı bulma mesafesi")]
        [SerializeField] private float detectionRange = 100f;
        
        [Tooltip("Player'a bakma hızı")]
        [SerializeField] private float rotationSpeed = 5f;
        
        [Tooltip("Player tag'i (otomatik bulunur)")]
        [SerializeField] private string playerTag = "Player";
        
        private Transform playerTransform;
        private Firearm currentWeapon;
        private BotFirearmController botFirearmController;
        private Inventory inventory;
        private NPC npc;
        private bool isPlayerInRange = false;
        
        private void Start()
        {
            npc = GetComponent<NPC>();
            inventory = GetComponentInChildren<Inventory>();
            
            // Player'ı bul
            FindPlayer();
            
            // Silahı bul
            FindWeapon();
        }
        
        private void Update()
        {
            if (!canAttack) return;
            if (npc != null && !npc.IsAlive) return;
            
            // Rage kontrolü: Eğer NPC rage modunda değilse saldırma
            if (npc != null && !npc.IsRage)
            {
                return;
            }
            
            // Player'ı bul (her frame kontrol et)
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }
            
            // Silahı bul
            if (currentWeapon == null)
            {
                FindWeapon();
                return;
            }
            
            // BotFirearmController'ı bul
            if (botFirearmController == null && currentWeapon != null)
            {
                botFirearmController = currentWeapon.GetComponent<BotFirearmController>();
                if (botFirearmController == null)
                {
                    // Eğer yoksa ekle
                    botFirearmController = currentWeapon.gameObject.AddComponent<BotFirearmController>();
                }
            }
            
            // Player'a mesafe kontrolü
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= detectionRange)
            {
                isPlayerInRange = true;
                
                // Player'a bak
                LookAtPlayer();
                
                // Saldırı mesafesindeyse ateş et
                if (distanceToPlayer <= attackRange)
                {
                    TryShoot();
                }
            }
            else
            {
                isPlayerInRange = false;
            }
        }
        
        /// <summary>
        /// Player'ı bul
        /// </summary>
        private void FindPlayer()
        {
            // Tag ile bul
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            
            // Tag yoksa isimle bul
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
            
            // PlayerMain singleton ile bul
            if (player == null)
            {
                PlayerMain playerMain = FindObjectOfType<PlayerMain>();
                if (playerMain != null)
                {
                    player = playerMain.gameObject;
                }
            }
            
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log($"[BotAI] Player bulundu: {player.name}", this);
            }
            else
            {
                Debug.LogWarning("[BotAI] Player bulunamadı! Player GameObject'inin tag'i 'Player' olmalı veya ismi 'Player' olmalı.", this);
            }
        }
        
        /// <summary>
        /// Silahı bul
        /// </summary>
        private void FindWeapon()
        {
            if (inventory == null)
            {
                inventory = GetComponentInChildren<Inventory>();
            }
            
            if (inventory != null)
            {
                // Aktif item'den silahı al
                if (inventory.currentItem != null)
                {
                    currentWeapon = inventory.currentItem.GetComponent<Firearm>();
                }
                
                // Eğer aktif item'de silah yoksa, inventory'deki tüm item'leri kontrol et
                if (currentWeapon == null)
                {
                    foreach (var item in inventory.items)
                    {
                        if (item != null)
                        {
                            currentWeapon = item.GetComponent<Firearm>();
                            if (currentWeapon != null)
                            {
                                // Bu silahı aktif et
                                int index = inventory.items.IndexOf(item);
                                if (index != -1)
                                {
                                    inventory.Switch(index);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            
            // Hala bulamadıysak child'larda ara
            if (currentWeapon == null)
            {
                currentWeapon = GetComponentInChildren<Firearm>();
            }
            
            if (currentWeapon != null)
            {
                Debug.Log($"[BotAI] Silah bulundu: {currentWeapon.name}", this);
                
                // BotFirearmController'ı ekle veya bul
                botFirearmController = currentWeapon.GetComponent<BotFirearmController>();
                if (botFirearmController == null)
                {
                    botFirearmController = currentWeapon.gameObject.AddComponent<BotFirearmController>();
                    Debug.Log("[BotAI] BotFirearmController eklendi.", this);
                }
            }
            else
            {
                Debug.LogWarning("[BotAI] Silah bulunamadı! Bot'a BotWeaponManager ile silah ekleyin.", this);
            }
        }
        
        /// <summary>
        /// Player'a bak
        /// </summary>
        private void LookAtPlayer()
        {
            if (playerTransform == null) return;
            
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0; // Y eksenini sıfırla (sadece yatay dönüş)
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
        
        /// <summary>
        /// Ateş etmeyi dene (delay yok, bot sürekli ateş edebilir)
        /// </summary>
        private void TryShoot()
        {
            if (currentWeapon == null)
            {
                Debug.LogWarning("[BotAI] currentWeapon null!", this);
                return;
            }
            
            if (playerTransform == null)
            {
                Debug.LogWarning("[BotAI] playerTransform null!", this);
                return;
            }
            
            // Muzzle transform'u kontrol et
            if (currentWeapon.muzzle == null)
            {
                Debug.LogWarning("[BotAI] Silahın muzzle transform'u yok! Silah prefab'ında muzzle transform'u ayarlanmalı.", this);
                return;
            }
            
            // HittableLayers'ı kontrol et ve Player layer'ını ekle (her ateş etmeden önce)
            if (currentWeapon.preset != null)
            {
                int playerLayer = 8;
                LayerMask currentLayers = currentWeapon.preset.hittableLayers;
                if ((currentLayers.value & (1 << playerLayer)) == 0)
                {
                    currentWeapon.preset.hittableLayers = currentLayers | (1 << playerLayer);
                }
            }
            
            // Player'a doğru ateş et
            Vector3 firePosition = currentWeapon.muzzle.position;
            
            // Player'a doğru yön hesapla (player'ın göğüs seviyesine doğru)
            Vector3 playerTargetPosition = playerTransform.position;
            // Player'ın göğüs seviyesine doğru ateş et (yaklaşık 1.5m yukarı)
            playerTargetPosition.y += 1.5f;
            Vector3 directionToPlayer = (playerTargetPosition - firePosition).normalized;
            Quaternion fireRotation = Quaternion.LookRotation(directionToPlayer);
            
            // BotFirearmController varsa onu kullan, yoksa normal Fire metodunu kullan
            if (botFirearmController != null)
            {
                bool fired = botFirearmController.TryFireBot(firePosition, fireRotation, directionToPlayer);
                if (!fired)
                {
                    Debug.LogWarning($"[BotAI] TryFireBot başarısız! readyToFire: {currentWeapon.readyToFire}, isReloading: {currentWeapon.isReloading}", this);
                }
            }
            else
            {
                Debug.LogWarning("[BotAI] BotFirearmController bulunamadı! Normal Fire metodunu kullanıyoruz.", this);
                // Fallback: Normal Fire metodunu kullan (fireTimer kontrolü olacak ama yine de dene)
                currentWeapon.Fire(firePosition, fireRotation, directionToPlayer);
            }
        }
        
        /// <summary>
        /// Silahı yeniden bul (runtime'da silah eklendiğinde çağrılabilir)
        /// </summary>
        public void RefreshWeapon()
        {
            currentWeapon = null;
            FindWeapon();
        }
        
        /// <summary>
        /// Player'ı yeniden bul (runtime'da çağrılabilir)
        /// </summary>
        public void RefreshPlayer()
        {
            playerTransform = null;
            FindPlayer();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Detection range görselleştirme
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Attack range görselleştirme
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}



