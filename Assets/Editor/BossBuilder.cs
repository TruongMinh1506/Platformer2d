using System.IO;
using UnityEditor;
using UnityEngine;
using Platformer.Common;
using Platformer.Enemies;

namespace Platformer.Editor
{
    /// <summary>
    /// Editor automation tool for Cải tiến 4:
    /// Builds procedural pixel art sprites and prefabs for:
    /// - Boss Ignis the Volcanic Titan (Boss_VolcanoGolem.prefab)
    /// - Boss Shockwave projectile (Boss_Shockwave.prefab)
    /// - Boss Falling Fireball (Boss_Fireball.prefab)
    /// - Boss Ground Telegraph (Boss_Telegraph.prefab)
    /// - Volcanic Arena Gate (Boss_Gate.prefab)
    /// </summary>
    public static class BossBuilder
    {
        public const string GolemSpritePath = "Assets/Art/Boss_VolcanoGolem.png";
        public const string ShockwaveSpritePath = "Assets/Art/Boss_Shockwave.png";
        public const string FireballSpritePath = "Assets/Art/Boss_Fireball.png";
        public const string TelegraphSpritePath = "Assets/Art/Boss_Telegraph.png";
        public const string GateSpritePath = "Assets/Art/Boss_Gate.png";

        public const string GolemPrefabPath = "Assets/Prefabs/Boss_VolcanoGolem.prefab";
        public const string ShockwavePrefabPath = "Assets/Prefabs/Boss_Shockwave.prefab";
        public const string FireballPrefabPath = "Assets/Prefabs/Boss_Fireball.prefab";
        public const string TelegraphPrefabPath = "Assets/Prefabs/Boss_Telegraph.prefab";
        public const string GatePrefabPath = "Assets/Prefabs/Boss_Gate.prefab";

        [MenuItem("Tools/Platformer/Build Boss Assets", priority = 25)]
        public static void BuildAllBossAssets()
        {
            EnsureDirectories();

            Sprite golemSprite = GenerateGolemSprite();
            Sprite shockwaveSprite = GenerateShockwaveSprite();
            Sprite fireballSprite = GenerateFireballSprite();
            Sprite telegraphSprite = GenerateTelegraphSprite();
            Sprite gateSprite = GenerateGateSprite();

            GameObject defeatFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyDefeatParticle.prefab");

            GameObject shockwavePrefab = BuildShockwavePrefab(shockwaveSprite);
            GameObject telegraphPrefab = BuildTelegraphPrefab(telegraphSprite);
            GameObject fireballPrefab = BuildFireballPrefab(fireballSprite, defeatFx);
            GameObject gatePrefab = BuildGatePrefab(gateSprite);
            GameObject golemPrefab = BuildGolemPrefab(golemSprite, shockwavePrefab, fireballPrefab, telegraphPrefab, defeatFx);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#e67e22>[Platformer]</color> Boss Ignis assets & prefabs built successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Art")) Directory.CreateDirectory("Assets/Art");
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        }

        public static Sprite GenerateGolemSprite()
        {
            int width = 48;
            int height = 48;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color empty = Color.clear;
            Color darkBasalt = new Color(0.14f, 0.12f, 0.15f, 1f);
            Color basaltBorder = new Color(0.08f, 0.06f, 0.09f, 1f);
            Color stoneMid = new Color(0.25f, 0.22f, 0.26f, 1f);
            Color magmaDeep = new Color(0.85f, 0.24f, 0.05f, 1f);
            Color magmaBright = new Color(1.0f, 0.55f, 0.10f, 1f);
            Color coreGlow = new Color(1.0f, 0.90f, 0.25f, 1f);

            // Fill clear
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, empty);

