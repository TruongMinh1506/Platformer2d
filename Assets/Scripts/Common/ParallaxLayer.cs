using UnityEngine;

namespace Platformer.Common
{
    /// <summary>
    /// Smooth parallax scrolling layer for 2D platformers.
    /// Moves relative to the Main Camera with seamless horizontal wrapping
    /// to guarantee infinite scrolling with zero visual gaps or pop-in.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Rates")]
        [Tooltip("Horizontal parallax multiplier. 0 = stationary in world, 1 = moves 1:1 with camera (skybox). Values between 0.3 - 0.85 give great depth.")]
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactorX = 0.5f;

        [Tooltip("Vertical parallax multiplier. Keep low (0 - 0.1) so jumping does not violently shift background.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float parallaxFactorY = 0.05f;

        [Tooltip("Constant auto-scrolling speed on X axis (for drifting clouds, rolling smoke, ambient mist).")]
        [SerializeField] private float autoScrollSpeedX = 0f;

        [Header("Seamless Tiling")]
        [Tooltip("Width of one repeating segment in world units.")]
        [SerializeField] private float unitWidth = 32f;

        [Tooltip("Enable seamless infinite horizontal wrapping.")]
        [SerializeField] private bool infiniteHorizontal = true;

        private Transform camTransform;
        private Vector3 startCamPos;
        private Vector3 startLayerPos;
        private float autoScrollOffset = 0f;

        private void Start()
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
                startCamPos = camTransform.position;
            }
            startLayerPos = transform.position;

            // Auto-detect unit width if not set
            if (unitWidth <= 0.01f)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    unitWidth = sr.bounds.size.x;
                }
                else
                {
                    unitWidth = 32f;
                }
            }
        }

        private void LateUpdate()
        {
            if (camTransform == null)
            {
                if (Camera.main != null)
                {
                    camTransform = Camera.main.transform;
                    startCamPos = camTransform.position;
                }
                else return;
            }

            if (autoScrollSpeedX != 0f)
            {
                autoScrollOffset += autoScrollSpeedX * Time.deltaTime;
            }

            Vector3 camDelta = camTransform.position - startCamPos;

            float targetX = startLayerPos.x + (camDelta.x * parallaxFactorX) + autoScrollOffset;
            float targetY = startLayerPos.y + (camDelta.y * parallaxFactorY);

            if (infiniteHorizontal && unitWidth > 0.1f)
            {
                float relativeDistance = camTransform.position.x - targetX;

                while (relativeDistance > unitWidth * 0.5f)
                {
                    startLayerPos.x += unitWidth;
                    targetX += unitWidth;
                    relativeDistance = camTransform.position.x - targetX;
                }
                while (relativeDistance < -unitWidth * 0.5f)
                {
                    startLayerPos.x -= unitWidth;
                    targetX -= unitWidth;
                    relativeDistance = camTransform.position.x - targetX;
                }
            }

            transform.position = new Vector3(targetX, targetY, transform.position.z);
        }

        public void Configure(float factorX, float factorY, float width, float autoScroll = 0f)
        {
            parallaxFactorX = factorX;
            parallaxFactorY = factorY;
            unitWidth = width;
            autoScrollSpeedX = autoScroll;
            infiniteHorizontal = true;
        }
    }
}
