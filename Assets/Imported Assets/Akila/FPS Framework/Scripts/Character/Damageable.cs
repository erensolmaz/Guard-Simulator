using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace Akila.FPSFramework
{
    [AddComponentMenu("Akila/FPS Framework/Health System/Damageable")]
    public class Damageable : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// Defines the type of this damageable (Player, NPC, Other).
        /// </summary>
        public DamagableType type = DamagableType.Other;

        /// <summary>
        /// Current health of this damageable.
        /// </summary>
        public float health = 100;

        /// <summary>
        /// Delay before destruction occurs after death.
        /// </summary>
        public float destroyDelay;

        /// <summary>
        /// How strong the camera shake should be on taking damage (0–1).
        /// </summary>
        [Range(0, 1)] public float damageCameraShake = 0.3f;

        [Space]
        [FormerlySerializedAs("destoryOnDeath")]
        /// <summary>
        /// Whether this object should be destroyed upon death.
        /// </summary>
        public bool destroyOnDeath;

        /// <summary>
        /// Whether the root object should be destroyed instead of this GameObject.
        /// </summary>
        public bool destroyRoot;

        /// <summary>
        /// Whether ragdoll physics should be enabled on death.
        /// </summary>
        public bool ragdolls;

        /// <summary>
        /// Optional death effect prefab to spawn on death.
        /// </summary>
        public GameObject deathEffect;

        [Space]
        /// <summary>
        /// If true, this damageable will automatically regenerate health.
        /// </summary>
        public bool autoHeal = true;

        /// <summary>
        /// Speed multiplier for auto healing.
        /// </summary>
        public float autoHealSpeed = 1;

        /// <summary>
        /// Delay in seconds before auto healing begins.
        /// </summary>
        public float autoHealDelay = 1;

        [Space]
        /// <summary>
        /// Event invoked when this damageable dies.
        /// </summary>
        public UnityEvent onDeath;

        [Header("Unconscious Settings")]
        /// <summary>
        /// If true, character becomes unconscious instead of dying.
        /// </summary>
        [Tooltip("If true, character becomes unconscious instead of dying (for NPCs that can be arrested).")]
        public bool becomesUnconscious = false;

        /// <summary>
        /// Health threshold for becoming unconscious (0 = at death).
        /// </summary>
        [Tooltip("Health threshold for becoming unconscious (0 = at death).")]
        public float unconsciousThreshold = 0f;

        /// <summary>
        /// Whether this character is currently unconscious.
        /// </summary>
        public bool IsUnconscious { get; private set; }

        /// <summary>
        /// Event invoked when unconscious state changes.
        /// </summary>
        public UnityEvent onUnconsciousStateChanged;

        /// <summary>
        /// The actor interface attached to this damageable.
        /// </summary>
        public IActor Actor { get; set; }

        /// <summary>
        /// The ragdoll component attached to this damageable (if any).
        /// </summary>
        public Ragdoll ragdoll { get; set; }

        /// <summary>
        /// The source GameObject responsible for the latest damage.
        /// </summary>
        public GameObject DamageSource { get; set; }


        /// <summary>
        /// Maximum health of this damageable.
        /// </summary>
        public float maxHealth { get; set; }

        /// <summary>
        /// Damageable parts associated with this entity.
        /// </summary>
        public IDamageablePart[] groups { get; set; }

        private bool died;

        /// <summary>
        /// Whether death has been fully confirmed.
        /// </summary>
        public bool DeadConfirmed { get; set; }

        private float autoHealDelayTime = 0;

        /// <summary>
        /// True if this damageable is currently healing.
        /// </summary>
        public bool isHealing { get; protected set; }

        private bool previoslyHealing;

        /// <summary>
        /// Event called when healing starts.
        /// </summary>
        public Action OnHealingStarted;

        /// <summary>
        /// Event called when healing ends.
        /// </summary>
        public Action OnHealingEnded;

        private void Awake()
        {
            maxHealth = health;
        }

        private void Start()
        {
            Actor = GetComponent<IActor>();
            ragdoll = GetComponent<Ragdoll>();

            Debug.Log($"[Damageable] ✅ Start() - GameObject: {gameObject.name}");
            Debug.Log($"[Damageable] - Type: {type}");
            Debug.Log($"[Damageable] - Health: {health}/{maxHealth}");
            Debug.Log($"[Damageable] - Ragdoll: {(ragdoll != null ? "VAR ✅" : "YOK ❌")}");
            Debug.Log($"[Damageable] - Ragdolls Checkbox: {(ragdolls ? "AÇIK" : "KAPALI")}");

            OnDeath.AddListener(Die);

            if (type == DamagableType.Player)
            {
                if (this.SearchFor<CharacterManager>() != null)
                    DeathCamera.Instance?.Disable();

                groups = GetComponentsInChildren<IDamageablePart>();
            }

            if (type == DamagableType.Other)
            {
                if (ragdoll || Actor != null)
                    Debug.LogWarning($"{this} has humanoid components and its type is Other. Please change type to Humanoid to avoid errors.");
            }
        }

        /// <summary>
        /// If true, damageable visual/audio effects are allowed.
        /// </summary>
        public bool allowDamageableEffects { get; set; } = true;

        /// <summary>
        /// If true, respawn is allowed after death.
        /// </summary>
        public bool allowRespawn { get; set; } = true;

        /// <summary>
        /// Current health of this damageable (public property for easy access).
        /// </summary>
        public float Health 
        { 
            get => health; 
            set => health = value; 
        }

        float IDamageable.Health { get => health; set => health = value; }

        private float previousHealth;

        private void Update()
        {
            if (autoHeal && DeadConfirmed == false)
            {
                if (autoHealDelayTime > 0)
                    autoHealDelayTime -= Time.deltaTime;

                if (autoHealDelayTime <= 0)
                {
                    if (health <= maxHealth)
                    {
                        health += Time.deltaTime * (maxHealth * 0.3f) * autoHealSpeed;
                    }

                    if (health > maxHealth)
                        health = maxHealth;

                    if (health < 0)
                        health = 0;
                }
            }

            if (isHealing)
            {
                if (allowDamageableEffects && GetComponent<ICharacterController>() != null)
                    DamageableEffectsVisualizer.instance.TriggerHealingEffect();
            }
        }

        private bool initialized = false;

        private void LateUpdate()
        {
            if (!initialized)
            {
                previousHealth = health;
                initialized = true;
                return;
            }

            isHealing = false;

            if (health != previousHealth)
            {
                // Healed
                if (health > previousHealth)
                {
                    isHealing = true;
                }
                // Damaged
                else
                {
                    UpdateSystem();
                }
            }

            if (isHealing != previoslyHealing)
            {
                if (isHealing && !previoslyHealing)
                {
                    OnHealingStarted?.Invoke();
                }
                else
                {
                    OnHealingEnded?.Invoke();
                }
            }

            previousHealth = health;
            previoslyHealing = isHealing;
        }

        private void UpdateSystem()
        {
            if (!died && health <= 0)
            {
                Debug.Log($"[Damageable] ⚠️ HEALTH 0'A İNDİ! Health: {health}, Died: {died}, GameObject: {gameObject.name}");
                Debug.Log($"[Damageable] OnDeath event tetikleniyor...");
                OnDeath?.Invoke();
            }

            CharacterManager characterManager = this.SearchFor<CharacterManager>();

            if (type == DamagableType.Player && characterManager != null)
            {
                characterManager.cameraManager.ShakeCameras(damageCameraShake);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (!allowDamageableEffects) return;
            if (type != DamagableType.Player) return;

            if (Actor == null)
            {
                Debug.LogError("Couldn't find Actor on Damageable", gameObject);
                return;
            }

            if (this.SearchFor<CharacterManager>() == null)
            {
                Debug.LogError("Couldn't find CharacterManager on Damagable.", gameObject);
                return;
            }

            UIManager uIManager = UIManager.Instance;

            if (uIManager == null)
            {
                Debug.LogError("UIManager is not set. Make sure to have a UIManager in your scene.", gameObject);
                return;
            }

            if (DamageSource != null)
                UIManager.Instance?.DamagableVisualizer?.TriggerDamageEffect();
        }

        private void Die()
        {
            if (isDamagableDisabled) return;

            // Eğer becomesUnconscious true ise, death yerine unconscious yap
            if (becomesUnconscious && unconsciousThreshold >= 0)
            {
                BecomeUnconscious();
                return; // Death'i tetikleme, sadece unconscious yap
            }

            if (destroyOnDeath && !destroyRoot) Destroy(gameObject, destroyDelay);
            if (destroyOnDeath && destroyRoot) Destroy(gameObject.transform.parent.gameObject, destroyDelay);

            if (died == false) Respawn();

            // Ragdoll aktivasyonu - checkbox'a bakmadan çalışır
            if (ragdoll != null)
            {
                Debug.Log($"[Damageable] 💀 Karakter öldü! Ragdoll aktif ediliyor... GameObject: {gameObject.name}");
                
                // Animator'ü kapat
                Animator animator = GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                    Debug.Log($"[Damageable] Animator devre dışı bırakıldı");
                }
                
                // Ragdoll'u aktif et
                ragdoll.isEnabled = true;
                ragdoll.Enable();
                
                // Tüm rigidbody'leri non-kinematic yap
                Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
                Debug.Log($"[Damageable] {rbs.Length} adet Rigidbody bulundu");
                
                if (rbs.Length == 0)
                {
                    Debug.LogError($"[Damageable] ❌ HİÇ RİGİDBODY YOK! Ragdoll Setup Tool ile ragdoll kurun!");
                }
                else
                {
                    int activeCount = 0;
                    foreach (Rigidbody rb in rbs)
                    {
                        if (rb != null && rb.transform != transform)
                        {
                            rb.isKinematic = false;
                            rb.useGravity = true;
                            activeCount++;
                        }
                    }
                    Debug.Log($"[Damageable] ✅ RAGDOLL AKTİF! isEnabled: {ragdoll.isEnabled}, {activeCount} Rigidbody aktif");
                }
            }
            else if (ragdolls)
            {
                // Eski yöntem - ragdoll checkbox'u açıksa
                Debug.Log($"[Damageable] Ragdoll checkbox ile aktif ediliyor (eski yöntem)");
            }
            else
            {
                Debug.LogWarning($"[Damageable] Ragdoll komponenti bulunamadı! GameObject: {gameObject.name}");
            }

            if (deathEffect)
            {
                GameObject effect = Instantiate(deathEffect, transform.position, transform.rotation);
                effect.SetActive(true);
            }

            if (type == DamagableType.Player)
            {
                Vector3 damagePos = transform.position;

                if (DamageSource)
                {
                    damagePos = DamageSource.transform.position;
                }

                DeathCamera.Instance?.Enable(gameObject, damagePos);
            }

            died = true;
            DeadConfirmed = true; // Ölüm onaylandı
        }

        /// <summary>
        /// Makes the character unconscious instead of dying.
        /// </summary>
        private void BecomeUnconscious()
        {
            if (IsUnconscious) return;

            IsUnconscious = true;
            DeadConfirmed = true;

            Debug.Log($"[Damageable] {gameObject.name} bilinçsiz oldu!");

            // Animator'ü kapat
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            // Ragdoll'u aktif et
            if (ragdoll != null)
            {
                ragdoll.isEnabled = true;
                ragdoll.Enable();

                Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in rbs)
                {
                    if (rb != null && rb.transform != transform)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                }
            }

            onUnconsciousStateChanged?.Invoke();
            onDeath?.Invoke(); // Diğer sistemler için death event'i de tetikle
        }

        /// <summary>
        /// Event invoked on respawn.
        /// </summary>
        public UnityEvent onRespawn;

        private void Respawn()
        {
            Actor = this.SearchFor<IActor>();

            if (type == DamagableType.Other || Actor == null) return;

            onRespawn?.Invoke();

            if (Actor != null)
            {
                Actor.Respawn(SpawnManager.Instance.respawnDelay);
            }
        }

        /// <summary>
        /// Applies damage to this damageable.
        /// </summary>
        /// <param name="amount">Damage amount.</param>
        /// <param name="damageSource">The source of damage.</param>
        public void Damage(float amount, GameObject damageSource)
        {
            health -= amount;
            this.DamageSource = damageSource;

            autoHealDelayTime = autoHealDelay;
            
            Debug.Log($"[Damageable] Hasar alındı! Damage: {amount}, Kalan Health: {health}/{maxHealth}, GameObject: {gameObject.name}");
        }

        /// <summary>
        /// Event accessor for death event.
        /// </summary>
        public UnityEvent OnDeath => onDeath;

        /// <summary>
        /// If true, this damageable is currently disabled and cannot take damage.
        /// </summary>
        public bool isDamagableDisabled { get; set; }

        /// <summary>
        /// Returns the type of this damageable.
        /// </summary>
        public DamagableType damagableType => type;

        [ContextMenu("Setup/Network Components")]
        private void SetupNetworkComponents()
        {
            FPSFrameworkCore.InvokeConvertMethod("ConvertDamageable", this, new object[] { this });
        }
    }

    public enum DamagableType
    {
        Player = 0,
        NPC = 1,
        Other = 2
    }
}
