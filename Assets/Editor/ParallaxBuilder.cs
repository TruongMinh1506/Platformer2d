using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Platformer.Common;

namespace Platformer.Editor
{
    /// <summary>
    /// Generates multi-layer parallax background textures, layer prefabs,
    /// and atmospheric weather particle systems for Levels 1, 2, and 3.
    /// </summary>
    public static class ParallaxBuilder
    {
        private const string ParallaxArtDir = "Assets/Art/Parallax";
        private const string MaterialsDir = "Assets/Art/Materials";
        private const string PrefabDir = "Assets/Prefabs";

        public const string WeatherL1PrefabPath = "Assets/Prefabs/Weather_Level1_Forest.prefab";
        public const string WeatherL2PrefabPath = "Assets/Prefabs/Weather_Level2_CrystalCavern.prefab";
        public const string WeatherL3PrefabPath = "Assets/Prefabs/Weather_Level3_Volcano.prefab";

        [MenuItem("Tools/Platformer/Parallax/Generate All Parallax & Weather Assets", priority = 20)]
        public static void GenerateAllAssets()
        {
            EnsureDirectories();
            EnsureMaterials();
            GenerateAllTextures();
            GenerateWeatherPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#2ecc71>[ParallaxBuilder]</color> All Parallax Textures and Weather Prefabs created successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(ParallaxArtDir)) Directory.CreateDirectory(ParallaxArtDir);
            if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        private static Material particleMaterial;
        private static void EnsureMaterials()
        {
            string matPath = $"{MaterialsDir}/Particle_Default.mat";
            particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (particleMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                particleMaterial = new Material(shader);
                particleMaterial.name = "Particle_Default";
                AssetDatabase.CreateAsset(particleMaterial, matPath);
            }
        }

        #region Texture Generation

        private static void GenerateAllTextures()
        {
            // Level 1: Verdant Forest & Mountain Plains
            CreateTexture($"{ParallaxArtDir}/BG_L1_MountainsFar.png", 512, 256, DrawL1MountainsFar);
            CreateTexture($"{ParallaxArtDir}/BG_L1_HillsMid.png", 512, 256, DrawL1HillsMid);
            CreateTexture($"{ParallaxArtDir}/BG_L1_CloudsNear.png", 512, 256, DrawL1CloudsNear);

            // Level 2: Subterranean Crystal Cavern
            CreateTexture($"{ParallaxArtDir}/BG_L2_CaveFar.png", 512, 256, DrawL2CaveFar);
            CreateTexture($"{ParallaxArtDir}/BG_L2_CrystalsMid.png", 512, 256, DrawL2CrystalsMid);
            CreateTexture($"{ParallaxArtDir}/BG_L2_MistNear.png", 512, 256, DrawL2MistNear);

            // Level 3: Volcanic Sky Fortress
            CreateTexture($"{ParallaxArtDir}/BG_L3_VolcanoFar.png", 512, 256, DrawL3VolcanoFar);
            CreateTexture($"{ParallaxArtDir}/BG_L3_FortressMid.png", 512, 256, DrawL3FortressMid);
            CreateTexture($"{ParallaxArtDir}/BG_L3_SmokeNear.png", 512, 256, DrawL3SmokeNear);

            AssetDatabase.Refresh();
        }

        private static void CreateTexture(string path, int width, int height, Action<Texture2D, int, int> drawAction)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            // Fill clear
            Color[] clearColors = new Color[width * height];
            for (int i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
            tex.SetPixels(clearColors);

            drawAction(tex, width, height);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f; // 512 px = 32 world units
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        #region Level 1 Procedural Art
        private static void DrawL1MountainsFar(Texture2D tex, int w, int h)
        {
            // Sky gradient from top to horizon
            Color skyTop = new Color(0.42f, 0.65f, 0.95f, 1f);
            Color skyBottom = new Color(0.78f, 0.88f, 1.0f, 1f);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float t = (float)y / h;
                    tex.SetPixel(x, y, Color.Lerp(skyBottom, skyTop, t));
                }
            }

            // Mountain ridge 1 (Back, Pale misty blue)
            Color ridge1Color = new Color(0.52f, 0.62f, 0.82f, 0.95f);
            Color ridge1Shade = new Color(0.42f, 0.52f, 0.74f, 0.95f);
            Color snowColor = new Color(0.95f, 0.98f, 1.0f, 0.95f);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w * Mathf.PI * 2f;
                float heightFraction = 0.52f + 0.16f * Mathf.Sin(nx * 2f) + 0.08f * Mathf.Cos(nx * 5f) + 0.04f * Mathf.Sin(nx * 9f);
                int ridgeY = Mathf.RoundToInt(heightFraction * h);

                for (int y = 0; y < ridgeY; y++)
                {
                    bool isEastSlope = Mathf.Cos(nx * 2f) > 0;
                    Color c = isEastSlope ? ridge1Shade : ridge1Color;
                    if (y > ridgeY - 18 && heightFraction > 0.60f)
                    {
                        c = snowColor; // Snow peak
                    }
                    // Atmospheric base haze
                    float fade = Mathf.Clamp01((float)y / (h * 0.45f));
                    tex.SetPixel(x, y, Color.Lerp(skyBottom, c, fade * 0.9f + 0.1f));
                }
            }

