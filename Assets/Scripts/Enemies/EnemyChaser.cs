using UnityEngine;
using Platformer.Common;
using Platformer.Player;

namespace Platformer.Enemies
{
    /// <summary>
    /// Chasing enemy that idles until the player enters its horizontal detection range.
    /// Pursues the player horizontally but stops before walking off ledges or into walls.
    /// Uses modern Unity 6 APIs (linearVelocity, FindFirstObjectByType).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public class EnemyChaser : MonoBehaviour, IStunnable
    {
        [Header("Chasing Settings")]
        [Tooltip("Horizontal movement speed when pursuing the player.")]
        [SerializeField] private float chaseSpeed = 3.6f;

        [Tooltip("Slow horizontal walking speed when idle/patrolling.")]
        [SerializeField] private float patrolSpeed = 1.5f;

        [Tooltip("Distance at which the chaser detects and pursues the player.")]
        [SerializeField] private float detectionRange = 7.5f;

        [Header("Ledge & Wall Detection")]
        [Tooltip("Forward distance to cast the edge detection downward ray.")]
        [SerializeField] private float edgeCheckForwardOffset = 0.55f;

        [Tooltip("Downward length of edge detection ray.")]
        [SerializeField] private float edgeCheckDistance = 0.8f;

        [Tooltip("Forward length of wall detection ray.")]
        [SerializeField] private float wallCheckDistance = 0.6f;

        [Tooltip("Vertical origin offset for raycasts.")]
        [SerializeField] private float rayOriginHeight = -0.2f;

        [Tooltip("LayerMask specifying ground obstacles and ledges.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Health health;

        [Header("Cosmetic Effects")]
        [Tooltip("Particle effect instantiated upon defeat.")]
        [SerializeField] private GameObject deathEffectPrefab;

        private Transform playerTransform;
        private int patrolDirection = -1;
        private float turnCooldownTimer = 0f;
        private float stuckTimer = 0f;
        private float stunTimer = 0f;
        private const float MinTurnInterval = 0.25f;

        public bool IsStunned => stunTimer > 0f;

        public void Stun(float duration = 1.5f)
        {
            stunTimer = duration;
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            StartCoroutine(StunBlinkRoutine(duration));
        }

        private System.Collections.IEnumerator StunBlinkRoutine(float duration)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            Color original = sr != null ? sr.color : Color.white;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (sr != null)
                {
                    sr.color = ((int)(elapsed * 12f) % 2 == 0) ? new Color(1f, 0.95f, 0.2f) : original;
                }
                yield return null;
            }
            if (sr != null) sr.color = original;
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();

            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (groundLayer.value == 0)
            {
                groundLayer = LayerMask.GetMask("Ground", "Default");
            }

            // Ensure enemies pass through each other instead of pushing each other
            Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
        }

        private void Start()
        {
            // Unity 6 modern API: FindFirstObjectByType
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
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
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsDead) return;

            // Destroy if fallen into the abyss
            if (transform.position.y < -5f)
            {
                Destroy(gameObject);
                return;
            }

            if (stunTimer > 0f)
            {
                stunTimer -= Time.fixedDeltaTime;
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            if (turnCooldownTimer > 0f)
            {
                turnCooldownTimer -= Time.fixedDeltaTime;
            }

            if (playerTransform == null)
            {
                var player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null) playerTransform = player.transform;
            }

            Vector2 origin = (Vector2)transform.position + new Vector2(0f, rayOriginHeight);
            RaycastHit2D currentGroundHit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDistance, groundLayer);
            bool isGrounded = currentGroundHit.collider != null;

            Vector2 toPlayer = playerTransform != null ? (Vector2)playerTransform.position - (Vector2)transform.position : Vector2.zero;
            float horizontalDistance = playerTransform != null ? Mathf.Abs(toPlayer.x) : 999f;
            float verticalDistance = playerTransform != null ? Mathf.Abs(toPlayer.y) : 999f;

            // Check if player is within horizontal range and reasonable vertical height
            if (playerTransform != null && horizontalDistance <= detectionRange && verticalDistance <= 4f && horizontalDistance > 0.25f)
            {
                int chaseDirection = toPlayer.x > 0 ? 1 : -1;

                // 1. Check for wall ahead in pursuit direction
                RaycastHit2D wallHit = Physics2D.Raycast(origin, Vector2.right * chaseDirection, wallCheckDistance, groundLayer);

                // 2. Check for ground edge ahead in pursuit direction
                Vector2 edgeOrigin = origin + new Vector2(chaseDirection * edgeCheckForwardOffset, 0f);
                RaycastHit2D groundAheadHit = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer);

                // If wall blocking or ledge ahead, stop immediately to avoid falling
                if (wallHit.collider != null || (isGrounded && groundAheadHit.collider == null))
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                else
                {
                    // Pursue player with modern Unity 6 linearVelocity
                    rb.linearVelocity = new Vector2(chaseDirection * chaseSpeed, rb.linearVelocity.y);
                    FaceDirection(chaseDirection);
                }
            }
            else
            {
                // Active patrol state when player is not nearby
                if (isGrounded && turnCooldownTimer <= 0f)
                {
                    RaycastHit2D wallHit = Physics2D.Raycast(origin, Vector2.right * patrolDirection, wallCheckDistance, groundLayer);
                    Vector2 edgeOrigin = origin + new Vector2(patrolDirection * edgeCheckForwardOffset, 0f);
                    RaycastHit2D groundAheadHit = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer);

                    if (Mathf.Abs(rb.linearVelocity.x) < 0.15f)
                    {
                        stuckTimer += Time.fixedDeltaTime;
                    }
                    else
                    {
                        stuckTimer = 0f;
                    }

                    if (wallHit.collider != null || groundAheadHit.collider == null || stuckTimer > 0.25f)
                    {
                        stuckTimer = 0f;
                        patrolDirection *= -1;
                        turnCooldownTimer = MinTurnInterval;
                    }
                }

                rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, rb.linearVelocity.y);
                FaceDirection(patrolDirection);
            }
        }

        private void FaceDirection(int dir)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * dir;
            transform.localScale = scale;
        }

        private void OnDeath()
        {
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            var cols = GetComponents<Collider2D>();
            foreach (var col in cols) col.enabled = false;

            rb.linearVelocity = new Vector2(0f, 6.5f);
            Destroy(gameObject, 0.6f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}

