using System.Collections;
using UnityEngine;
using Platformer.Player;
using Platformer.Managers;

namespace Platformer.Common
{
    /// <summary>
    /// Spring / Jump Pad that launches the player upward with substantial velocity,
    /// refreshes air jumps and dash, and performs a juicy squash-and-stretch spring bounce.
    /// Uses modern Unity 6 linearVelocity.
    /// </summary>
    public class JumpPad : MonoBehaviour
    {
        [Header("Launch Settings")]
        [Tooltip("Upward velocity applied to player upon contact.")]
        [SerializeField] private float launchVelocity = 17.5f;

        [Header("Cosmetic Effects")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject bounceDustPrefab;

        private Vector3 initialScale;
        private Coroutine bounceCoroutine;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            initialScale = transform.localScale;
        }

        private void OnDisable()
        {
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
                bounceCoroutine = null;
            }
            transform.localScale = initialScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleActivation(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleActivation(collision.gameObject);
        }

        private void HandleActivation(GameObject target)
        {
            if (!target.CompareTag("Player")) return;

            Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
            PlayerController playerCtrl = target.GetComponent<PlayerController>();

            if (playerRb != null)
            {
                // Launch player upward using Unity 6 linearVelocity
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, launchVelocity);
            }

            if (playerCtrl != null)
            {
                playerCtrl.ResetJumps();
                playerCtrl.ResetDash();
            }

            // Audio & Camera Shake
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("spring");
            }
            CameraFollow.Shake(0.12f, 0.18f);

            // Particle dust
            if (bounceDustPrefab != null)
            {
                Instantiate(bounceDustPrefab, transform.position, Quaternion.identity);
            }

            // Visual spring squash & pop
            if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
            bounceCoroutine = StartCoroutine(SpringBounceRoutine());
        }

        private IEnumerator SpringBounceRoutine()
        {
            // Compress down
            transform.localScale = new Vector3(initialScale.x * 1.35f, initialScale.y * 0.45f, initialScale.z);
            yield return new WaitForSeconds(0.08f);

            // Pop upward
            transform.localScale = new Vector3(initialScale.x * 0.85f, initialScale.y * 1.35f, initialScale.z);
            yield return new WaitForSeconds(0.1f);

            // Return to normal
            float elapsed = 0f;
            Vector3 current = transform.localScale;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(current, initialScale, elapsed / 0.15f);
                yield return null;
            }
            transform.localScale = initialScale;
            bounceCoroutine = null;
        }
    }
}
