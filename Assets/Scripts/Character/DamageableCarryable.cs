using UnityEngine;
using Akila.FPSFramework;

/// <summary>
/// Damageable ile çalışan taşıma sistemi. Ragdoll olmuş karakterleri taşımak için.
/// </summary>
[RequireComponent(typeof(Damageable))]
public class DamageableCarryable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactionName = "Carry";
    [SerializeField] private string deadDisplayName = "Dead Character";

    [Header("Carry Settings")]
    [SerializeField] private Vector3 carryRotationOffset = new Vector3(0, 180f, 0);
    [SerializeField] private bool disableCollisionWhenCarried = false;

    [Header("Ragdoll Settings")]
    [SerializeField] private string[] mainBoneNames = { "Pelvis", "Hips", "pelvis", "hips", "Spine", "spine_01" };
    [SerializeField] private bool moveWholeCharacter = true; // Ana transform'u taşı (mesh dahil)

    private Damageable damageable;
    private Ragdoll ragdoll;
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;
    private Rigidbody mainRigidbody;
    private Transform skeletonRoot; // Skeleton'un root transform'u
    private bool isCarryable = false;
    private bool isBeingCarried = false;
    private Transform carryPointTransform;

    public bool IsBeingCarried => isBeingCarried;
    public Vector3 CarryRotationOffset => carryRotationOffset;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        ragdoll = GetComponent<Ragdoll>();
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();

        FindMainRigidbody();
    }

    private void Start()
    {
        if (damageable != null)
        {
            damageable.OnDeath.AddListener(OnDeath);
        }
    }

    private void FindMainRigidbody()
    {
        // Skeleton root'u bul
        skeletonRoot = FindChildByName(transform, "skeleton");
        if (skeletonRoot == null)
            skeletonRoot = FindChildByName(transform, "Skeleton");
        if (skeletonRoot == null)
            skeletonRoot = FindChildByName(transform, "root");
        
        if (skeletonRoot != null)
        {
            Debug.Log($"[DamageableCarryable] Skeleton root bulundu: {skeletonRoot.name}");
        }

        // Ana rigidbody'yi bul (pelvis veya hips)
        foreach (string boneName in mainBoneNames)
        {
            Transform bone = transform.Find(boneName);
            if (bone == null)
            {
                // Recursive ara
                bone = FindChildByName(transform, boneName);
            }

            if (bone != null)
            {
                mainRigidbody = bone.GetComponent<Rigidbody>();
                if (mainRigidbody != null)
                {
                    Debug.Log($"[DamageableCarryable] Ana rigidbody bulundu: {bone.name}");
                    Debug.Log($"[DamageableCarryable] Parent hierarchy: {GetFullPath(bone)}");
                    break;
                }
            }
        }

        if (mainRigidbody == null)
        {
            Debug.LogWarning($"[DamageableCarryable] Ana rigidbody bulunamadı! Varsayılan olarak transform kullanılacak.");
        }
    }
    
    private string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null && t.parent != transform)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name.ToLower().Contains(name.ToLower()))
            {
                return child;
            }
        }
        return null;
    }

    private void OnDeath()
    {
        // Öldüğünde taşınabilir yap
        SetCarryable(true);
        Debug.Log($"[DamageableCarryable] {gameObject.name} öldü ve taşınabilir hale geldi!");
    }

    public void SetCarryable(bool canBeCarried)
    {
        isCarryable = canBeCarried;
    }

    public void Interact(InteractionsManager source)
    {
        if (!isCarryable || damageable == null || !damageable.DeadConfirmed)
        {
            Debug.LogWarning($"[DamageableCarryable] Etkileşim reddedildi. Carryable: {isCarryable}, Dead: {damageable?.DeadConfirmed}");
            return;
        }

        // Toggle carry/drop
        if (!isBeingCarried)
        {
            Debug.Log($"[DamageableCarryable] Taşınma başlatılıyor...");
            StartCarrying(source.transform);
        }
        else
        {
            Debug.Log($"[DamageableCarryable] Bırakılma başlatılıyor...");
            StopCarrying();
        }
    }

    public string GetInteractionName()
    {
        if (!isCarryable)
            return "";

        return isBeingCarried
            ? $"Drop {deadDisplayName}"
            : $"{interactionName} {deadDisplayName}";
    }

    private void StartCarrying(Transform carrier)
    {
        isBeingCarried = true;
        
        // Player'ın carry point'ini bul
        carryPointTransform = carrier.Find("CarryPoint");
        if (carryPointTransform == null)
        {
            // CarryPoint yoksa carrier'ın kendisini kullan
            carryPointTransform = carrier;
            Debug.LogWarning($"[DamageableCarryable] CarryPoint bulunamadı, carrier kullanılıyor: {carrier.name}");
        }

        Debug.Log($"[DamageableCarryable] Taşıma başladı. Main bone: {(mainRigidbody != null ? mainRigidbody.name : "None")}");

        // Ragdoll'u taşıma moduna al (ÖNCE!)
        if (ragdoll != null)
        {
            ragdoll.isBeingCarried = true;
            Debug.Log($"[DamageableCarryable] Ragdoll.isBeingCarried = true");
        }

        // ÖNEMLİ: Tüm velocity'leri temizle ÖNCE!
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Tüm rigidbody'leri kinematic yap
        SetRigidbodiesKinematic(true);

        // Pozisyonu hemen ayarla (ANA TRANSFORM)
        if (moveWholeCharacter)
        {
            // Tüm karakteri taşı (mesh + skeleton)
            transform.position = carryPointTransform.position;
            transform.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
            Debug.Log($"[DamageableCarryable] Tüm karakter taşıma pozisyonunda: {transform.position}");
        }
        else if (mainRigidbody != null)
        {
            // Sadece skeleton taşı
            mainRigidbody.position = carryPointTransform.position;
            mainRigidbody.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
            UpdateAllBonesPosition();
        }
        else
        {
            // Fallback
            transform.position = carryPointTransform.position;
            transform.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
        }

        if (disableCollisionWhenCarried)
        {
            SetCollidersEnabled(false);
        }

        Debug.Log($"[DamageableCarryable] Taşıma kuruldu, pozisyon: {carryPointTransform.position}");
    }

    public void StopCarrying()
    {
        isBeingCarried = false;
        carryPointTransform = null;

        Debug.Log($"[DamageableCarryable] Taşıma bitti.");

        // Ragdoll'u normal moda al
        if (ragdoll != null)
        {
            ragdoll.isBeingCarried = false;
            Debug.Log($"[DamageableCarryable] Ragdoll.isBeingCarried = false");
        }

        // Rigidbody'leri non-kinematic yap (ragdoll devam etsin)
        SetRigidbodiesKinematic(false);

        if (disableCollisionWhenCarried)
        {
            SetCollidersEnabled(true);
        }
    }

    private void LateUpdate()
    {
        if (isBeingCarried && carryPointTransform != null)
        {
            // Tüm rigidbody'leri kinematic olduğundan emin ol
            int nonKinematicCount = 0;
            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        nonKinematicCount++;
                    }
                    // Velocity'leri sürekli sıfırla
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            
            if (nonKinematicCount > 0)
            {
                Debug.LogWarning($"[DamageableCarryable] {nonKinematicCount} rigidbody kinematic değildi, düzeltildi!");
            }

            // ANA TRANSFORM'U TAŞI (mesh dahil tüm karakter)
            if (moveWholeCharacter)
            {
                // Tüm karakteri taşı (mesh + skeleton birlikte)
                transform.position = carryPointTransform.position;
                transform.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
                
                Debug.Log($"[DamageableCarryable] Ana transform taşınıyor: {transform.position}");
            }
            else if (mainRigidbody != null)
            {
                // Sadece ana kemik üzerinden taşı
                mainRigidbody.position = carryPointTransform.position;
                mainRigidbody.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
                
                // Hızları sürekli sıfırla
                mainRigidbody.linearVelocity = Vector3.zero;
                mainRigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                // Fallback: Ana transform'u taşı
                transform.position = carryPointTransform.position;
                transform.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
            }
        }
    }

    private void FixedUpdate()
    {
        // FixedUpdate'te de kontrol et (fizik güncellemeleri için)
        if (isBeingCarried && carryPointTransform != null)
        {
            // Tüm rigidbody'leri kinematic olduğundan emin ol
            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                    
                    // Velocity'leri sürekli sıfırla
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // ANA TRANSFORM'U TAŞI (fizik güncellemesi için)
            if (moveWholeCharacter)
            {
                transform.position = carryPointTransform.position;
                transform.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
            }
            else if (mainRigidbody != null)
            {
                mainRigidbody.position = carryPointTransform.position;
                mainRigidbody.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
                mainRigidbody.linearVelocity = Vector3.zero;
                mainRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private void SetRigidbodiesKinematic(bool isKinematic)
    {
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
            {
                // Her durumda velocity'leri temizle
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                // Kinematic durumunu ayarla
                rb.isKinematic = isKinematic;
                
                // Gravity'yi ayarla
                if (isKinematic)
                {
                    rb.useGravity = false;
                }
                else
                {
                    rb.useGravity = true;
                }
            }
        }
        
        Debug.Log($"[DamageableCarryable] {rigidbodies.Length} rigidbody {(isKinematic ? "kinematic" : "non-kinematic")} yapıldı");
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider col in colliders)
        {
            if (col != null && !col.isTrigger)
            {
                col.enabled = enabled;
            }
        }
    }

    private void UpdateAllBonesPosition()
    {
        // Ana kemik pozisyonunu ayarla
        if (mainRigidbody != null)
        {
            Vector3 targetPosition = carryPointTransform.position;
            Quaternion targetRotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
            
            // Ana kemik pozisyonunu ayarla
            mainRigidbody.position = targetPosition;
            mainRigidbody.rotation = targetRotation;
            
            // Diğer kemiklerin relative pozisyonlarını koru
            // (Sadece ana kemik hareket eder, diğerleri joint'lerle takip eder)
        }
    }

    private void OnDestroy()
    {
        if (damageable != null)
        {
            damageable.OnDeath.RemoveListener(OnDeath);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test - Find Main Rigidbody")]
    private void TestFindMainRigidbody()
    {
        FindMainRigidbody();
    }
#endif
}

