using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer.Common;

namespace Platformer.Player
{
    /// <summary>
    /// Handles player death by disabling player controls, playing a death pop,
    /// waiting for a short delay, and reloading the scene (stub for checkpoints).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerDeath : MonoBehaviour
    {
        [Tooltip("Delay in seconds before reloading the level upon death.")]
        [SerializeField] private float reloadDelay = 1.2f;

        [SerializeField] private Health health;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDied.AddListener(HandleDeath);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDied.RemoveListener(HandleDeath);
            }
        }

        private void Update()
        {
            // Safety net: if player ever falls into the abyss below Y = -4.5f
            if (transform.position.y < -4.5f)
            {
                if (health != null && !health.IsDead)
                {
                    health.Kill();
                }
                else if (rb != null && rb.simulated)
                {
                    // If already dead and falling below bottom abyss boundary, freeze downward velocity
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false;
                }
            }
        }

        private void HandleDeath()
        {
            // 1. Disable player movement and combat input
            if (playerController != null) playerController.enabled = false;
            if (playerCombat != null) playerCombat.enabled = false;

            // 2. Visual feedback: upward pop and tint red
            if (rb != null)
            {
                if (transform.position.y < -1f)
                {
                    // If died in an abyss/pit, freeze falling movement so player doesn't plunge forever during delay
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false;
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, 9f);
                }
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 0.9f);
            }

            // 3. Disable collider to prevent extraneous collisions
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // 4. Delegate to GameManager if present; otherwise fallback to direct reload
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnPlayerDied(gameObject);
            }
            else
            {
                StartCoroutine(ReloadSceneAfterDelay());
            }
        }

        /// <summary>
        /// Restores player state upon respawning at a checkpoint.
        /// </summary>
        public void OnRespawn()
        {
            if (health != null) health.ResetHealth();
            var energy = GetComponent<PlayerEnergy>();
            if (energy != null) energy.AddEnergy(energy.MaxEnergy);
            if (playerController != null) playerController.enabled = true;
            if (playerCombat != null) playerCombat.enabled = true;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            if (rb != null)
            {
                rb.simulated = true;
                rb.linearVelocity = Vector2.zero;
            }
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }

        private IEnumerator ReloadSceneAfterDelay()
        {
            yield return new WaitForSecondsRealtime(reloadDelay);
            Managers.SceneLoader.ReloadCurrentScene();
        }
    }
}

