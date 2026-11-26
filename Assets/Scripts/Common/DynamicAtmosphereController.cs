using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Platformer.Common
{
    /// <summary>
    /// Procedural atmospheric lighting and particulate controller:
    /// - Level 1: Translucent 2D God Rays drifting with wind sway.
    /// - Level 2: Bioluminescent crystal point lights & spore ambience.
    /// - Level 3: Heat breathing amber lava glow & volcanic ash ambience.
    /// </summary>
    public class DynamicAtmosphereController : MonoBehaviour
    {
        [Header("God Rays (Level 1)")]
        [SerializeField] private bool enableGodRays = true;
        [SerializeField] private int rayCount = 4;
        [SerializeField] private Color godRayColor = new Color(1.0f, 0.95f, 0.72f, 0.12f);

        [Header("Crystal Ambient (Level 2)")]
        [SerializeField] private bool enableCrystalLights = true;

        [Header("Lava Breathing (Level 3)")]
        [SerializeField] private bool enableLavaBreathing = true;
        [SerializeField] private Color lavaGlowColor = new Color(1.0f, 0.28f, 0.05f, 0.22f);

        private Transform camTransform;
        private Transform godRaysRoot;
        private Transform crystalLightsRoot;
        private Light2D crystalLight2D;
        private SpriteRenderer lavaGlowRenderer;
        private float timeAccumulator;

        private void Start()
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.Contains("Level1") || sceneName.Contains("MainMenu"))
            {
                SetupLevel1Atmosphere();
            }
            else if (sceneName.Contains("Level2"))
            {
                SetupLevel2Atmosphere();
            }
            else if (sceneName.Contains("Level3") || sceneName.Contains("BossRush"))
            {
                SetupLevel3Atmosphere();
            }
        }

        private void Update()
        {
            timeAccumulator += Time.deltaTime;

            if (camTransform != null)
            {
                transform.position = new Vector3(camTransform.position.x, camTransform.position.y, 0f);
            }

            // Animate God Rays without cumulative drift
            if (godRaysRoot != null)
            {
                float[] xOffsets = new float[] { -6f, -2f, 2.5f, 7f };
                for (int i = 0; i < godRaysRoot.childCount; i++)
                {
                    Transform ray = godRaysRoot.GetChild(i);
                    float offset = i * 1.5f;
                    float alpha = Mathf.PingPong((timeAccumulator + offset) * 0.35f, 0.12f) + 0.04f;
                    var sr = ray.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = new Color(godRayColor.r, godRayColor.g, godRayColor.b, alpha);
                    }
                    float baseX = xOffsets[i % xOffsets.Length];
                    ray.localPosition = new Vector3(baseX + Mathf.Sin(timeAccumulator * 0.5f + offset) * 0.45f, 2f, 0f);
                }
            }

            // Animate Crystal Light Glow
            if (crystalLight2D != null)
            {
                crystalLight2D.intensity = 0.4f + Mathf.PingPong(timeAccumulator * 0.6f, 0.2f);
            }

            // Animate Lava Breathing
            if (lavaGlowRenderer != null)
            {
                float breathe = Mathf.PingPong(timeAccumulator * 0.8f, 0.15f) + 0.1f;
                lavaGlowRenderer.color = new Color(lavaGlowColor.r, lavaGlowColor.g, lavaGlowColor.b, breathe);
            }
        }

        private void SetupLevel1Atmosphere()
        {
            if (!enableGodRays) return;

            godRaysRoot = new GameObject("GodRaysRoot").transform;
            godRaysRoot.SetParent(transform, false);

            Texture2D rayTex = CreateGodRayTexture();
            Sprite raySprite = Sprite.Create(rayTex, new Rect(0, 0, rayTex.width, rayTex.height), new Vector2(0.5f, 0.5f), 100f);

            float[] xOffsets = new float[] { -6f, -2f, 2.5f, 7f };
            for (int i = 0; i < rayCount; i++)
            {
                GameObject rayGo = new GameObject($"GodRay_{i + 1}");
                rayGo.transform.SetParent(godRaysRoot, false);
                rayGo.transform.localPosition = new Vector3(xOffsets[i % xOffsets.Length], 2f, 0f);
                rayGo.transform.localRotation = Quaternion.Euler(0f, 0f, -28f); // Angled sun rays
                rayGo.transform.localScale = new Vector3(2.5f, 16f, 1f);

                SpriteRenderer sr = rayGo.AddComponent<SpriteRenderer>();
                sr.sprite = raySprite;
                sr.color = godRayColor;
                sr.sortingOrder = 5; // Foreground atmospheric overlay
            }
        }

        private void SetupLevel2Atmosphere()
        {
            if (!enableCrystalLights) return;

            crystalLightsRoot = new GameObject("CrystalLightsRoot").transform;
            crystalLightsRoot.SetParent(transform, false);

            // Add subtle ambient pulsating 2D light
            GameObject lightGo = new GameObject("AmbientSporeGlow");
            lightGo.transform.SetParent(crystalLightsRoot, false);
            lightGo.transform.localPosition = Vector3.zero;

            var light2D = lightGo.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Point;
            light2D.pointLightInnerRadius = 2.5f;
            light2D.pointLightOuterRadius = 14f;
            light2D.color = new Color(0.2f, 0.8f, 1.0f, 1f);
            light2D.intensity = 0.45f;
            crystalLight2D = light2D;
        }

        private void SetupLevel3Atmosphere()
        {
            if (!enableLavaBreathing) return;

            GameObject lavaGlowGo = new GameObject("LavaHeatBreathing");
            lavaGlowGo.transform.SetParent(transform, false);
            lavaGlowGo.transform.localPosition = new Vector3(0f, -5f, 0f); // Bottom of screen
            lavaGlowGo.transform.localScale = new Vector3(32f, 6.5f, 1f);

            Texture2D gradTex = CreateLavaGlowTexture();
            Sprite gradSprite = Sprite.Create(gradTex, new Rect(0, 0, gradTex.width, gradTex.height), new Vector2(0.5f, 0.5f), 100f);

            lavaGlowRenderer = lavaGlowGo.AddComponent<SpriteRenderer>();
            lavaGlowRenderer.sprite = gradSprite;
            lavaGlowRenderer.color = lavaGlowColor;
            lavaGlowRenderer.sortingOrder = 6;
        }

        private Texture2D CreateGodRayTexture()
        {
            int width = 32;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float verticalFade = Mathf.Sin(v * Mathf.PI);

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float horizontalFade = Mathf.Sin(u * Mathf.PI);
                    float alpha = verticalFade * horizontalFade;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D CreateLavaGlowTexture()
        {
            int width = 16;
            int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float alpha = Mathf.Pow(1f - v, 1.8f); // Strong at bottom, fades towards top

                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 0.4f, 0.1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
