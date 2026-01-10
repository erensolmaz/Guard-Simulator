using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using Akila.FPSFramework;
using Akila.FPSFramework.Animation;
using UnityEngine.Playables;
using UnityEngine.Animations;
using GuardSimulator.Gameplay;

namespace GuardSimulator.Character
{
    /// <summary>
    /// NPC/Düşman/Polis için genel kimlik ve taşıma sistemi.
    /// </summary>
    [AddComponentMenu("Guard Simulator/Character/NPC")]
    [DisallowMultipleComponent]
    public class NPC : MonoBehaviour, IInteractable, ICharacterController
    {
        [Header("Identity")]
        [Tooltip("Karakter adı")]
        [SerializeField] private string npcName = "Unknown";
        
        [Header("Class")]
        [Tooltip("Karakter sınıfı: NPC, Enemy, Police, vb.")]
        [SerializeField] private CharacterClass characterClass = CharacterClass.NPC;
        
        [Header("Carryable Settings")]
        [Tooltip("Bu karakter taşınabilir mi?")]
        [SerializeField] private bool canBeCarried = true;
        
        [Tooltip("Taşıma için gerekli durum")]
        [SerializeField] private CarryRequirement carryRequirement = CarryRequirement.Unconscious;
        
        [Header("Carry Interaction")]
        // Removed unused fields: interactionName, unconsciousDisplayName
        
        [Header("Carry Settings")]
        [SerializeField] private Vector3 carryPositionOffset = new Vector3(0, -0.5f, 1.5f);
        [SerializeField] private Vector3 carryRotationOffset = new Vector3(0, 180f, 0);
        [SerializeField] private bool disableCollisionWhenCarried = true;

        [Header("Arrest Settings")]
        [Tooltip("Tutuklama pozisyonu için referans Animation Clip (pose.fbx dosyasından alınan clip - Generic rig olmalı)")]
        [SerializeField] private AnimationClip arrestPoseClip;
        
        [Tooltip("Kelepçe prefab'ı (tutuklama sonrası spawn edilir)")]
        [SerializeField] private GameObject handcuffPrefab;
        
        [Tooltip("Kelepçe pozisyon offset'i (bileklere tam oturması için ayarlanabilir)")]
        [SerializeField] private Vector3 handcuffPositionOffset = Vector3.zero;
        
        [Tooltip("Tutuklama animasyon hızı")]
        [SerializeField] private float arrestRotationSpeed = 2f;

        [Header("Escort Settings")]
        [Tooltip("Escort sırasında player kamerasından ne kadar önde duracak")]
        [SerializeField] private float escortDistanceFromCamera = 1.5f;
        
        [Tooltip("Hedef pozisyona ne kadar hızlı hareket edecek")]
        [SerializeField] private float escortMoveSpeed = 5f;
        
        [Tooltip("Player'ın rotasyonuna ne kadar hızlı uyum sağlayacak")]
        [SerializeField] private float escortRotationSpeed = 10f;
        
        [Tooltip("Escort pozisyon Y offset (yere gömülmeyi önlemek için)")]
        [SerializeField] private float escortHeightOffset = 0f;
        
        [Tooltip("Escort bırakıldıktan sonra Y pozisyon offset (yere gömülmeyi önlemek için)")]
        [SerializeField] private float escortDropHeightOffset = 1.0f;

        [Header("Events")]
        public UnityEvent<string> OnNameChanged;

        // Components
        private Damageable damageable;
        private Rigidbody[] rigidbodies;
        private Collider[] colliders;
        private Collider mainCollider;
        private Ragdoll ragdoll;
        private Animator animator;
        
        // Carry State
        private bool isCarryable = false;
        private bool isBeingCarried = false;
        private Transform carryPointTransform;
        private Coroutine colliderProtectionCoroutine;
        
        // Surrender State
        private bool isSurrendering = false;
        
        // Arrest State
        private bool isArrested = false;
        private Coroutine arrestAnimationCoroutine;
        private GameObject spawnedHandcuff;
        private PlayableGraph arrestPoseGraph;
        private RuntimeAnimatorController originalAnimatorController;
        
        // Escort State
        private bool isBeingEscorted = false;
        private Transform escortingPlayer;
        private Camera playerCamera;
        
        // Rage State
        private bool isRage = false;

        // Public Properties
        public string NPCName 
        { 
            get => npcName; 
            set 
            { 
                npcName = value;
                OnNameChanged?.Invoke(npcName);
            }
        }
        
