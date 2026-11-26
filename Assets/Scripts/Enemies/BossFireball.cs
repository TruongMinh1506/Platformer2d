using UnityEngine;
using Platformer.Common;
using Platformer.Managers;

namespace Platformer.Enemies
{
    /// <summary>
    /// Fiery volcanic projectile falling from the fortress ceiling during Phase 2.
    /// Explodes on ground impact, shaking the screen and damaging nearby players.
    /// </summary>
    public class BossFireball : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [Tooltip("Downward falling speed.")]
        [SerializeField] private float fallSpeed = 16f;

        [Tooltip("Target ground Y coordinate where the fireball detonates.")]
        [SerializeField] private float targetGroundY = 5.0f;

        [Tooltip("Detonation blast radius in world units.")]
        [SerializeField] private float blastRadius = 1.35f;

        [Tooltip("Damage dealt to players caught in the blast.")]
        [SerializeField] private int damage = 1;

        [Tooltip("Optional particle prefab instantiated upon explosion.")]
        [SerializeField] private GameObject explosionEffectPrefab;

        public void Initialize(float groundY, GameObject explosionPrefab = null)
        {
            targetGroundY = groundY;
            if (explosionPrefab != null)
            {
                explosionEffectPrefab = explosionPrefab;
            }
        }

        private void Update()
        {
            float step = fallSpeed * Time.deltaTime;
            int groundMask = LayerMask.GetMask("Ground");
            if (groundMask != 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, step, groundMask);
                if (hit.collider != null)
                {
                    transform.position = hit.point;
                    Explode();
                    return;
                }
            }

            transform.position += Vector3.down * step;

            if (transform.position.y <= targetGroundY)
            {
                Explode();
            }
        }

        private void Explode()
        {
            CameraFollow.Shake(0.2f, 0.28f);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("hit");
            }

            // Damage player if inside blast radius
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blastRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player") || hit.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    Health pHealth = hit.GetComponentInParent<Health>();
                    if (pHealth != null && !pHealth.IsInvincible)
                    {
                        Vector2 knock = (hit.transform.position - transform.position).normalized * 8f;
                        knock.y = Mathf.Max(knock.y, 4.5f);
                        pHealth.TakeDamage(damage, knock);
                        UI.FloatingTextManager.ShowPlayerDamage(hit.transform.position, damage);
                    }
                }
            }

            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
