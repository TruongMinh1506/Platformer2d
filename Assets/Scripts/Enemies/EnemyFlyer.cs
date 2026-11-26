using UnityEngine;
using Platformer.Common;

namespace Platformer.Enemies
{
    /// <summary>
    /// Aerial flying bat / drone enemy.
    /// Hovers smoothly on an aerial patrol path in a sine wave.
    /// When the player passes beneath, it screeches, telegraphs,
    /// dive-bombs the player, and returns to its hover perch.
    /// Can be stomped from above or slashed with the player's sword.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public class EnemyFlyer : MonoBehaviour, IStunnable
    {
        public enum FlyerState { Patrol, Telegraph, Dive, Return }

        [Header("Patrol Hover")]
        [Tooltip("Horizontal cruising speed.")]
        [SerializeField] private float hoverSpeed = 2.0f;

        [Tooltip("Horizontal range of patrol relative to anchor.")]
        [SerializeField] private float hoverWidth = 3.5f;

        [Tooltip("Vertical wave oscillation frequency.")]
        [SerializeField] private float waveFrequency = 3.2f;

        [Tooltip("Vertical wave height amplitude.")]
        [SerializeField] private float waveAmplitude = 0.55f;

        [Header("Dive Bomb Attack")]
        [Tooltip("Radius within which flyer senses player beneath it.")]
        [SerializeField] private float detectionRadius = 7.0f;

        [Tooltip("Warning telegraph duration before swooping down.")]
        [SerializeField] private float telegraphDuration = 0.4f;

        [Tooltip("Fast swooping dive speed towards player.")]
        [SerializeField] private float diveSpeed = 8.0f;

        [Tooltip("Ascent speed returning to original patrol height.")]
        [SerializeField] private float returnSpeed = 4.2f;

        [Tooltip("Cooldown period between consecutive dive attacks.")]
        [SerializeField] private float diveCooldown = 2.5f;

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject deathEffectPrefab;

        private FlyerState currentState = FlyerState.Patrol;
        private Vector3 anchorPos;
        private Transform playerTransform;
        private Vector3 targetDivePos;
        private float stateTimer = 0f;
        private float cooldownTimer = 0f;
        private float hoverTime = 0f;
        private float stunTimer = 0f;
        private Vector3 originalScale;

        public bool IsStunned => stunTimer > 0f;

        public void Stun(float duration = 1.5f)
        {
            stunTimer = duration;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) spriteRenderer.transform.localPosition = Vector3.zero;
            StartCoroutine(StunBlinkRoutine(duration));
        }

        private System.Collections.IEnumerator StunBlinkRoutine(float duration)
        {
            Color original = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = ((int)(elapsed * 12f) % 2 == 0) ? new Color(1f, 0.95f, 0.2f) : original;
                }
                yield return null;
            }
            if (spriteRenderer != null) spriteRenderer.color = original;
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            anchorPos = transform.position;
            originalScale = transform.localScale;
        }

        private void Start()
        {
            LocatePlayer();
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

        private void LocatePlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsDead) return;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.fixedDeltaTime;
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.fixedDeltaTime;
            }

            if (playerTransform == null)
            {
                LocatePlayer();
            }

            switch (currentState)
            {
                case FlyerState.Patrol:
                    UpdatePatrol();
                    break;
                case FlyerState.Telegraph:
                    UpdateTelegraph();
                    break;
                case FlyerState.Dive:
                    UpdateDive();
                    break;
                case FlyerState.Return:
                    UpdateReturn();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            hoverTime += Time.fixedDeltaTime * hoverSpeed;

            float targetX = anchorPos.x + Mathf.Sin(hoverTime) * hoverWidth;
            float targetY = anchorPos.y + Mathf.Sin(hoverTime * waveFrequency) * waveAmplitude;

            Vector2 targetPos = new Vector2(targetX, targetY);
            Vector2 toTarget = targetPos - (Vector2)transform.position;
            rb.linearVelocity = toTarget * 4f;

            // Face movement direction
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                int dir = rb.linearVelocity.x > 0 ? 1 : -1;
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * dir, originalScale.y, originalScale.z);
            }

            // Check for player underneath
            if (cooldownTimer <= 0f && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                bool isUnderneath = playerTransform.position.y < transform.position.y + 0.5f;

                if (dist <= detectionRadius && isUnderneath)
                {
                    currentState = FlyerState.Telegraph;
                    stateTimer = telegraphDuration;
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        private void UpdateTelegraph()
        {
            stateTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero;

            // Visual shudder / flash during warning
            if (spriteRenderer != null)
            {
                float shake = Mathf.Sin(Time.time * 45f) * 0.08f;
                spriteRenderer.transform.localPosition = new Vector3(shake, 0f, 0f);
                spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), Mathf.PingPong(Time.time * 8f, 1f));
            }

            if (stateTimer <= 0f)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                    spriteRenderer.transform.localPosition = Vector3.zero;
                }
                currentState = FlyerState.Dive;
                stateTimer = 1.2f; // Max dive duration fallback

                if (playerTransform != null)
                {
                    targetDivePos = playerTransform.position;
                }
                else
                {
                    targetDivePos = transform.position + Vector3.down * 4f;
                }

                // Face dive target
                int dir = targetDivePos.x >= transform.position.x ? 1 : -1;
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * dir, originalScale.y, originalScale.z);
            }
        }

        private void UpdateDive()
        {
            stateTimer -= Time.fixedDeltaTime;

            Vector2 toTarget = (Vector2)targetDivePos - (Vector2)transform.position;
            rb.linearVelocity = toTarget.normalized * diveSpeed;

            // Reached dive target or timed out
            if (toTarget.sqrMagnitude < 0.25f || stateTimer <= 0f)
            {
                currentState = FlyerState.Return;
                stateTimer = 2.0f; // Return timeout
            }
        }

        private void UpdateReturn()
        {
            stateTimer -= Time.fixedDeltaTime;

            Vector2 toAnchor = (Vector2)anchorPos - (Vector2)transform.position;
            rb.linearVelocity = toAnchor.normalized * returnSpeed;

            // Face return direction
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                int dir = rb.linearVelocity.x > 0 ? 1 : -1;
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * dir, originalScale.y, originalScale.z);
            }

            // Returned near anchor
            if (toAnchor.sqrMagnitude < 0.35f || stateTimer <= 0f)
            {
                currentState = FlyerState.Patrol;
                cooldownTimer = diveCooldown;
            }
        }

        private void OnDeath()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localPosition = Vector3.zero;
            }

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            var cols = GetComponents<Collider2D>();
            foreach (var col in cols) col.enabled = false;

            rb.gravityScale = 3f;
            rb.linearVelocity = new Vector2(0f, 4.5f);
            Destroy(gameObject, 0.6f);
        }

        public void SetAnchorPosition(Vector3 newAnchor)
        {
            anchorPos = newAnchor;
            transform.position = newAnchor;
        }
    }
}
