using UnityEngine;
using Platformer.Managers;
using Platformer.Player;

namespace Platformer.Common
{
    /// <summary>
    /// Volcanic steam vent / thermal air draft.
    /// Propels the player high up into the air, renewing double jumps and air dashes!
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ThermalAirGeyser : MonoBehaviour
    {
        [Header("Geyser Launch Properties")]
        [Tooltip("Upward velocity applied to player inside the draft.")]
        [SerializeField] private float launchVelocity = 18.5f;

        [Tooltip("Horizontal drag/centering force to keep player in the draft.")]
        [SerializeField] private float centeringForce = 3.5f;

        [Tooltip("Cooldown period between consecutive audio whooshes.")]
        [SerializeField] private float soundCooldown = 0.6f;

        private float soundTimer = 0f;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void Update()
        {
            if (soundTimer > 0f) soundTimer -= Time.deltaTime;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            PlayerController controller = other.GetComponent<PlayerController>();

            if (rb != null)
            {
                // Smoothly pull player towards draft center horizontally
                float dx = transform.position.x - other.transform.position.x;
                float newVx = Mathf.MoveTowards(rb.linearVelocity.x, dx * centeringForce, 25f * Time.deltaTime);

                // Powerful thermal upward surge
                rb.linearVelocity = new Vector2(newVx, launchVelocity);
            }

            if (controller != null)
            {
                controller.ResetJumps();
                controller.ResetDash();
            }

            if (soundTimer <= 0f)
            {
                soundTimer = soundCooldown;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound("geyser", 0.75f);
                }
            }
        }
    }
}
