using UnityEngine;
using Platformer.Common;

namespace Platformer.Enemies
{
    /// <summary>
    /// Shockwave projectile emitted horizontally along the ground when the Boss slams down.
    /// Can be jumped over or air-dashed through. Dissipates upon hitting arena walls or gates.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossShockwave : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Horizontal movement speed.")]
        [SerializeField] private float speed = 7.0f;

        [Tooltip("Maximum lifetime before self-destruction.")]
        [SerializeField] private float lifetime = 2.4f;

        [Tooltip("Damage dealt to the player upon contact.")]
        [SerializeField] private int damage = 1;

        private float direction = 1f;

        public void Initialize(float dir)
        {
            direction = Mathf.Sign(dir);
            transform.localScale = new Vector3(direction > 0 ? 1f : -1f, 1f, 1f);
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;

            // Check if hitting a solid wall/obstacle ahead
            int groundMask = LayerMask.GetMask("Ground");
            if (groundMask != 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * direction, step + 0.2f, groundMask);
                if (hit.collider != null && !hit.collider.isTrigger)
                {
                    if (Mathf.Abs(hit.normal.x) > 0.5f)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            transform.position += Vector3.right * (direction * step);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Health playerHealth = other.GetComponentInParent<Health>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    Vector2 knock = new Vector2(direction * 7.5f, 6.5f);
                    playerHealth.TakeDamage(damage, knock);
                    UI.FloatingTextManager.ShowPlayerDamage(other.transform.position, damage);
                }
            }
            else if (other.GetComponentInParent<BossGate>() != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
