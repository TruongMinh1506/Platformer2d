using UnityEngine;
using UnityEngine.SceneManagement;

namespace Platformer.Common
{
    /// <summary>
    /// Trigger zone placed below death pits. When the player enters this area,
    /// the current active scene is reloaded.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Tag required to trigger a scene reload.")]
        [SerializeField] private string targetTag = "Player";

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ProcessTrigger(other);
        }

        private void ProcessTrigger(Collider2D other)
        {
            if (other.CompareTag(targetTag))
            {
                Health health = other.GetComponent<Health>();
                if (health != null && !health.IsDead)
                {
                    health.Kill();
                }
                else if (health != null && health.IsDead)
                {
                    var rb = other.GetComponent<Rigidbody2D>();
                    if (rb != null && rb.simulated)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.simulated = false;
                    }
                }
                else if (health == null)
                {
                    Managers.SceneLoader.ReloadCurrentScene();
                }
            }
            else if (other.CompareTag("Enemy"))
            {
                Health enemyHealth = other.GetComponent<Health>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    enemyHealth.Kill();
                }
                else
                {
                    Destroy(other.gameObject);
                }
            }
        }
    }
}

