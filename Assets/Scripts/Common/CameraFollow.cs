using UnityEngine;

namespace Platformer.Common
{
    /// <summary>
    /// Smoothly follows a target transform (usually the player) using Vector3.SmoothDamp,
    /// with optional min/max horizontal and vertical clamping bounds,
    /// and built-in screen shake capability.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        [Header("Target & Offset")]
        [Tooltip("Target transform to follow.")]
        [SerializeField] private Transform target;

        [Tooltip("Positional offset relative to the target.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);

        [Tooltip("Approximately the time it takes to reach the target.")]
        [SerializeField] private float smoothTime = 0.15f;

        [Header("Clamping Bounds (Optional)")]
        [Tooltip("Enable to restrict camera movement within specific coordinate bounds.")]
        [SerializeField] private bool useBounds = false;

        [Tooltip("Minimum X and Y boundaries for the camera.")]
        [SerializeField] private Vector2 minBounds = new Vector2(-20f, -5f);

        [Tooltip("Maximum X and Y boundaries for the camera.")]
        [SerializeField] private Vector2 maxBounds = new Vector2(20f, 15f);

        public Vector2 MinBounds
        {
            get => minBounds;
            set => minBounds = value;
        }

        public Vector2 MaxBounds
        {
            get => maxBounds;
            set => maxBounds = value;
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            useBounds = true;
        }

        private Vector3 currentVelocity = Vector3.zero;

        // Screen shake state
        private float shakeDuration = 0f;
        private float shakeTimer = 0f;
        private float shakeIntensity = 0f;
        private Vector2 shakeDirection = Vector2.zero;

        // Camera Zoom state
        private Camera cam;
        private float defaultOrthoSize = 7.5f;
        private float targetOrthoSize = 7.5f;
        private float zoomSpeed = 5f;
        private float zoomHoldTimer = 0f;
        private bool isZooming = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                defaultOrthoSize = cam.orthographicSize;
                targetOrthoSize = defaultOrthoSize;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Explicitly set or switch the follow target at runtime.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Triggers a screen shake effect with decaying intensity.
        /// </summary>
        public void TriggerShake(float duration = 0.2f, float intensity = 0.3f)
        {
            if (intensity > shakeIntensity || shakeTimer <= 0f)
            {
                shakeDuration = duration;
                shakeTimer = duration;
                shakeIntensity = intensity;
                shakeDirection = Vector2.zero;
            }
        }

        /// <summary>
        /// Triggers a directional camera kick/shake along a specified vector.
        /// </summary>
        public void TriggerDirectionalShake(Vector2 direction, float duration = 0.18f, float intensity = 0.4f)
        {
            shakeDuration = duration;
            shakeTimer = duration;
            shakeIntensity = intensity;
            shakeDirection = direction.normalized;
        }

        /// <summary>
        /// Smoothly zooms the camera to a target orthographic size, holds for a duration, then zooms back.
        /// </summary>
        public void ZoomTo(float targetSize, float duration, float holdTime = 0.5f)
        {
            targetOrthoSize = targetSize;
            zoomHoldTimer = holdTime;
            zoomSpeed = duration > 0f ? Mathf.Abs(defaultOrthoSize - targetSize) / duration : 10f;
            isZooming = true;
        }

        /// <summary>
        /// Static convenience method to trigger camera shake from anywhere.
        /// </summary>
        public static void Shake(float duration = 0.2f, float intensity = 0.3f)
        {
            if (Instance != null)
            {
                Instance.TriggerShake(duration, intensity);
            }
        }

        /// <summary>
        /// Static convenience method to trigger directional camera shake.
        /// </summary>
        public static void DirectionalShake(Vector2 direction, float duration = 0.18f, float intensity = 0.4f)
        {
            if (Instance != null)
            {
                Instance.TriggerDirectionalShake(direction, duration, intensity);
            }
        }

        /// <summary>
        /// Static convenience method to trigger cinematic camera zoom.
        /// </summary>
        public static void CinematicZoom(float targetSize, float duration, float holdTime = 0.5f)
        {
            if (Instance != null)
            {
                Instance.ZoomTo(targetSize, duration, holdTime);
            }
        }

        private void Start()
        {
            // Auto-locate player if not assigned in Inspector
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

            // Optional bounds clamping in world coordinates
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            }

            // Apply screen shake offset (supports directional impulse + omnidirectional noise)
            if (shakeTimer > 0f)
            {
                float progress = shakeDuration > 0f ? (shakeTimer / shakeDuration) : 0f;
                Vector2 randomOffset = Random.insideUnitCircle * (shakeIntensity * progress * 0.4f);
                Vector2 dirOffset = shakeDirection * (shakeIntensity * progress);
                desiredPosition += new Vector3(randomOffset.x + dirOffset.x, randomOffset.y + dirOffset.y, 0f);
                shakeTimer -= Time.unscaledDeltaTime;
            }

            // Smoothly interpolate towards target position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

            // Smoothly handle camera zoom
            if (isZooming && cam != null)
            {
                if (Mathf.Abs(cam.orthographicSize - targetOrthoSize) > 0.05f)
                {
                    cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, targetOrthoSize, zoomSpeed * Time.unscaledDeltaTime);
                }
                else
                {
                    zoomHoldTimer -= Time.unscaledDeltaTime;
                    if (zoomHoldTimer <= 0f)
                    {
                        // Return to default
                        targetOrthoSize = defaultOrthoSize;
                        if (Mathf.Abs(cam.orthographicSize - defaultOrthoSize) < 0.05f)
                        {
                            cam.orthographicSize = defaultOrthoSize;
                            isZooming = false;
                        }
                    }
                }
            }
        }
    }
}