        public CharacterClass CharacterClass 
        { 
            get => characterClass; 
            set => characterClass = value; 
        }
        
        public bool IsBeingCarried => isBeingCarried;
        public Vector3 CarryPositionOffset => carryPositionOffset;
        public Vector3 CarryRotationOffset => carryRotationOffset;
        public bool CanBeCarried => canBeCarried && isCarryable;
        public bool IsSurrendering => isSurrendering;
        public bool IsArrested => isArrested;
        public bool IsBeingEscorted => isBeingEscorted;
        public bool IsRage => isRage;
        
        /// <summary>
        /// NPC'nin canlı olup olmadığını kontrol et
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (damageable == null) return true; // Damageable yoksa varsayılan olarak canlı kabul et
                
                // Health <= 0 ise ölü
                if (damageable.Health <= 0)
                {
                    return false;
                }
                
                // Bilinçsiz ise ölü sayılır
                if (damageable.IsUnconscious)
                {
                    return false;
                }
                
                // Ölüm onaylanmışsa ölü
                if (damageable.DeadConfirmed)
                {
                    return false;
                }
                
                // Yukarıdaki koşullardan hiçbiri geçerli değilse canlı
                return true;
            }
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(npcName))
            {
                npcName = gameObject.name;
            }

            damageable = GetComponent<Damageable>();
            ragdoll = GetComponent<Ragdoll>();
            rigidbodies = GetComponentsInChildren<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            
            mainCollider = GetComponent<Collider>() ?? GetComponent<CapsuleCollider>();
            
            // Animator'ı bul
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            if (damageable != null)
            {
                damageable.OnDeath.AddListener(OnUnconsciousStateChanged);
            }
            UpdateCarryableState();
        }

        private void OnUnconsciousStateChanged()
        {
            if (damageable != null && damageable.Health <= 0 && damageable.DeadConfirmed)
            {
                if (carryRequirement == CarryRequirement.Unconscious)
                {
                    SetCarryable(true);
                }
                
                // NPC öldüğünde QuestSystem'e bildir (bot öldürme görevi için)
                if (QuestSystem.Instance != null)
                {
                    QuestSystem.Instance.CheckBotKilled(this);
                }
                
                // NPC öldüğünde, eğer rage modundaydıysa rage müziğini kontrol et
                if (isRage && SoundManager.Instance != null)
                {
                    CheckAndStopRageMusic();
                }
            }
        }

        private void UpdateCarryableState()
        {
            if (!canBeCarried)
            {
                isCarryable = false;
                return;
            }

            switch (carryRequirement)
            {
                case CarryRequirement.Always:
                    isCarryable = true;
                    break;
                case CarryRequirement.Unconscious:
                    bool isUnconscious = damageable != null && damageable.Health <= 0 && damageable.DeadConfirmed;
                    isCarryable = isUnconscious;
                    break;
                case CarryRequirement.Never:
                    isCarryable = false;
                    break;
            }
        }

        public void SetCarryable(bool canBeCarried)
        {
            isCarryable = canBeCarried;
        }
        
        /// <summary>
        /// NPC'yi teslim olma durumuna geçir
        /// </summary>
        public void Surrender()
        {
            if (isSurrendering || !IsAlive)
            {
                return;
            }
            
            isSurrendering = true;
            
            // Teslim olunca taşınabilir hale getir
            SetCarryable(true);
        }
        
        /// <summary>
        /// NPC'yi tutuklama durumuna geçir
        /// </summary>
        public void Arrest()
        {
            if (isArrested || !IsAlive)
            {
                return;
            }
            
            if (arrestPoseClip == null)
            {
                return;
            }
            
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
                if (animator == null)
                {
                    return;
                }
            }
            
            isArrested = true;
            
            // Tutuklama animasyonunu başlat
            if (arrestAnimationCoroutine != null)
                StopCoroutine(arrestAnimationCoroutine);
            
            arrestAnimationCoroutine = StartCoroutine(ArrestAnimationSequence());
            
            // Tutuklanınca taşınabilir hale getir
            SetCarryable(true);
        }
        
        /// <summary>
        /// Tutuklanmış NPC'yi serbest bırak (pose'dan çık ve animasyonuna devam et)
        /// </summary>
        public void ReleaseFromArrest()
        {
            if (!isArrested)
            {
                return;
            }
            
            // Tutuklama flag'ini kaldır
            isArrested = false;
            
            // Arrest animasyon coroutine'ini durdur
            if (arrestAnimationCoroutine != null)
            {
                StopCoroutine(arrestAnimationCoroutine);
                arrestAnimationCoroutine = null;
            }
            
            // PlayableGraph'i destroy et (pose'dan çık)
            if (arrestPoseGraph.IsValid())
            {
                arrestPoseGraph.Destroy();
            }
            
            // Kelepçeyi yok et
            if (spawnedHandcuff != null)
            {
                Destroy(spawnedHandcuff);
                spawnedHandcuff = null;
            }
            
            // Animator'ı enable et ve orijinal controller'ı geri yükle
            if (animator != null)
            {
                animator.enabled = true;
                
                if (originalAnimatorController != null)
                {
                    animator.runtimeAnimatorController = originalAnimatorController;
                    originalAnimatorController = null;
                }
            }
        }
        
        /// <summary>
        /// Tutuklanmış NPC'yi escort moduna al (player önünde yürüt)
        /// </summary>
        public void StartEscort(Transform player)
        {
            if (!isArrested)
            {
                return;
            }
            
            escortingPlayer = player;
            isBeingEscorted = true;
            
            // Player kamerasını bul
            if (player != null)
            {
                playerCamera = player.GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null && mainCam.transform.IsChildOf(player))
                    {
                        playerCamera = mainCam;
                    }
                }
            }
            
            // ÖNCE player collision ignore et (trigger olmadan önce)
            IgnorePlayerCollisionForEscort(true);
            
            // Collider'ları trigger yap (player ile çakışmasın)
            SetCollidersToTrigger(true);
            
            // Rigidbody'yi kinematic yap (fizik etkileşimi olmasın)
            SetRigidbodiesKinematic(true);
        }
        
        /// <summary>
        /// Escort modunu sonlandır
        /// </summary>
        public void StopEscort()
        {
            if (!isBeingEscorted)
            {
                return;
            }
            
            isBeingEscorted = false;
            escortingPlayer = null;
            playerCamera = null;
            
            // Rigidbody'yi normale döndür (önce)
            SetRigidbodiesKinematic(false);
            
            // Collider'ları normale döndür
            SetCollidersToTrigger(false);
            
            // Y pozisyonunu doğru şekilde ayarla (yere gömülmeyi önle)
            CorrectGroundPosition();
            
            // Player collision'ı 3 saniye sonra aç (coroutine ile)
            StartCoroutine(DelayedCollisionRestore());
        }
        
        /// <summary>
        /// Raycast ile yere doğru mesafeyi bulup, NPC'yi doğru yüksekliğe yerleştir
        /// </summary>
        private void CorrectGroundPosition()
        {
            // Raycast başlangıç noktası: NPC'nin üstünden biraz yukarı
            Vector3 rayStart = transform.position + Vector3.up * 3f;
            
            // Aşağı doğru raycast at
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Yeni Y pozisyonu = zemin Y + manuel offset
                float newY = hit.point.y + escortDropHeightOffset;
                
                Vector3 correctedPosition = transform.position;
                correctedPosition.y = newY;
                transform.position = correctedPosition;
                
                Debug.Log($"CorrectGroundPosition: Ground at Y={hit.point.y}, NPC placed at Y={newY} (offset={escortDropHeightOffset})");
            }
            else
            {
                // Raycast başarısız olduysa mevcut Y'yi kullan + offset
                Vector3 correctedPosition = transform.position;
                correctedPosition.y += escortDropHeightOffset;
                transform.position = correctedPosition;
                
                Debug.LogWarning("CorrectGroundPosition: Raycast failed, using current Y + offset");
            }
        }
        
        /// <summary>
        /// 3 saniye bekleyip player collision'ı geri aç
        /// </summary>
        private System.Collections.IEnumerator DelayedCollisionRestore()
        {
            yield return new WaitForSeconds(3f);
            IgnorePlayerCollisionForEscort(false);
        }
        
        /// <summary>
        /// Escort için player collision'ı ignore et (trigger olup olmadığına bakmadan)
        /// </summary>
        private void IgnorePlayerCollisionForEscort(bool ignore)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("Player");
            
            if (player != null)
            {
                Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
                
                foreach (Collider npcCol in colliders)
                {
                    if (npcCol != null)
                    {
                        foreach (Collider playerCol in playerColliders)
                        {
                            if (playerCol != null)
                            {
                                Physics.IgnoreCollision(npcCol, playerCol, ignore);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Her frame escort pozisyonunu güncelle
        /// </summary>
        private void UpdateEscortPosition()
        {
            if (playerCamera == null || escortingPlayer == null)
            {
                return;
            }
            
            // Kameranın önünde hedef pozisyon hesapla
            Vector3 cameraForward = playerCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            Vector3 targetPosition = playerCamera.transform.position + cameraForward * escortDistanceFromCamera;
            
            // Y pozisyonunu player transform'unun tam pozisyonuna eşitle + offset
            targetPosition.y = escortingPlayer.position.y + escortHeightOffset;
            
            // Pozisyonu smooth şekilde güncelle
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * escortMoveSpeed);
            
            // Rotasyonu player ile eşitle (aynı yöne baksın)
            Quaternion targetRotation = Quaternion.Euler(0, escortingPlayer.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * escortRotationSpeed);
        }
        
        /// <summary>
        /// NPC'yi rage moduna geçir (saldırıya geçmesi için)
        /// </summary>
        public void Rage()
        {
            if (isRage || !IsAlive)
            {
                return;
            }
            
            isRage = true;
            
            // Bot rage moduna girdiğinde quest marker'ını kaldır
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.RemoveMarkerForNPC(this);
            }
            
            // Rage müziğini çal (eğer zaten çalmıyorsa)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayRageMusic();
            }
        }
        
        /// <summary>
        /// Rage modunu kapat
        /// </summary>
        public void StopRage()
        {
            if (!isRage)
            {
                return;
            }
            
            isRage = false;
            
            // Eğer başka rage modunda NPC yoksa, normal müziğe geri dön
            if (SoundManager.Instance != null)
            {
                CheckAndStopRageMusic();
            }
        }
        
        /// <summary>
        /// Sahnedeki tüm NPC'leri kontrol et, eğer hiçbiri rage modunda değilse rage müziğini durdur
        /// </summary>
        private void CheckAndStopRageMusic()
        {
            // Sahnedeki tüm NPC'leri bul
            NPC[] allNPCs = FindObjectsOfType<NPC>();
            
            bool anyNPCInRage = false;
            foreach (NPC npc in allNPCs)
            {
                if (npc != null && npc.IsRage && npc.IsAlive)
                {
                    anyNPCInRage = true;
                    break;
                }
            }
            
            // Eğer hiçbir NPC rage modunda değilse, normal müziğe geri dön
            if (!anyNPCInRage && SoundManager.Instance != null)
            {
                SoundManager.Instance.StopRageMusic();
            }
        }
        
        /// <summary>
        /// Tutuklama animasyon dizisi: Dönüş → Pose'dan pozisyon al → Kelepçe spawn
        /// </summary>
        private IEnumerator ArrestAnimationSequence()
        {
            // 1. Karakterin mevcut pozisyonunu kaydet
            Vector3 originalPosition = transform.position;
            
            // 2. Ana rigidbody'yi kinematic yap (karakterin yerinde kalması için)
            Rigidbody mainRb = GetComponent<Rigidbody>();
            if (mainRb != null && !mainRb.isKinematic)
            {
                mainRb.linearVelocity = Vector3.zero;
                mainRb.angularVelocity = Vector3.zero;
                mainRb.isKinematic = true;
            }
            
            // 3. Karakteri oyuncuya doğru döndür (arka dönmek için)
            yield return StartCoroutine(RotateToPlayer());
            
            // 4. Pozisyonu garantile (karakterin yerinde kalması için)
            transform.position = originalPosition;
            if (mainRb != null)
            {
                mainRb.position = originalPosition;
            }
            
            // 5. Pose'u animator'a kalıcı olarak uygula
            ApplyPoseToAnimator();
            
            // 6. Pose'dan el pozisyonlarını al
            Vector3 leftHandPos, rightHandPos;
            GetHandPositionsFromPose(out leftHandPos, out rightHandPos);
            
            // 7. Kelepçe spawn et
            SpawnHandcuff(leftHandPos, rightHandPos);
        }
        
        /// <summary>
        /// Pose'u animator'a kalıcı olarak uygula
        /// </summary>
        private void ApplyPoseToAnimator()
        {
            if (arrestPoseClip == null || animator == null)
            {
                return;
            }
            
            // Eski graph'i temizle
            if (arrestPoseGraph.IsValid())
            {
                arrestPoseGraph.Destroy();
            }
            
            // Animator'ı aç ve mevcut animasyonu durdur
            bool animatorWasEnabled = animator.enabled;
            if (!animatorWasEnabled)
            {
                animator.enabled = true;
            }
            
            // Animator Controller'ı geçici olarak devre dışı bırak (pose'un uygulanması için)
            // Orijinal controller'ı kaydet (serbest bırakma için)
            if (originalAnimatorController == null)
            {
                originalAnimatorController = animator.runtimeAnimatorController;
            }
            animator.runtimeAnimatorController = null;
            
            // Animator'ı geçici olarak durdur ve tekrar başlat (mevcut state'i temizlemek için)
            animator.enabled = false;
            animator.enabled = true;
            
            // Animator'ı bir frame bekle (state'lerin temizlenmesi için)
            animator.Update(0f);
            
            // Yeni PlayableGraph oluştur
            arrestPoseGraph = PlayableGraph.Create("ArrestPose");
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(arrestPoseGraph, "Animation", animator);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(arrestPoseGraph, arrestPoseClip);
            output.SetSourcePlayable(clipPlayable);
            
            // Clip'i ilk frame'inde ayarla (tutuklama pozisyonu)
            clipPlayable.SetTime(0f);
            
            // Graph'i çalıştır
            arrestPoseGraph.Play();
            
            // İlk sample (pose'u uygula)
            for (int i = 0; i < 5; i++)
            {
                arrestPoseGraph.Evaluate(0f); // Her zaman ilk frame'i evaluate et
                animator.Update(0f);
            }
        }
        
        private void Update()
        {
            // Eğer tutuklanmışsa, pose'u her frame evaluate et (her zaman ilk frame'de kal)
            if (isArrested && arrestPoseGraph.IsValid())
            {
                arrestPoseGraph.Evaluate(0f);
            }
        }
        
        /// <summary>
        /// Karakteri oyuncuya doğru döndür (arka dönmek için)
        /// </summary>
        private IEnumerator RotateToPlayer()
        {
            Quaternion targetRotation;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("Player");
            
            if (player == null)
            {
                targetRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
            }
            else
            {
                Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                directionToPlayer.y = 0;
                targetRotation = Quaternion.LookRotation(-directionToPlayer);
            }
            
            yield return StartCoroutine(RotateSmoothly(transform, targetRotation, arrestRotationSpeed));
        }
        
        /// <summary>
        /// Smooth rotasyon coroutine
        /// </summary>
        private IEnumerator RotateSmoothly(Transform target, Quaternion targetRotation, float speed)
        {
            Quaternion startRotation = target.rotation;
            float elapsed = 0f;
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * speed;
                float t = Mathf.Clamp01(elapsed);
                float smoothT = t * t * (3f - 2f * t);
                target.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
                yield return null;
            }
            
            target.rotation = targetRotation;
        }
        /// <summary>
        /// Pose clip'ten el pozisyonlarını al (basit yaklaşım)
        /// </summary>
        private void GetHandPositionsFromPose(out Vector3 leftHandPos, out Vector3 rightHandPos)
        {
            leftHandPos = Vector3.zero;
            rightHandPos = Vector3.zero;
            
            if (arrestPoseClip == null || animator == null)
            {
                return;
            }
            
            // Kemikleri bul (sample etmeden önce) - Generic rig için
            Transform leftHand = FindHandBone("left");
            Transform rightHand = FindHandBone("right");
            
            // Pelvis için birden fazla alternatif isim dene
            Transform pelvis = FindBoneRecursive(transform, "pelvis");
            if (pelvis == null) pelvis = FindBoneRecursive(transform, "Pelvis");
            if (pelvis == null) pelvis = FindBoneRecursive(transform, "Hips");
            if (pelvis == null) pelvis = FindBoneRecursive(transform, "hips");
            if (pelvis == null) pelvis = FindBoneRecursive(transform, "root"); // Bazı riglerde root pelvis olabilir
            
            if (leftHand == null || rightHand == null || pelvis == null)
            {
                return;
            }
            
            // Sample etmeden önce pozisyonları kaydet
            Vector3 beforePelvisPos = pelvis.position;
            Vector3 beforeLeftHandPos = leftHand.position;
            Vector3 beforeRightHandPos = rightHand.position;
            
            // Animator'ı aç
            bool animatorWasEnabled = animator.enabled;
            if (!animatorWasEnabled)
            {
                animator.enabled = true;
            }
            
            // PlayableGraph ile pose clip'i sample et (Generic rig için optimize edilmiş)
            PlayableGraph graph = PlayableGraph.Create("PoseSampling");
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, arrestPoseClip);
            output.SetSourcePlayable(clipPlayable);
            
            // Clip'i ilk frame'inde sample et (tutuklama pozisyonu)
            clipPlayable.SetTime(0f);
            
            // Generic rig için daha agresif sample (pose'un uygulanması için)
            // Birden fazla kez evaluate et ve animator'ı güncelle
            for (int i = 0; i < 10; i++)
            {
                graph.Evaluate(0.016f); // ~60 FPS delta time
                animator.Update(0.016f);
            }
            
            // Son bir kez daha büyük delta time ile sample et
            graph.Evaluate(0.1f);
            animator.Update(0.1f);
            
            // Animator'ı force update et
            animator.Update(0f);
            
            // Pose sample edildikten sonra pozisyonları oku
            Vector3 posePelvisPos = pelvis.position;
            Vector3 poseLeftHandPos = leftHand.position;
            Vector3 poseRightHandPos = rightHand.position;
            
            // Pelvis'e göre offset hesapla
            Vector3 leftHandOffset = poseLeftHandPos - posePelvisPos;
            Vector3 rightHandOffset = poseRightHandPos - posePelvisPos;
            
            // Graph'i destroy etme (pose pozisyonlarını okumak için geçici olarak kullanıyoruz)
            // Not: Kalıcı pose uygulaması ApplyPoseToAnimator() metodunda yapılıyor
            graph.Destroy();
            if (!animatorWasEnabled)
            {
                animator.enabled = false;
            }
            
            // Mevcut karakterin pelvis pozisyonunu al (pose geri döndükten sonra)
            Vector3 currentPelvisPos = pelvis.position;
            
            // Mevcut karakterin pelvis pozisyonuna göre hedef pozisyonları hesapla
            leftHandPos = currentPelvisPos + leftHandOffset;
            rightHandPos = currentPelvisPos + rightHandOffset;
        }
        
        /// <summary>
        /// El kemiklerini bul (Animator veya recursive search ile)
        /// </summary>
        private Transform FindHandBone(string side)
        {
            // Önce Animator'dan dene
            if (animator != null && animator.isHuman)
            {
                if (side == "left")
                {
                    Transform hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    if (hand != null) return hand;
                }
                else
                {
                    Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    if (hand != null) return hand;
                }
            }
            
            // Animator'dan bulunamazsa recursive search yap (Generic rig için)
            string[] searchNames = side == "left" 
                ? new[] { "hand_l", "Hand_L", "leftHand", "LeftHand", "hand_l", "hand_L", "hand", "Hand", "l_hand", "L_Hand" }
                : new[] { "hand_r", "Hand_R", "rightHand", "RightHand", "hand_r", "hand_R", "hand", "Hand", "r_hand", "R_Hand" };
            
            foreach (string name in searchNames)
            {
                Transform found = FindBoneRecursive(transform, name);
                if (found != null) return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Recursive olarak kemik bul
        /// </summary>
        private Transform FindBoneRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;
                
                Transform found = FindBoneRecursive(child, name);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Kelepçe prefab'ını ellere spawn et (bileklere tam oturacak şekilde)
        /// </summary>
        private void SpawnHandcuff(Vector3 leftHandPos, Vector3 rightHandPos)
        {
            if (handcuffPrefab == null)
            {
                return;
            }
            
            // Eski kelepçeyi kaldır
            if (spawnedHandcuff != null)
            {
                Destroy(spawnedHandcuff);
            }
            
            // El kemiklerini bul
            Transform leftHand = FindHandBone("left");
            Transform rightHand = FindHandBone("right");
            
            if (leftHand == null || rightHand == null)
            {
                return;
            }
            
            // İki el arasındaki orta noktayı hesapla (bilek pozisyonu)
            Vector3 handcuffPosition = (leftHandPos + rightHandPos) / 2f;
            
            // Pozisyonu bileklerin içine almak için karaktere doğru (forward yönünde) kaydır
            handcuffPosition += transform.forward * -0.05f; // Biraz dışarı (negatif artırıldı)
            
            // Pozisyonu biraz yukarı al (bileklerin tam üzerine oturması için)
            handcuffPosition += transform.up * 0.02f; // Biraz yukarı
            
            // Offset uygula (bileklere tam oturması için)
            // Offset'i karakterin local space'ine göre uygula
            handcuffPosition += transform.TransformDirection(handcuffPositionOffset);
            
            // İki el arasındaki yönü hesapla (sağ el - sol el)
            Vector3 handDirection = (rightHandPos - leftHandPos).normalized;
            
            // Eğer yön sıfırsa (eller çok yakınsa), varsayılan yön kullan
            if (handDirection.magnitude < 0.01f)
            {
                handDirection = transform.right; // Karakterin sağa doğru yönü
            }
            
            // Kelepçenin rotasyonunu hesapla
            // Kelepçe bileklerin etrafında yatay durur, bu yüzden:
            // - Forward: İki el arasındaki yön (handDirection)
            // - Up: Karakterin up yönü (dikey)
            Vector3 up = transform.up;
            Quaternion handcuffRotation = Quaternion.LookRotation(handDirection, up);
            
            // Rotasyonu 90 dereceye ayarla (Y ekseni etrafında)
            handcuffRotation *= Quaternion.Euler(0, 90, 0);
            
            // Kelepçeyi spawn et
            spawnedHandcuff = Instantiate(handcuffPrefab, handcuffPosition, handcuffRotation);
            spawnedHandcuff.name = "Handcuff_" + npcName;
            
            // Kelepçeyi sol ele parent et (böylece karakterle birlikte hareket eder)
            // Local pozisyonu iki el arasındaki mesafeye göre ayarla
            spawnedHandcuff.transform.SetParent(leftHand);
            
            // Sol elin world pozisyonuna göre local pozisyonu hesapla
            Vector3 localOffset = leftHand.InverseTransformPoint(handcuffPosition);
            spawnedHandcuff.transform.localPosition = localOffset;
            
            // Local rotasyonu ayarla (sol elin rotasyonuna göre)
            Quaternion localRotation = Quaternion.Inverse(leftHand.rotation) * handcuffRotation;
            spawnedHandcuff.transform.localRotation = localRotation;
        }

        // IInteractable Implementation
        // CarryController kaldırıldığı için bu etkileşim şimdilik devre dışı.
        public void Interact(InteractionsManager source)
        {
            // taşıma sistemi devre dışı
            return;
        }

        public string GetInteractionName()
        {
            // Taşıma sistemi devre dışı - etkileşim gösterilmesin
            return "";
        }

        // Carry Methods
        public void StartCarrying(Transform carryPoint)
        {
            isBeingCarried = true;
            carryPointTransform = carryPoint;

            if (ragdoll != null)
            {
                ragdoll.isEnabled = false;
                ragdoll.enabled = false;
            }

            SetRigidbodiesKinematic(true);

            if (disableCollisionWhenCarried)
            {
                SetCollidersToTrigger(true);
                IgnorePlayerCollision(true);
            }
            else
            {
                IgnorePlayerCollision(true);
            }
            
            EnsureCollidersEnabled();
            
            if (colliderProtectionCoroutine != null)
            {
                StopCoroutine(colliderProtectionCoroutine);
            }
            colliderProtectionCoroutine = StartCoroutine(ProtectCollidersCoroutine());
        }

        public void StopCarrying()
        {
            isBeingCarried = false;
            carryPointTransform = null;

            if (colliderProtectionCoroutine != null)
            {
                StopCoroutine(colliderProtectionCoroutine);
                colliderProtectionCoroutine = null;
            }

            if (disableCollisionWhenCarried)
            {
                SetCollidersToTrigger(false);
                IgnorePlayerCollision(false);
            }
            else
            {
                IgnorePlayerCollision(false);
            }

            PlaceOnGround();
            SetRigidbodiesKinematic(false);

            if (ragdoll != null)
            {
                ragdoll.enabled = true;
                ragdoll.isEnabled = true;
            }
        }

        // Helper Methods
        private void SetRigidbodiesKinematic(bool isKinematic)
        {
            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb != null)
                {
                    rb.isKinematic = isKinematic;
                }
            }
        }

        private void SetCollidersToTrigger(bool isTrigger)
        {
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    col.isTrigger = isTrigger;
                }
            }
        }

        private void IgnorePlayerCollision(bool ignore)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("Player");
            
            if (player != null)
            {
                Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
                
                foreach (Collider npcCol in colliders)
                {
                    if (npcCol != null && !npcCol.isTrigger)
                    {
                        foreach (Collider playerCol in playerColliders)
                        {
                            if (playerCol != null && !playerCol.isTrigger)
                            {
                                Physics.IgnoreCollision(npcCol, playerCol, ignore);
                            }
                        }
                    }
                }
            }
        }

        private void EnsureCollidersEnabled()
        {
            if (mainCollider != null && !mainCollider.enabled)
            {
                mainCollider.enabled = true;
            }
        }

        private void PlaceOnGround()
        {
            Vector3 currentPosition = transform.position;
            float raycastDistance = 10f;
            
            int npcLayer = gameObject.layer;
            int layerMask = ~(1 << npcLayer);
            
            Vector3 raycastOrigin = currentPosition + Vector3.up * 2f;
            
            if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hit, raycastDistance, layerMask))
            {
                float offset = 0.2f;
                Vector3 groundPosition = new Vector3(currentPosition.x, hit.point.y + offset, currentPosition.z);
                transform.position = groundPosition;
                
                Rigidbody mainRb = GetComponent<Rigidbody>();
                if (mainRb != null)
                {
                    mainRb.position = groundPosition;
                    mainRb.linearVelocity = Vector3.zero;
                    mainRb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void LateUpdate()
        {
            // Escort modundaysa öncelik escort'a
            if (isBeingEscorted && escortingPlayer != null && playerCamera != null)
            {
                UpdateEscortPosition();
                return;
            }
            
            // Carry modundaysa pozisyonu carry point'e eşitle
            if (isBeingCarried && carryPointTransform != null)
            {
                EnsureCollidersEnabled();
                // Offset'i carry point'in local space'ine göre uygula (TransformPoint hem pozisyon hem rotasyon uygular)
                transform.position = carryPointTransform.TransformPoint(carryPositionOffset);
                transform.rotation = carryPointTransform.rotation * Quaternion.Euler(carryRotationOffset);
            }
        }

        private IEnumerator ProtectCollidersCoroutine()
        {
            while (isBeingCarried)
            {
                EnsureCollidersEnabled();
                
                if (mainCollider != null && !mainCollider.enabled)
                {
                    mainCollider.enabled = true;
                }
                
                yield return null;
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(npcName))
            {
                npcName = gameObject.name;
            }
        }

        private void OnDestroy()
        {
            // ProceduralAnimator'ı durdur (NaN rotation hatasını önlemek için)
            ProceduralAnimator proceduralAnimator = GetComponent<ProceduralAnimator>();
            if (proceduralAnimator != null)
            {
                proceduralAnimator.isActive = false;
                proceduralAnimator.enabled = false;
            }
            
            // Tüm ProceduralAnimator component'lerini child'larda da durdur
            ProceduralAnimator[] allProceduralAnimators = GetComponentsInChildren<ProceduralAnimator>();
            foreach (ProceduralAnimator animator in allProceduralAnimators)
            {
                if (animator != null)
                {
                    animator.isActive = false;
                    animator.enabled = false;
                }
            }
            
            // Graph'i temizle
            if (arrestPoseGraph.IsValid())
            {
                arrestPoseGraph.Destroy();
            }
            
            if (damageable != null)
            {
                damageable.OnDeath.RemoveListener(OnUnconsciousStateChanged);
            }
            
            if (colliderProtectionCoroutine != null)
            {
                StopCoroutine(colliderProtectionCoroutine);
            }
            
            // Kelepçeyi temizle
            if (spawnedHandcuff != null)
            {
                Destroy(spawnedHandcuff);
            }
        }
    }

    /// <summary>
    /// Karakter sınıfları
    /// </summary>
    public enum CharacterClass
    {
        NPC,        // Normal vatandaş
        Enemy,      // Düşman/Suçlu
        Police,     // Polis
        Guard,      // Güvenlik görevlisi
        Civilian,   // Sivil
        Suspect,    // Şüpheli
        Other       // Diğer
    }

    /// <summary>
    /// Taşıma gereksinimi
    /// </summary>
    public enum CarryRequirement
    {
        Never,          // Asla taşınamaz
        Unconscious,    // Sadece bilinçsizken taşınabilir
        Always          // Her zaman taşınabilir
    }
    
    /// <summary>
    /// Tutuklama sistemi için kemik referansları
    /// </summary>
    [System.Serializable]
    public class ArrestBoneReferences
    {
        [Header("Otomatik Bulunan Kemikler")]
        public Transform pelvis;
        public Transform spine_03;
        public Transform leftClavicle;
        public Transform rightClavicle;
        public Transform leftUpperArm;
        public Transform rightUpperArm;
        public Transform leftLowerArm;
        public Transform rightLowerArm;
        public Transform leftHand;
        public Transform rightHand;
        
        [Header("Durum")]
        public bool bonesFound = false;
    }
}

