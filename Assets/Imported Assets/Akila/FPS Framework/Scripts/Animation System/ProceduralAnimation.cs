using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Linq;
namespace Akila.FPSFramework.Animation
{
    [AddComponentMenu("Akila/FPS Framework/Animation/Procedural Animation")]
    public class ProceduralAnimation : MonoBehaviour
    {
        public enum AnimationType
        {
            Override,
            Additive
        }

        [Header("BASE"), Space]
        public string Name = "New Procedural Animation";
        public float length = 0.15f;
        [Range(0, 1)] public float weight = 1;
        public bool loop = false;
        public bool autoStop = false;
        public bool perModifierConnections = true;
        public bool playOnAwake = false;
        public bool unidirectinalPlay = false;
        public bool resetOnPlayed = false;
        public bool isolate = false;
        public AnimationType isolationMode = AnimationType.Override;
        public UpdateMode updateMode;

        [Space]
        public TriggerType triggerType;
        public InputAction triggerInputAction = new InputAction();

        [Space(6), Separator, Space(6)]
        public ProceduralAnimationEvents events = new ProceduralAnimationEvents();
        public List<CustomProceduralAnimationEvent> customEvents = new List<CustomProceduralAnimationEvent>();
        public List<ProceduralAnimationConnection> connections = new List<ProceduralAnimationConnection>();

        public MoveAnimationModifier[] moveAnimationModifiers { get; protected set; }
        public SpringAnimationModifier[] springAnimationModifiers { get; protected set; }
        public KickAnimationModifier[] kickAnimationModifiers { get; protected set; }
        public SwayAnimationModifier[] swayAnimationModifiers { get; protected set; }
        public WaveAnimationModifier[] waveAnimationModifiers { get; protected set; }
        public OffsetAnimationModifier[] offsetAnimationModifiers { get; protected set; }

        public bool isActive { get; set; } = true;

        /// <summary>
        /// final position result for this clip
        /// </summary>
        public Vector3 targetPosition
        {
            get
            {
                return GetTargetModifiersPosition() * weight * FPSFrameworkSettings.globalAnimationWeight;
            }
        }

        /// <summary>
        /// final rotation result for this clip
        /// </summary>
        public Vector3 targetRotation
        {
            get
            {
                return GetTargetModifiersRotation() * weight * FPSFrameworkSettings.globalAnimationWeight;
            }
        }

        /// <summary>
        /// current animation progress by value from 0 to 1
        /// </summary>
        public float progress { get; set; }

        private bool _isPlaying;

        public bool isPlaying
        {
            get
            {
                return _isPlaying;
            }

            set
            {
                triggerType = TriggerType.None;

                if (HasToAvoid())
                    _isPlaying = false;
                else
                {
                    _isPlaying = value;
                }
            }
        }
        public bool isPaused { get; set; }

        private bool isTrigged;

        //acutal velocity
        private float currentVelocity;

        /// <summary>
        /// current animation movement speed
        /// </summary>
        public float velocity { get => currentVelocity; }

        /// <summary>
        /// List of all modifieres applied to this animation
        /// </summary>
        public List<ProceduralAnimationModifier> modifiers { get; set; } = new List<ProceduralAnimationModifier>();
        public bool alwaysStayIdle { get; set; }

        private Vector3 defaultPosition;
        private Quaternion defaultRotation;

        private void Awake()
        {
            RefreshModifiers();
            triggerInputAction.Enable();

            foreach (ProceduralAnimationModifier modifier in modifiers)
            {
                modifier.targetAnimation = this;
            }

            moveAnimationModifiers = GetComponents<MoveAnimationModifier>();
            springAnimationModifiers = GetComponents<SpringAnimationModifier>();
            kickAnimationModifiers = GetComponents<KickAnimationModifier>();
            swayAnimationModifiers = GetComponents<SwayAnimationModifier>();
            waveAnimationModifiers = GetComponents<WaveAnimationModifier>();
            offsetAnimationModifiers = GetComponents<OffsetAnimationModifier>();

            defaultPosition = transform.localPosition;
            defaultRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            if(playOnAwake == true)
            {
                Play(0);
            }
        }

        private void Start()
        {
            GetComponentInParent<ProceduralAnimator>()?.RefreshClips();
        }

        bool isTriggred;
        float lastTriggerTime;

        private void Update()
        {
            if (updateMode == UpdateMode.Update)
                Tick();
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
                Tick();
        }

        private void LateUpdate()
        {
            if(updateMode == UpdateMode.LateUpdate)
                Tick();
        }

        private Vector3 isolatedPosition;
        private Quaternion isolatedRotation;

        public void Tick()
        {
            if (isActive == false) return;

            //Handles the custom events and progress for this animation.
            HandleEvents();

            foreach(ProceduralAnimationConnection connection in connections)
            {
                if(connection.type == ProceduralAnimationConnectionType.PauseInIdle)
                {
                    if (!connection.target.isPlaying)
                        Pause();
                    else
                        Unpause();
                }

                if(connection.type == ProceduralAnimationConnectionType.PauseInTrigger)
                {
                    if (connection.target.isPlaying)
                        Pause();
                    else
                        Unpause();
                }
            }

            if (triggerType == TriggerType.Hold)
            {
                if (triggerInputAction.IsPressed() && progress < 0.9f) Play();
                else Stop();
            }

            if (triggerType == TriggerType.Tab)
            {
                if (triggerInputAction.triggered) isTrigged = !isTrigged;

                if (isTrigged) Play();
                else Stop();
            }

            if (triggerType == TriggerType.DoubleTab)
            {
                triggerInputAction.HasDoupleClicked(ref isTrigged, ref lastTriggerTime, 0.3f);

                if (isTrigged) Play();
                else Stop();
            }

            if (triggerType == TriggerType.Trigger)
            {
                if (triggerInputAction.triggered)
                {
                    Play(0);
                }
            }

            if (!isPaused)
                UpdateProgress();

            if (loop && progress >= 0.999f)
            {
                progress = 0;
            }

            if (autoStop && progress >= 0.999f || HasToAvoid())
            {
                Stop();
            }

            if (isolate)
            {
                isolatedPosition = targetPosition;
                isolatedRotation = Quaternion.Euler(targetRotation);

                if(isolationMode == AnimationType.Additive)
                {
                    isolatedPosition += transform.localPosition;
                    isolatedRotation *= transform.localRotation;
                }
                else
                {
                    isolatedPosition += defaultPosition;
                    isolatedRotation *= defaultRotation;
                }

                transform.localPosition = targetPosition + defaultPosition;
                transform.localRotation = isolatedRotation;
            }
        }

