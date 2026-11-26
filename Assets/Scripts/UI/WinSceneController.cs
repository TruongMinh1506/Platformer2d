using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// UI Controller for the Victory / Win Scene upon conquering all levels.
    /// Displays the high score, total 3-star rating, and provides Play Again, Level Select, and Main Menu actions.
    /// </summary>
    public class WinSceneController : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Score & Star Displays")]
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI totalStarsText;

        public void SetReferences(Button playAgain, Button levelSelect, Button mainMenu, TextMeshProUGUI bestScore, TextMeshProUGUI totalStars)
        {
            playAgainButton = playAgain;
            levelSelectButton = levelSelect;
            mainMenuButton = mainMenu;
            bestScoreText = bestScore;
            totalStarsText = totalStars;
        }

        private void Start()
        {
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OnLevelSelectClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            if (bestScoreText != null)
            {
                int bestScore = ProgressManager.GetBestScore();
                bestScoreText.text = $"BEST SCORE: <color=#FFD700>{bestScore:D5}</color>";
            }

            if (totalStarsText != null)
            {
                int stars = ProgressManager.GetTotalStars();
                totalStarsText.text = $"TOTAL STARS: <color=#FFD700>{stars} / 9</color>";
            }
        }

        private void OnDestroy()
        {
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            if (levelSelectButton != null) levelSelectButton.onClick.RemoveListener(OnLevelSelectClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void OnPlayAgainClicked()
        {
            SceneLoader.LoadScene("Level1");
        }

        private void OnLevelSelectClicked()
        {
            SceneLoader.LoadScene("MainMenu");
        }

        private void OnMainMenuClicked()
        {
            SceneLoader.LoadScene("MainMenu");
        }
    }
}
