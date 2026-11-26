using System.Collections;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// Spawns fading silhouette ghost afterimages behind the player during a Dash.
    /// Creates a crisp, stylish visual trail matching the current sprite animation frame.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(SpriteRenderer))]
    public class PlayerGhostTrail : MonoBehaviour
    {
        [Header("Ghost Trail Settings")]
        [Tooltip("Interval in seconds between each ghost afterimage spawn.")]
        [SerializeField] private float spawnInterval = 0.035f;

        [Tooltip("Duration in seconds before a ghost sprite fades out and disappears.")]
        [SerializeField] private float ghostLifetime = 0.22f;

        [Tooltip("Color tint applied to the ghost afterimage.")]
        [SerializeField] private Color trailColor = new Color(0.25f, 0.85f, 1f, 0.65f);

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Coroutine trailCoroutine;

        private void Awake()
        {
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.OnDashed += HandleDashed;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnDashed -= HandleDashed;
            }
            if (trailCoroutine != null)
            {
                StopCoroutine(trailCoroutine);
                trailCoroutine = null;
            }
        }

        private void HandleDashed()
        {
            if (trailCoroutine != null)
            {
                StopCoroutine(trailCoroutine);
            }
            trailCoroutine = StartCoroutine(GhostTrailRoutine());
        }

        private IEnumerator GhostTrailRoutine()
        {
            while (playerController != null && playerController.IsDashing)
            {
                SpawnGhostSprite();
                yield return new WaitForSeconds(spawnInterval);
            }
            trailCoroutine = null;
        }

        private void SpawnGhostSprite()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;

            GameObject ghost = new GameObject("GhostTrail");
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = transform.localScale;

            SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
            ghostSr.sprite = spriteRenderer.sprite;
            ghostSr.color = trailColor;
            ghostSr.sortingLayerID = spriteRenderer.sortingLayerID;
            ghostSr.sortingOrder = Mathf.Max(0, spriteRenderer.sortingOrder - 1);
            ghostSr.flipX = spriteRenderer.flipX;
            ghostSr.flipY = spriteRenderer.flipY;

            StartCoroutine(FadeAndDestroy(ghost, ghostSr));
        }

        private IEnumerator FadeAndDestroy(GameObject ghost, SpriteRenderer ghostSr)
        {
            float elapsed = 0f;
            Color initialColor = ghostSr.color;

            while (elapsed < ghostLifetime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(initialColor.a, 0f, elapsed / ghostLifetime);
                if (ghostSr != null)
                {
                    ghostSr.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
                }
                yield return null;
            }

            if (ghost != null)
            {
                Destroy(ghost);
            }
        }
    }
}
