using UnityEngine;
using Platformer.Common;

namespace Platformer.Enemies
{
    /// <summary>
    /// Patrolling enemy that walks horizontally between two points or platform edges.
    /// Turns around upon hitting a wall or detecting an upcoming ledge drop.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public class EnemyPatroller : MonoBehaviour, IStunnable
    {
        [Header("Movement")]
        [Tooltip("Horizontal walking speed.")]
        [SerializeField] private float moveSpeed = 2.4f;

        [Tooltip("Initial movement direction (-1 for left, 1 for right).")]
        [SerializeField] private int initialDirection = -1;

        [Header("Edge & Wall Detection")]
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

        private int direction;
        private float turnCooldownTimer = 0f;
        private float stuckTimer = 0f;
        private float stunTimer = 0f;
        private const float MinTurnInterval = 0.2f;

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

            direction = initialDirection >= 0 ? 1 : -1;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (groundLayer.value == 0)
            {
                groundLayer = LayerMask.GetMask("Ground", "Default");
            }

            // Ensure enemies pass through each other instead of pushing each other
            Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
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

            Vector2 origin = (Vector2)transform.position + new Vector2(0f, rayOriginHeight);

            // Only perform edge drop reversal if currently grounded
            RaycastHit2D currentGroundHit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDistance, groundLayer);
            bool isGrounded = currentGroundHit.collider != null;

            if (isGrounded && turnCooldownTimer <= 0f)
            {
                // 1. Check for wall ahead
                RaycastHit2D wallHit = Physics2D.Raycast(origin, Vector2.right * direction, wallCheckDistance, groundLayer);

                // 2. Check for ground edge ahead
                Vector2 edgeOrigin = origin + new Vector2(direction * edgeCheckForwardOffset, 0f);
                RaycastHit2D groundAheadHit = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer);

                // 3. Check if physically blocked or stuck against an obstacle
                if (Mathf.Abs(rb.linearVelocity.x) < 0.15f)
                {
                    stuckTimer += Time.fixedDeltaTime;
                }
                else
                {
                    stuckTimer = 0f;
                }

                // If wall hit, ledge ahead, or stuck against something, reverse patrol direction
                if (wallHit.collider != null || groundAheadHit.collider == null || stuckTimer > 0.25f)
                {
                    stuckTimer = 0f;
                    FlipDirection();
                }
            }

            // Move with modern Unity 6 linearVelocity
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }

        private void FlipDirection()
        {
            direction *= -1;
            turnCooldownTimer = MinTurnInterval;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void OnDeath()
        {
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            // Disable colliders so player can pass through
            var cols = GetComponents<Collider2D>();
            foreach (var col in cols) col.enabled = false;

            // Little defeat pop
            rb.linearVelocity = new Vector2(0f, 6f);

            // Destroy after short visual pop
            Destroy(gameObject, 0.6f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            int dir = direction != 0 ? direction : (initialDirection >= 0 ? 1 : -1);
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, rayOriginHeight);
            Gizmos.DrawLine(origin, origin + Vector2.right * (dir * wallCheckDistance));

            Vector2 edgeOrigin = origin + new Vector2(dir * edgeCheckForwardOffset, 0f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * edgeCheckDistance);
        }
    }
}