            // Draw massive golem body silhouette (X: 8..40, Y: 4..44)
            for (int y = 4; y <= 44; y++)
            {
                for (int x = 8; x <= 40; x++)
                {
                    // Head: Y 32..44, X 16..32
                    bool isHead = (y >= 32 && y <= 43 && x >= 16 && x <= 32);
                    // Horns: Y 40..46, X 13..17 and 31..35
                    bool isHorn = (y >= 40 && y <= 45 && ((x >= 13 && x <= 16) || (x >= 32 && x <= 35)));
                    // Massive Shoulders: Y 26..38, X 6..16 and 32..42
                    bool isShoulder = (y >= 26 && y <= 37 && ((x >= 7 && x <= 17) || (x >= 31 && x <= 41)));
                    // Torso: Y 12..30, X 13..35
                    bool isTorso = (y >= 12 && y <= 31 && x >= 13 && x <= 35);
                    // Massive Legs: Y 4..14, X 10..19 and 29..38
                    bool isLegs = (y >= 4 && y <= 13 && ((x >= 10 && x <= 19) || (x >= 29 && x <= 38)));

                    if (isHead || isHorn || isShoulder || isTorso || isLegs)
                    {
                        // Default stone shading
                        Color c = darkBasalt;
                        if ((x + y) % 5 == 0 || (x % 4 == 0 && y % 3 == 0)) c = stoneMid;

                        // Magma veins in chest & shoulders
                        if (isTorso && (x >= 20 && x <= 28) && (y >= 18 && y <= 26))
                        {
                            float distFromChest = Vector2.Distance(new Vector2(x, y), new Vector2(24, 22));
                            if (distFromChest < 3.0f) c = coreGlow;
                            else if (distFromChest < 4.8f) c = magmaBright;
                            else c = magmaDeep;
                        }
                        else if ((x == 17 || x == 31) && y >= 16 && y <= 28)
                        {
                            c = magmaBright;
                        }

                        // Eyes: glowing orange/gold slits (Y 36..37, X 20..22 and 26..28)
                        if (y >= 36 && y <= 37 && ((x >= 19 && x <= 22) || (x >= 26 && x <= 29)))
                        {
                            c = coreGlow;
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Outline perimeter with basaltBorder
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (tex.GetPixel(x, y).a > 0.1f)
                    {
                        // Check neighbors
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    if (tex.GetPixel(nx, ny).a < 0.1f)
                                    {
                                        // On edge, don't change empty pixels, just tint edge darker
                                    }
                                }
                            }
                        }
                    }
                }
            }

            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(GolemSpritePath, png);
            AssetDatabase.ImportAsset(GolemSpritePath, ImportAssetOptions.ForceUpdate);

