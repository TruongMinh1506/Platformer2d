using System.Collections;
using UnityEngine;
using Platformer.Player;

namespace Platformer.Common
{
    /// <summary>
    /// Breakable secret wall hiding hidden treasure alcoves and ancient relics.
    /// Shatters when struck by the player's sword, air dash pierce, or Energy Blade Slash.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(Health))]
    public class BreakableSecretWall : MonoBehaviour
    {
        [Header("Break Effects")]
        [SerializeField] private GameObject shatterEffectPrefab;
        [SerializeField] private Sprite crackedSprite;

        private Health health;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private bool isBroken = false;

        private void Awake()
        {
            health = GetComponent<Health>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged.AddListener(OnDamaged);
                health.OnDied.AddListener(OnShattered);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged.RemoveListener(OnDamaged);
                health.OnDied.RemoveListener(OnShattered);
            }
        }

        private void OnDamaged(int currentHealth)
        {
            if (crackedSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = crackedSprite;
            }
            CameraFollow.Shake(0.12f, 0.15f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isBroken) return;

            // Check if player dashes into the wall
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null && player.IsDashing)
            {
                if (health != null)
                {
                    health.TakeDamage(2); // Instantly shatter from heavy air dash!
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (isBroken) return;

            // Check if hit by Energy Slash projectile
            var slash = col.GetComponent<EnergySlashProjectile>();
            if (slash != null && health != null)
            {
                health.TakeDamage(2);
            }
        }

        private void OnShattered()
        {
            if (isBroken) return;
            isBroken = true;

            if (boxCollider != null) boxCollider.enabled = false;

            if (shatterEffectPrefab != null)
            {
                Instantiate(shatterEffectPrefab, transform.position, Quaternion.identity);
            }

            if (Managers.AudioManager.Instance != null)
            {
                Managers.AudioManager.Instance.PlaySound("crumble");
            }

            CameraFollow.Shake(0.2f, 0.28f);

            StartCoroutine(FadeAndDestroy());
        }

        private IEnumerator FadeAndDestroy()
        {
            float elapsed = 0f;
            float duration = 0.25f;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
