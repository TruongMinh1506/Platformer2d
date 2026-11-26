using System.Collections;
using UnityEngine;
using Platformer.Enemies;
using Platformer.Managers;
using Platformer.UI;

namespace Platformer.Common
{
    /// <summary>
    /// Interactive environmental stalactite hanging from ceilings.
    /// Can be dislodged by striking it, shooting it, or walking underneath.
    /// Crushes enemies below for massive physical impact damage!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class FallingStalactite : MonoBehaviour
    {
        [Header("Detection & Triggering")]
        [Tooltip("Downward detection ray length to spot targets walking beneath.")]
        [SerializeField] private float triggerRayDistance = 8.0f;

        [Tooltip("LayerMask of entities that trigger the fall.")]
        [SerializeField] private LayerMask triggerLayers;

        [Header("Physics Properties")]
        [Tooltip("Damage dealt to enemies crushed beneath the falling stalactite.")]
        [SerializeField] private int crushDamage = 10;

        [Tooltip("Downward falling speed.")]
        [SerializeField] private float fallSpeed = 16f;

        [Header("Cosmetic Effects")]
        [SerializeField] private GameObject shatterFxPrefab;

        private Rigidbody2D rb;
        private Collider2D col;
        private bool isFalling = false;
        private bool isTriggered = false;
        private Vector3 initialPosition;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            initialPosition = transform.position;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            col.isTrigger = false;
        }

        private void Update()
        {
            if (isTriggered || isFalling) return;

            // Check if player or enemy walks directly underneath
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, triggerRayDistance, triggerLayers);
            if (hit.collider != null)
            {
                TriggerFall();
            }
        }

        public void TriggerFall()
        {
            if (isTriggered) return;
            isTriggered = true;
            StartCoroutine(WiggleAndFallRoutine());
        }

        private IEnumerator WiggleAndFallRoutine()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("stalactite_fall");
            }

            // Wiggle / shake warning for 0.25s
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * 50f) * 0.08f;
                transform.position = initialPosition + new Vector3(xOffset, 0f, 0f);
                yield return null;
            }

            // Begin rapid downward plummet
            isFalling = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 4.5f;
            rb.linearVelocity = new Vector2(0f, -fallSpeed);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isFalling) return;

            // Check if crushed an enemy
            Health enemyHealth = collision.gameObject.GetComponentInParent<Health>();
            bool isPlayer = collision.gameObject.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player");

            if (isPlayer)
            {
                Health playerHealth = collision.gameObject.GetComponentInParent<Health>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    playerHealth.TakeDamage(1, Vector2.down * 4f);
                    FloatingTextManager.ShowPlayerDamage(collision.transform.position, 1);
                    CombatJuice.HitStop(0.08f);
                    CameraFollow.Shake(0.3f, 0.4f);
                }
                ShatterAndDestroy();
                return;
            }

            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyHealth.TakeDamage(crushDamage, Vector2.down * 10f);
                FloatingTextManager.ShowDamage(collision.transform.position, crushDamage, true);
                CombatJuice.HitStop(0.1f);
                CameraFollow.Shake(0.35f, 0.45f);

                AchievementManager.Instance?.Unlock("ACH_STALACTITE_CRUSH");
                ShatterAndDestroy();
                return;
            }

            // Hit ground or obstacle
            ShatterAndDestroy();
        }

        private void ShatterAndDestroy()
        {
            if (shatterFxPrefab != null)
            {
                Instantiate(shatterFxPrefab, transform.position, Quaternion.identity);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("crumble", 0.8f);
            }

            CameraFollow.Shake(0.2f, 0.25f);
            Destroy(gameObject);
        }
    }
}
