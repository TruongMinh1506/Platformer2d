using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Platformer.UI
{
    /// <summary>
    /// Displays the Boss Health Bar at the top of the HUD during the Boss encounter.
    /// Smoothly lerps health updates and changes to an enraged color scheme in Phase 2.
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Root panel or container to toggle visibility.")]
        [SerializeField] private GameObject container;

        [Tooltip("Text displaying the Boss's title/name.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("Text displaying numerical health (e.g. '10 / 10').")]
        [SerializeField] private TextMeshProUGUI hpText;

        [Tooltip("Slider displaying remaining health percentage.")]
        [SerializeField] private Slider healthSlider;

        [Tooltip("Fill image of the slider to tint on enrage.")]
        [SerializeField] private Image fillImage;

        [Header("Visual Themes")]
        [SerializeField] private Color normalColor = new Color(0.96f, 0.42f, 0.12f, 1f);
        [SerializeField] private Color enragedColor = new Color(0.98f, 0.15f, 0.15f, 1f);

        private Coroutine lerpRoutine;
        private int currentMaxHp = 10;

        public void SetReferences(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI hp, Slider slider, Image fill)
        {
            container = panel;
            titleText = title;
            hpText = hp;
            healthSlider = slider;
            fillImage = fill;
        }

        private void Awake()
        {
            if (container != null)
            {
                container.SetActive(false);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            lerpRoutine = null;
        }

        /// <summary>
        /// Activates the Boss health bar with specified boss name and initial health.
        /// Immediately cancels any pending fade out routines.
        /// </summary>
        public void Show(string bossName, int maxHp)
        {
            StopAllCoroutines();
            lerpRoutine = null;

            currentMaxHp = Mathf.Max(1, maxHp);

            if (container != null)
            {
                container.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = bossName;
            }

            if (healthSlider != null)
            {
                healthSlider.maxValue = currentMaxHp;
                healthSlider.value = currentMaxHp;
            }

            if (fillImage != null)
            {
                fillImage.color = normalColor;
            }

            if (hpText != null)
            {
                hpText.text = $"{currentMaxHp} / {currentMaxHp}";
            }
        }

        /// <summary>
        /// Updates the current HP with smooth slider interpolation and phase color change.
        /// Ensures health bar container is visible.
        /// </summary>
        public void UpdateHealth(int currentHp, int maxHp, bool isEnraged = false)
        {
            if (container != null && !container.activeSelf)
            {
                container.SetActive(true);
            }

            currentMaxHp = Mathf.Max(1, maxHp);
            int displayHp = Mathf.Max(0, currentHp);

            if (hpText != null)
            {
                hpText.text = $"{displayHp} / {currentMaxHp}";
            }

            if (fillImage != null)
            {
                fillImage.color = isEnraged ? enragedColor : normalColor;
            }

            if (healthSlider != null)
            {
                if (lerpRoutine != null)
                {
                    StopCoroutine(lerpRoutine);
                }
                lerpRoutine = StartCoroutine(LerpSliderRoutine(displayHp));
            }
        }

        private IEnumerator LerpSliderRoutine(float targetValue)
        {
            float startValue = healthSlider.value;
            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                healthSlider.value = Mathf.Lerp(startValue, targetValue, elapsed / duration);
                yield return null;
            }

            healthSlider.value = targetValue;
            lerpRoutine = null;
        }

        /// <summary>
        /// Hides the boss health bar immediately without delay (for retries / resets).
        /// </summary>
        public void HideInstant()
        {
            StopAllCoroutines();
            lerpRoutine = null;
            if (container != null)
            {
                container.SetActive(false);
            }
        }

        /// <summary>
        /// Hides the boss health bar after a brief victory delay.
        /// </summary>
        public void Hide()
        {
            StopAllCoroutines();
            lerpRoutine = null;
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            yield return new WaitForSeconds(1.2f);
            if (container != null)
            {
                container.SetActive(false);
            }
        }
    }
}
