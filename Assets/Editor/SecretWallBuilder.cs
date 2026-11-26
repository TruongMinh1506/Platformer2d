using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Platformer.Common;

namespace Platformer.Editor
{
    /// <summary>
    /// Builds procedural pixel art sprites and prefabs for Secret Breakable Walls
    /// and Ancient Relic collectibles.
    /// </summary>
    public static class SecretWallBuilder
    {
        public const string WallSpritePath = "Assets/Art/BreakableWall.png";
        public const string WallDamagedSpritePath = "Assets/Art/BreakableWall_Damaged.png";
        public const string RelicSpritePath = "Assets/Art/Relic.png";

        public const string WallPrefabPath = "Assets/Prefabs/Secret_BreakableWall.prefab";
        public const string RelicPrefabPath = "Assets/Prefabs/Collectible_Relic.prefab";

        [MenuItem("Tools/Platformer/Secrets/Generate Secret Wall & Relic Assets", priority = 50)]
        public static void GenerateSecretAssets()
        {
            EnsureDirectories();
            GenerateSprites();
            BuildWallPrefab();
            BuildRelicPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#2ecc71>[SecretWallBuilder]</color> Secret Wall & Ancient Relic assets created successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Art")) Directory.CreateDirectory("Assets/Art");
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        }

        private static void GenerateSprites()
        {
            // 1. Breakable Wall (Cracked stone bricks)
            CreateSpriteAsset(WallSpritePath, 32, 32, DrawWallNormal);

            // 2. Breakable Wall Damaged (Heavily fractured bricks)
            CreateSpriteAsset(WallDamagedSpritePath, 32, 32, DrawWallDamaged);

            // 3. Ancient Relic (Gleaming gold & sapphire jewel)
            CreateSpriteAsset(RelicSpritePath, 16, 16, DrawRelic);

            AssetDatabase.Refresh();
        }

        private static void CreateSpriteAsset(string path, int width, int height, Action<Texture2D, int, int> drawAction)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clears = new Color[width * height];
            for (int i = 0; i < clears.Length; i++) clears[i] = Color.clear;
            tex.SetPixels(clears);

            drawAction(tex, width, height);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f; // 32 px = 2 world units, 16 px = 1 unit
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void DrawWallNormal(Texture2D tex, int w, int h)
        {
            Color stoneDark = new Color(0.22f, 0.20f, 0.25f, 1f);
            Color stoneMid = new Color(0.38f, 0.35f, 0.42f, 1f);
            Color stoneLight = new Color(0.55f, 0.50f, 0.58f, 1f);
            Color crackColor = new Color(0.10f, 0.08f, 0.12f, 1f);

            // Brick pattern
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    bool isMortar = (y % 16 == 0) || (y < 16 && x % 16 == 0) || (y >= 16 && (x + 8) % 16 == 0);
                    Color c = isMortar ? stoneDark : ((x + y) % 3 == 0 ? stoneLight : stoneMid);
                    tex.SetPixel(x, y, c);
                }
            }

            // Visible hairline cracks indicating breakable secret
            int[] crackXs = { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            int[] crackYs = { 6, 8, 11, 13, 16, 18, 20, 22, 24, 25, 27 };
            for (int i = 0; i < crackXs.Length; i++)
            {
                tex.SetPixel(crackXs[i], crackYs[i], crackColor);
                if (i % 2 == 0) tex.SetPixel(crackXs[i] + 1, crackYs[i], crackColor);
            }
        }

        private static void DrawWallDamaged(Texture2D tex, int w, int h)
        {
            DrawWallNormal(tex, w, h);
            Color deepCrack = new Color(0.06f, 0.04f, 0.08f, 1f);
            Color debrisGlow = new Color(0.85f, 0.75f, 0.4f, 0.8f);

            // Heavy fractured spiderweb cracks
            for (int i = 4; i < 28; i++)
            {
                tex.SetPixel(i, i, deepCrack);
                tex.SetPixel(i, 31 - i, deepCrack);
                tex.SetPixel(16, i, deepCrack);
                tex.SetPixel(i, 16, deepCrack);
                if (i % 5 == 0) tex.SetPixel(i + 1, i, debrisGlow);
            }
        }

        private static void DrawRelic(Texture2D tex, int w, int h)
        {
            Color goldBright = new Color(1.0f, 0.92f, 0.35f, 1f);
            Color goldMid = new Color(0.92f, 0.72f, 0.18f, 1f);
            Color goldDark = new Color(0.65f, 0.45f, 0.10f, 1f);
            Color gemCyan = new Color(0.2f, 0.95f, 1.0f, 1f);
            Color gemWhite = new Color(0.9f, 1.0f, 1.0f, 1f);

            // Golden ornamental Chalice / Relic Diamond
            for (int x = 2; x <= 13; x++)
            {
                for (int y = 2; y <= 13; y++)
                {
                    float dx = (x - 7.5f) / 5.5f;
                    float dy = (y - 7.5f) / 5.5f;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 1.0f)
                    {
                        Color c = dx < 0 ? goldBright : goldMid;
                        if (dy < -0.3f) c = goldDark;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Central Inset Sapphire Gemstone
            for (int x = 6; x <= 9; x++)
            {
                for (int y = 6; y <= 9; y++)
                {
                    tex.SetPixel(x, y, gemCyan);
                }
            }
            tex.SetPixel(7, 8, gemWhite); // Glimmer
        }

        private static void BuildWallPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WallSpritePath);
            Sprite damagedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WallDamagedSpritePath);
            GameObject go = new GameObject("Secret_BreakableWall");

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1) go.layer = groundLayer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.0f, 2.0f); // 32 px at 16 PPU = 2x2 world units

            Health health = go.AddComponent<Health>();
            SerializedObject hSo = new SerializedObject(health);
            hSo.FindProperty("maxHealth").intValue = 2;
            hSo.FindProperty("invincibilityDuration").floatValue = 0.1f;
            hSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            hSo.ApplyModifiedProperties();

            BreakableSecretWall wall = go.AddComponent<BreakableSecretWall>();
            GameObject shatterFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DustParticle.prefab");
            SerializedObject wSo = new SerializedObject(wall);
            wSo.FindProperty("shatterEffectPrefab").objectReferenceValue = shatterFx;
            wSo.FindProperty("crackedSprite").objectReferenceValue = damagedSprite;
            wSo.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, WallPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void BuildRelicPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RelicSpritePath);
            GameObject go = new GameObject("Collectible_Relic");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;
            col.isTrigger = true;

            Collectible c = go.AddComponent<Collectible>();
            GameObject pickupFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CoinPickupParticle.prefab");
            SerializedObject cSo = new SerializedObject(c);
            cSo.FindProperty("type").enumValueIndex = (int)CollectibleType.Coin;
            cSo.FindProperty("scoreValue").intValue = 500;
            cSo.FindProperty("coinAmount").intValue = 5;
            cSo.FindProperty("bobAmplitude").floatValue = 0.22f;
            cSo.FindProperty("bobFrequency").floatValue = 2.8f;
            cSo.FindProperty("pickupEffectPrefab").objectReferenceValue = pickupFx;
            cSo.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, RelicPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
