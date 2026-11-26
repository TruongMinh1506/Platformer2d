using UnityEngine;

namespace Platformer.Common
{
    /// <summary>
    /// Static or placed spike hazard. Upon player contact, delivers damage
    /// and repelling upward/outward knockback.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HazardSpikes : MonoBehaviour
    {
        [Header("Hazard Settings")]
        [Tooltip("Damage delivered to the player on contact.")]
        [SerializeField] private int damage = 1;

        [Tooltip("Impulse force applied away from spikes upon hit.")]
        [SerializeField] private float knockbackForce = 12f;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ProcessCollision(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            ProcessCollision(collision);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ProcessTrigger(other);
        }

        private void ProcessCollision(Collision2D collision)
        {
            if (collision.contactCount > 0)
            {
                HandleContact(collision.gameObject, -collision.GetContact(0).normal);
            }
            else
            {
                Vector2 normal = ((Vector2)collision.gameObject.transform.position - (Vector2)transform.position).normalized;
                HandleContact(collision.gameObject, normal);
            }
        }

        private void ProcessTrigger(Collider2D other)
        {
            Vector2 normal = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
            HandleContact(other.gameObject, normal);
        }

        private void HandleContact(GameObject target, Vector2 contactNormal)
        {
            if (!target.CompareTag("Player")) return;

            Health playerHealth = target.GetComponent<Health>();
            if (playerHealth != null && !playerHealth.IsInvincible && !playerHealth.IsDead)
            {
                // Force an upward component to pop the player off the spikes
                Vector2 knockDir = contactNormal;
                knockDir.y = Mathf.Max(knockDir.y, 0.65f);
                knockDir = knockDir.normalized;

                playerHealth.TakeDamage(damage, knockDir * knockbackForce);
            }
        }
    }
}

