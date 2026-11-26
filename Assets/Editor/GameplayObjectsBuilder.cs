using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Platformer.Common;
using Platformer.Managers;
using Platformer.UI;

namespace Platformer.Editor
{
    /// <summary>
    /// Editor automation tool for Task 4:
    /// Menu item Tools/Platformer/4 - Add Gameplay Objects To Level1.
    /// Creates procedural sprites & prefabs for Collectibles (Coin, Heart), Checkpoints, Spikes,
    /// and constructs the Canvas + TextMeshPro HUD, GameManager, and places them across Level1.
    /// </summary>
    public static class GameplayObjectsBuilder
    {
        private const string CoinSpritePath = "Assets/Art/Coin.png";
        private const string HeartSpritePath = "Assets/Art/Heart.png";
        private const string CheckpointSpritePath = "Assets/Art/CheckpointFlag.png";
        private const string SpikesSpritePath = "Assets/Art/Spikes.png";

        private const string CoinPrefabPath = "Assets/Prefabs/Collectible_Coin.prefab";
        private const string HeartPrefabPath = "Assets/Prefabs/Collectible_Heart.prefab";
        private const string CheckpointPrefabPath = "Assets/Prefabs/Checkpoint.prefab";
        private const string SpikesPrefabPath = "Assets/Prefabs/Hazard_Spikes.prefab";

        private const string Level1ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/Platformer/4 - Add Gameplay Objects To Level1", priority = 4)]
        public static void AddGameplayObjectsToLevel1()
        {
            // 1. Ensure tags and layers are ready
            TestSceneBuilder.SetupLayersAndTags();

            // 2. Ensure Level 1 exists
            if (!File.Exists(Level1ScenePath))
            {
                Level1Builder.CreateLevel1();
            }

            // 3. Ensure TMP Essential Resources are imported
            EnsureTMPEssentials();

            // 4. Generate procedural pixel sprites
            Sprite coinSprite = GenerateCoinSprite();
            Sprite heartSprite = GenerateHeartSprite();
            Sprite checkpointSprite = GenerateCheckpointSprite();
            Sprite spikesSprite = GenerateSpikesSprite();

            // 5. Build Prefabs
            GameObject coinPrefab = BuildCoinPrefab(coinSprite);
            GameObject heartPrefab = BuildHeartPrefab(heartSprite);
            GameObject checkpointPrefab = BuildCheckpointPrefab(checkpointSprite);
            GameObject spikesPrefab = BuildSpikesPrefab(spikesSprite);

            // 6. Open Level 1 scene
            Scene scene = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);

            // 7. Ensure GameManager exists in scene
            EnsureGameManagerInScene();

            // 8. Place Checkpoints (3 throughout the level)
            PlaceCheckpoints(checkpointPrefab, scene);

            // 9. Place Collectibles (Coins and Hearts)
            PlaceCollectibles(coinPrefab, heartPrefab, scene);

            // 10. Place Spikes Hazards
            PlaceSpikesHazards(spikesPrefab, scene);

            // 11. Create or update Canvas HUD with TextMeshPro
            BuildHUD(scene);

            // 12. Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#2ecc71>[Platformer]</color> Successfully added GameManager, Checkpoints, Collectibles, Spikes, and HUD to Level1!");
        }

        private static void EnsureTMPEssentials()
        {
            if (!Directory.Exists("Assets/TextMesh Pro"))
            {
                string packagePath = Path.GetFullPath("Library/PackageCache/com.unity.ugui@cd4936b8279f/Package Resources/TMP Essential Resources.unitypackage");
                if (File.Exists(packagePath))
                {
                    AssetDatabase.ImportPackage(packagePath, false);
                    AssetDatabase.Refresh();
                }
            }
        }

