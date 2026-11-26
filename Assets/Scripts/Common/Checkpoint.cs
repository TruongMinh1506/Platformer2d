using UnityEngine;
using Platformer.Managers;

namespace Platformer.Common
{
    /// <summary>
    /// Checkpoint banner that activates upon player contact.
    /// Changes color to visually signal activation and records respawn coordinates in the GameManager.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Colors")]
        [Tooltip("Visual tint when inactive.")]
        [SerializeField] private Color inactiveColor = new Color(0.6f, 0.65f, 0.7f, 1f);

        [Tooltip("Visual tint when activated.")]
        [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 0.35f, 1f);

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform respawnPoint;

        private bool isActivated;

        public bool IsActivated => isActivated;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.color = inactiveColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated) return;
            if (!other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

            ActivateCheckpoint();
        }

        public void ActivateCheckpoint()
        {
            isActivated = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = activeColor;
            }

            Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : transform.position + new Vector3(0f, 0.5f, 0f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(spawnPos);
                GameManager.Instance.AddScore(250); // Checkpoint bonus
            }

            UI.SpeedrunTimerUI.Instance?.RecordCheckpointSplit(gameObject.name);
        }
    }
}

