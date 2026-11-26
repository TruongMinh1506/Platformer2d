using System.Collections;
using UnityEngine;
using Platformer.Common;
using Platformer.Managers;

namespace Platformer.Enemies
{
    /// <summary>
    /// Reinforced volcanic stone portcullis that locks the entrance to the Boss Arena.
    /// Slams down upon arena trigger and raises/opens upon Boss defeat or player retry.
    /// </summary>
    public class BossGate : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Collider2D gateCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animation Settings")]
        [Tooltip("Speed at which the gate lowers or rises.")]
        [SerializeField] private float dropSpeed = 14f;

        [Tooltip("Vertical height offset for the open state.")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 6f, 0f);

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isClosed = false;

        public bool IsClosed => isClosed;

        private void Awake()
        {
            if (gateCollider == null) gateCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            closedPosition = transform.position;
            openPosition = closedPosition + openOffset;

            // Start open/raised above view
            ResetGate();
        }

        public void CloseGate()
        {
            if (isClosed) return;
            isClosed = true;

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            if (gateCollider != null)
            {
                gateCollider.enabled = true;
            }

            StopAllCoroutines();
            StartCoroutine(DropRoutine());
        }

        private IEnumerator DropRoutine()
        {
            while (Vector3.Distance(transform.position, closedPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, closedPosition, dropSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = closedPosition;

            // Slam impact feedback
            CameraFollow.Shake(0.32f, 0.42f);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("slam");
            }
        }

        public void OpenGate()
        {
            isClosed = false;

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            StopAllCoroutines();
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            CameraFollow.Shake(0.22f, 0.28f);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("crumble");
            }

            float elapsed = 0f;
            Color initColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (elapsed < 1.4f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, openPosition, (dropSpeed * 0.5f) * Time.deltaTime);

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(initColor.r, initColor.g, initColor.b, 1f - (elapsed / 1.4f));
                }
                yield return null;
            }

            transform.position = openPosition;

            if (gateCollider != null)
            {
                gateCollider.enabled = false;
            }

            // Note: Keep GameObject active to prevent coroutine execution errors
        }

        /// <summary>
        /// Instantly restores the gate to its initial open state without animation delays.
        /// </summary>
        public void ResetGate()
        {
            StopAllCoroutines();

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            transform.position = openPosition;

            if (gateCollider != null)
            {
                gateCollider.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            isClosed = false;
        }
    }
}
