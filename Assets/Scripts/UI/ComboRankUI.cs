using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// Renders the Style Combo Rank (D -> SSS) and Overdrive Bar on the HUD.
    /// </summary>
    public class ComboRankUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject container;
        [SerializeField] private TextMeshProUGUI rankLetterText;
        [SerializeField] private TextMeshProUGUI rankTitleText;
        [SerializeField] private Slider overdriveSlider;
        [SerializeField] private TextMeshProUGUI overdriveReadyText;

        private RectTransform rankRect;
        private float popTimer = 0f;

        private readonly Color colD = new Color(0.4f, 0.8f, 1f);
        private readonly Color colC = new Color(0.3f, 0.6f, 1f);
        private readonly Color colB = new Color(0.3f, 0.9f, 0.4f);
        private readonly Color colA = new Color(1f, 0.9f, 0.2f);
        private readonly Color colS = new Color(1f, 0.55f, 0.1f);
        private readonly Color colSSS = new Color(1f, 0.15f, 0.35f);

        private void Awake()
        {
            if (rankLetterText != null) rankRect = rankLetterText.GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (ComboRankManager.Instance != null)
            {
                ComboRankManager.Instance.OnRankChanged += HandleRankChanged;
                ComboRankManager.Instance.OnOverdriveChanged += HandleOverdriveChanged;
            }

            UpdateDisplay(ComboRank.None, 0f);
            HandleOverdriveChanged(0f);
        }

        private void OnDestroy()
        {
            if (ComboRankManager.Instance != null)
            {
                ComboRankManager.Instance.OnRankChanged -= HandleRankChanged;
                ComboRankManager.Instance.OnOverdriveChanged -= HandleOverdriveChanged;
            }
        }

        private void Update()
        {
            if (popTimer > 0f)
            {
                popTimer -= Time.deltaTime;
                if (rankRect != null)
                {
                    float scale = 1f + Mathf.Sin(popTimer * 15f) * 0.3f;
                    rankRect.localScale = Vector3.one * scale;
                }
            }
            else if (rankRect != null)
            {
                rankRect.localScale = Vector3.one;
            }
        }

        private void HandleRankChanged(ComboRank rank, float score)
        {
            UpdateDisplay(rank, score);
            popTimer = 0.35f;
        }

        private void HandleOverdriveChanged(float percent)
        {
            if (overdriveSlider != null) overdriveSlider.value = percent;
            if (overdriveReadyText != null)
            {
                bool isReady = percent >= 0.99f;
                overdriveReadyText.gameObject.SetActive(isReady);
            }
        }

        private void UpdateDisplay(ComboRank rank, float score)
        {
            if (rank == ComboRank.None)
            {
                if (container != null) container.SetActive(false);
                return;
            }

            if (container != null) container.SetActive(true);

            Color rankColor = GetColorForRank(rank);

            if (rankLetterText != null)
            {
                rankLetterText.text = rank == ComboRank.SSS ? "SSS" : rank.ToString();
                rankLetterText.color = rankColor;
            }

            if (rankTitleText != null && ComboRankManager.Instance != null)
            {
                rankTitleText.text = ComboRankManager.Instance.GetRankTitle(rank);
                rankTitleText.color = rankColor;
            }
        }

        private Color GetColorForRank(ComboRank rank)
        {
            switch (rank)
            {
                case ComboRank.D: return colD;
                case ComboRank.C: return colC;
                case ComboRank.B: return colB;
                case ComboRank.A: return colA;
                case ComboRank.S: return colS;
                case ComboRank.SSS: return colSSS;
                default: return Color.white;
            }
        }
    }
}
