using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Platformer.Common;
using Platformer.Enemies;

namespace Platformer.Editor
{
    /// <summary>
    /// Builds procedural pixel art sprites and prefabs for the new enemy types:
    /// 1. Enemy_Flyer (Aerial hovering Bat/Drone with swooping dive attack)
    /// 2. Enemy_Shooter (Stationary Turret/Spitter firing timed projectiles)
    /// 3. Enemy_Projectile (Energy bolt that can be sliced out of the air)
    /// </summary>
    public static class NewEnemiesBuilder
    {
        public const string FlyerSpritePath = "Assets/Art/EnemyFlyer.png";
        public const string ShooterSpritePath = "Assets/Art/EnemyShooter.png";
        public const string ProjectileSpritePath = "Assets/Art/EnemyProjectile.png";

        public const string FlyerPrefabPath = "Assets/Prefabs/Enemy_Flyer.prefab";
        public const string ShooterPrefabPath = "Assets/Prefabs/Enemy_Shooter.prefab";
        public const string ProjectilePrefabPath = "Assets/Prefabs/Enemy_Projectile.prefab";

        [MenuItem("Tools/Platformer/Enemies/Generate New Enemies (Flyer & Shooter)", priority = 30)]
        public static void GenerateNewEnemyAssets()
        {
            EnsureDirectories();
            GenerateSprites();
            GameObject projPrefab = BuildProjectilePrefab();
            BuildFlyerPrefab();
            BuildShooterPrefab(projPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#2ecc71>[NewEnemiesBuilder]</color> New Enemy Sprites & Prefabs (Flyer, Shooter, Projectile) created successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Art")) Directory.CreateDirectory("Assets/Art");
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        }

        #region Sprite Generation

        private static void GenerateSprites()
        {
            // 1. Enemy Flyer (Bat / Aerial Drone - 32x32)
            CreateSpriteAsset(FlyerSpritePath, 32, 32, DrawFlyerSprite);

            // 2. Enemy Shooter (Turret / Spitter - 32x32)
            CreateSpriteAsset(ShooterSpritePath, 32, 32, DrawShooterSprite);

            // 3. Enemy Projectile (Fiery Energy Dart - 16x16)
            CreateSpriteAsset(ProjectileSpritePath, 16, 16, DrawProjectileSprite);

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
                importer.spritePixelsPerUnit = 16f; // 32 px = 2 world units
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void DrawFlyerSprite(Texture2D tex, int w, int h)
        {
            Color bodyColor = new Color(0.28f, 0.16f, 0.38f, 1f); // Dark shadow purple
            Color wingColor = new Color(0.42f, 0.22f, 0.55f, 1f); // Outer wing
            Color wingMembrane = new Color(0.62f, 0.28f, 0.72f, 0.95f); // Wing membrane
            Color eyeGlow = new Color(1.0f, 0.22f, 0.22f, 1f); // Glowing red eye
            Color fangs = new Color(0.95f, 0.95f, 1f, 1f);

            // Bat body (Center circle)
            for (int x = 11; x <= 20; x++)
            {
                for (int y = 9; y <= 21; y++)
                {
                    float dx = (x - 15.5f) / 4.5f;
                    float dy = (y - 15f) / 6f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        tex.SetPixel(x, y, bodyColor);
                    }
                }
            }

            // Bat ears / horns
            tex.SetPixel(13, 22, bodyColor);
            tex.SetPixel(12, 23, bodyColor);
            tex.SetPixel(12, 24, wingColor);
            tex.SetPixel(18, 22, bodyColor);
            tex.SetPixel(19, 23, bodyColor);
            tex.SetPixel(19, 24, wingColor);

            // Left Wing
            for (int x = 1; x <= 12; x++)
            {
                for (int y = 14; y <= 26; y++)
                {
                    float t = (float)(12 - x) / 11f;
                    int wingTop = 20 + Mathf.RoundToInt(6f * Mathf.Sin(t * Mathf.PI));
                    int wingBottom = 15 - Mathf.RoundToInt(3f * Mathf.Cos(t * Mathf.PI * 2f));
                    if (y >= wingBottom && y <= wingTop)
                    {
                        Color c = (y >= wingTop - 2 || x <= 3) ? wingColor : wingMembrane;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Right Wing
            for (int x = 19; x <= 30; x++)
            {
                for (int y = 14; y <= 26; y++)
                {
                    float t = (float)(x - 19) / 11f;
                    int wingTop = 20 + Mathf.RoundToInt(6f * Mathf.Sin(t * Mathf.PI));
                    int wingBottom = 15 - Mathf.RoundToInt(3f * Mathf.Cos(t * Mathf.PI * 2f));
                    if (y >= wingBottom && y <= wingTop)
                    {
                        Color c = (y >= wingTop - 2 || x >= 28) ? wingColor : wingMembrane;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Glowing Red Eyes
            tex.SetPixel(14, 16, eyeGlow);
            tex.SetPixel(17, 16, eyeGlow);

            // Fangs
            tex.SetPixel(14, 12, fangs);
            tex.SetPixel(17, 12, fangs);
        }

        private static void DrawShooterSprite(Texture2D tex, int w, int h)
        {
            Color stoneDark = new Color(0.18f, 0.16f, 0.22f, 1f);
            Color stoneMid = new Color(0.32f, 0.28f, 0.38f, 1f);
            Color stoneLight = new Color(0.48f, 0.44f, 0.55f, 1f);
            Color coreGlow = new Color(1.0f, 0.55f, 0.12f, 1f); // Fiery molten orange core
            Color coreBright = new Color(1.0f, 0.92f, 0.35f, 1f);

            // Heavy pedestal base
            for (int x = 4; x <= 27; x++)
            {
                for (int y = 2; y <= 8; y++)
                {
                    Color c = (y == 8 || x == 4 || x == 27) ? stoneLight : (x % 4 == 0 ? stoneDark : stoneMid);
                    tex.SetPixel(x, y, c);
                }
            }

            // Upper turret carapace
            for (int x = 7; x <= 24; x++)
            {
                for (int y = 9; y <= 25; y++)
                {
                    float dx = (x - 15.5f) / 8.5f;
                    float dy = (y - 17f) / 8f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        Color c = (dx < 0 && dy > 0) ? stoneLight : stoneMid;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Cannon / Eye Aperture on right side
            for (int x = 16; x <= 25; x++)
            {
                for (int y = 14; y <= 20; y++)
                {
                    tex.SetPixel(x, y, stoneDark);
                }
            }

            // Glowing Molten Core inside the cannon
            for (int x = 17; x <= 23; x++)
            {
                for (int y = 15; y <= 19; y++)
                {
                    Color c = (x >= 19 && y == 17) ? coreBright : coreGlow;
                    tex.SetPixel(x, y, c);
                }
            }

            // Crown spikes / battlements on top
            tex.SetPixel(9, 26, stoneLight);
            tex.SetPixel(9, 27, stoneLight);
            tex.SetPixel(15, 26, stoneLight);
            tex.SetPixel(15, 28, stoneLight);
            tex.SetPixel(22, 26, stoneLight);
            tex.SetPixel(22, 27, stoneLight);
        }

        private static void DrawProjectileSprite(Texture2D tex, int w, int h)
        {
            Color coreWhite = new Color(1.0f, 1.0f, 0.95f, 1f);
            Color coreYellow = new Color(1.0f, 0.88f, 0.22f, 1f);
            Color auraOrange = new Color(1.0f, 0.42f, 0.08f, 0.85f);
            Color trailRed = new Color(0.85f, 0.12f, 0.05f, 0.45f);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float dx = (x - 8f) / 6f;
                    float dy = (y - 7.5f) / 4f;
                    float dist = dx * dx + dy * dy;

                    if (dist <= 0.25f) tex.SetPixel(x, y, coreWhite);
                    else if (dist <= 0.6f) tex.SetPixel(x, y, coreYellow);
                    else if (dist <= 1.0f) tex.SetPixel(x, y, auraOrange);
                    else if (x < 6 && Mathf.Abs(y - 7.5f) <= 2.5f) tex.SetPixel(x, y, trailRed); // Jet trail
                }
            }
        }
        #endregion

        #region Prefab Builders

        private static GameObject BuildProjectilePrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectileSpritePath);
            GameObject go = new GameObject("Enemy_Projectile");

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;

            Health health = go.AddComponent<Health>();
            SerializedObject hSo = new SerializedObject(health);
            hSo.FindProperty("maxHealth").intValue = 1;
            hSo.FindProperty("invincibilityDuration").floatValue = 0f;
            hSo.ApplyModifiedProperties();

            EnemyProjectile proj = go.AddComponent<EnemyProjectile>();
            GameObject impactFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DustParticle.prefab");
            SerializedObject pSo = new SerializedObject(proj);
            pSo.FindProperty("impactEffectPrefab").objectReferenceValue = impactFx;
            pSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ProjectilePrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildFlyerPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FlyerSpritePath);
            GameObject go = new GameObject("Enemy_Flyer");

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.65f;

            Health health = go.AddComponent<Health>();
            SerializedObject hSo = new SerializedObject(health);
            hSo.FindProperty("maxHealth").intValue = 1;
            hSo.FindProperty("invincibilityDuration").floatValue = 0.2f;
            hSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            hSo.FindProperty("rb").objectReferenceValue = rb;
            hSo.ApplyModifiedProperties();

            EnemyFlyer flyer = go.AddComponent<EnemyFlyer>();
            GameObject defeatFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyDefeatParticle.prefab");
            SerializedObject fSo = new SerializedObject(flyer);
            fSo.FindProperty("rb").objectReferenceValue = rb;
            fSo.FindProperty("health").objectReferenceValue = health;
            fSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            fSo.FindProperty("deathEffectPrefab").objectReferenceValue = defeatFx;
            fSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, FlyerPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildShooterPrefab(GameObject projPrefab)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShooterSpritePath);
            GameObject go = new GameObject("Enemy_Shooter");

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.1f, 1.2f);
            col.offset = new Vector2(0f, 0.1f);

            Health health = go.AddComponent<Health>();
            SerializedObject hSo = new SerializedObject(health);
            hSo.FindProperty("maxHealth").intValue = 2;
            hSo.FindProperty("invincibilityDuration").floatValue = 0.25f;
            hSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            hSo.FindProperty("rb").objectReferenceValue = rb;
            hSo.ApplyModifiedProperties();

            EnemyShooter shooter = go.AddComponent<EnemyShooter>();
            GameObject defeatFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyDefeatParticle.prefab");
            SerializedObject sSo = new SerializedObject(shooter);
            sSo.FindProperty("rb").objectReferenceValue = rb;
            sSo.FindProperty("health").objectReferenceValue = health;
            sSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            sSo.FindProperty("deathEffectPrefab").objectReferenceValue = defeatFx;
            sSo.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
            sSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ShooterPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }
        #endregion
    }
}