            // Mountain ridge 2 (Front, Deeper slate blue-indigo)
            Color ridge2Color = new Color(0.32f, 0.44f, 0.66f, 1f);
            Color ridge2Shade = new Color(0.24f, 0.35f, 0.56f, 1f);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w * Mathf.PI * 2f;
                float heightFraction = 0.36f + 0.12f * Mathf.Sin(nx * 3f + 1.2f) + 0.06f * Mathf.Cos(nx * 7f);
                int ridgeY = Mathf.RoundToInt(heightFraction * h);

                for (int y = 0; y < ridgeY; y++)
                {
                    bool isEastSlope = Mathf.Cos(nx * 3f + 1.2f) > 0;
                    Color c = isEastSlope ? ridge2Shade : ridge2Color;
                    float fade = Mathf.Clamp01((float)y / (h * 0.35f));
                    tex.SetPixel(x, y, Color.Lerp(skyBottom, c, fade * 0.85f + 0.15f));
                }
            }
        }

        private static void DrawL1HillsMid(Texture2D tex, int w, int h)
        {
            // Rolling green hills with pine & oak silhouettes
            Color hillBase = new Color(0.18f, 0.44f, 0.22f, 1f);
            Color hillHighlight = new Color(0.32f, 0.65f, 0.32f, 1f);
            Color hillShadow = new Color(0.12f, 0.32f, 0.16f, 1f);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w * Mathf.PI * 2f;
                float hillHeight = 0.32f + 0.12f * Mathf.Sin(nx * 2f) + 0.06f * Mathf.Cos(nx * 4f);
                int topY = Mathf.RoundToInt(hillHeight * h);

                for (int y = 0; y < topY; y++)
                {
                    float factor = (float)y / topY;
                    Color c = Color.Lerp(hillShadow, hillBase, factor);
                    if (y >= topY - 3) c = hillHighlight; // Sunny ridge rim
                    tex.SetPixel(x, y, c);
                }

                // Add stylized pine tree silhouettes along hills
                if (x % 38 >= 14 && x % 38 <= 24)
                {
                    int treeCenter = (x / 38) * 38 + 19;
                    int dist = Mathf.Abs(x - treeCenter);
                    int treeHeight = 22 - dist * 4;
                    for (int ty = topY; ty < topY + treeHeight && ty < h; ty++)
                    {
                        tex.SetPixel(x, ty, new Color(0.12f, 0.28f, 0.14f, 0.95f));
                    }
                }
            }
        }

        private static void DrawL1CloudsNear(Texture2D tex, int w, int h)
        {
            // Beautiful stylized fluffy cumulus clouds on transparent background
            DrawPuffyCloud(tex, 100, 195, 55, 26);
            DrawPuffyCloud(tex, 280, 215, 65, 30);
            DrawPuffyCloud(tex, 440, 185, 50, 24);
        }

        private static void DrawPuffyCloud(Texture2D tex, int cx, int cy, int rx, int ry)
        {
            Color cloudTop = new Color(1f, 1f, 1f, 0.88f);
            Color cloudBottom = new Color(0.76f, 0.82f, 0.94f, 0.82f);
            int w = tex.width;

            for (int dx = -rx; dx <= rx; dx++)
            {
                for (int dy = -ry; dy <= ry; dy++)
                {
                    float distNorm = ((float)(dx * dx) / (rx * rx)) + ((float)(dy * dy) / (ry * ry));
                    if (distNorm <= 1f)
                    {
                        int px = (cx + dx + w) % w;
                        int py = cy + dy;
                        if (py >= 0 && py < tex.height)
                        {
                            float verticalGrad = (float)(dy + ry) / (ry * 2);
                            float edgeFade = 1f - distNorm;
                            Color c = Color.Lerp(cloudBottom, cloudTop, verticalGrad);
                            c.a *= Mathf.Clamp01(edgeFade * 3f);

                            Color existing = tex.GetPixel(px, py);
                            tex.SetPixel(px, py, BlendAlpha(existing, c));
                        }
                    }
                }
            }
        }
        #endregion

        #region Level 2 Procedural Art
        private static void DrawL2CaveFar(Texture2D tex, int w, int h)
        {
            // Dark abyss subterranean background
            Color deepAbyss = new Color(0.04f, 0.05f, 0.09f, 1f);
            Color stoneDark = new Color(0.10f, 0.12f, 0.18f, 1f);
            Color cyanGlow = new Color(0.08f, 0.32f, 0.44f, 1f);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    tex.SetPixel(x, y, deepAbyss);
                }
            }

            // Hanging Stalactites from ceiling
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w * Mathf.PI * 2f;
                float stalactiteLen = 0.25f + 0.12f * Mathf.Sin(nx * 4f) + 0.08f * Mathf.Cos(nx * 9f);
                int bottomY = h - Mathf.RoundToInt(stalactiteLen * h);

                for (int y = bottomY; y < h; y++)
                {
                    tex.SetPixel(x, y, stoneDark);
                }
            }

            // Rising Stalagmites from floor
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w * Mathf.PI * 2f;
                float stalagmiteLen = 0.22f + 0.10f * Mathf.Cos(nx * 3f) + 0.07f * Mathf.Sin(nx * 8f);
                int topY = Mathf.RoundToInt(stalagmiteLen * h);

                for (int y = 0; y < topY; y++)
                {
                    float factor = (float)y / topY;
                    tex.SetPixel(x, y, Color.Lerp(stoneDark, cyanGlow, factor * 0.35f));
                }
            }
        }

        private static void DrawL2CrystalsMid(Texture2D tex, int w, int h)
        {
            // Glowing cyan & amethyst crystal spires and cavern stone pillars
            Color cyanLight = new Color(0.2f, 0.95f, 1.0f, 0.95f);
            Color cyanDark = new Color(0.05f, 0.55f, 0.75f, 0.9f);
            Color violetLight = new Color(0.85f, 0.35f, 1.0f, 0.95f);
            Color violetDark = new Color(0.45f, 0.15f, 0.65f, 0.9f);
            Color stonePillar = new Color(0.14f, 0.16f, 0.24f, 0.95f);

            // Cavern pillars
            int[] pillarXs = { 60, 190, 320, 450 };
            foreach (int px in pillarXs)
            {
                for (int x = px - 16; x <= px + 16; x++)
                {
                    int wx = (x + w) % w;
                    for (int y = 0; y < h; y++)
                    {
                        float edge = 1f - Mathf.Abs(x - px) / 16f;
                        tex.SetPixel(wx, y, Color.Lerp(stonePillar * 0.7f, stonePillar, edge));
                    }
                }
            }

            // Draw glowing crystals clustering on pillars and floor
            DrawCrystalSpire(tex, 60, 70, 22, 60, cyanLight, cyanDark);
            DrawCrystalSpire(tex, 80, 50, 16, 42, violetLight, violetDark);
            DrawCrystalSpire(tex, 190, 85, 26, 75, violetLight, violetDark);
            DrawCrystalSpire(tex, 215, 60, 18, 50, cyanLight, cyanDark);
            DrawCrystalSpire(tex, 320, 75, 24, 68, cyanLight, cyanDark);
            DrawCrystalSpire(tex, 450, 80, 25, 70, violetLight, violetDark);
            DrawCrystalSpire(tex, 475, 55, 18, 48, cyanLight, cyanDark);
        }

        private static void DrawCrystalSpire(Texture2D tex, int cx, int cy, int width, int height, Color bright, Color dark)
        {
            int w = tex.width;
            int halfW = width / 2;

            for (int dy = 0; dy < height; dy++)
            {
                float t = (float)dy / height;
                int currentWidth = Mathf.RoundToInt(halfW * (1f - t * 0.85f));
                for (int dx = -currentWidth; dx <= currentWidth; dx++)
                {
                    int px = (cx + dx + w) % w;
                    int py = cy + dy;
                    if (py >= 0 && py < tex.height)
                    {
                        Color c = dx < 0 ? bright : dark;
                        float facet = 1f - (Mathf.Abs(dx) / (float)(currentWidth + 1)) * 0.4f;
                        tex.SetPixel(px, py, c * facet);
                    }
                }
            }
        }

        private static void DrawL2MistNear(Texture2D tex, int w, int h)
        {
            // Soft bioluminescent fog ribbons
            Color mistCyan = new Color(0.15f, 0.85f, 0.95f, 0.22f);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w * Mathf.PI * 2f;
                int bandY1 = 80 + Mathf.RoundToInt(25f * Mathf.Sin(nx * 2f));
                int bandY2 = 150 + Mathf.RoundToInt(20f * Mathf.Cos(nx * 3f));

                for (int y = 0; y < h; y++)
                {
                    float d1 = Mathf.Abs(y - bandY1) / 35f;
                    float d2 = Mathf.Abs(y - bandY2) / 30f;
                    float alpha1 = d1 < 1f ? (1f - d1 * d1) * mistCyan.a : 0f;
                    float alpha2 = d2 < 1f ? (1f - d2 * d2) * (mistCyan.a * 0.8f) : 0f;
                    float totalAlpha = Mathf.Clamp01(alpha1 + alpha2);

                    if (totalAlpha > 0.01f)
                    {
                        Color c = mistCyan;
                        c.a = totalAlpha;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }
        #endregion

        #region Level 3 Procedural Art
        private static void DrawL3VolcanoFar(Texture2D tex, int w, int h)
        {
            // Smoldering dark blood-red volcanic sky
            Color skyTop = new Color(0.12f, 0.04f, 0.06f, 1f);
            Color skyBottom = new Color(0.38f, 0.12f, 0.08f, 1f);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float t = (float)y / h;
                    tex.SetPixel(x, y, Color.Lerp(skyBottom, skyTop, t));
                }
            }

            // Volcanic Peaks with molten cracks
            Color volcanoRock = new Color(0.16f, 0.08f, 0.08f, 1f);
            Color volcanoShade = new Color(0.10f, 0.05f, 0.05f, 1f);
            Color lavaGlow = new Color(1.0f, 0.55f, 0.10f, 1f);

            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w * Mathf.PI * 2f;
                float heightFraction = 0.54f + 0.22f * Mathf.Sin(nx * 2f) + 0.10f * Mathf.Cos(nx * 4f);
                int ridgeY = Mathf.RoundToInt(heightFraction * h);

                for (int y = 0; y < ridgeY; y++)
                {
                    bool isEast = Mathf.Cos(nx * 2f) > 0;
                    Color c = isEast ? volcanoShade : volcanoRock;

                    // Lava rivers cutting down the volcano
                    if (Mathf.Abs(x % 128 - 64) < 3 && y < ridgeY - 10)
                    {
                        c = lavaGlow;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawL3FortressMid(Texture2D tex, int w, int h)
        {
            // Obsidian fortress ramparts, battlements & spires
            Color fortDark = new Color(0.12f, 0.10f, 0.14f, 1f);
            Color fortLight = new Color(0.22f, 0.18f, 0.24f, 1f);
            Color windowLava = new Color(1.0f, 0.45f, 0.05f, 0.95f);

            for (int x = 0; x < w; x++)
            {
                int wallHeight = 85;
                // Towers
                if (x % 128 < 36) wallHeight = 145;
                // Crenellations (battlements)
                if (wallHeight == 85 && (x % 16 < 8)) wallHeight = 98;
                if (wallHeight == 145 && (x % 12 < 6)) wallHeight = 158;

                for (int y = 0; y < wallHeight; y++)
                {
                    Color c = (x % 32 < 16) ? fortLight : fortDark;

                    // Glowing furnace arrow slits / windows
                    if (y >= 105 && y <= 125 && (x % 128 >= 14 && x % 128 <= 22))
                    {
                        c = windowLava;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawL3SmokeNear(Texture2D tex, int w, int h)
        {
            // Dark billowing smoke clouds with ember tint
            DrawBillowingSmoke(tex, 120, 180, 70, 35);
            DrawBillowingSmoke(tex, 340, 200, 85, 40);
        }

        private static void DrawBillowingSmoke(Texture2D tex, int cx, int cy, int rx, int ry)
        {
            Color smokeDark = new Color(0.15f, 0.12f, 0.14f, 0.55f);
            Color smokeEmber = new Color(0.35f, 0.18f, 0.12f, 0.65f);
            int w = tex.width;

            for (int dx = -rx; dx <= rx; dx++)
            {
                for (int dy = -ry; dy <= ry; dy++)
                {
                    float distNorm = ((float)(dx * dx) / (rx * rx)) + ((float)(dy * dy) / (ry * ry));
                    if (distNorm <= 1f)
                    {
                        int px = (cx + dx + w) % w;
                        int py = cy + dy;
                        if (py >= 0 && py < tex.height)
                        {
                            float factor = 1f - distNorm;
                            Color c = Color.Lerp(smokeDark, smokeEmber, (float)(dy + ry) / (ry * 2f));
                            c.a *= factor;
                            Color existing = tex.GetPixel(px, py);
                            tex.SetPixel(px, py, BlendAlpha(existing, c));
                        }
                    }
                }
            }
        }
        #endregion

        private static Color BlendAlpha(Color under, Color over)
        {
            float a = over.a + under.a * (1f - over.a);
            if (a <= 0.001f) return Color.clear;
            Color rgb = (over * over.a + under * under.a * (1f - over.a)) / a;
            rgb.a = a;
            return rgb;
        }

        #endregion

        #region Weather Prefab Generation

        public static void GenerateWeatherPrefabs()
        {
            EnsureMaterials();
            BuildWeatherL1Prefab();
            BuildWeatherL2Prefab();
            BuildWeatherL3Prefab();
        }

        private static void BuildWeatherL1Prefab()
        {
            GameObject root = new GameObject("Weather_Level1_Forest");
            root.AddComponent<WeatherFollowCamera>();

            // Child 1: Falling Leaves
            GameObject leavesGo = new GameObject("FallingLeaves");
            leavesGo.transform.SetParent(root.transform);
            leavesGo.transform.localPosition = new Vector3(0f, 7.5f, 0f); // Top of camera

            ParticleSystem ps = leavesGo.AddComponent<ParticleSystem>();
            SetupParticleRenderer(leavesGo);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 7f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.20f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.simulationSpace = ParticleSystemSimulationSpace.World; // Naturally drifts in world space!

            var emission = ps.emission;
            emission.rateOverTime = 3.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 1f, 1f);
            shape.rotation = new Vector3(0f, 0f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.6f, 1.8f); // Gentle rightward wind
            vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -2.4f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f); // Match TwoConstants curve mode to prevent Unity error

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.35f, 0.78f, 0.32f), 0f), // Emerald green
                    new GradientColorKey(new Color(0.92f, 0.72f, 0.22f), 0.5f), // Autumn gold
                    new GradientColorKey(new Color(0.85f, 0.45f, 0.18f), 1f)  // Warm bronze
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.40f, 0.15f),
                    new GradientAlphaKey(0.40f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            // Child 2: Golden Dust Motes
            GameObject dustGo = new GameObject("GoldenDustMotes");
            dustGo.transform.SetParent(root.transform);
            dustGo.transform.localPosition = Vector3.zero;

            ParticleSystem psDust = dustGo.AddComponent<ParticleSystem>();
            SetupParticleRenderer(dustGo);

            var dustMain = psDust.main;
            dustMain.duration = 4f;
            dustMain.loop = true;
            dustMain.startLifetime = 5f;
            dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            dustMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.10f);
            dustMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var dustEmission = psDust.emission;
            dustEmission.rateOverTime = 5f;

            var dustShape = psDust.shape;
            dustShape.shapeType = ParticleSystemShapeType.Box;
            dustShape.scale = new Vector3(26f, 14f, 1f);

            var dustCol = psDust.colorOverLifetime;
            dustCol.enabled = true;
            Gradient dustGrad = new Gradient();
            dustGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.92f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.4f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.25f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            dustCol.color = dustGrad;

            PrefabUtility.SaveAsPrefabAsset(root, WeatherL1PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildWeatherL2Prefab()
        {
            GameObject root = new GameObject("Weather_Level2_CrystalCavern");
            root.AddComponent<WeatherFollowCamera>();

            // Child 1: Glowing Crystal Spores
            GameObject sporeGo = new GameObject("CrystalSpores");
            sporeGo.transform.SetParent(root.transform);
            sporeGo.transform.localPosition = Vector3.zero;

            ParticleSystem ps = sporeGo.AddComponent<ParticleSystem>();
            SetupParticleRenderer(sporeGo);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 15f, 1f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 0.95f, 1f), 0f), // Cyan
                    new GradientColorKey(new Color(0.85f, 0.35f, 1f), 0.5f), // Violet
                    new GradientColorKey(new Color(0.3f, 1f, 0.85f), 1f) // Aqua
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.35f, 0.3f),
                    new GradientAlphaKey(0.35f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            // Child 2: Water Droplets
            GameObject dripGo = new GameObject("CaveWaterDrips");
            dripGo.transform.SetParent(root.transform);
            dripGo.transform.localPosition = new Vector3(0f, 7.5f, 0f);

            ParticleSystem psDrip = dripGo.AddComponent<ParticleSystem>();
            SetupParticleRenderer(dripGo);

            var dripMain = psDrip.main;
            dripMain.duration = 4f;
            dripMain.loop = true;
            dripMain.startLifetime = 2.5f;
            dripMain.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 7f);
            dripMain.startSize = 0.08f;
            dripMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var dripEmission = psDrip.emission;
            dripEmission.rateOverTime = 2f;

            var dripShape = psDrip.shape;
            dripShape.shapeType = ParticleSystemShapeType.Box;
            dripShape.scale = new Vector3(26f, 0.5f, 1f);

            var dripCol = psDrip.colorOverLifetime;
            dripCol.enabled = true;
            Gradient dripGrad = new Gradient();
            dripGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.65f, 0.88f, 1f), 0f), new GradientColorKey(new Color(0.75f, 0.95f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.40f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            dripCol.color = dripGrad;

            PrefabUtility.SaveAsPrefabAsset(root, WeatherL2PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildWeatherL3Prefab()
        {
            GameObject root = new GameObject("Weather_Level3_Volcano");
            root.AddComponent<WeatherFollowCamera>();

            // Child 1: Rising Volcanic Embers & Sparks
            GameObject emberGo = new GameObject("VolcanicEmbers");
            emberGo.transform.SetParent(root.transform);
            emberGo.transform.localPosition = new Vector3(0f, -7.5f, 0f); // Bottom of screen

            ParticleSystem ps = emberGo.AddComponent<ParticleSystem>();
            SetupParticleRenderer(emberGo);

            var main = ps.main;
            main.duration = 4f;
            main.loop = true;
            main.startLifetime = 4.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 5.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 1f, 1f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.8f, 1.2f); // Turbulent updraft
            vel.y = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f); // Match TwoConstants curve mode to prevent Unity error

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0f), // Brilliant yellow
                    new GradientColorKey(new Color(1f, 0.45f, 0.05f), 0.4f), // Molten orange
                    new GradientColorKey(new Color(0.85f, 0.12f, 0.05f), 0.8f), // Deep fiery crimson
                    new GradientColorKey(new Color(0.25f, 0.10f, 0.10f), 1f) // Dark cooling ash
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.40f, 0.15f),
                    new GradientAlphaKey(0.35f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            // Child 2: Ash Smog Flurries
            GameObject ashGo = new GameObject("AshSmog");
            ashGo.transform.SetParent(root.transform);
            ashGo.transform.localPosition = Vector3.zero;

            ParticleSystem psAsh = ashGo.AddComponent<ParticleSystem>();
            SetupParticleRenderer(ashGo);

            var ashMain = psAsh.main;
            ashMain.duration = 5f;
            ashMain.loop = true;
            ashMain.startLifetime = 5f;
            ashMain.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            ashMain.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            ashMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var ashEmission = psAsh.emission;
            ashEmission.rateOverTime = 3.5f;

            var ashShape = psAsh.shape;
            ashShape.shapeType = ParticleSystemShapeType.Box;
            ashShape.scale = new Vector3(26f, 14f, 1f);

            var ashCol = psAsh.colorOverLifetime;
            ashCol.enabled = true;
            Gradient ashGrad = new Gradient();
            ashGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.18f, 0.14f, 0.16f), 0f), new GradientColorKey(new Color(0.24f, 0.18f, 0.20f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.16f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            ashCol.color = ashGrad;

            PrefabUtility.SaveAsPrefabAsset(root, WeatherL3PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SetupParticleRenderer(GameObject go)
        {
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null && particleMaterial != null)
            {
                psr.sharedMaterial = particleMaterial;
                psr.sortingOrder = -2; // Render in background layer (behind player, enemies and ground tilemaps)
            }
        }
        #endregion

        #region Scene Assembly Helpers

        /// <summary>
        /// Builds and instantiates the multi-layer parallax system for a level.
        /// </summary>
        public static GameObject BuildParallaxForLevel(string levelName)
        {
            EnsureDirectories();

            GameObject root = new GameObject("ParallaxBackground");
            ParallaxController controller = root.AddComponent<ParallaxController>();

            float unitWidth = 32f; // 512 px / 16 PPU = 32 world units
            float baseY = 6.0f;

            string farSpritePath = "";
            string midSpritePath = "";
            string nearSpritePath = "";
            float farFactorX = 0.82f;
            float midFactorX = 0.55f;
            float nearFactorX = 0.28f;
            float nearAutoScroll = 0.6f;

            if (levelName.Contains("Level 2"))
            {
                farSpritePath = $"{ParallaxArtDir}/BG_L2_CaveFar.png";
                midSpritePath = $"{ParallaxArtDir}/BG_L2_CrystalsMid.png";
                nearSpritePath = $"{ParallaxArtDir}/BG_L2_MistNear.png";
                nearAutoScroll = 0.45f;
            }
            else if (levelName.Contains("Level 3"))
            {
                farSpritePath = $"{ParallaxArtDir}/BG_L3_VolcanoFar.png";
                midSpritePath = $"{ParallaxArtDir}/BG_L3_FortressMid.png";
                nearSpritePath = $"{ParallaxArtDir}/BG_L3_SmokeNear.png";
                nearAutoScroll = 0.75f;
            }
            else // Level 1 / Default
            {
                farSpritePath = $"{ParallaxArtDir}/BG_L1_MountainsFar.png";
                midSpritePath = $"{ParallaxArtDir}/BG_L1_HillsMid.png";
                nearSpritePath = $"{ParallaxArtDir}/BG_L1_CloudsNear.png";
                nearAutoScroll = 0.65f;
            }

            Sprite farSprite = AssetDatabase.LoadAssetAtPath<Sprite>(farSpritePath);
            Sprite midSprite = AssetDatabase.LoadAssetAtPath<Sprite>(midSpritePath);
            Sprite nearSprite = AssetDatabase.LoadAssetAtPath<Sprite>(nearSpritePath);

            // Layer 1: Far
            if (farSprite != null)
            {
                GameObject layerGo = CreateTiledParallaxLayer("Layer_Far", root.transform, farSprite, unitWidth, baseY, -20, farFactorX, 0.03f, 0f);
                controller.RegisterLayer(layerGo.GetComponent<ParallaxLayer>());
            }

            // Layer 2: Mid
            if (midSprite != null)
            {
                GameObject layerGo = CreateTiledParallaxLayer("Layer_Mid", root.transform, midSprite, unitWidth, baseY, -15, midFactorX, 0.07f, 0f);
                controller.RegisterLayer(layerGo.GetComponent<ParallaxLayer>());
            }

            // Layer 3: Near
            if (nearSprite != null)
            {
                GameObject layerGo = CreateTiledParallaxLayer("Layer_Near", root.transform, nearSprite, unitWidth, baseY + 1.5f, -10, nearFactorX, 0.12f, nearAutoScroll);
                controller.RegisterLayer(layerGo.GetComponent<ParallaxLayer>());
            }

            return root;
        }

        private static GameObject CreateTiledParallaxLayer(string layerName, Transform parent, Sprite sprite, float unitWidth, float posY, int sortingOrder, float factorX, float factorY, float autoScroll)
        {
            GameObject layerGo = new GameObject(layerName);
            layerGo.transform.SetParent(parent);
            layerGo.transform.position = new Vector3(0f, posY, 0f);

            ParallaxLayer pLayer = layerGo.AddComponent<ParallaxLayer>();
            pLayer.Configure(factorX, factorY, unitWidth, autoScroll);

            // 3 seamless tiled child segments: Left (-unitWidth), Center (0), Right (+unitWidth)
            CreateSegment(layerGo.transform, "Segment_Left", sprite, -unitWidth, sortingOrder);
            CreateSegment(layerGo.transform, "Segment_Center", sprite, 0f, sortingOrder);
            CreateSegment(layerGo.transform, "Segment_Right", sprite, unitWidth, sortingOrder);

            return layerGo;
        }

        private static void CreateSegment(Transform parent, string segName, Sprite sprite, float localX, int sortingOrder)
        {
            GameObject segGo = new GameObject(segName);
            segGo.transform.SetParent(parent, false);
            segGo.transform.localPosition = new Vector3(localX, 0f, 0f);

            SpriteRenderer sr = segGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        /// <summary>
        /// Instantiates the atmospheric weather system prefab for a level.
        /// </summary>
        public static GameObject BuildWeatherForLevel(string levelName)
        {
            string prefabPath = WeatherL1PrefabPath;
            if (levelName.Contains("Level 2")) prefabPath = WeatherL2PrefabPath;
            else if (levelName.Contains("Level 3")) prefabPath = WeatherL3PrefabPath;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject weatherInstance = UnityEngine.Object.Instantiate(prefab);
                weatherInstance.name = "AtmosphericWeatherSystem";
                return weatherInstance;
            }
            return null;
        }
        #endregion
    }
}
