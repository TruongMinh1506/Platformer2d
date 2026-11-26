using System.Collections;
using UnityEngine;
using Platformer.Common;
using Platformer.Managers;
using Platformer.UI;

namespace Platformer.Enemies
{
    /// <summary>
    /// Manages the Level 3 Boss Arena encounter:
    /// 1. Detects player entry, saves a pre-boss checkpoint, and slams the volcanic gate shut.
    /// 2. Clamps camera boundaries so player stays inside the arena.
    /// 3. Awakens Ignis the Volcanic Titan and displays the Boss Health Bar.
    /// 4. Handles Player death: restores camera bounds, opens gate, resets boss, and allows re-entry.
    /// 5. Opens the gate and activates the LevelGoal exit portal upon Boss defeat.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BossArenaController : MonoBehaviour
    {
        [Header("Arena Boundaries")]
        [Tooltip("Minimum X clamp for camera and boss.")]
        [SerializeField] private float arenaMinX = 103.5f;

        [Tooltip("Maximum X clamp for camera and boss.")]
        [SerializeField] private float arenaMaxX = 118.5f;

        [Tooltip("Ground surface Y coordinate.")]
        [SerializeField] private float groundY = 5.0f;

        [Header("Scene References")]
        [SerializeField] private BossVolcanoGolem boss;
        [SerializeField] private BossGate gate;
        [SerializeField] private GameObject levelGoal;
        [SerializeField] private BossHealthBarUI healthBarUI;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private GameObject victoryParticlePrefab;

        private bool hasTriggered = false;
        public bool HasTriggered => hasTriggered;
        private Vector2 originalCameraMinBounds;
        private Vector2 originalCameraMaxBounds;
        private Health playerHealth;

        public void SetReferences(BossVolcanoGolem b, BossGate g, GameObject goal, BossHealthBarUI barUI, CameraFollow cam, GameObject victoryFx = null)
        {
            boss = b;
            gate = g;
            levelGoal = goal;
            healthBarUI = barUI;
            cameraFollow = cam;
            victoryParticlePrefab = victoryFx;
        }

        private void Start()
        {
            if (boss == null)
            {
                boss = Object.FindFirstObjectByType<BossVolcanoGolem>();
            }

            if (gate == null)
            {
                gate = Object.FindFirstObjectByType<BossGate>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
            }

            if (cameraFollow != null)
            {
                originalCameraMinBounds = cameraFollow.MinBounds;
                originalCameraMaxBounds = cameraFollow.MaxBounds;
            }
            else
            {
                // Level 3 default camera bounds fallback
                originalCameraMinBounds = new Vector2(10.5f, 5.5f);
                originalCameraMaxBounds = new Vector2(109.5f, 10.5f);
            }

            if (healthBarUI == null)
            {
                healthBarUI = Object.FindFirstObjectByType<BossHealthBarUI>();
            }

            // Keep LevelGoal inactive until the Boss is defeated
            if (levelGoal == null)
            {
                var goal = Object.FindFirstObjectByType<LevelGoal>(FindObjectsInactive.Include);
                if (goal != null) levelGoal = goal.gameObject;
            }

            if (levelGoal != null)
            {
                levelGoal.SetActive(false);
            }

            // Hook player death listener
            HookPlayerDeath();
        }

        private void HookPlayerDeath()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.OnDied.RemoveListener(HandlePlayerDiedInArena);
                    playerHealth.OnDied.AddListener(HandlePlayerDiedInArena);
                }
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDied.RemoveListener(HandlePlayerDiedInArena);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered || !other.CompareTag("Player")) return;

            TriggerArena(other.gameObject);
        }

        /// <summary>
        /// Locks the arena, raises the health bar, and awakens the Boss.
        /// Can be triggered by the entrance trigger, or directly by Boss proximity/damage.
        /// </summary>
        public void TriggerArena(GameObject playerObj = null)
        {
            if (hasTriggered) return;
            hasTriggered = true;

            // Ensure player death listener is hooked
            if (playerHealth == null)
            {
                if (playerObj != null)
                {
                    playerHealth = playerObj.GetComponent<Health>();
                }
                if (playerHealth == null)
                {
                    var p = GameObject.FindWithTag("Player");
                    if (p != null) playerHealth = p.GetComponent<Health>();
                }

                if (playerHealth != null)
                {
                    playerHealth.OnDied.RemoveListener(HandlePlayerDiedInArena);
                    playerHealth.OnDied.AddListener(HandlePlayerDiedInArena);
                }
            }

            // Lock camera inside arena
            if (cameraFollow != null)
            {
                cameraFollow.SetBounds(new Vector2(110.0f, originalCameraMinBounds.y), new Vector2(110.5f, originalCameraMaxBounds.y));
            }

            // Slam gate shut
            if (gate != null)
            {
                gate.CloseGate();
            }

            if (healthBarUI == null)
            {
                healthBarUI = Object.FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);
            }

            // Awaken Boss
            if (boss != null)
            {
                boss.SetArenaContext(arenaMinX, arenaMaxX, groundY, this, healthBarUI);
                boss.Awaken();
            }

            // Temporarily disable entrance trigger during boss battle
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        /// <summary>
        /// Invoked when the player dies. If this happens while fighting the boss,
        /// restores the camera bounds, reopens the gate, and resets the boss.
        /// </summary>
        private void HandlePlayerDiedInArena()
        {
            if (!hasTriggered) return;
            if (boss == null || (boss.Health != null && boss.Health.IsDead)) return;

            StartCoroutine(ResetArenaRoutine());
        }

        private IEnumerator ResetArenaRoutine()
        {
            // Give a short delay for death animation to begin
            yield return new WaitForSeconds(0.4f);

            // 1. Restore camera bounds back to whole-level view so camera tracks player respawn
            if (cameraFollow != null)
            {
                cameraFollow.SetBounds(originalCameraMinBounds, originalCameraMaxBounds);
            }

            // 2. Reset gate so player can re-enter freely
            if (gate != null)
            {
                gate.ResetGate();
            }

            // 3. Reset Boss Ignis to initial spawn state
            if (boss != null)
            {
                boss.ResetBoss();
            }

            if (healthBarUI != null)
            {
                healthBarUI.HideInstant();
            }

            // 4. Re-enable arena trigger collider
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = true;
            }

            hasTriggered = false;
        }

        /// <summary>
        /// Invoked by the Boss when defeated to restore level flow and reveal the exit portal.
        /// </summary>
        public void OnBossDefeated()
        {
            // Restore camera bounds
            if (cameraFollow != null)
            {
                cameraFollow.SetBounds(originalCameraMinBounds, originalCameraMaxBounds);
            }

            // Open gate
            if (gate != null)
            {
                gate.OpenGate();
            }

            BossRushManager.Instance?.OnBossDefeated();

            // Activate LevelGoal portal
            if (levelGoal == null)
            {
                var goal = Object.FindFirstObjectByType<LevelGoal>(FindObjectsInactive.Include);
                if (goal != null) levelGoal = goal.gameObject;
            }

            if (levelGoal != null)
            {
                // Ensure goal sits solidly on the ground right in the arena center
                levelGoal.transform.position = new Vector3(113.5f, groundY, 0f);
                levelGoal.SetActive(true);

                if (victoryParticlePrefab != null)
                {
                    Instantiate(victoryParticlePrefab, levelGoal.transform.position + Vector3.up * 0.75f, Quaternion.identity);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound("checkpoint");
                }
            }
        }
    }
}