        public void Play(float fixedTime = -1)
        {
            if (unidirectinalPlay && progress > 0.1f)
                return;

            foreach(ProceduralAnimationConnection connection in connections)
            {
                if(connection.target == null)
                {
                    Debug.LogError($"[Procedural Animation] Connection's target reference is null or missing on {gameObject.name}. This instance will be ignored.", gameObject);
                }
            }

            if (!HasToAvoid())
            {
                isPaused = false;
                _isPlaying = true;
            }

            if (resetOnPlayed)
            {
                progress = 0;
            }
            else if (fixedTime >= 0)
            {
                progress = fixedTime;
            }

            events.OnPlay?.Invoke();
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Unpause()
        {
            isPaused = false;
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        private void UpdateProgress()
        {
            float masterSpeed = 1;

            masterSpeed = FPSFrameworkSettings.globalAnimationSpeed;

            if (_isPlaying)
                progress = Mathf.SmoothDamp(progress, 1, ref currentVelocity, length / masterSpeed);

            if (!_isPlaying || HasToAvoid())
                progress = Mathf.SmoothDamp(progress, 0, ref currentVelocity, length / masterSpeed);
        }

        private bool prevPlaying;

        private void HandleEvents()
        {
            if(_isPlaying && !prevPlaying)
            {
                events.OnPlayed?.Invoke();
            }

            if(!_isPlaying && prevPlaying)
            {
                events.OnStoped?.Invoke();
            }

            foreach (CustomProceduralAnimationEvent animationEvent in customEvents) animationEvent.UpdateEvent(this);

            prevPlaying = _isPlaying;
        }

        /// <summary>
        /// returns all the clip modifiers for this clip in a List of ProceduralAnimationClip and refreshes the animtor clips 
        /// </summary>
        public List<ProceduralAnimationModifier> RefreshModifiers()
        {
            modifiers = GetComponentsInChildren<ProceduralAnimationModifier>().ToList();

            return modifiers;
        }

        public bool HasToAvoid()
        {
            bool result = false;

            if (alwaysStayIdle || FPSFrameworkCore.IsActive == false) return true;

            foreach (ProceduralAnimationConnection connection in connections)
            {
                if (connection.target != null)
                {
                    if (connection.type == ProceduralAnimationConnectionType.AvoidInTrigger)
                    {
                        if (connection.target && connection.target._isPlaying) result = true;
                    }

                    if (connection.type == ProceduralAnimationConnectionType.AvoidInIdle)
                    {
                        if (!connection.target._isPlaying) result = true;
                    }
                }
            }

            return result;
        }

        public float GetAvoidanceFactor(ProceduralAnimation animation)
        {
            if(animation == null) return 0f;

            return Mathf.Lerp(1, 0, animation.progress);
        }

        /// <summary>
        /// final position result for this modifier
        /// </summary>
        public Vector3 GetTargetModifiersPosition()
        {
            Vector3 result = Vector3.zero;

            float avoidanceFactor = 1;

            foreach (ProceduralAnimationConnection connection in connections)
            {
                if (connection.target != null)
                {
                    if (connection.type == ProceduralAnimationConnectionType.AvoidInTrigger)
                    {
                        avoidanceFactor *= GetAvoidanceFactor(connection.target);
                    }

                    if (connection.type == ProceduralAnimationConnectionType.AvoidInIdle)
                    {
                        avoidanceFactor *= Mathf.Lerp(1, 0, GetAvoidanceFactor(connection.target));
                    }
                }
            }

            foreach (ProceduralAnimationModifier modifier in modifiers) result += modifier.targetPosition;

            if (perModifierConnections)
                result *= avoidanceFactor;

            return result;
        }

        /// <summary>
        /// final rotation result for this modifier
        /// </summary>
        public Vector3 GetTargetModifiersRotation()
        {
            Vector3 result = Vector3.zero;

            float avoidanceFactor = 1;

            foreach (ProceduralAnimationConnection connection in connections)
            {
                if (connection.target != null)
                {
                    if (connection.type == ProceduralAnimationConnectionType.AvoidInTrigger)
                    {
                        avoidanceFactor *= GetAvoidanceFactor(connection.target);
                    }

                    if (connection.type == ProceduralAnimationConnectionType.AvoidInIdle)
                    {
                        avoidanceFactor *= Mathf.Lerp(1, 0, GetAvoidanceFactor(connection.target));
                    }
                }
            }

            foreach (ProceduralAnimationModifier modifier in modifiers) result += modifier.targetRotation;

            if (perModifierConnections)
                result *= avoidanceFactor;

            return result;
        }

        public enum TriggerType
        {
            None = 0,
            Tab = 1,
            Hold = 2,
            DoubleTab = 3,
            Trigger = 4
        }
    }
}