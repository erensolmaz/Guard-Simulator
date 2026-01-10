using System.Collections.Generic;
using UnityEngine;

namespace Akila.FPSFramework
{
    /// <summary>
    /// Controls the transition between animated and ragdoll states for a player character.
    /// Supports both standard ragdolling and a hybrid mode that preserves external forces.
    /// </summary>
    [AddComponentMenu("Akila/FPS Framework/Player/Ragdoll")]
    public class Ragdoll : MonoBehaviour
    {
        public Animator animator;
        public bool isEnabled;
        
        [Tooltip("Taşınırken ragdoll update'ini devre dışı bırak")]
        public bool isBeingCarried = false;

        protected Rigidbody[] rigidbodies;

        protected virtual void Start()
        {
            if (animator == null)
                animator = transform.SearchFor<Animator>();
            
            rigidbodies = GetComponentsInChildren<Rigidbody>();

            if (isEnabled)
                Enable();
            else
                Disable();
        }

        protected virtual void Update()
        {
            // Taşınıyorsa update yapma!
            if (isBeingCarried)
            {
                // Taşınırken tüm rigidbody'leri kinematic tut
                foreach(Rigidbody rb in rigidbodies)
                {
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
                return;
            }
            
            foreach(Rigidbody rb in rigidbodies) 
            {
                if (rb != null)
                    rb.isKinematic = !isEnabled;
            }
        }

        protected virtual void FixedUpdate()
        {
            // FixedUpdate'te de taşınırken kontrol et
            if (isBeingCarried)
            {
                // Taşınırken tüm rigidbody'leri kinematic tut
                foreach(Rigidbody rb in rigidbodies)
                {
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }

        public virtual void Enable()
        {
            isEnabled = true;
            if (animator != null)
                animator.enabled = false;
        }

        public virtual void Disable()
        { 
            isEnabled = false;
            if (animator != null)
                animator.enabled = true;
        }
    }
}