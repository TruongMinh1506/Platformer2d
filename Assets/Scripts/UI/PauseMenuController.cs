using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// Manages the in-game Pause Menu toggled via the Escape key.
    /// Controls Time.timeScale and provides Resume, Restart, and Main Menu actions.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Root panel GameObject for the pause menu.")]
        [SerializeField] private GameObject pausePanel;

        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button charmsButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Upgrade Shop References")]
        [SerializeField] private UpgradeShopUI shopUI;

        [Header("Volume Controls")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        public static bool IsGamePaused { get; private set; }
        private bool isPaused;

        private void Awake()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (shopButton != null) shopButton.onClick.AddListener(OnShopClicked);
            if (charmsButton != null) charmsButton.onClick.AddListener(OnCharmsClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
                musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            }
        }

        private void OnMusicSliderChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val);
        }

        private void OnSfxSliderChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(val);
        }

        private void Update()
        {
            if (ReadPauseInput())
            {
                // If game over screen is active, allow player to interact directly with game over options
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                {
                    return;
                }

                if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up UI event listeners
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (shopButton != null) shopButton.onClick.RemoveListener(OnShopClicked);
            if (charmsButton != null) charmsButton.onClick.RemoveListener(OnCharmsClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(LoadMainMenu);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);

            // Always restore normal time scale when leaving the scene
            IsGamePaused = false;
            Time.timeScale = 1f;
        }

        public void Pause()
        {
            isPaused = true;
            IsGamePaused = true;
            Time.timeScale = 0f;
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
        }

        public void Resume()
        {
            isPaused = false;
            IsGamePaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        public void Restart()
        {
            isPaused = false;
            IsGamePaused = false;
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSession();
            }
            SceneLoader.ReloadCurrentScene();
        }

        public void SetShopReferences(Button sBtn, UpgradeShopUI sUI, Button cBtn = null)
        {
            shopButton = sBtn;
            shopUI = sUI;
            charmsButton = cBtn;
            if (charmsButton != null)
            {
                charmsButton.onClick.RemoveListener(OnCharmsClicked);
                charmsButton.onClick.AddListener(OnCharmsClicked);
            }
        }

        private void OnCharmsClicked()
        {
            CharmSocketUI.Instance?.OpenShop();
        }

        private void OnShopClicked()
        {
            if (shopUI != null)
            {
                shopUI.OpenShop();
            }
        }

        public void LoadMainMenu()
        {
            isPaused = false;
            IsGamePaused = false;
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetSession();
            }
            SceneLoader.LoadScene("MainMenu");
        }

        private bool ReadPauseInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
#endif

            return false;
        }
    }
}

