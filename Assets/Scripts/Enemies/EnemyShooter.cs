using UnityEngine;
using Platformer.Common;

namespace Platformer.Enemies
{
    /// <summary>
    /// Ranged turret / spitter enemy.
    /// Stations itself on ledges or pillars and fires timed energy projectiles at the player.
    /// Telegraphs its shot with an intense charging glow, giving the player time to dodge,
    /// jump over, or slash the projectile out of the air!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public class EnemyShooter : MonoBehaviour
    {
        [Header("Shooting & Ranged Combat")]
        [Tooltip("Interval in seconds between projectile shots.")]
        [SerializeField] private float shootInterval = 2.6f;

        [Tooltip("Warning telegraph duration before firing.")]
        [SerializeField] private float telegraphDuration = 0.5f;

        [Tooltip("Max detection range to engage player.")]
        [SerializeField] private float detectionRange = 12.0f;

        [Tooltip("Speed of the fired projectile.")]
        [SerializeField] private float projectileSpeed = 6.5f;

        [Tooltip("Prefab of the fired projectile.")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("Spawn offset relative to shooter facing direction.")]
        [SerializeField] private Vector2 fireOffset = new Vector2(0.6f, 0.1f);

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject deathEffectPrefab;

        private Transform playerTransform;
        private float shootTimer = 0f;
        private bool isTelegraphing = false;
        private float telegraphTimer = 0f;
        private Vector3 originalScale;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
            originalScale = transform.localScale;
        }

        private void Start()
        {
            LocatePlayer();
            shootTimer = Random.Range(0.8f, shootInterval); // Stagger initial shots
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDied.AddListener(OnDeath);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDied.RemoveListener(OnDeath);
            }
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            isTelegraphing = false;
        }

        private void LocatePlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;

            if (playerTransform == null)
            {
                LocatePlayer();
                return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= detectionRange;

            // Face player
            int facing = playerTransform.position.x >= transform.position.x ? 1 : -1;
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * facing, originalScale.y, originalScale.z);

            if (!isTelegraphing)
            {
                if (inRange)
                {
                    shootTimer -= Time.deltaTime;
                    if (shootTimer <= 0f)
                    {
                        StartTelegraph();
                    }
                }
            }
            else
            {
                telegraphTimer -= Time.deltaTime;

                // Pulsing telegraph color
                if (spriteRenderer != null)
                {
                    float pulse = Mathf.PingPong(Time.time * 12f, 1f);
                    spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.1f), pulse);
                }

                if (telegraphTimer <= 0f)
                {
                    FireProjectile(facing);
                    isTelegraphing = false;
                    shootTimer = shootInterval;
                    if (spriteRenderer != null) spriteRenderer.color = Color.white;
                }
            }
        }

        private void StartTelegraph()
        {
            isTelegraphing = true;
            telegraphTimer = telegraphDuration;
        }

        private void FireProjectile(int facing)
        {
            if (projectilePrefab == null) return;

            Vector3 spawnPos = transform.position + new Vector3(fireOffset.x * facing, fireOffset.y, 0f);
            GameObject projGo = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            EnemyProjectile proj = projGo.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                Vector2 aimDir = ((Vector2)playerTransform.position - (Vector2)spawnPos).normalized;
                // Clamp aim angle so it doesn't shoot straight down into its own platform
                aimDir.y = Mathf.Clamp(aimDir.y, -0.45f, 0.45f);
                aimDir.Normalize();

                proj.Initialize(aimDir, projectileSpeed);
            }
        }

        private void OnDeath()
        {
            isTelegraphing = false;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            var cols = GetComponents<Collider2D>();
            foreach (var col in cols) col.enabled = false;

            rb.constraints = RigidbodyConstraints2D.None;
            rb.linearVelocity = new Vector2(0f, 4f);
            Destroy(gameObject, 0.6f);
        }
    }
}
