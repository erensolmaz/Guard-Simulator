using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Akila.FPSFramework.Experimental
{
    [AddComponentMenu("Akila/FPS Framework/Player/Fall Damage")]
    [RequireComponent(typeof(CharacterController), typeof(Damageable))]
    public class FallDamage : MonoBehaviour
    {
        private CharacterController controller;
        private IActor actor;
        private Damageable healthSystem;

        public float velocityThreshold = 10f; // Velocity at which damage starts
        public float damage = 30f; // Max possible damage

        private bool hasLanded;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            actor = GetComponent<IActor>();
            healthSystem = GetComponent<Damageable>();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.transform.TryGetComponent(out Ignore _ignore) && _ignore.ignoreFallDamage) return;
            if (!hasLanded && controller.velocity.y < -velocityThreshold)
            {
                ApplyDamage();
                hasLanded = true;
            }
        }

        private void ApplyDamage()
        {
            float impactVelocity = Mathf.Abs(controller.velocity.y);
            float damage = (impactVelocity / velocityThreshold) * this.damage;
            healthSystem.Damage(damage, actor.gameObject);
        }
    }
}
