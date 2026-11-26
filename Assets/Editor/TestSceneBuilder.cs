using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer.Player;
using Platformer.Common;

namespace Platformer.Editor
{
    /// <summary>
    /// Editor automation tool to create required layers, tags, placeholder procedural sprites,
    /// Player prefab, and the TestLevel scene under Tools/Platformer/...
    /// </summary>
    public static class TestSceneBuilder
    {
        private const string PlayerSpritePath = "Assets/Art/PlayerPlaceholder.png";
        private const string GroundSpritePath = "Assets/Art/GroundTile.png";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string TestScenePath = "Assets/Scenes/TestLevel.unity";

        [MenuItem("Tools/Platformer/Setup Layers and Tags", priority = 10)]
        public static void SetupLayersAndTags()
        {
            var tagManagerObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerObjects == null || tagManagerObjects.Length == 0)
            {
                Debug.LogError("[Platformer] Unable to load TagManager.asset.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerObjects[0]);

            // Setup Tag: Player, Enemy, Ground
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            EnsureTag(tagsProp, "Player");
            EnsureTag(tagsProp, "Enemy");
            EnsureTag(tagsProp, "Ground");

            // Setup Layers: Ground, Player, Enemy
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            EnsureLayer(layersProp, "Ground");
            EnsureLayer(layersProp, "Player");
            EnsureLayer(layersProp, "Enemy");

            tagManager.ApplyModifiedProperties();

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            }

            AssetDatabase.SaveAssets();

            Debug.Log("[Platformer] Tags and Layers verified/configured successfully.");
        }

        [MenuItem("Tools/Platformer/1 - Create Test Scene", priority = 1)]
        public static void CreateTestScene()
        {
            // 1. Ensure tags and layers exist
            SetupLayersAndTags();

            // 2. Ensure folder layout
            EnsureFolders();

            // 3. Generate procedural placeholder sprites
            Sprite playerSprite = GeneratePlayerSprite();
            Sprite groundSprite = GenerateGroundSprite();

            // 4. Create Player prefab
            GameObject playerPrefab = BuildPlayerPrefab(playerSprite);

            // 5. Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 6. Camera setup
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.20f, 1f); // Dark slate backdrop

            camGo.AddComponent<AudioListener>();

            CameraFollow camFollow = camGo.AddComponent<CameraFollow>();
            SerializedObject camSo = new SerializedObject(camFollow);
            camSo.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
            camSo.FindProperty("smoothTime").floatValue = 0.15f;
            camSo.FindProperty("useBounds").boolValue = false;
            camSo.ApplyModifiedProperties();

            // 7. Light2D setup for Universal 2D Pipeline
            TryAddGlobalLight2D();

            // 8. Ground and platforms setup
            GameObject envRoot = new GameObject("Environment");
            int groundLayer = GetOrFindLayer("Ground", 6);

            // Flat ground
            CreatePlatform(envRoot.transform, "Ground_Main", new Vector3(0f, -4f, 0f), new Vector2(32f, 1.5f), groundSprite, groundLayer);

            // Floating platforms
            CreatePlatform(envRoot.transform, "Platform_Left_1", new Vector3(-6f, -1.8f, 0f), new Vector2(5f, 0.8f), groundSprite, groundLayer);
            CreatePlatform(envRoot.transform, "Platform_Center", new Vector3(0f, -0.2f, 0f), new Vector2(4.5f, 0.8f), groundSprite, groundLayer);
            CreatePlatform(envRoot.transform, "Platform_Right_1", new Vector3(6.5f, 1.4f, 0f), new Vector2(5f, 0.8f), groundSprite, groundLayer);
            CreatePlatform(envRoot.transform, "Platform_High", new Vector3(0f, 3.0f, 0f), new Vector2(3.5f, 0.8f), groundSprite, groundLayer);
            CreatePlatform(envRoot.transform, "Platform_Left_2", new Vector3(-10f, 0.8f, 0f), new Vector2(4f, 0.8f), groundSprite, groundLayer);

