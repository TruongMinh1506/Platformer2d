using System.Collections;
using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// Visual telegraph marker placed on the ground warning the player of an incoming lava fireball.
    /// Blinks rapidly with pulsing transparency before disappearing as the fireball lands.
    /// </summary>
    public class BossTelegraph : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float duration = 0.85f;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float elapsed = 0f;
            Color baseColor = new Color(1f, 0.22f, 0.15f, 0.85f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pulse = (Mathf.Sin(elapsed * 22f) + 1f) * 0.5f;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f + pulse * 0.65f);
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
