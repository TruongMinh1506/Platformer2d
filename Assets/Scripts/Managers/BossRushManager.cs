using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Common;
using Platformer.Enemies;
using Platformer.UI;

namespace Platformer.Managers
{
    /// <summary>
    /// Manages the standalone Boss Rush Arena:
    /// - Spawns player directly inside the volcanic arena against an enraged Titan Boss.
    /// - Runs the hardcore Speedrun Timer.
    /// - Displays the Boss Rush Victory Summary with time ranking (S/A/B rank).
    /// </summary>
    public class BossRushManager : MonoBehaviour
    {
        public static BossRushManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private BossVolcanoGolem boss;
        [SerializeField] private BossArenaController arenaController;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private float battleTimer = 0f;
        private bool isBattleActive = false;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
            if (menuButton != null) menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        private void Start()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);

            StartCoroutine(InitBossRushBattle());
        }

        private IEnumerator InitBossRushBattle()
        {
            yield return new WaitForSeconds(0.2f);

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // Place player in the arena
                player.transform.position = new Vector3(107f, 5.5f, 0f);
            }

            if (boss == null) boss = Object.FindFirstObjectByType<BossVolcanoGolem>();
            if (arenaController == null) arenaController = Object.FindFirstObjectByType<BossArenaController>();

            if (arenaController != null && player != null)
            {
                arenaController.TriggerArena(player);
            }

            battleTimer = 0f;
            isBattleActive = true;
        }

        private void Update()
        {
            if (isBattleActive && (GameManager.Instance == null || !GameManager.Instance.IsGameOver))
            {
                battleTimer += Time.deltaTime;
            }

            if (victoryPanel != null && victoryPanel.activeSelf)
            {
                bool retry = false;
                bool menu = false;

#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) retry = true;
                    if (kb.escapeKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame) menu = true;
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) retry = true;
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M)) menu = true;
#endif

                if (retry)
                {
                    OnRetryClicked();
                }
                else if (menu)
                {
                    OnMenuClicked();
                }
            }
        }

        public void OnBossDefeated()
        {
            isBattleActive = false;
            SpeedrunTimerUI.Instance?.StopTimer();

            StartCoroutine(ShowVictoryRoutine());
        }

        private IEnumerator ShowVictoryRoutine()
        {
            yield return new WaitForSeconds(1.5f);

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);

                // Compute rank
                string rank;
                if (battleTimer < 45f)
                {
                    rank = "<color=#FFD700>RANK S (LEGENDARY)</color>";
                }
                else if (battleTimer < 65f)
                {
                    rank = "<color=#00e5ff>RANK A (MASTER)</color>";
                }
                else
                {
                    rank = "<color=#2ecc71>RANK B (CHALLENGER)</color>";
                }

                if (rankText != null) rankText.text = rank;
                if (timeText != null) timeText.text = $"TIME: {SpeedrunTimerUI.FormatTime(battleTimer)}";

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound("win");
                }
            }
        }

        public void OnRetryClicked()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.ResetSession();
            SceneLoader.ReloadCurrentScene();
        }

        public void OnMenuClicked()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.ResetSession();
            SceneLoader.LoadScene("MainMenu");
        }
    }
}