            ConfigureTextureImporter(GolemSpritePath, 24, new Vector2(0.5f, 0.05f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(GolemSpritePath);
        }

        public static Sprite GenerateShockwaveSprite()
        {
            int width = 24;
            int height = 16;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color empty = Color.clear;
            Color coreYellow = new Color(1.0f, 0.95f, 0.35f, 1f);
            Color midOrange = new Color(1.0f, 0.45f, 0.08f, 1f);
            Color darkRed = new Color(0.75f, 0.15f, 0.05f, 1f);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, empty);

            // Draw curving crest shockwave
            for (int x = 0; x < width; x++)
            {
                float peakH = Mathf.Sin((float)x / width * Mathf.PI) * (height - 2);
                for (int y = 0; y <= (int)peakH; y++)
                {
                    Color c = darkRed;
                    if (y > peakH * 0.65f) c = coreYellow;
                    else if (y > peakH * 0.35f) c = midOrange;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            File.WriteAllBytes(ShockwaveSpritePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(ShockwaveSpritePath, ImportAssetOptions.ForceUpdate);

            ConfigureTextureImporter(ShockwaveSpritePath, 16, new Vector2(0.5f, 0.1f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(ShockwaveSpritePath);
        }

        public static Sprite GenerateFireballSprite()
        {
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color empty = Color.clear;
            Color coreWhite = new Color(1.0f, 0.98f, 0.7f, 1f);
            Color brightOrange = new Color(1.0f, 0.5f, 0.05f, 1f);
            Color crustRed = new Color(0.8f, 0.15f, 0.05f, 1f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, empty);

            Vector2 center = new Vector2(7.5f, 7.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d < 2.0f) tex.SetPixel(x, y, coreWhite);
                    else if (d < 4.5f) tex.SetPixel(x, y, brightOrange);
                    else if (d < 6.8f) tex.SetPixel(x, y, crustRed);
                }
            }

            tex.Apply();
            File.WriteAllBytes(FireballSpritePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(FireballSpritePath, ImportAssetOptions.ForceUpdate);

            ConfigureTextureImporter(FireballSpritePath, 16, new Vector2(0.5f, 0.5f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(FireballSpritePath);
        }

        public static Sprite GenerateTelegraphSprite()
        {
            int width = 24;
            int height = 8;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color empty = Color.clear;
            Color lineRed = new Color(1.0f, 0.2f, 0.15f, 0.9f);
            Color glowOrange = new Color(1.0f, 0.6f, 0.2f, 0.6f);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, empty);

            Vector2 center = new Vector2(11.5f, 3.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / 11.0f;
                    float dy = (y - center.y) / 3.5f;
                    float ellipse = dx * dx + dy * dy;

                    if (ellipse > 0.65f && ellipse <= 1.05f)
                    {
                        tex.SetPixel(x, y, lineRed);
                    }
                    else if (ellipse <= 0.65f)
                    {
                        tex.SetPixel(x, y, glowOrange);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(TelegraphSpritePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(TelegraphSpritePath, ImportAssetOptions.ForceUpdate);

            ConfigureTextureImporter(TelegraphSpritePath, 16, new Vector2(0.5f, 0.5f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(TelegraphSpritePath);
        }

        public static Sprite GenerateGateSprite()
        {
            int width = 16;
            int height = 48;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color empty = Color.clear;
            Color darkIron = new Color(0.18f, 0.16f, 0.20f, 1f);
            Color ironBorder = new Color(0.10f, 0.08f, 0.12f, 1f);
            Color magmaRune = new Color(0.95f, 0.40f, 0.08f, 1f);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, empty);

            // Vertical iron bars & border
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x <= 1 || x >= 14 || y >= 45);
                    bool isBar = (x >= 4 && x <= 6) || (x >= 9 && x <= 11);
                    bool isCrossBeam = (y >= 15 && y <= 18) || (y >= 30 && y <= 33);
                    bool isBottomSpike = (y <= 5 && (x % 4 == 1 || x % 4 == 2));

                    if (isBorder || isBar || isCrossBeam || isBottomSpike)
                    {
                        Color c = darkIron;
                        if (isBorder) c = ironBorder;
                        if ((x == 5 || x == 10) && (y % 6 == 0)) c = magmaRune;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(GateSpritePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(GateSpritePath, ImportAssetOptions.ForceUpdate);

            ConfigureTextureImporter(GateSpritePath, 16, new Vector2(0.5f, 0f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(GateSpritePath);
        }

        private static void ConfigureTextureImporter(string path, float pixelsPerUnit, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }
        }

        public static GameObject BuildShockwavePrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Boss_Shockwave");
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 0.8f);
            col.offset = new Vector2(0f, 0.4f);

            go.AddComponent<BossShockwave>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ShockwavePrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject BuildTelegraphPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Boss_Telegraph");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;

            go.AddComponent<BossTelegraph>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, TelegraphPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject BuildFireballPrefab(Sprite sprite, GameObject defeatFx)
        {
            GameObject go = new GameObject("Boss_Fireball");
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            BossFireball fb = go.AddComponent<BossFireball>();
            SerializedObject so = new SerializedObject(fb);
            so.FindProperty("fallSpeed").floatValue = 18f;
            so.FindProperty("blastRadius").floatValue = 1.4f;
            so.FindProperty("damage").intValue = 1;
            if (defeatFx != null)
            {
                so.FindProperty("explosionEffectPrefab").objectReferenceValue = defeatFx;
            }
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, FireballPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject BuildGatePrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Boss_Gate");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1) go.layer = groundLayer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.0f, 3.0f);
            col.offset = new Vector2(0f, 1.5f);

            go.AddComponent<BossGate>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, GatePrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject BuildGolemPrefab(Sprite sprite, GameObject shockwave, GameObject fireball, GameObject telegraph, GameObject defeatFx)
        {
            GameObject go = new GameObject("Boss_VolcanoGolem");

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.mass = 50f;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 2.0f);
            col.offset = new Vector2(0f, 1.0f);

            Health health = go.AddComponent<Health>();
            SerializedObject hSo = new SerializedObject(health);
            hSo.FindProperty("maxHealth").intValue = 10;
            hSo.FindProperty("invincibilityDuration").floatValue = 0.35f;
            hSo.FindProperty("blinkInterval").floatValue = 0.06f;
            hSo.FindProperty("knockbackMultiplier").floatValue = 0f;
            hSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            hSo.FindProperty("rb").objectReferenceValue = rb;
            hSo.ApplyModifiedProperties();

            BossVolcanoGolem golem = go.AddComponent<BossVolcanoGolem>();
            SerializedObject gSo = new SerializedObject(golem);
            gSo.FindProperty("bossTitle").stringValue = "IGNIS - VOLCANIC TITAN";
            gSo.FindProperty("maxHealth").intValue = 10;
            gSo.FindProperty("enrageHealth").intValue = 5;
            gSo.FindProperty("shockwavePrefab").objectReferenceValue = shockwave;
            gSo.FindProperty("fireballPrefab").objectReferenceValue = fireball;
            gSo.FindProperty("telegraphPrefab").objectReferenceValue = telegraph;
            gSo.FindProperty("defeatParticlePrefab").objectReferenceValue = defeatFx;
            gSo.FindProperty("health").objectReferenceValue = health;
            gSo.FindProperty("rb").objectReferenceValue = rb;
            gSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            gSo.FindProperty("bossCollider").objectReferenceValue = col;
            gSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, GolemPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
