using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Managers;
using Platformer.Common;
using Platformer.Player;

namespace Platformer.UI
{
    /// <summary>
    /// Event-driven HUD controller updating Health, Energy/Mana, Coins, Score, and Lives.
    /// Manages the Game Over interactive screen with Retry and Main Menu actions.
    /// Strictly reactive with ZERO polling in Update() except when Game Over screen is active.
    /// Uses modern Unity 6 APIs (FindFirstObjectByType).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("UI Text Displays")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private Slider energySlider;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI livesText;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverMenuButton;

        private Health subscribedPlayerHealth;
        private PlayerEnergy subscribedPlayerEnergy;

        private void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged += HandleScoreChanged;
                GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                GameManager.Instance.OnLivesChanged += HandleLivesChanged;
                GameManager.Instance.OnGameOver += HandleGameOver;

                // Initial UI values
                HandleScoreChanged(GameManager.Instance.Score);
                HandleCoinsChanged(GameManager.Instance.Coins);
                HandleLivesChanged(GameManager.Instance.Lives);
            }

            // Locate player and subscribe to Health events
            HookPlayerHealth();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (gameOverMenuButton != null)
            {
                gameOverMenuButton.onClick.RemoveListener(OnGameOverMenuClicked);
                gameOverMenuButton.onClick.AddListener(OnGameOverMenuClicked);
            }
        }

        private void HookPlayerHealth()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                subscribedPlayerHealth = player.GetComponent<Health>();
                if (subscribedPlayerHealth != null)
                {
                    subscribedPlayerHealth.OnDamaged.AddListener(HandleHealthChanged);
                    HandleHealthChanged(subscribedPlayerHealth.CurrentHealth);
                }

                subscribedPlayerEnergy = player.GetComponent<PlayerEnergy>();
                if (subscribedPlayerEnergy != null)
                {
                    subscribedPlayerEnergy.OnEnergyChanged += HandleEnergyChanged;
                    HandleEnergyChanged(subscribedPlayerEnergy.CurrentEnergy, subscribedPlayerEnergy.MaxEnergy);
                }
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
                GameManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
                GameManager.Instance.OnLivesChanged -= HandleLivesChanged;
                GameManager.Instance.OnGameOver -= HandleGameOver;
            }

            if (subscribedPlayerHealth != null)
            {
                subscribedPlayerHealth.OnDamaged.RemoveListener(HandleHealthChanged);
            }

            if (subscribedPlayerEnergy != null)
            {
                subscribedPlayerEnergy.OnEnergyChanged -= HandleEnergyChanged;
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            if (gameOverMenuButton != null)
            {
                gameOverMenuButton.onClick.RemoveListener(OnGameOverMenuClicked);
            }
        }

        private void Update()
        {
            // When game over panel is active, enable convenient one-key keyboard shortcuts
            if (gameOverPanel != null && gameOverPanel.activeInHierarchy)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                        UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        OnRetryClicked();
                    }
                    else if (UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame ||
                             UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        OnGameOverMenuClicked();
                    }
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    OnRetryClicked();
                }
                else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
                {
                    OnGameOverMenuClicked();
                }
