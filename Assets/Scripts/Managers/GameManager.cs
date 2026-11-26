using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer.Common;
using Platformer.Player;

namespace Platformer.Managers
{
    /// <summary>
    /// Singleton game manager tracking score, coins, player lives, and checkpoints.
    /// Handles player respawning and game over state. Does not persist between scenes yet.
    /// Uses modern Unity 6 APIs (FindFirstObjectByType, linearVelocity).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Rules")]
        [Tooltip("Starting number of player lives.")]
        [SerializeField] private int initialLives = 3;

        [Tooltip("Delay in seconds before respawning at the checkpoint.")]
        [SerializeField] private float respawnDelay = 1.0f;

        // Events for UI and systems (zero-polling)
        public event Action<int> OnScoreChanged;
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnLivesChanged;
        public event Action OnGameOver;

        private int score;
        private int coins;
        private int lives;
        private Vector3 currentCheckpointPosition;
        private Vector3 initialSpawnPosition;
        private bool hasCheckpoint;
        private bool isGameOver;

        public int Score => score;
        public int Coins => coins;
        public int Lives => lives;
        public bool HasCheckpoint => hasCheckpoint;
        public bool IsGameOver => isGameOver;
        public Vector3 CurrentCheckpoint => currentCheckpointPosition;
        public bool PlayerTookDamageThisLevel { get; private set; }

        public void RecordPlayerDamaged()
        {
            PlayerTookDamageThisLevel = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigurePhysicsLayers()
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                // Ensure enemies do not physically collide with or push each other
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            }

            lives = initialLives;
            score = 0;
            coins = 0;
            hasCheckpoint = false;
            isGameOver = false;
            PlayerTookDamageThisLevel = false;
        }

        private void Start()
        {
            LocateInitialSpawn();

            // Initial event broadcasts
            OnScoreChanged?.Invoke(score);
            OnCoinsChanged?.Invoke(coins);
            OnLivesChanged?.Invoke(lives);
        }

        private void LocateInitialSpawn()
        {
            var spawnObj = GameObject.Find("PlayerSpawn");
            if (spawnObj != null)
            {
                initialSpawnPosition = spawnObj.transform.position;
                return;
            }

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                initialSpawnPosition = player.transform.position;
            }
        }

        /// <summary>
        /// Fully resets gameplay state (lives, score, coins, checkpoint, game over flag)
        /// for retrying or restarting a level.
        /// </summary>
        public void ResetSession()
        {
            lives = initialLives;
            score = 0;
            coins = 0;
            hasCheckpoint = false;
            isGameOver = false;
            PlayerTookDamageThisLevel = false;
            Time.timeScale = 1f;

            LocateInitialSpawn();

            OnScoreChanged?.Invoke(score);
            OnCoinsChanged?.Invoke(coins);
            OnLivesChanged?.Invoke(lives);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddScore(int amount)
        {
            if (amount <= 0 || isGameOver) return;
            score += amount;
            OnScoreChanged?.Invoke(score);
        }

        public void AddCoin(int amount = 1)
        {
            if (amount <= 0 || isGameOver) return;
            coins += amount;
            AddScore(100 * amount);
            OnCoinsChanged?.Invoke(coins);
        }

        public void SetCheckpoint(Vector3 position)
        {
            currentCheckpointPosition = position;
            hasCheckpoint = true;
        }

        /// <summary>
        /// Called when the player dies. Decreases lives and triggers respawn or game over.
        /// </summary>
        public void OnPlayerDied(GameObject player)
        {
            if (isGameOver)
            {
                // Safety net: re-invoke OnGameOver to guarantee Game Over UI remains visible and interactable
                Time.timeScale = 1f;
                OnGameOver?.Invoke();
                return;
            }

            lives--;
            OnLivesChanged?.Invoke(lives);

            if (lives > 0)
            {
                StartCoroutine(RespawnRoutine(player));
            }
            else
            {
                TriggerGameOver();
            }
        }

        private IEnumerator RespawnRoutine(GameObject player)
        {
            // Use realtime so hit-stops or time dilation never freeze the respawn timer
            yield return new WaitForSecondsRealtime(respawnDelay);

            if (player != null)
            {
                player.transform.SetParent(null);
                Vector3 targetPos = hasCheckpoint ? currentCheckpointPosition : initialSpawnPosition;

                // Safety validation: ensure respawn target is never beneath death boundary
                if (targetPos.y < -2f)
                {
                    targetPos = initialSpawnPosition;
                }
                if (targetPos.y < -2f)
                {
                    targetPos = new Vector3(targetPos.x, 2.5f, 0f);
                }

                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = true;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.position = targetPos;
                }
                player.transform.position = targetPos;
                Physics2D.SyncTransforms();

                var health = player.GetComponent<Health>();
                if (health != null)
                {
                    health.ResetHealth();
                }

                var death = player.GetComponent<PlayerDeath>();
                if (death != null)
                {
                    death.OnRespawn();
                }

                // Snap camera directly to player respawn position
                if (CameraFollow.Instance != null)
                {
                    CameraFollow.Instance.transform.position = new Vector3(targetPos.x, targetPos.y + 1.2f, -10f);
                }
            }
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;

            // Ensure normal time scale so Game Over UI and buttons remain fully responsive
            Time.timeScale = 1f;

            OnGameOver?.Invoke();
            // Interactive UI with RETRY (R) and MAIN MENU buttons handles level transitions cleanly without arbitrary forced timers.
        }

        private Coroutine hitStopCoroutine;

        /// <summary>
        /// Pauses game time briefly (hit-stop / freeze frame) to provide impact feedback.
        /// </summary>
        public void TriggerHitStop(float duration = 0.05f)
        {
            if (duration <= 0f) return;
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }
            hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            if (!UI.PauseMenuController.IsGamePaused)
            {
                Time.timeScale = 1f;
            }
            hitStopCoroutine = null;
        }

        public static void HitStop(float duration = 0.05f)
        {
            if (Instance != null)
            {
                Instance.TriggerHitStop(duration);
            }
        }
    }
}

