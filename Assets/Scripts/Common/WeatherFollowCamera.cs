using UnityEngine;

namespace Platformer.Common
{
    /// <summary>
    /// Follows the Main Camera's viewport in LateUpdate to ensure atmospheric weather
    /// particles (leaves, spores, embers) remain centered around the active player view
    /// across wide scrolling levels without spawning unnecessary offscreen particles.
    /// </summary>
    public class WeatherFollowCamera : MonoBehaviour
    {
        [Tooltip("Positional offset relative to the camera viewport.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, 10f);

        [Tooltip("Lock vertical following to keep particles anchored to a fixed height if desired.")]
        [SerializeField] private bool followY = true;

        private Transform camTransform;
        private float initialY;

        private void Awake()
        {
            // Ensure atmospheric weather particles always render in the background (sortingOrder -2)
            // behind tilemaps (0) and entities (5), preventing any occlusion of player or enemies
            var renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in renderers)
            {
                r.sortingOrder = -2;
            }

            // Gently tame particle density and size for clean, unhindered visibility
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var emission = ps.emission;
                if (emission.rateOverTime.constant > 8f)
                {
                    emission.rateOverTime = Mathf.Min(emission.rateOverTime.constant * 0.35f, 7f);
                }
                var main = ps.main;
                if (main.startSizeMultiplier > 0.7f)
                {
                    main.startSizeMultiplier = 0.6f;
                }
            }
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }
            initialY = transform.position.y;
        }

        private void LateUpdate()
        {
            if (camTransform == null)
            {
                if (Camera.main != null)
                {
                    camTransform = Camera.main.transform;
                }
                else return;
            }

            float posX = camTransform.position.x + offset.x;
            float posY = followY ? (camTransform.position.y + offset.y) : initialY;
            transform.position = new Vector3(posX, posY, offset.z);
        }
    }
}
