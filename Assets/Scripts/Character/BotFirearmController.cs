using UnityEngine;
using Akila.FPSFramework;
using System.Collections;

namespace GuardSimulator.Character
{
    /// <summary>
    /// Bot silahları için özel ateş etme kontrol sistemi
    /// Player'dan bağımsız çalışır ve 4 saniyede bir ateş eder
    /// </summary>
    [AddComponentMenu("Guard Simulator/Bot/Bot Firearm Controller")]
    [RequireComponent(typeof(Firearm))]
    public class BotFirearmController : MonoBehaviour
    {
        [Header("Bot Ateş Ayarları")]
        [Tooltip("Bot'un kolunu gizle (renderer'ı disable et)")]
        [SerializeField] private bool hideBotArm = true;
        
        [Tooltip("Bot atış hızı delay'i (saniye) - her ateş arasında bekleme süresi")]
        [SerializeField] private float fireDelay = 2f;
        
        private Firearm firearm;
        private bool isBotWeapon = false;
        private float lastFireTime = 0f;
        
        // Firearm'ın orijinal fireRate'ini sakla (player için)
        private float originalFireRate;
        
        private void Awake()
        {
            firearm = GetComponent<Firearm>();
            
            // Bu silahın bot silahı olup olmadığını kontrol et
            CheckIfBotWeapon();
            
            // Eğer bot silahıysa, kolunu gizle
            if (isBotWeapon && hideBotArm)
            {
                HideBotArm();
            }
        }
        
        private void Start()
        {
            if (firearm == null)
            {
                Debug.LogError("[BotFirearmController] Firearm component bulunamadı!", this);
                enabled = false;
                return;
            }
            
            // Orijinal fireRate'i sakla
            if (firearm.preset != null)
            {
                originalFireRate = firearm.preset.fireRate;
            }
            
            // Bot silahıysa fireRate'i çok yüksek yap (delay olmasın)
            if (isBotWeapon && firearm.preset != null)
            {
                // Bot için fireRate'i çok yüksek yap (delay olmasın, sürekli ateş edebilsin)
                firearm.preset.fireRate = 1000f; // Çok yüksek RPM = neredeyse delay yok
                Debug.Log($"[BotFirearmController] Bot silahı için fireRate ayarlandı: {firearm.preset.fireRate} RPM (delay yok)", this);
            }
        }
        
        /// <summary>
        /// Bu silahın bot silahı olup olmadığını kontrol et
        /// </summary>
        private void CheckIfBotWeapon()
        {
            // NPC component'i parent'larda varsa bu bot silahıdır
            NPC npc = GetComponentInParent<NPC>();
            if (npc != null)
            {
                isBotWeapon = true;
                return;
            }
            
            // BotWeaponManager component'i varsa bu bot silahıdır
            BotWeaponManager botWeaponManager = GetComponentInParent<BotWeaponManager>();
            if (botWeaponManager != null)
            {
                isBotWeapon = true;
                return;
            }
            
            // BotAI component'i varsa bu bot silahıdır
            BotAI botAI = GetComponentInParent<BotAI>();
            if (botAI != null)
            {
                isBotWeapon = true;
                return;
            }
            
            isBotWeapon = false;
        }
        
        /// <summary>
        /// Bot'un kolunu gizle (renderer'ları disable et)
        /// </summary>
        private void HideBotArm()
        {
            // Silahın parent'ında (bot karakterinde) kol renderer'larını bul ve gizle
            Transform parent = transform.parent;
            if (parent != null)
            {
                // "Arm", "LeftArm", "RightArm", "Hand", "LeftHand", "RightHand" gibi isimlerdeki renderer'ları bul
                Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
                
                foreach (Renderer renderer in renderers)
                {
                    string objName = renderer.gameObject.name.ToLower();
                    
                    // Kol veya el ile ilgili objeleri gizle
                    if (objName.Contains("arm") || objName.Contains("hand") || 
                        objName.Contains("el") || objName.Contains("kol"))
                    {
                        renderer.enabled = false;
                        Debug.Log($"[BotFirearmController] Bot kolu gizlendi: {renderer.gameObject.name}", this);
                    }
                }
            }
        }
        
        /// <summary>
        /// Bot için özel ateş etme metodu (fireTimer ve readyToFire kontrolünü bypass eder, delay var)
        /// </summary>
        public bool TryFireBot(Vector3 position, Quaternion rotation, Vector3 direction)
        {
            if (firearm == null)
            {
                Debug.LogWarning("[BotFirearmController] Firearm null!", this);
                return false;
            }
            
            if (!isBotWeapon)
            {
                Debug.LogWarning("[BotFirearmController] Bu silah bot silahı değil!", this);
                return false;
            }
            
            // Fire delay kontrolü
            if (Time.time - lastFireTime < fireDelay)
            {
                return false;
            }
            
            // Ammo kontrolü (eğer mermi yoksa reload et veya false dön)
            if (firearm.remainingAmmoCount <= 0)
            {
                Debug.Log($"[BotFirearmController] Mermi yok! (remainingAmmoCount: {firearm.remainingAmmoCount})", this);
                // Otomatik reload dene
                if (firearm.preset != null && firearm.preset.canAutomaticallyReload)
                {
                    firearm.Reload();
                }
                return false;
            }
            
            // Reloading kontrolü
            if (firearm.isReloading)
            {
                Debug.Log("[BotFirearmController] Reloading devam ediyor, ateş edilemez.", this);
                return false;
            }
            
            // Firearm'ın fireTimer field'ını geçici olarak sıfırla (reflection ile)
            // Böylece Firearm.Fire() metodundaki fireTimer kontrolünü bypass ederiz
            var fireTimerField = typeof(Firearm).GetField("fireTimer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fireTimerField != null)
            {
                // Timer'ı Time.time'dan küçük yap ki Firearm.Fire() kontrolü geçsin
                fireTimerField.SetValue(firearm, Time.time - 1f);
            }
            
            // readyToFire property'sini bypass etmek için Firearm.Fire() metodunu direkt çağırmak yerine
            // FireDone metodunu çağıralım (Firearm.Fire() içindeki kontrolleri bypass eder)
            var fireDoneMethod = typeof(Firearm).GetMethod("FireDone", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fireDoneMethod != null)
            {
                // FireDone metodunu çağır (Fire() metodundaki kontrolleri bypass eder)
                fireDoneMethod.Invoke(firearm, new object[] { position, rotation, direction });
                lastFireTime = Time.time;
                Debug.Log($"[BotFirearmController] Bot ateş etti! (FireDone kullanıldı) Position: {position}, Delay: {fireDelay}s", this);
                return true;
            }
            else
            {
                // FireDone bulunamazsa normal Fire metodunu kullan (fireTimer zaten bypass edildi)
                firearm.Fire(position, rotation, direction);
                lastFireTime = Time.time;
                Debug.Log($"[BotFirearmController] Bot ateş etti! (Fire kullanıldı) Position: {position}, Delay: {fireDelay}s", this);
                return true;
            }
        }
        
        /// <summary>
        /// Bu silahın bot silahı olup olmadığını kontrol et
        /// </summary>
        public bool IsBotWeapon()
        {
            return isBotWeapon;
        }
        
        /// <summary>
        /// Orijinal fireRate'i geri yükle (player silahına dönüştürülürse)
        /// </summary>
        public void RestoreOriginalFireRate()
        {
            if (firearm != null && firearm.preset != null)
            {
                firearm.preset.fireRate = originalFireRate;
            }
        }
    }
}
