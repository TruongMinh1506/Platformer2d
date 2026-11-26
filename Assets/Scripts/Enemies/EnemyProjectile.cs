using UnityEngine;
using Platformer.Common;
using Platformer.Managers;

namespace Platformer.Enemies
{
    /// <summary>
    /// Ranged projectile fired by EnemyShooter (fiery venom bolt / plasma needle).
    /// Can be dodged, dashed through, or sliced out of the air with the player's sword!
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(Health))]
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        [SerializeField] private float speed = 6.0f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifetime = 4.0f;
        [SerializeField] private float knockbackForce = 7.5f;

        [Header("Effects")]
        [SerializeField] private GameObject impactEffectPrefab;

        private Rigidbody2D rb;
        private Health health;
        private Vector2 moveDirection = Vector2.left;
        private float lifeTimer = 0f;
        private bool isReflected = false;

        public bool IsReflected => isReflected;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();

            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDied.AddListener(OnSlicedByPlayer);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDied.RemoveListener(OnSlicedByPlayer);
            }
        }

        public void Initialize(Vector2 direction, float projSpeed = 6.0f)
        {
            moveDirection = direction.normalized;
            speed = projSpeed;

            // Orient sprite towards travel direction
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (rb != null)
            {
                rb.linearVelocity = moveDirection * speed;
            }
        }

        /// <summary>
        /// Deflects this projectile back towards enemies with accelerated speed and doubled damage!
        /// </summary>
        public void Deflect(Vector2 reflectDir)
        {
            isReflected = true;
            lifeTimer = 0f;
            lifetime += 2.0f; // Extend lifetime to reach target
            speed *= 1.5f;
            damage *= 2;
            moveDirection = reflectDir.normalized;

            // Orient sprite towards reflected direction
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (rb != null)
            {
                rb.linearVelocity = moveDirection * speed;
            }

            // Tint sprite to vibrant cyan/electric blue
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0.2f, 0.95f, 1f, 1f);
            }

            // Spawn metallic clink sparks
            SpawnImpactEffect();
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
                rb.linearVelocity = moveDirection * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (isReflected)
            {
                // Reflected projectile: Ignores player, damages enemies!
                if (col.CompareTag("Player") || (1 << col.gameObject.layer & LayerMask.GetMask("Player")) != 0)
                {
                    return;
                }

                Health targetHealth = col.GetComponentInParent<Health>();
                if (targetHealth != null && !targetHealth.IsDead && col.gameObject != gameObject)
                {
                    Vector2 knockback = (moveDirection + Vector2.up * 0.35f).normalized * knockbackForce;
                    targetHealth.TakeDamage(damage, knockback);
                    UI.FloatingTextManager.ShowDamage(col.transform.position, damage, true);
                    CameraFollow.Shake(0.18f, 0.28f);
                    SpawnImpactEffect();
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                // Normal projectile: Ignore other enemies
                if (col.CompareTag("Enemy") || (1 << col.gameObject.layer & LayerMask.GetMask("Enemy")) != 0)
                {
                    return;
                }

                // Check if hit player
                if (col.CompareTag("Player") || (1 << col.gameObject.layer & LayerMask.GetMask("Player")) != 0)
                {
                    Health playerHealth = col.GetComponentInParent<Health>();
                    if (playerHealth != null && !playerHealth.IsDead && !playerHealth.IsInvincible)
                    {
                        Vector2 knockback = (moveDirection + Vector2.up * 0.35f).normalized * knockbackForce;
                        playerHealth.TakeDamage(damage, knockback);
                        UI.FloatingTextManager.ShowPlayerDamage(col.transform.position, damage);
                        SpawnImpactEffect();
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            // Hit ground or solid obstacle
            int groundLayer = LayerMask.NameToLayer("Ground");
            if ((groundLayer != -1 && col.gameObject.layer == groundLayer) || col.CompareTag("Ground"))
            {
                SpawnImpactEffect();
                Destroy(gameObject);
            }
        }

        private void OnSlicedByPlayer()
        {
            // Player successfully sliced the projectile!
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("hit", 0.6f);
            }
            CameraFollow.Shake(0.12f, 0.18f);
            SpawnImpactEffect();
            Destroy(gameObject);
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
