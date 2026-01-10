using UnityEngine;
using Akila.FPSFramework;

/// <summary>
/// Damageable ile Ragdoll arasında köprü. Health 0 olunca ragdoll aktif eder.
/// Damageable'da "Ragdolls" checkbox'u KAPALI olmalı!
/// </summary>
[RequireComponent(typeof(Damageable))]
[RequireComponent(typeof(Ragdoll))]
public class DamageableRagdollController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Damageable damageable;
    [SerializeField] private Ragdoll ragdoll;
    [SerializeField] private Animator animator;

    [Header("Ragdoll Ayarları")]
    [SerializeField] private bool activateRagdollOnDeath = true;
    [SerializeField] private bool disableAnimatorOnDeath = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isDead = false;

    private void Awake()
    {
        // Otomatik referans bulma
        if (damageable == null)
            damageable = GetComponent<Damageable>();

        if (ragdoll == null)
            ragdoll = GetComponent<Ragdoll>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Kontroller
        if (damageable == null)
        {
            Debug.LogError($"[DamageableRagdollController] Damageable komponenti bulunamadı! GameObject: {gameObject.name}");
            enabled = false;
            return;
        }

        if (ragdoll == null)
        {
            Debug.LogError($"[DamageableRagdollController] Ragdoll komponenti bulunamadı! GameObject: {gameObject.name}");
            enabled = false;
            return;
        }

        // Damageable'da ragdoll checkbox'u kapalı mı kontrol et
        if (damageable.ragdolls)
        {
            Debug.LogWarning($"[DamageableRagdollController] Damageable'da 'Ragdolls' checkbox'u AÇIK! " +
                           $"Bu script çalışması için checkbox'u KAPALI yapın. GameObject: {gameObject.name}");
        }
    }

    private void OnEnable()
    {
        if (damageable != null)
        {
            damageable.OnDeath.AddListener(OnDeath);
            if (showDebugLogs)
            {
                Debug.Log($"[DamageableRagdollController] OnDeath event'ine abone olundu. GameObject: {gameObject.name}");
            }
        }
    }

    private void OnDisable()
    {
        if (damageable != null)
        {
            damageable.OnDeath.RemoveListener(OnDeath);
        }
    }

    private void Start()
    {
        // Başlangıçta ragdoll kapalı olmalı
        if (ragdoll != null && ragdoll.isEnabled)
        {
            ragdoll.Disable();
            if (showDebugLogs)
                Debug.Log($"[DamageableRagdollController] Ragdoll başlangıçta devre dışı bırakıldı. GameObject: {gameObject.name}");
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[DamageableRagdollController] ✅ Script başlatıldı. GameObject: {gameObject.name}");
            Debug.Log($"[DamageableRagdollController] - Damageable: {(damageable != null ? "VAR ✅" : "YOK ❌")}");
            Debug.Log($"[DamageableRagdollController] - Ragdoll: {(ragdoll != null ? "VAR ✅" : "YOK ❌")}");
            Debug.Log($"[DamageableRagdollController] - Animator: {(animator != null ? "VAR ✅" : "YOK ❌")}");
            Debug.Log($"[DamageableRagdollController] - Initial Health: {(damageable != null ? damageable.health.ToString() : "N/A")}");
        }
    }

    private void Update()
    {
        // Health 0'a indiğinde ragdoll aktif et
        if (!isDead && activateRagdollOnDeath && damageable != null && damageable.health <= 0)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[DamageableRagdollController] ⚠️ Update() tespit etti: Health <= 0! Health: {damageable.health}");
            }
            OnDeath();
        }
        
        // Her 60 frame'de bir health durumunu logla (debug için)
        if (showDebugLogs && Time.frameCount % 60 == 0 && damageable != null)
        {
            Debug.Log($"[DamageableRagdollController] Health Check: {damageable.health}, IsDead: {isDead}, DeadConfirmed: {damageable.DeadConfirmed}");
        }
    }

    /// <summary>
    /// Damageable öldüğünde çağrılır
    /// </summary>
    private void OnDeath()
    {
        if (isDead) return;

        isDead = true;

        if (showDebugLogs)
        {
            Debug.Log($"[DamageableRagdollController] 💀 Karakter öldü! Health: {damageable.health}");
        }

        // Animator'ü kapat
        if (disableAnimatorOnDeath && animator != null)
        {
            animator.enabled = false;
            if (showDebugLogs)
                Debug.Log($"[DamageableRagdollController] Animator devre dışı bırakıldı");
        }

        // Ragdoll'u aktif et
        if (activateRagdollOnDeath && ragdoll != null)
        {
            // Rigidbody kontrolü
            Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
            
            if (rbs.Length == 0)
            {
                Debug.LogError($"[DamageableRagdollController] ❌ HİÇ RİGİDBODY YOK! " +
                             $"Lütfen Ragdoll Setup Tool kullanarak ragdoll kurun! GameObject: {gameObject.name}");
                return;
            }

            if (showDebugLogs)
            {
                Debug.Log($"[DamageableRagdollController] Ragdoll aktif ediliyor... {rbs.Length} adet Rigidbody bulundu");
            }

            // Ragdoll'u aktif et
            ragdoll.isEnabled = true;
            ragdoll.Enable();

            // Tüm rigidbody'leri manuel olarak non-kinematic yap
            int activeCount = 0;
            foreach (Rigidbody rb in rbs)
            {
                if (rb != null && rb.transform != transform) // Ana transform'u atla
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    activeCount++;
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[DamageableRagdollController] ✅ RAGDOLL AKTİF! " +
                         $"isEnabled: {ragdoll.isEnabled}, " +
                         $"{activeCount} Rigidbody non-kinematic yapıldı");
            }
        }
    }

    /// <summary>
    /// Karakteri diriltir (test için)
    /// </summary>
    public void Revive(float healthAmount = 100f)
    {
        if (!isDead) return;

        isDead = false;

        // Health'i geri yükle
        if (damageable != null)
        {
            damageable.health = healthAmount;
            damageable.DeadConfirmed = false;
        }

        // Ragdoll'u deaktif et
        if (ragdoll != null)
        {
            ragdoll.isEnabled = false;
            ragdoll.Disable();
        }

        // Animator'ü aç
        if (animator != null)
        {
            animator.enabled = true;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[DamageableRagdollController] ✨ Karakter dirildi! Health: {damageable.health}");
        }
    }

    // Inspector test menüleri
