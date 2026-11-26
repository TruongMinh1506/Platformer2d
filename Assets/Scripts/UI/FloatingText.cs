using UnityEngine;
using TMPro;

namespace Platformer.UI
{
    /// <summary>
    /// Animates a world-space floating combat text (damage, critical, parry, deflect).
    /// Pops up with a bounce, floats upward, and fades out.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private float floatSpeed = 2.2f;
        [SerializeField] private float lifetime = 0.75f;
        [SerializeField] private Vector2 horizontalDrift = new Vector2(-0.4f, 0.4f);

        private TextMeshPro tmp;
        private float timer = 0f;
        private Vector3 moveDirection;
        private Color initialColor;

        public void Initialize(string message, Color color, float scaleMultiplier = 1f)
        {
            tmp = GetComponent<TextMeshPro>();
            if (tmp == null)
            {
                tmp = gameObject.AddComponent<TextMeshPro>();
            }

            tmp.text = message;
            tmp.color = color;
            initialColor = color;
            tmp.fontSize = 5.5f * scaleMultiplier;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 100;

            float randomX = Random.Range(horizontalDrift.x, horizontalDrift.y);
            moveDirection = new Vector3(randomX, 1f, 0f).normalized;

            transform.localScale = Vector3.one * 0.4f;
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / lifetime;

            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Move upward with subtle drift
            transform.position += moveDirection * (floatSpeed * (1f - progress * 0.5f) * Time.unscaledDeltaTime);

            // Scale bounce: 0.4 -> 1.25 -> 1.0
            float scale;
            if (progress < 0.2f)
            {
                scale = Mathf.Lerp(0.4f, 1.25f, progress / 0.2f);
            }
            else if (progress < 0.4f)
            {
                scale = Mathf.Lerp(1.25f, 1.0f, (progress - 0.2f) / 0.2f);
            }
            else
            {
                scale = Mathf.Lerp(1.0f, 0.7f, (progress - 0.4f) / 0.6f);
            }
            transform.localScale = Vector3.one * scale;

            // Fade out
            if (tmp != null && progress > 0.45f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (progress - 0.45f) / 0.55f);
                tmp.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            }
        }
    }
}
