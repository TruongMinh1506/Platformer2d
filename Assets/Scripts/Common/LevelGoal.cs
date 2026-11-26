using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Platformer.Managers;
using Platformer.Player;

namespace Platformer.Common
{
    /// <summary>
    /// Level exit goal (portal/flag) that triggers level completion upon player contact.
    /// Evaluates 3-Star rating (Clear, Coin Target, Speedrun Target), saves persistent stats,
    /// and displays a polished completion summary with action buttons (Next Level, Level Select, Retry).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        [Header("Next Destination")]
        [Tooltip("Name of the scene to load upon completion.")]
        [SerializeField] private string nextSceneName = "Level2";

        [Tooltip("Delay in seconds before auto-advancing (if no button clicked). Set 0 to disable auto-advance.")]
        [SerializeField] private float autoAdvanceDelay = 5.0f;

        [Header("UI Panel")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI starsText;
        [SerializeField] private TextMeshProUGUI criteriaText;

        [Header("Action Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private bool isCompleted;

        public void SetNextSceneName(string sceneName)
        {
            nextSceneName = sceneName;
        }

        public void SetUIReferences(
            GameObject panel,
            TextMeshProUGUI coins,
            TextMeshProUGUI time,
            TextMeshProUGUI score,
            TextMeshProUGUI stars = null,
            TextMeshProUGUI criteria = null,
            Button nextBtn = null,
            Button retryBtn = null,
            Button menuBtn = null)
        {
            completionPanel = panel;
            coinsText = coins;
            timeText = time;
            scoreText = score;
            starsText = stars;
            criteriaText = criteria;
            nextButton = nextBtn;
            retryButton = retryBtn;
            menuButton = menuBtn;

            if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
            if (menuButton != null) menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        private void Update()
        {
            if (isCompleted && completionPanel != null && completionPanel.activeSelf)
            {
                bool next = false;
                bool retry = false;
                bool menu = false;

#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) next = true;
                    if (kb.rKey.wasPressedThisFrame) retry = true;
                    if (kb.escapeKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame) menu = true;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) next = true;
                if (Input.GetKeyDown(KeyCode.R)) retry = true;
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M)) menu = true;
#endif

                if (next) OnNextClicked();
                else if (retry) OnRetryClicked();
                else if (menu) OnMenuClicked();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCompleted) return;
            if (!other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

            CompleteLevel(other.gameObject);
        }

        private void CompleteLevel(GameObject player)
        {
            isCompleted = true;

            // 1. Freeze player controls upon victory
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            UI.SpeedrunTimerUI.Instance?.RecordLevelComplete();

            // 2. Play win chime
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("win");
            }

            // 3. Compute stats
            float levelTime = Time.timeSinceLevelLoad;
            int coins = GameManager.Instance != null ? GameManager.Instance.Coins : 0;
            int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;

            // Determine level number
            string curScene = SceneManager.GetActiveScene().name;
            int levelNum = 1;
            if (curScene.Contains("2")) levelNum = 2;
            else if (curScene.Contains("3")) levelNum = 3;

            // Unlock next level
            ProgressManager.UnlockLevel(levelNum + 1);

            // Bank earned coins into persistent progression for Upgrade Shop
            ProgressManager.AddBankedCoins(coins);

            // Check Untouchable achievement (no damage taken throughout the entire level)
            if (GameManager.Instance != null && !GameManager.Instance.PlayerTookDamageThisLevel)
            {
                AchievementManager.Instance?.Unlock("ACH_UNTOUCHABLE");
            }

            // Calculate and save 3-Star progress
            int stars = ProgressManager.CalculateStars(levelNum, coins, levelTime, out bool starClear, out bool starCoin, out bool starTime);
            ProgressManager.SaveLevelStars(levelNum, stars);
            ProgressManager.SaveLevelBestTime(levelNum, levelTime);
            ProgressManager.SaveLevelBestCoins(levelNum, coins);
            ProgressManager.SaveScore(score);

            ProgressManager.GetStarRequirements(levelNum, out int targetCoins, out float targetTime);

            // 4. Display completion panel
            if (completionPanel != null)
            {
                completionPanel.SetActive(true);

                if (coinsText != null) coinsText.text = $"COINS: {coins}";
                if (timeText != null) timeText.text = $"TIME: {levelTime:F1}s";
                if (scoreText != null) scoreText.text = $"SCORE: {score}";
                if (starsText != null) starsText.text = ProgressManager.FormatStarsString(stars);
                if (criteriaText != null)
                {
                    criteriaText.text = $"Clear: Pass  |  Coins ({coins}/{targetCoins}): {(starCoin ? "<color=#2ecc71>PASS</color>" : "<color=#e74c3c>MISS</color>")}  |  Speed ({levelTime:F1}s/{targetTime:F0}s): {(starTime ? "<color=#2ecc71>PASS</color>" : "<color=#e74c3c>MISS</color>")}";
                }
            }

            // 5. Optional auto-advance fallback
            if (autoAdvanceDelay > 0f)
            {
                StartCoroutine(AutoAdvanceRoutine());
            }
        }

        private IEnumerator AutoAdvanceRoutine()
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            OnNextClicked();
        }

        private void OnNextClicked()
        {
            StopAllCoroutines();
            SceneLoader.LoadScene(nextSceneName);
        }

        private void OnRetryClicked()
        {
            StopAllCoroutines();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSession();
            }
            SceneLoader.ReloadCurrentScene();
        }

        private void OnMenuClicked()
        {
            StopAllCoroutines();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSession();
            }
            SceneLoader.LoadScene("MainMenu");
        }
    }
}
