using System.Collections.Generic;
using UnityEngine;
using Platformer.Common;
using Platformer.Enemies;

namespace Platformer.Player
{
    /// <summary>
    /// Piercing energy blade projectile unleashed by the player's ranged attack (Key L / Right Click).
    /// Cuts through enemies, destroys incoming enemy projectiles, and shatters breakable walls.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class EnergySlashProjectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        [SerializeField] private float speed = 14.0f;
        [SerializeField] private int damage = 2;
        [SerializeField] private float knockbackForce = 10.0f;
        [SerializeField] private float lifetime = 1.4f;

        [Header("Effects")]
        [SerializeField] private GameObject impactEffectPrefab;

        private Rigidbody2D rb;
        private Vector2 travelDirection = Vector2.right;
        private float lifeTimer = 0f;
        private readonly HashSet<Collider2D> piercedColliders = new HashSet<Collider2D>();

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Initialize(Vector2 direction, float projSpeed = 14.0f)
        {
            travelDirection = direction.normalized;
            speed = projSpeed;

            // Orient sprite towards travel direction
            float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (rb != null)
            {
                rb.linearVelocity = travelDirection * speed;
            }
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (rb != null)
            {
                rb.linearVelocity = travelDirection * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            // Avoid double hits on same enemy segment
            if (piercedColliders.Contains(col)) return;

            // Ignore player
            if (col.CompareTag("Player") || (1 << col.gameObject.layer & LayerMask.GetMask("Player")) != 0)
            {
                return;
            }

            // Hit enemy
            if (col.CompareTag("Enemy") || (1 << col.gameObject.layer & LayerMask.GetMask("Enemy")) != 0)
            {
                piercedColliders.Add(col);

                Health enemyHealth = col.GetComponentInParent<Health>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    Vector2 knockback = travelDirection * knockbackForce;
                    enemyHealth.TakeDamage(damage, knockback);
                    UI.FloatingTextManager.ShowDamage(col.transform.position, damage, true);
                    Common.CombatJuice.HitStop(0.06f);
                    CameraFollow.Shake(0.14f, 0.25f);
                    SpawnImpactEffect();
                }

                // If hit enemy projectile, destroy it!
                EnemyProjectile enemyProj = col.GetComponentInParent<EnemyProjectile>();
                if (enemyProj != null)
                {
                    Destroy(enemyProj.gameObject);
                    SpawnImpactEffect();
                }
                return;
            }

            // Hit ground or solid obstacle
            int groundLayer = LayerMask.NameToLayer("Ground");
            if ((groundLayer != -1 && col.gameObject.layer == groundLayer) || col.CompareTag("Ground") || col.GetComponentInParent<BreakableSecretWall>() != null)
            {
                var secretWall = col.GetComponentInParent<BreakableSecretWall>();
                if (secretWall != null)
                {
                    var wallHealth = secretWall.GetComponent<Health>();
                    if (wallHealth != null && !wallHealth.IsDead)
                    {
                        wallHealth.TakeDamage(damage);
                    }
                }
                SpawnImpactEffect();
                Destroy(gameObject);
            }
        }

        private void SpawnImpactEffect()
        {
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
