using System.Collections;
using UnityEngine;
using Platformer.Common;
using Platformer.Managers;
using Platformer.UI;

namespace Platformer.Player
{
    /// <summary>
    /// Implements Shadow Dash with afterimage ghost trails, Invulnerability Frames (i-Frames),
    /// passing through enemy entities, and synergy with the ThunderStep charm.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health))]
    public class ShadowDashEffect : MonoBehaviour
    {
        [Header("Afterimage Settings")]
        [Tooltip("Seconds between each spawned afterimage.")]
        [SerializeField] private float spawnInterval = 0.035f;

        [Tooltip("Seconds before an afterimage completely fades out.")]
        [SerializeField] private float ghostLifetime = 0.26f;

        [Tooltip("Primary spectral tint for afterimage.")]
        [SerializeField] private Color primaryGhostColor = new Color(0.35f, 0.85f, 1f, 0.75f);

        [Tooltip("Secondary spectral tint for afterimage alternation.")]
        [SerializeField] private Color secondaryGhostColor = new Color(0.85f, 0.45f, 1f, 0.75f);

        private PlayerController controller;
        private Health health;
        private SpriteRenderer playerRenderer;
        private Coroutine shadowDashRoutine;
        private int playerLayer;
        private int enemyLayer;
        private bool isAlternatingColor = false;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            playerRenderer = GetComponentInChildren<SpriteRenderer>();

            playerLayer = LayerMask.NameToLayer("Player");
            enemyLayer = LayerMask.NameToLayer("Enemy");
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.OnDashed += HandleDashStarted;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.OnDashed -= HandleDashStarted;
            }

            EndShadowDash();
        }

        private void HandleDashStarted()
        {
            if (shadowDashRoutine != null)
            {
                StopCoroutine(shadowDashRoutine);
            }
            shadowDashRoutine = StartCoroutine(ShadowDashRoutine());
        }

        private IEnumerator ShadowDashRoutine()
        {
            // 1. Enable i-Frames & pass through enemies
            if (health != null)
            {
                health.SetInvincible(true);
            }

            if (playerLayer != -1 && enemyLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
            }

            // 2. Play distinct shadow dash whoosh
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("shadow_dash", 0.75f);
            }

            // 3. Spawn ghost afterimages while dashing
            float timer = 0f;
            while (controller != null && controller.IsDashing)
            {
                SpawnAfterimage();

                // ThunderStep Charm Synergy: zap nearby enemies along dash trajectory
                if (CharmManager.Instance != null && CharmManager.Instance.IsEquipped(CharmType.ThunderStep))
                {
                    PerformThunderStepZap();
                }

                yield return new WaitForSeconds(spawnInterval);
                timer += spawnInterval;
            }

            EndShadowDash();
        }

        private void EndShadowDash()
        {
            if (shadowDashRoutine != null)
            {
                StopCoroutine(shadowDashRoutine);
                shadowDashRoutine = null;
            }

            // Restore normal vulnerability
            if (health != null && !health.IsDead)
            {
                health.SetInvincible(false);
            }

            // Restore enemy collisions
            if (playerLayer != -1 && enemyLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
            }
        }

        private void SpawnAfterimage()
        {
            if (playerRenderer == null || playerRenderer.sprite == null) return;

            GameObject ghostGo = new GameObject("GhostTrail");
            ghostGo.transform.position = transform.position;
            ghostGo.transform.rotation = transform.rotation;
            ghostGo.transform.localScale = transform.localScale;

            SpriteRenderer sr = ghostGo.AddComponent<SpriteRenderer>();
            sr.sprite = playerRenderer.sprite;
            sr.sortingLayerID = playerRenderer.sortingLayerID;
            sr.sortingOrder = playerRenderer.sortingOrder - 1; // Render just behind player
            sr.flipX = playerRenderer.flipX;
            sr.flipY = playerRenderer.flipY;

            Color baseColor = isAlternatingColor ? secondaryGhostColor : primaryGhostColor;
            isAlternatingColor = !isAlternatingColor;
            sr.color = baseColor;

            StartCoroutine(FadeAndDestroyGhost(ghostGo, sr, baseColor, ghostLifetime));
        }

        private IEnumerator FadeAndDestroyGhost(GameObject ghostObj, SpriteRenderer sr, Color startColor, float lifetime)
        {
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / lifetime;
                if (sr != null)
                {
                    sr.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, progress));
                }
                yield return null;
            }

            if (ghostObj != null)
            {
                Destroy(ghostObj);
            }
        }

        private void PerformThunderStepZap()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.85f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    Health enemyHealth = hit.GetComponent<Health>();
                    if (enemyHealth != null && !enemyHealth.IsDead)
                    {
                        enemyHealth.TakeDamage(2, Vector2.zero);
                        FloatingTextManager.ShowDamage(hit.transform.position + Vector3.up * 0.5f, 2, true);
                    }
                }
            }
        }
    }
}
