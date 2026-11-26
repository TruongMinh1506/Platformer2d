using UnityEngine;
using Platformer.Common;

namespace Platformer.Player
{
    /// <summary>
    /// Procedural 2D squash and stretch controller for the Player.
    /// Deforms the player's visual scale on jump, land, and stomp,
    /// then smoothly springs back to normal scale.
    /// Preserves player facing direction seamlessly.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerSquashAndStretch : MonoBehaviour
    {
        [Header("Squash & Stretch Factors")]
        [Tooltip("Scale (X, Y) applied when the player jumps.")]
        [SerializeField] private Vector2 jumpStretch = new Vector2(0.82f, 1.25f);

        [Tooltip("Scale (X, Y) applied when the player lands.")]
        [SerializeField] private Vector2 landSquash = new Vector2(1.25f, 0.78f);

        [Tooltip("Scale (X, Y) applied when stomping an enemy.")]
        [SerializeField] private Vector2 stompSquash = new Vector2(1.32f, 0.72f);

        [Tooltip("Scale (X, Y) applied when the player dashes.")]
        [SerializeField] private Vector2 dashStretch = new Vector2(1.35f, 0.72f);

        [Tooltip("Speed at which scale returns to normal.")]
        [SerializeField] private float returnSpeed = 12f;

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private Health playerHealth;

        private Vector2 currentScaleModifier = Vector2.one;
        private Vector3 baseScale = Vector3.one;

        private void Awake()
        {
            baseScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
            if (playerHealth == null) playerHealth = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.OnJumped += HandleJumped;
                playerController.OnLanded += HandleLanded;
                playerController.OnDashed += HandleDashed;
            }
            if (playerCombat != null)
            {
                playerCombat.OnStomped += HandleStomped;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnJumped -= HandleJumped;
                playerController.OnLanded -= HandleLanded;
                playerController.OnDashed -= HandleDashed;
            }
            if (playerCombat != null)
            {
                playerCombat.OnStomped -= HandleStomped;
            }
        }

        private void HandleJumped()
        {
            currentScaleModifier = jumpStretch;
        }

        private void HandleLanded()
        {
            currentScaleModifier = landSquash;
        }

        private void HandleStomped()
        {
            currentScaleModifier = stompSquash;
        }

        private void HandleDashed()
        {
            currentScaleModifier = dashStretch;
        }

        private void LateUpdate()
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            // Smoothly recover back to Vector2.one
            currentScaleModifier = Vector2.Lerp(currentScaleModifier, Vector2.one, returnSpeed * Time.deltaTime);

            // Apply squash & stretch while preserving facing direction and original base scale
            float facingDir = (playerController != null && !playerController.IsFacingRight) ? -1f : 1f;
            transform.localScale = new Vector3(facingDir * baseScale.x * currentScaleModifier.x, baseScale.y * currentScaleModifier.y, baseScale.z);
        }
    }
}
