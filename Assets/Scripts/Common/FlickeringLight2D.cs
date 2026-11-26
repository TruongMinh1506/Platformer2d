using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Platformer.Common
{
    /// <summary>
    /// Animates a 2D Light with organic flickering using Perlin noise.
    /// Ideal for torches, braziers, lava pits, and campfire lighting.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class FlickeringLight2D : MonoBehaviour
    {
        [Tooltip("Base light intensity.")]
        [SerializeField] private float baseIntensity = 1.0f;

        [Tooltip("Intensity variance range.")]
        [SerializeField] private float flickerRange = 0.25f;

        [Tooltip("Speed of light flicker.")]
        [SerializeField] private float flickerSpeed = 7.5f;

        private Light2D light2D;
        private float noiseOffset;

        private void Awake()
        {
            light2D = GetComponent<Light2D>();
            noiseOffset = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            if (light2D == null) return;

            float noise = Mathf.PerlinNoise((Time.time * flickerSpeed) + noiseOffset, 0f);
            light2D.intensity = baseIntensity + (noise - 0.5f) * flickerRange * 2f;
        }
    }
}
