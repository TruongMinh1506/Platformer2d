using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Platformer.Common
{
    /// <summary>
    /// Animates a 2D Light with a smooth, pulsing breathing cycle.
    /// Perfect for magical crystals, power-ups, and enchanted relics.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class PulsingLight2D : MonoBehaviour
    {
        [Tooltip("Minimum light intensity during pulse.")]
        [SerializeField] private float minIntensity = 0.6f;

        [Tooltip("Maximum light intensity during pulse.")]
        [SerializeField] private float maxIntensity = 1.4f;

        [Tooltip("Speed/frequency of the pulse cycle.")]
        [SerializeField] private float pulseSpeed = 2.0f;

        private Light2D light2D;
        private float seed;

        private void Awake()
        {
            light2D = GetComponent<Light2D>();
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (light2D == null) return;

            float t = (Mathf.Sin((Time.time + seed) * pulseSpeed) + 1f) * 0.5f;
            light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }
    }
}
