using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Platformer.Managers;

namespace Platformer.Common
{
    /// <summary>
    /// Reusable health component for both Player and Enemies.
    /// Manages hit points, damage, healing, invincibility frames with sprite blinking,
    /// and OnDamaged / OnDied UnityEvents.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health points.")]
        [SerializeField] private int maxHealth = 3;

        [Tooltip("Duration of invulnerability frames in seconds after taking damage.")]
        [SerializeField] private float invincibilityDuration = 1.0f;

        [Tooltip("Blink flash interval during invulnerability.")]
        [SerializeField] private float blinkInterval = 0.08f;

        [Header("Knockback Settings")]
        [Tooltip("Multiplier applied to incoming knockback impulse. 1 = standard, 0 = immune (Bosses).")]
        [SerializeField] private float knockbackMultiplier = 1.0f;

        [Header("Events")]
        [Tooltip("Triggered when damage is sustained. Passes the remaining health points.")]
        [SerializeField] private UnityEvent<int> onDamaged;

        [Tooltip("Triggered when health reaches zero.")]
        [SerializeField] private UnityEvent onDied;

        [Header("Optional References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;

        private int currentHealth;
        private bool isInvincible;
        private bool isDead;
        private Coroutine invincibilityCoroutine;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsInvincible => isInvincible;
        public bool IsDead => isDead;
        public float KnockbackMultiplier
        {
            get => knockbackMultiplier;
            set => knockbackMultiplier = value;
        }
        public UnityEvent<int> OnDamaged => onDamaged;
        public UnityEvent OnDied => onDied;

        /// <summary>
        /// Explicitly activates or clears invincibility state (e.g. during shadow dash i-frames).
        /// </summary>
        public void SetInvincible(bool value)
        {
            isInvincible = value;
        }

        private void Awake()
        {
            currentHealth = maxHealth;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
        }

        private void OnDisable()
        {
            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
                invincibilityCoroutine = null;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            isInvincible = false;
        }

        /// <summary>
        /// Inflicts damage, applies optional knockback impulse, and triggers invincibility blinking.
        /// </summary>
        public void TakeDamage(int damage, Vector2 knockback = default)
        {
            if (isDead || isInvincible || damage <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            // Apply modern Unity 6 linearVelocity knockback impulse scaled by knockbackMultiplier
            if (knockback != Vector2.zero && rb != null && knockbackMultiplier > 0f)
            {
                rb.linearVelocity = knockback * knockbackMultiplier;
            }

            onDamaged?.Invoke(currentHealth);

            // Screen shake and hit-stop impact feedback
            if (CompareTag("Player") || gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                CameraFollow.Shake(0.25f, 0.35f);
                GameManager.HitStop(0.06f);
                GameManager.Instance?.RecordPlayerDamaged();
            }
            else
            {
                CameraFollow.Shake(0.12f, 0.18f);
                GameManager.HitStop(0.04f);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else if (invincibilityDuration > 0f)
            {
                if (invincibilityCoroutine != null)
                {
                    StopCoroutine(invincibilityCoroutine);
                }
                invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
            }
        }

        /// <summary>
        /// Restores health up to maxHealth and updates observers.
        /// </summary>
        public void Heal(int amount)
        {
            if (isDead || amount <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            onDamaged?.Invoke(currentHealth);
        }

        public void SetMaxHealth(int newMax, bool healToFull = false)
        {
            maxHealth = Mathf.Max(1, newMax);
            if (healToFull) currentHealth = maxHealth;
            else currentHealth = Mathf.Min(currentHealth, maxHealth);
            onDamaged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Resets health to maxHealth, clears invincibility and death state (for respawning).
        /// </summary>
        public void ResetHealth()
        {
            isDead = false;
            isInvincible = false;
            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
                invincibilityCoroutine = null;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            currentHealth = maxHealth;
            onDamaged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Immediately eliminates the entity, bypassing invincibility.
        /// Used for pits, hazards, and instant-kill zones.
        /// </summary>
        public void Kill()
        {
            if (isDead) return;
            isInvincible = false;
            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
                invincibilityCoroutine = null;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            currentHealth = 0;
            onDamaged?.Invoke(0);
            Die();
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            onDied?.Invoke();
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;
            float elapsed = 0f;

            while (elapsed < invincibilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            isInvincible = false;
            invincibilityCoroutine = null;
        }
    }
}

