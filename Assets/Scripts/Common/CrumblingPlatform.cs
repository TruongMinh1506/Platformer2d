using System.Collections;
using UnityEngine;
using Platformer.Managers;

namespace Platformer.Common
{
    /// <summary>
    /// Fragile platform that shakes upon player contact, falls away,
    /// and respawns after a delay.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class CrumblingPlatform : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Seconds the platform shakes before falling.")]
        [SerializeField] private float shakeDuration = 0.75f;

        [Tooltip("Shake trembling magnitude.")]
        [SerializeField] private float shakeMagnitude = 0.06f;

        [Tooltip("Seconds before the platform reappears after falling.")]
        [SerializeField] private float respawnDelay = 3.0f;

        [Header("Cosmetic Effects")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D col;

        private Vector3 initialPosition;
        private bool isCrumbling = false;
        private Color initialColor = Color.white;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (col == null) col = GetComponent<Collider2D>();

            initialPosition = transform.position;
            if (spriteRenderer != null) initialColor = spriteRenderer.color;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            transform.position = initialPosition;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = initialColor;
                spriteRenderer.enabled = true;
            }
            if (col != null) col.enabled = true;
            isCrumbling = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isCrumbling) return;
            if (!collision.gameObject.CompareTag("Player")) return;

            // Trigger when player lands on top of the platform
            bool landedOnTop = collision.transform.position.y > (transform.position.y + 0.15f);
            if (!landedOnTop)
            {
                for (int i = 0; i < collision.contactCount; i++)
                {
                    if (collision.GetContact(i).normal.y < -0.4f)
                    {
                        landedOnTop = true;
                        break;
                    }
                }
            }

            if (landedOnTop)
            {
                StartCoroutine(CrumbleRoutine());
            }
        }

        private IEnumerator CrumbleRoutine()
        {
            isCrumbling = true;

            // Audio creak/shake
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("hit", 0.5f);
            }

            // 1. Shaking phase
            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
                float yOffset = Random.Range(-shakeMagnitude * 0.5f, shakeMagnitude * 0.5f);
                transform.position = initialPosition + new Vector3(xOffset, yOffset, 0f);
                yield return null;
            }

            transform.position = initialPosition;

            // 2. Fall / Collapse phase
            if (col != null) col.enabled = false;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("crumble");
            }

            // Fall and fade out
            float fadeElapsed = 0f;
            float fadeDuration = 0.4f;
            Vector3 fallVelocity = new Vector3(0f, -8f, 0f);

            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                transform.position += fallVelocity * Time.deltaTime;
                if (spriteRenderer != null)
                {
                    float a = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                    spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, a);
                }
                yield return null;
            }

            if (spriteRenderer != null) spriteRenderer.enabled = false;

            // 3. Respawn wait phase
            yield return new WaitForSeconds(respawnDelay);

            // Reset back to initial state
            transform.position = initialPosition;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = initialColor;
                spriteRenderer.enabled = true;
            }
            if (col != null) col.enabled = true;
            isCrumbling = false;
        }
    }
}