            // Boundary walls
            CreatePlatform(envRoot.transform, "Wall_Left", new Vector3(-16f, 1.5f, 0f), new Vector2(1.5f, 13f), groundSprite, groundLayer);
            CreatePlatform(envRoot.transform, "Wall_Right", new Vector3(16f, 1.5f, 0f), new Vector2(1.5f, 13f), groundSprite, groundLayer);

            // 9. Instantiate Player Prefab in Scene
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.transform.position = new Vector3(0f, -2.5f, 0f);

            // Connect Camera follow target
            camFollow.SetTarget(playerInstance.transform);

            // 10. Save Scene
            EditorSceneManager.SaveScene(scene, TestScenePath);

            // Add scene to Build Settings
            var currentScenes = EditorBuildSettings.scenes.ToList();
            if (!currentScenes.Any(s => s.path == TestScenePath))
            {
                currentScenes.Add(new EditorBuildSettingsScene(TestScenePath, true));
                EditorBuildSettings.scenes = currentScenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#2ecc71>[Platformer]</color> Scene '{TestScenePath}' created successfully with Player, platforms, and camera!");
        }

        private static int GetOrFindLayer(string layerName, int fallbackSlot)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1) return layer;

            var tagManagerObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerObjects != null && tagManagerObjects.Length > 0)
            {
                SerializedObject tagManager = new SerializedObject(tagManagerObjects[0]);
                SerializedProperty layersProp = tagManager.FindProperty("layers");
                for (int i = 0; i < layersProp.arraySize; i++)
                {
                    if (layersProp.GetArrayElementAtIndex(i).stringValue.Equals(layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return fallbackSlot;
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Scripts/Player",
                "Assets/Scripts/Enemies",
                "Assets/Scripts/Managers",
                "Assets/Scripts/UI",
                "Assets/Scripts/Common",
                "Assets/Editor",
                "Assets/Prefabs",
                "Assets/Scenes",
                "Assets/Art",
                "Assets/Audio"
            };

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static GameObject BuildPlayerPrefab(Sprite playerSprite)
        {
            GameObject playerGo = new GameObject("Player");

            int playerLayer = GetOrFindLayer("Player", 7);
            playerGo.layer = playerLayer;
            playerGo.tag = "Player";

            // Visual SpriteRenderer
            SpriteRenderer sr = playerGo.AddComponent<SpriteRenderer>();
            sr.sprite = playerSprite;
            sr.sortingOrder = 10;

            // Rigidbody2D (Unity 6 standards)
            Rigidbody2D rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // BoxCollider2D
            BoxCollider2D col = playerGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.45f);
            col.offset = Vector2.zero;

            // PlayerController
            PlayerController controller = playerGo.AddComponent<PlayerController>();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("moveSpeed").floatValue = 8.5f;
            so.FindProperty("acceleration").floatValue = 65f;
            so.FindProperty("deceleration").floatValue = 65f;
            so.FindProperty("jumpForce").floatValue = 14.5f;
            so.FindProperty("jumpCutMultiplier").floatValue = 0.5f;
            so.FindProperty("coyoteTime").floatValue = 0.1f;
            so.FindProperty("jumpBufferTime").floatValue = 0.1f;
            so.FindProperty("allowDoubleJump").boolValue = true;
            so.FindProperty("groundCheckOffset").vector2Value = new Vector2(0f, -0.73f);
            so.FindProperty("groundCheckSize").vector2Value = new Vector2(0.7f, 0.15f);

            int groundLayer = GetOrFindLayer("Ground", 6);
            int groundMask = 1 << groundLayer;
            so.FindProperty("groundLayer").intValue = groundMask;
            so.FindProperty("rb").objectReferenceValue = rb;
            so.ApplyModifiedProperties();

            // Health component
            Health health = playerGo.AddComponent<Health>();
            SerializedObject healthSo = new SerializedObject(health);
            healthSo.FindProperty("maxHealth").intValue = 3;
            healthSo.FindProperty("invincibilityDuration").floatValue = 1.0f;
            healthSo.FindProperty("blinkInterval").floatValue = 0.08f;
            healthSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            healthSo.FindProperty("rb").objectReferenceValue = rb;
            healthSo.ApplyModifiedProperties();

            // PlayerCombat component
            PlayerCombat combat = playerGo.AddComponent<PlayerCombat>();
            SerializedObject combatSo = new SerializedObject(combat);
            combatSo.FindProperty("attackCooldown").floatValue = 0.4f;
            combatSo.FindProperty("attackDamage").intValue = 1;
            combatSo.FindProperty("attackRadius").floatValue = 0.85f;
            combatSo.FindProperty("attackOffset").vector2Value = new Vector2(0.95f, 0f);
            combatSo.FindProperty("enemyKnockbackForce").floatValue = 8.5f;
            combatSo.FindProperty("stompDamage").intValue = 1;
            combatSo.FindProperty("bounceForce").floatValue = 13.5f;
            combatSo.FindProperty("sideKnockbackForce").floatValue = 9.5f;
            int enemyLayer = GetOrFindLayer("Enemy", 8);
            combatSo.FindProperty("enemyLayer").intValue = 1 << enemyLayer;
            combatSo.FindProperty("playerController").objectReferenceValue = controller;
            combatSo.FindProperty("playerHealth").objectReferenceValue = health;
            combatSo.FindProperty("rb").objectReferenceValue = rb;
            combatSo.ApplyModifiedProperties();

            // PlayerDeath component
            PlayerDeath death = playerGo.AddComponent<PlayerDeath>();
            SerializedObject deathSo = new SerializedObject(death);
            deathSo.FindProperty("reloadDelay").floatValue = 1.2f;
            deathSo.FindProperty("health").objectReferenceValue = health;
            deathSo.FindProperty("playerController").objectReferenceValue = controller;
            deathSo.FindProperty("playerCombat").objectReferenceValue = combat;
            deathSo.FindProperty("rb").objectReferenceValue = rb;
            deathSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            deathSo.ApplyModifiedProperties();

            // Save Prefab Asset
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerGo, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(playerGo);

            return prefab;
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector2 size, Sprite sprite, int layer)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.layer = layer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 0;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            col.offset = Vector2.zero;

            return go;
        }

        private static Sprite GeneratePlayerSprite()
        {
            int width = 32;
            int height = 48;

            Color bodyColor = new Color(0f, 0.82f, 0.83f, 1f); // Vibrant cyan
            Color borderColor = new Color(0.05f, 0.28f, 0.38f, 1f);
            Color visorColor = new Color(0.08f, 0.12f, 0.18f, 1f);
            Color eyeColor = new Color(1f, 0.85f, 0.2f, 1f); // Glowing amber
            Color pupilColor = Color.white;
            Color beltColor = new Color(0.95f, 0.65f, 0.15f, 1f);
            Color bootColor = new Color(0.12f, 0.18f, 0.24f, 1f);

            return CreateProceduralSprite(PlayerSpritePath, width, height, (x, y) =>
            {
                // Inactive transparent space outside main shape
                if (x < 4 || x > 27 || y < 2 || y > 45) return Color.clear;

                // Rounded corners at top and bottom
                if ((x <= 6 || x >= 25) && (y <= 4 || y >= 43)) return Color.clear;

                // Outer border
                if (x == 4 || x == 27 || y == 2 || y == 45 ||
                    ((x == 5 || x == 26) && (y <= 5 || y >= 42)))
                {
                    return borderColor;
                }

                // Boots at the bottom
                if (y <= 7)
                {
                    if (x >= 5 && x <= 13) return bootColor;
                    if (x >= 18 && x <= 26) return bootColor;
                    return Color.clear; // Space between legs
                }

                // Belt/accent stripe
                if (y >= 19 && y <= 21) return beltColor;

                // Face Visor area (looking forward/right)
                if (y >= 29 && y <= 40 && x >= 7 && x <= 26)
                {
                    // Visor border
                    if (x == 7 || x == 26 || y == 29 || y == 40) return borderColor;

                    // Eyes inside visor:
                    // Left Eye (x: 13..16, y: 32..36)
                    if (x >= 13 && x <= 16 && y >= 32 && y <= 36)
                    {
                        if (x == 15 && y == 35) return pupilColor;
                        return eyeColor;
                    }

                    // Right Eye (x: 20..24, y: 32..36)
                    if (x >= 20 && x <= 24 && y >= 32 && y <= 36)
                    {
                        if (x == 23 && y == 35) return pupilColor;
                        return eyeColor;
                    }

                    return visorColor;
                }

                // Main body fill
                return bodyColor;
            }, 32f);
        }

        private static Sprite GenerateGroundSprite()
        {
            int width = 32;
            int height = 32;

            Color grassTop = new Color(0.55f, 0.85f, 0.35f, 1f);
            Color grassMain = new Color(0.28f, 0.72f, 0.30f, 1f);
            Color grassBorder = new Color(0.15f, 0.45f, 0.18f, 1f);
            Color stoneBase = new Color(0.35f, 0.30f, 0.28f, 1f);
            Color stoneHighlight = new Color(0.44f, 0.38f, 0.35f, 1f);
            Color stoneShadow = new Color(0.22f, 0.18f, 0.16f, 1f);

            return CreateProceduralSprite(GroundSpritePath, width, height, (x, y) =>
            {
                // Top Grass highlight strip
                if (y == 31) return grassTop;
                if (y >= 26) return grassMain;
                if (y == 25) return grassBorder;

                // Edge borders for stone
                if (x == 0) return stoneHighlight;
                if (x == 31 || y == 0) return stoneShadow;

                // Subtle textured speckles
                if ((x * 7 + y * 13) % 11 == 0) return stoneHighlight;
                if ((x * 11 + y * 5) % 13 == 0) return stoneShadow;

                return stoneBase;
            }, 32f);
        }

        private static Sprite CreateProceduralSprite(string relativePath, int width, int height, Func<int, int, Color> pixelColor, float ppu)
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
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        }

        private static void TryAddGlobalLight2D()
        {
            var lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime")
                         ?? Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");

            if (lightType != null)
            {
                GameObject lightGo = new GameObject("Global Light 2D");
                var lightComp = lightGo.AddComponent(lightType);

                var lightTypeProp = lightType.GetProperty("lightType");
                if (lightTypeProp != null)
                {
                    var globalVal = Enum.Parse(lightTypeProp.PropertyType, "Global");
                    lightTypeProp.SetValue(lightComp, globalVal);
                }

                var intensityProp = lightType.GetProperty("intensity");
                if (intensityProp != null)
                {
                    intensityProp.SetValue(lightComp, 1.0f);
                }
            }
        }

        private static void EnsureTag(SerializedProperty tagsProp, string tagName)
        {
            if (UnityEditorInternal.InternalEditorUtility.tags.Contains(tagName))
            {
                return;
            }

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            int index = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(index);
            tagsProp.GetArrayElementAtIndex(index).stringValue = tagName;
        }

        private static void EnsureLayer(SerializedProperty layersProp, string layerName)
        {
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                var elem = layersProp.GetArrayElementAtIndex(i);
                if (elem.stringValue.Equals(layerName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            // Assign to first free slot starting at layer 6
            for (int i = 6; i < layersProp.arraySize; i++)
            {
                var elem = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(elem.stringValue))
                {
                    elem.stringValue = layerName;
                    return;
                }
            }
        }
    }
}