        private static void EnsureGameManagerInScene()
        {
            GameManager gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject gmGo = new GameObject("GameManager");
                gmGo.AddComponent<GameManager>();
            }
        }

        private static void PlaceCheckpoints(GameObject prefab, Scene scene)
        {
            GameObject root = GameObject.Find("Checkpoints");
            if (root != null)
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                root = new GameObject("Checkpoints");
            }

            Vector3[] positions = new Vector3[]
            {
                new Vector3(26f, 4.25f, 0f), // Checkpoint 1: on wide safe ground after initial section
                new Vector3(69f, 4.25f, 0f), // Checkpoint 2: open cavern ground
                new Vector3(97f, 4.25f, 0f)  // Checkpoint 3: before final approach stretch
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                cp.name = $"Checkpoint_{i + 1}";
                cp.transform.position = positions[i];
                cp.transform.SetParent(root.transform);
            }
        }

        private static void PlaceCollectibles(GameObject coinPrefab, GameObject heartPrefab, Scene scene)
        {
            GameObject root = GameObject.Find("Collectibles");
            if (root != null)
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                root = new GameObject("Collectibles");
            }

            // Coin placements (18 coins)
            Vector3[] coinPositions = new Vector3[]
            {
                new Vector3(8f, 4.6f, 0f),
                new Vector3(10f, 4.6f, 0f),
                new Vector3(16f, 6.5f, 0f),
                new Vector3(17f, 6.5f, 0f),
                new Vector3(22f, 4.6f, 0f),
                new Vector3(28f, 6.5f, 0f),
                new Vector3(38f, 7.6f, 0f),
                new Vector3(45f, 7.6f, 0f),
                new Vector3(53f, 9.6f, 0f),
                new Vector3(60f, 8.6f, 0f),
                new Vector3(64f, 12.6f, 0f),
                new Vector3(66f, 12.6f, 0f),
                new Vector3(74f, 7.6f, 0f),
                new Vector3(83f, 8.6f, 0f),
                new Vector3(94f, 7.6f, 0f),
                new Vector3(100f, 4.6f, 0f),
                new Vector3(105f, 4.6f, 0f),
                new Vector3(110f, 5.6f, 0f)
            };

            for (int i = 0; i < coinPositions.Length; i++)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, scene);
                c.name = $"Coin_{i + 1}";
                c.transform.position = coinPositions[i];
                c.transform.SetParent(root.transform);
            }

            // Heart placements (2 secret/challenging hearts)
            Vector3[] heartPositions = new Vector3[]
            {
                new Vector3(54f, 9.6f, 0f), // Atop cavern brick platform
                new Vector3(84f, 12.6f, 0f) // High challenge route
            };

            for (int i = 0; i < heartPositions.Length; i++)
            {
                GameObject h = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab, scene);
                h.name = $"Heart_{i + 1}";
                h.transform.position = heartPositions[i];
                h.transform.SetParent(root.transform);
            }
        }

        private static void PlaceSpikesHazards(GameObject prefab, Scene scene)
        {
            GameObject root = GameObject.Find("Hazards");
            if (root != null)
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                root = new GameObject("Hazards");
            }

            // Spikes on ground and challenging platform surfaces
            Vector3[] spikePositions = new Vector3[]
            {
                new Vector3(12.5f, 4.47f, 0f), // Early ground jump obstacle
                new Vector3(46.5f, 7.47f, 0f), // Stepping platform obstacle
                new Vector3(65.5f, 8.47f, 0f), // High route platform hazard
                new Vector3(81.5f, 5.47f, 0f), // Dais step obstacle
                new Vector3(101.5f, 4.47f, 0f) // Final stretch ground hazard
            };

            for (int i = 0; i < spikePositions.Length; i++)
            {
                GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                s.name = $"Hazard_Spikes_{i + 1}";
                s.transform.position = spikePositions[i];
                s.transform.SetParent(root.transform);
            }
        }

        private static void BuildHUD(Scene scene)
        {
            LevelBuilder.EnsureEventSystem();

            GameObject existingCanvas = GameObject.Find("HUD_Canvas");
            if (existingCanvas != null)
            {
                UnityEngine.Object.DestroyImmediate(existingCanvas);
            }

            GameObject canvasGo = new GameObject("HUD_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();

            HUDController hud = canvasGo.AddComponent<HUDController>();

            // Top Bar Texts
            TextMeshProUGUI hpText = CreateTMP(canvasGo.transform, "HP_Text", "HP: 3 / 3",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -30f), new Vector2(180f, 45f), TextAlignmentOptions.Left, 28f, new Color(1f, 0.35f, 0.35f, 1f));

            TextMeshProUGUI coinsText = CreateTMP(canvasGo.transform, "Coins_Text", "COINS: 00",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(230f, -30f), new Vector2(180f, 45f), TextAlignmentOptions.Left, 28f, new Color(0.98f, 0.82f, 0.15f, 1f));

            TextMeshProUGUI livesText = CreateTMP(canvasGo.transform, "Lives_Text", "LIVES: 3",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(420f, -30f), new Vector2(180f, 45f), TextAlignmentOptions.Left, 28f, new Color(0.28f, 0.86f, 0.98f, 1f));

            TextMeshProUGUI scoreText = CreateTMP(canvasGo.transform, "Score_Text", "SCORE: 00000",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -30f), new Vector2(260f, 45f), TextAlignmentOptions.Right, 28f, Color.white);

            // Game Over Overlay Panel
            GameObject goPanel = new GameObject("GameOver_Panel");
            goPanel.transform.SetParent(canvasGo.transform, false);

            RectTransform goRect = goPanel.AddComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.sizeDelta = Vector2.zero;

            Image goBg = goPanel.AddComponent<Image>();
            goBg.color = new Color(0f, 0f, 0f, 0.82f);

            CreateTMP(goPanel.transform, "GameOver_Title", "GAME OVER",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(600f, 100f), TextAlignmentOptions.Center, 64f, new Color(0.95f, 0.25f, 0.25f, 1f));

            CreateTMP(goPanel.transform, "GameOver_Subtitle", "LIVES EXHAUSTED - RELOADING...",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f), new Vector2(500f, 50f), TextAlignmentOptions.Center, 22f, new Color(0.85f, 0.85f, 0.85f, 0.9f));

            goPanel.SetActive(false);

            // Wire SerializedObject fields on HUDController
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("healthText").objectReferenceValue = hpText;
            hudSo.FindProperty("coinsText").objectReferenceValue = coinsText;
            hudSo.FindProperty("scoreText").objectReferenceValue = scoreText;
            hudSo.FindProperty("livesText").objectReferenceValue = livesText;
            hudSo.FindProperty("gameOverPanel").objectReferenceValue = goPanel;
            hudSo.ApplyModifiedProperties();
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align, float fontSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.color = color;

            return tmp;
        }

        private static GameObject BuildCoinPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Collectible_Coin");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            Collectible c = go.AddComponent<Collectible>();
            SerializedObject cSo = new SerializedObject(c);
            cSo.FindProperty("type").enumValueIndex = (int)CollectibleType.Coin;
            cSo.FindProperty("coinAmount").intValue = 1;
            cSo.FindProperty("scoreValue").intValue = 100;
            cSo.FindProperty("bobAmplitude").floatValue = 0.12f;
            cSo.FindProperty("bobFrequency").floatValue = 3.2f;
            cSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, CoinPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildHeartPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Collectible_Heart");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.7f, 0.7f);

            Collectible c = go.AddComponent<Collectible>();
            SerializedObject cSo = new SerializedObject(c);
            cSo.FindProperty("type").enumValueIndex = (int)CollectibleType.Heart;
            cSo.FindProperty("healAmount").intValue = 1;
            cSo.FindProperty("bobAmplitude").floatValue = 0.14f;
            cSo.FindProperty("bobFrequency").floatValue = 2.8f;
            cSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, HeartPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildCheckpointPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Checkpoint");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 1.4f);
            col.offset = new Vector2(0f, 0.1f);

            Checkpoint cp = go.AddComponent<Checkpoint>();
            SerializedObject cpSo = new SerializedObject(cp);
            cpSo.FindProperty("inactiveColor").colorValue = new Color(0.65f, 0.70f, 0.75f, 1f);
            cpSo.FindProperty("activeColor").colorValue = new Color(0.20f, 0.92f, 0.40f, 1f);
            cpSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            cpSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, CheckpointPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildSpikesPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Hazard_Spikes");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;

            PolygonCollider2D col = go.AddComponent<PolygonCollider2D>();
            // 3 triangular teeth collider points
            col.points = new Vector2[]
            {
                new Vector2(-0.48f, -0.48f),
                new Vector2(-0.35f, 0.35f),
                new Vector2(-0.16f, -0.48f),
                new Vector2(0f, 0.42f),
                new Vector2(0.16f, -0.48f),
                new Vector2(0.35f, 0.35f),
                new Vector2(0.48f, -0.48f)
            };

            HazardSpikes spikes = go.AddComponent<HazardSpikes>();
            SerializedObject sSo = new SerializedObject(spikes);
            sSo.FindProperty("damage").intValue = 1;
            sSo.FindProperty("knockbackForce").floatValue = 12f;
            sSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, SpikesPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite GenerateCoinSprite()
        {
            int width = 24;
            int height = 24;

            Color goldRim = new Color(0.72f, 0.50f, 0.08f, 1f);
            Color goldBody = new Color(0.98f, 0.78f, 0.15f, 1f);
            Color goldHighlight = new Color(1f, 0.94f, 0.55f, 1f);
            Color goldCenter = new Color(0.88f, 0.65f, 0.10f, 1f);

            return CreateSprite(CoinSpritePath, width, height, (x, y) =>
            {
                float cx = 11.5f;
                float cy = 11.5f;
                float rSq = (x - cx) * (x - cx) + (y - cy) * (y - cy);

                if (rSq > 95f) return Color.clear;
                if (rSq >= 75f) return goldRim;

                // Center embossing
                if (Mathf.Abs(x - cx) <= 2.2f && Mathf.Abs(y - cy) <= 5.5f) return goldCenter;

                // Shiny specular dot
                if (x >= 7 && x <= 9 && y >= 14 && y <= 16) return goldHighlight;

                return goldBody;
            });
        }

        private static Sprite GenerateHeartSprite()
        {
            int width = 24;
            int height = 24;

            Color heartBorder = new Color(0.48f, 0.10f, 0.12f, 1f);
            Color heartFill = new Color(0.92f, 0.22f, 0.25f, 1f);
            Color heartShine = new Color(1f, 0.82f, 0.85f, 1f);

            return CreateSprite(HeartSpritePath, width, height, (x, y) =>
            {
                // Heart shape definition
                if (y < 3 || y > 19 || x < 2 || x > 21) return Color.clear;

                // Bottom V taper
                int dy = y - 3;
                if (x < 11.5f - dy * 0.95f || x > 11.5f + dy * 0.95f) return Color.clear;

                // Top cleavage dip
                if (y >= 17 && (x >= 10 && x <= 13)) return Color.clear;

                // Outer border check
                if (y == 3 || x == 2 || x == 21 || y == 19 ||
                    (y >= 16 && (x == 10 || x == 13)))
                {
                    return heartBorder;
                }

                // Top-left shine
                if (x >= 5 && x <= 8 && y >= 14 && y <= 16) return heartShine;

                return heartFill;
            });
        }

        private static Sprite GenerateCheckpointSprite()
        {
            int width = 32;
            int height = 48;

            Color silverPole = new Color(0.75f, 0.78f, 0.82f, 1f);
            Color silverHighlight = new Color(0.92f, 0.94f, 0.96f, 1f);
            Color flagFabric = Color.white; // Tinted by Checkpoint component
            Color stoneBase = new Color(0.35f, 0.40f, 0.45f, 1f);

            return CreateSprite(CheckpointSpritePath, width, height, (x, y) =>
            {
                // Base pediment
                if (y <= 4)
                {
                    if (x >= 4 && x <= 27) return stoneBase;
                    return Color.clear;
                }

                // Vertical flagpole (x: 7..8)
                if (x == 7) return silverHighlight;
                if (x == 8) return silverPole;

                // Top sphere
                if (y >= 43 && y <= 46 && x >= 6 && x <= 9) return silverHighlight;

                // Pennant flag fabric (x: 9..27, y: 24..42)
                if (x >= 9 && x <= 27 && y >= 24 && y <= 42)
                {
                    int relX = x - 9;
                    int inset = relX / 2;
                    if (y >= (24 + inset) && y <= (42 - inset))
                    {
                        return flagFabric;
                    }
                }

                return Color.clear;
            });
        }

        private static Sprite GenerateSpikesSprite()
        {
            int width = 32;
            int height = 32;

            Color ironBase = new Color(0.25f, 0.28f, 0.32f, 1f);
            Color steelBlade = new Color(0.68f, 0.72f, 0.78f, 1f);
            Color steelHighlight = new Color(0.92f, 0.95f, 0.98f, 1f);
            Color steelShadow = new Color(0.38f, 0.42f, 0.48f, 1f);

            return CreateSprite(SpikesSpritePath, width, height, (x, y) =>
            {
                // Base plate
                if (y <= 3) return ironBase;

                // Tooth 1 (x: 2..10, tip: 6, y: 26)
                if (x >= 2 && x <= 10)
                {
                    float maxH = 4f + (1f - Mathf.Abs(x - 6) / 4f) * 22f;
                    if (y <= maxH)
                    {
                        if (x == 6 && y >= maxH - 2f) return Color.white;
                        if (x < 6) return steelHighlight;
                        return steelShadow;
                    }
                }

                // Tooth 2 (x: 12..20, tip: 16, y: 28)
                if (x >= 12 && x <= 20)
                {
                    float maxH = 4f + (1f - Mathf.Abs(x - 16) / 4f) * 24f;
                    if (y <= maxH)
                    {
                        if (x == 16 && y >= maxH - 2f) return Color.white;
                        if (x < 16) return steelHighlight;
                        return steelShadow;
                    }
                }

                // Tooth 3 (x: 22..30, tip: 26, y: 26)
                if (x >= 22 && x <= 30)
                {
                    float maxH = 4f + (1f - Mathf.Abs(x - 26) / 4f) * 22f;
                    if (y <= maxH)
                    {
                        if (x == 26 && y >= maxH - 2f) return Color.white;
                        if (x < 26) return steelHighlight;
                        return steelShadow;
                    }
                }

                return Color.clear;
            });
        }

        private static Sprite CreateSprite(string relativePath, int width, int height, Func<int, int, Color> pixelColor)
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, pixelColor(x, y));
                }
            }
            texture.Apply();

            byte[] pngData = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            File.WriteAllBytes(fullPath, pngData);

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        }
    }
}