#if UNITY_EDITOR
    [ContextMenu("Test - Öldür (Health 0)")]
    private void TestKill()
    {
        if (damageable != null)
        {
            damageable.health = 0;
            OnDeath();
        }
    }

    [ContextMenu("Test - Diril")]
    private void TestRevive()
    {
        Revive(100f);
    }

    [ContextMenu("Test - 25 Hasar Ver")]
    private void TestDamage25()
    {
        if (damageable != null)
        {
            damageable.Damage(25f, gameObject);
            Debug.Log($"25 hasar verildi. Kalan health: {damageable.health}");
        }
    }

    [ContextMenu("Test - Ragdoll Info")]
    private void TestRagdollInfo()
    {
        if (ragdoll != null)
        {
            Debug.Log($"Ragdoll isEnabled: {ragdoll.isEnabled}");
            Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
            Debug.Log($"Toplam Rigidbody: {rbs.Length}");
            
            int kinematicCount = 0;
            foreach (Rigidbody rb in rbs)
            {
                if (rb != null && rb.isKinematic) kinematicCount++;
            }
            Debug.Log($"Kinematic Rigidbody: {kinematicCount}, Non-Kinematic: {rbs.Length - kinematicCount}");
        }
    }
#endif

    private void OnValidate()
    {
        // Editor'da otomatik referans bulma
        if (damageable == null)
            damageable = GetComponent<Damageable>();

        if (ragdoll == null)
            ragdoll = GetComponent<Ragdoll>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }
}

