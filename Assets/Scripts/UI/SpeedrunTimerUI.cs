using System;
using UnityEngine;
using TMPro;
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// Esports-grade Speedrun Timer HUD with millisecond accuracy (00:00.000),
    /// checkpoint split comparisons, and personal best persistence.
    /// Toggleable via Key [T].
    /// </summary>
    public class SpeedrunTimerUI : MonoBehaviour
    {
        public static SpeedrunTimerUI Instance { get; private set; }

        [Header("UI Text Displays")]
        [SerializeField] private GameObject container;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI splitText;
        [SerializeField] private TextMeshProUGUI pbText;

        private float elapsedTime = 0f;
        private bool isRunning = true;
        private bool isVisible = true;
        private int currentLevelIndex = 1;
        private float personalBest = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.Contains("Level1")) currentLevelIndex = 1;
            else if (sceneName.Contains("Level2")) currentLevelIndex = 2;
            else if (sceneName.Contains("Level3")) currentLevelIndex = 3;
            else if (sceneName.Contains("BossRush")) currentLevelIndex = 99;

            personalBest = PlayerPrefs.GetFloat($"SPEEDRUN_PB_{currentLevelIndex}", 0f);
            if (pbText != null)
            {
                pbText.text = personalBest > 0f ? $"PB: {FormatTime(personalBest)}" : "PB: --:--.---";
            }

            if (splitText != null)
            {
                splitText.text = "";
            }

            elapsedTime = 0f;
            isRunning = true;
        }

        private void Update()
        {
            // Toggle visibility with T key (supporting both Input System and Legacy)
            bool toggleKey = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
            {
                toggleKey = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.T))
            {
                toggleKey = true;
            }
#endif
            if (toggleKey)
            {
                isVisible = !isVisible;
                if (container != null)
                {
                    container.SetActive(isVisible);
                }
            }

            if (isRunning && (GameManager.Instance == null || !GameManager.Instance.IsGameOver))
            {
                elapsedTime += Time.unscaledDeltaTime;
                if (timerText != null)
                {
                    timerText.text = FormatTime(elapsedTime);
                }
            }
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        public void RecordCheckpointSplit(string checkpointName)
        {
            if (splitText != null)
            {
                splitText.text = $"<color=#00e5ff>{checkpointName}</color>  {FormatTime(elapsedTime)}";
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("speedrun_split");
            }
        }

        public void RecordLevelComplete()
        {
            StopTimer();

            bool isNewPB = personalBest <= 0f || elapsedTime < personalBest;
            if (isNewPB)
            {
                personalBest = elapsedTime;
                PlayerPrefs.SetFloat($"SPEEDRUN_PB_{currentLevelIndex}", personalBest);
                PlayerPrefs.Save();

                if (splitText != null)
                {
                    splitText.text = "<color=#FFD700>* NEW PERSONAL BEST! *</color>";
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound("speedrun_pb");
                }
            }
            else
            {
                float diff = elapsedTime - personalBest;
                if (splitText != null)
                {
                    splitText.text = $"<color=#e74c3c>+{diff:F2}s vs PB</color>";
                }
            }

            if (pbText != null)
            {
                pbText.text = $"PB: {FormatTime(personalBest)}";
            }
        }

        public static string FormatTime(float timeInSeconds)
        {
            int minutes = (int)(timeInSeconds / 60f);
            int seconds = (int)(timeInSeconds % 60f);
            int fraction = (int)((timeInSeconds * 1000f) % 1000f);
            return $"{minutes:00}:{seconds:00}.{fraction:000}";
        }

        public void SetReferences(GameObject rootContainer, TextMeshProUGUI timer, TextMeshProUGUI split, TextMeshProUGUI pb)
        {
            container = rootContainer;
            timerText = timer;
            splitText = split;
            pbText = pb;
        }
    }
}
