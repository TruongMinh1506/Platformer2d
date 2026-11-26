using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// UI Controller for the Main Menu and integrated Level Select system:
    /// - Fast-play highest unlocked level.
    /// - Interactive Level Select overlay showing per-level 3-Star rating, best time, and coins.
    /// - Lock/unlock state visual indicators.
    /// - Progress reset capability.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button bossRushButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button quitButton;

        [Header("Upgrade Shop References")]
        [SerializeField] private UpgradeShopUI shopUI;

        [Header("Main Menu Text Displays")]
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI totalStarsText;

        [Header("Level Select Panel")]
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetProgressButton;

        [Header("Level 1 Card References")]
        [SerializeField] private Button level1Button;
        [SerializeField] private TextMeshProUGUI level1StarsText;
        [SerializeField] private TextMeshProUGUI level1StatsText;

        [Header("Level 2 Card References")]
        [SerializeField] private Button level2Button;
        [SerializeField] private TextMeshProUGUI level2StarsText;
        [SerializeField] private TextMeshProUGUI level2StatsText;

        [Header("Level 3 Card References")]
        [SerializeField] private Button level3Button;
        [SerializeField] private TextMeshProUGUI level3StarsText;
        [SerializeField] private TextMeshProUGUI level3StatsText;

        public void SetReferences(
            Button play, Button levelSelect, Button quit,
            TextMeshProUGUI score, TextMeshProUGUI totalStars,
            GameObject selectPanel, Button back, Button reset,
            Button l1Btn, TextMeshProUGUI l1Stars, TextMeshProUGUI l1Stats,
            Button l2Btn, TextMeshProUGUI l2Stars, TextMeshProUGUI l2Stats,
            Button l3Btn, TextMeshProUGUI l3Stars, TextMeshProUGUI l3Stats)
        {
            playButton = play;
            levelSelectButton = levelSelect;
            quitButton = quit;
            bestScoreText = score;
            totalStarsText = totalStars;

            levelSelectPanel = selectPanel;
            backButton = back;
            resetProgressButton = reset;

            level1Button = l1Btn;
            level1StarsText = l1Stars;
            level1StatsText = l1Stats;

            level2Button = l2Btn;
            level2StarsText = l2Stars;
            level2StatsText = l2Stats;

            level3Button = l3Btn;
            level3StarsText = l3Stars;
            level3StatsText = l3Stats;
        }

        private void Start()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OnLevelSelectClicked);
            if (bossRushButton != null) bossRushButton.onClick.AddListener(OnBossRushClicked);
            if (shopButton != null) shopButton.onClick.AddListener(OnShopClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (resetProgressButton != null) resetProgressButton.onClick.AddListener(OnResetProgressClicked);

            if (level1Button != null) level1Button.onClick.AddListener(() => LaunchLevel(1));
            if (level2Button != null) level2Button.onClick.AddListener(() => LaunchLevel(2));
            if (level3Button != null) level3Button.onClick.AddListener(() => LaunchLevel(3));

            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(false);
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (levelSelectButton != null) levelSelectButton.onClick.RemoveListener(OnLevelSelectClicked);
            if (bossRushButton != null) bossRushButton.onClick.RemoveListener(OnBossRushClicked);
            if (shopButton != null) shopButton.onClick.RemoveListener(OnShopClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);

            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
            if (resetProgressButton != null) resetProgressButton.onClick.RemoveListener(OnResetProgressClicked);

            if (level1Button != null) level1Button.onClick.RemoveAllListeners();
            if (level2Button != null) level2Button.onClick.RemoveAllListeners();
            if (level3Button != null) level3Button.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (levelSelectPanel != null && levelSelectPanel.activeSelf)
            {
                bool escape = false;
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) escape = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Escape)) escape = true;
#endif
                if (escape)
                {
                    OnBackClicked();
                }
            }
        }

        public void SetShopReferences(Button shopBtn, UpgradeShopUI shop)
        {
            shopButton = shopBtn;
            shopUI = shop;
        }

        private void OnShopClicked()
        {
            if (shopUI != null)
            {
                shopUI.OpenShop();
            }
        }

        public void RefreshUI()
        {
            int bestScore = ProgressManager.GetBestScore();
            if (bestScoreText != null)
            {
                bestScoreText.text = $"BEST SCORE: <color=#FFD700>{bestScore:D5}</color>";
            }

            int totalStars = ProgressManager.GetTotalStars();
            if (totalStarsText != null)
            {
                totalStarsText.text = $"TOTAL STARS: <color=#FFD700>{totalStars} / 9</color>";
            }

            RefreshLevelCards();
        }

        private void RefreshLevelCards()
        {
            int unlockedIndex = ProgressManager.GetUnlockedLevelIndex();

            // Level 1: Always unlocked
            UpdateCard(1, true, level1Button, level1StarsText, level1StatsText);

            // Level 2: Unlocked if index >= 2
            bool l2Unlocked = unlockedIndex >= 2;
            UpdateCard(2, l2Unlocked, level2Button, level2StarsText, level2StatsText);

            // Level 3: Unlocked if index >= 3
            bool l3Unlocked = unlockedIndex >= 3;
            UpdateCard(3, l3Unlocked, level3Button, level3StarsText, level3StatsText);
        }

        private void UpdateCard(int levelNum, bool isUnlocked, Button btn, TextMeshProUGUI starsText, TextMeshProUGUI statsText)
        {
            int stars = ProgressManager.GetLevelStars(levelNum);
            float bestTime = ProgressManager.GetLevelBestTime(levelNum);
            int bestCoins = ProgressManager.GetLevelBestCoins(levelNum);

            if (isUnlocked)
            {
                if (btn != null)
                {
                    btn.interactable = true;
                    var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "PLAY";
                }

                if (starsText != null)
                {
                    starsText.text = ProgressManager.FormatStarsString(stars);
                }

                if (statsText != null)
                {
                    if (bestTime > 0f)
                    {
                        statsText.text = $"Best: {bestTime:F1}s  •  Coins: {bestCoins}";
                    }
                    else
                    {
                        ProgressManager.GetStarRequirements(levelNum, out int tCoins, out float tTime);
                        statsText.text = $"Target: {tCoins} Coins  •  Under {tTime:F0}s";
                    }
                }
            }
            else
            {
                if (btn != null)
                {
                    btn.interactable = false;
                    var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "LOCKED";
                }

                if (starsText != null)
                {
                    starsText.text = "<color=#444444>* * *</color>";
                }

                if (statsText != null)
                {
                    statsText.text = $"<color=#888888>Clear Level {levelNum - 1} to Unlock</color>";
                }
            }
        }

        private void OnPlayClicked()
        {
            // Play always starts the adventure from Level 1
            SceneLoader.LoadScene("Level1");
        }

        private void OnLevelSelectClicked()
        {
            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(true);
                RefreshLevelCards();
            }
        }

        private void OnBackClicked()
        {
            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(false);
            }
        }

        private void OnResetProgressClicked()
        {
            ProgressManager.ResetProgress();
            RefreshUI();
            Debug.Log("[MainMenu] Progression reset to default.");
        }

        private void LaunchLevel(int levelNumber)
        {
            SceneLoader.LoadScene($"Level{levelNumber}");
        }

        public void SetBossRushButton(Button btn)
        {
            bossRushButton = btn;
            if (bossRushButton != null)
            {
                bossRushButton.onClick.RemoveListener(OnBossRushClicked);
                bossRushButton.onClick.AddListener(OnBossRushClicked);
            }
        }

        private void OnBossRushClicked()
        {
            SceneLoader.LoadScene("BossRush");
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Exiting application...");
            Application.Quit();
        }
    }
}