#endif
            }
        }

        public void OnRetryClicked()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSession();
            }
            SceneLoader.ReloadCurrentScene();
        }

        public void OnGameOverMenuClicked()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSession();
            }
            SceneLoader.LoadScene("MainMenu");
        }

        private void HandleEnergyChanged(float current, float max)
        {
            if (energyText != null)
            {
                energyText.text = $"MP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
            if (energySlider != null)
            {
                energySlider.maxValue = max;
                energySlider.value = current;
            }
        }

        public void SetReferences(TextMeshProUGUI hp, TextMeshProUGUI coins, TextMeshProUGUI score, TextMeshProUGUI lives, GameObject goPanel, TextMeshProUGUI energy = null, Slider eSlider = null, Button retryBtn = null, Button goMenuBtn = null)
        {
            healthText = hp;
            coinsText = coins;
            scoreText = score;
            livesText = lives;
            gameOverPanel = goPanel;
            energyText = energy;
            energySlider = eSlider;
            retryButton = retryBtn;
            gameOverMenuButton = goMenuBtn;

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (gameOverMenuButton != null)
            {
                gameOverMenuButton.onClick.RemoveListener(OnGameOverMenuClicked);
                gameOverMenuButton.onClick.AddListener(OnGameOverMenuClicked);
            }
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score:D5}";
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = $"COINS: {coins:D2}";
            }
        }

        private void HandleLivesChanged(int lives)
        {
            if (livesText != null)
            {
                livesText.text = $"LIVES: {Mathf.Max(0, lives)}";
            }
        }

        private void HandleHealthChanged(int currentHealth)
        {
            if (healthText != null)
            {
                int max = subscribedPlayerHealth != null ? subscribedPlayerHealth.MaxHealth : 3;
                healthText.text = $"HP: {currentHealth} / {max}";
            }
        }

        private void HandleGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                EnsureGameOverButtons();
            }
        }

        private void EnsureGameOverButtons()
        {
            if (gameOverPanel == null) return;

            TMP_FontAsset hudFont = healthText != null ? healthText.font : null;

            // 1. Retry Button
            Transform retryTr = gameOverPanel.transform.Find("GameOver_RetryBtn");
            if (retryTr == null && retryButton == null)
            {
                GameObject rBtnGo = new GameObject("GameOver_RetryBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                rBtnGo.transform.SetParent(gameOverPanel.transform, false);
                RectTransform rRect = rBtnGo.GetComponent<RectTransform>();
                rRect.anchorMin = new Vector2(0.5f, 0.5f);
                rRect.anchorMax = new Vector2(0.5f, 0.5f);
                rRect.pivot = new Vector2(0.5f, 0.5f);
                rRect.anchoredPosition = new Vector2(0f, -40f);
                rRect.sizeDelta = new Vector2(260f, 52f);

                Image rImg = rBtnGo.GetComponent<Image>();
                rImg.color = new Color(0.2f, 0.75f, 0.35f, 1f);

                Button rBtn = rBtnGo.GetComponent<Button>();
                rBtn.onClick.AddListener(OnRetryClicked);
                retryButton = rBtn;

                GameObject rTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                rTextGo.transform.SetParent(rBtnGo.transform, false);
                RectTransform rtRect = rTextGo.GetComponent<RectTransform>();
                rtRect.anchorMin = Vector2.zero;
                rtRect.anchorMax = Vector2.one;
                rtRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI rTmp = rTextGo.GetComponent<TextMeshProUGUI>();
                if (hudFont != null) rTmp.font = hudFont;
                rTmp.text = "RETRY (R)";
                rTmp.fontSize = 24f;
                rTmp.alignment = TextAlignmentOptions.Center;
                rTmp.color = Color.white;
                rTmp.fontStyle = FontStyles.Bold;
            }

            // 2. Main Menu Button
            Transform menuTr = gameOverPanel.transform.Find("GameOver_MenuBtn");
            if (menuTr == null && gameOverMenuButton == null)
            {
                GameObject mBtnGo = new GameObject("GameOver_MenuBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                mBtnGo.transform.SetParent(gameOverPanel.transform, false);
                RectTransform mRect = mBtnGo.GetComponent<RectTransform>();
                mRect.anchorMin = new Vector2(0.5f, 0.5f);
                mRect.anchorMax = new Vector2(0.5f, 0.5f);
                mRect.pivot = new Vector2(0.5f, 0.5f);
                mRect.anchoredPosition = new Vector2(0f, -105f);
                mRect.sizeDelta = new Vector2(260f, 52f);

                Image mImg = mBtnGo.GetComponent<Image>();
                mImg.color = new Color(0.22f, 0.45f, 0.70f, 1f);

                Button mBtn = mBtnGo.GetComponent<Button>();
                mBtn.onClick.AddListener(OnGameOverMenuClicked);
                gameOverMenuButton = mBtn;

                GameObject mTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                mTextGo.transform.SetParent(mBtnGo.transform, false);
                RectTransform mtRect = mTextGo.GetComponent<RectTransform>();
                mtRect.anchorMin = Vector2.zero;
                mtRect.anchorMax = Vector2.one;
                mtRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI mTmp = mTextGo.GetComponent<TextMeshProUGUI>();
                if (hudFont != null) mTmp.font = hudFont;
                mTmp.text = "MAIN MENU (M)";
                mTmp.fontSize = 24f;
                mTmp.alignment = TextAlignmentOptions.Center;
                mTmp.color = Color.white;
                mTmp.fontStyle = FontStyles.Bold;
            }
        }
    }
}
