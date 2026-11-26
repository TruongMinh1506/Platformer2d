using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Common;
using Platformer.Managers;

namespace Platformer.UI
{
    /// <summary>
    /// Interactive 2D Minimap and Secret Radar HUD:
    /// - Tracks real-time player position relative to level bounds.
    /// - Displays checkpoints, collectibles, and boss arenas.
    /// - Emits secret radar pulse when approaching cracked secret walls!
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [Header("Minimap Canvas Elements")]
        [SerializeField] private RectTransform mapFrame;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private TextMeshProUGUI secretRadarText;

        [Header("Level Bounds & Scaling")]
        [SerializeField] private Vector2 levelMin = new Vector2(0f, 0f);
        [SerializeField] private Vector2 levelMax = new Vector2(120f, 20f);

        [Header("Secret Radar")]
        [SerializeField] private float secretDetectionRange = 7.5f;
        [SerializeField] private float radarPingInterval = 2.0f;

        private Transform playerTransform;
        private List<BreakableSecretWall> secretWalls = new List<BreakableSecretWall>();
        private float radarTimer = 0f;
        private bool isExpanded = false;
        private Vector2 defaultFrameSize = new Vector2(180f, 100f);
        private Vector2 expandedFrameSize = new Vector2(360f, 200f);

        private void Start()
        {
            LocatePlayer();
            FindSecretWalls();

            if (mapFrame != null)
            {
                mapFrame.sizeDelta = defaultFrameSize;
            }
        }

        public void SetLevelBounds(Vector2 min, Vector2 max)
        {
            levelMin = min;
            levelMax = max;
        }

        private void LocatePlayer()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void FindSecretWalls()
        {
            secretWalls.Clear();
            var walls = FindObjectsByType<BreakableSecretWall>(FindObjectsSortMode.None);
            secretWalls.AddRange(walls);
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                LocatePlayer();
                if (playerTransform == null) return;
            }

            // Toggle enlarged minimap with M or Tab
            bool toggleMinimap = false;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.mKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
            {
                toggleMinimap = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
            {
                toggleMinimap = true;
            }
#endif
            if (toggleMinimap)
            {
                ToggleMinimapSize();
            }

            // Update player marker on minimap frame
            if (playerMarker != null && mapFrame != null)
            {
                float normalizedX = Mathf.InverseLerp(levelMin.x, levelMax.x, playerTransform.position.x);
                float normalizedY = Mathf.InverseLerp(levelMin.y, levelMax.y, playerTransform.position.y);

                float frameW = mapFrame.rect.width;
                float frameH = mapFrame.rect.height;

                float markerX = (normalizedX - 0.5f) * frameW;
                float markerY = (normalizedY - 0.5f) * frameH;

                playerMarker.anchoredPosition = new Vector2(markerX, markerY);
            }

            // Secret Wall Radar detection
            UpdateSecretRadar();
        }

        private void UpdateSecretRadar()
        {
            if (secretWalls.Count == 0 || playerTransform == null)
            {
                if (secretRadarText != null) secretRadarText.gameObject.SetActive(false);
                return;
            }

            bool foundNear = false;
            foreach (var wall in secretWalls)
            {
                if (wall != null && wall.gameObject.activeInHierarchy)
                {
                    float dist = Vector2.Distance(playerTransform.position, wall.transform.position);
                    if (dist <= secretDetectionRange)
                    {
                        foundNear = true;
                        break;
                    }
                }
            }

            if (foundNear)
            {
                if (secretRadarText != null) secretRadarText.gameObject.SetActive(true);

                radarTimer -= Time.deltaTime;
                if (radarTimer <= 0f)
                {
                    radarTimer = radarPingInterval;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySound("secret_radar", 0.65f);
                    }
                }
            }
            else
            {
                if (secretRadarText != null) secretRadarText.gameObject.SetActive(false);
                radarTimer = 0f;
            }
        }

        private void ToggleMinimapSize()
        {
            isExpanded = !isExpanded;
            if (mapFrame != null)
            {
                mapFrame.sizeDelta = isExpanded ? expandedFrameSize : defaultFrameSize;
            }
        }
    }
}
