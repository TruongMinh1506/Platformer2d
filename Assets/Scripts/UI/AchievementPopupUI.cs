using System.Collections;
using UnityEngine;
using TMPro;
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// Displays a luxurious Steam/PlayStation style popup banner when an achievement is unlocked.
    /// Smoothly slides down from the top edge, shines, and slides back out.
    /// </summary>
    public class AchievementPopupUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform bannerPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float displayDuration = 3.5f;

        private Vector2 hiddenPos = new Vector2(0f, 150f);
        private Vector2 visiblePos = new Vector2(0f, -40f);
        private Coroutine activePopupRoutine;

        private void Start()
        {
            if (bannerPanel != null)
            {
                bannerPanel.anchoredPosition = hiddenPos;
                bannerPanel.gameObject.SetActive(false);
            }

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked += ShowAchievement;
            }
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= ShowAchievement;
            }
        }

        public void ShowAchievement(AchievementData data)
        {
            if (activePopupRoutine != null) StopCoroutine(activePopupRoutine);
            activePopupRoutine = StartCoroutine(PopupRoutine(data));
        }

        private IEnumerator PopupRoutine(AchievementData data)
        {
            if (bannerPanel == null) yield break;

            bannerPanel.gameObject.SetActive(true);
            if (titleText != null) titleText.text = $"<color=#FFD700>ACHIEVEMENT UNLOCKED!</color>\n{data.title}";
            if (descriptionText != null) descriptionText.text = data.description;

            // Slide in
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                bannerPanel.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, t);
                yield return null;
            }
            bannerPanel.anchoredPosition = visiblePos;

            // Hold
            yield return new WaitForSecondsRealtime(displayDuration);

            // Slide out
            elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                bannerPanel.anchoredPosition = Vector2.Lerp(visiblePos, hiddenPos, t);
                yield return null;
            }
            bannerPanel.anchoredPosition = hiddenPos;
            bannerPanel.gameObject.SetActive(false);
            activePopupRoutine = null;
        }
    }
}
