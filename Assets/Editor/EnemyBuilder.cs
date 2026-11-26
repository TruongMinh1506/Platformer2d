using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer.Common;
using Platformer.Enemies;
using Platformer.Player;

namespace Platformer.Editor
{
    /// <summary>
    /// Editor automation tool for Task 3:
    /// Menu item Tools/Platformer/3 - Add Enemies To Level1.
    /// Creates procedural sprites and prefabs for Enemy_Patroller and Enemy_Chaser,
    /// updates the Player prefab with combat and health capabilities,
    /// and spawns 4 Patrollers and 2 Chasers in Level1.unity.
    /// </summary>
    public static class EnemyBuilder
    {
        private const string PatrollerSpritePath = "Assets/Art/EnemyPatroller.png";
        private const string ChaserSpritePath = "Assets/Art/EnemyChaser.png";
        private const string AttackSlashSpritePath = "Assets/Art/AttackSlash.png";

        private const string PatrollerPrefabPath = "Assets/Prefabs/Enemy_Patroller.prefab";
        private const string ChaserPrefabPath = "Assets/Prefabs/Enemy_Chaser.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string Level1ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/Platformer/3 - Add Enemies To Level1", priority = 3)]
        public static void AddEnemiesToLevel1()
        {
            // 1. Ensure tags and layers are fully set up
            TestSceneBuilder.SetupLayersAndTags();

            // 2. Ensure Level 1 exists
            if (!File.Exists(Level1ScenePath))
            {
                Level1Builder.CreateLevel1();
            }

            // 3. Generate procedural enemy sprites and slash effect
            Sprite patrollerSprite = GeneratePatrollerSprite();
            Sprite chaserSprite = GenerateChaserSprite();
            Sprite attackSlashSprite = GenerateAttackSlashSprite();

            // 4. Update Player Prefab with combat components and assign slash sprite
            UpdatePlayerPrefab(attackSlashSprite);

            // 5. Build Enemy_Patroller prefab
            GameObject patrollerPrefab = BuildPatrollerPrefab(patrollerSprite);

            // 6. Build Enemy_Chaser prefab
            GameObject chaserPrefab = BuildChaserPrefab(chaserSprite);

            // 7. Open Level 1 scene
            Scene scene = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);

            // 8. Find or create Enemies container
            GameObject enemiesRoot = GameObject.Find("Enemies");
            if (enemiesRoot != null)
            {
                // Clear existing enemies to prevent stacking
                for (int i = enemiesRoot.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(enemiesRoot.transform.GetChild(i).gameObject);
                }
            }
            else
            {
                enemiesRoot = new GameObject("Enemies");
            }

            // 9. Spawn 4 Patrollers on level platforms
            Vector3[] patrollerPositions = new Vector3[]
            {
                new Vector3(12f, 4f, 0f),  // Starting meadow (X: 1..20)
                new Vector3(30f, 4f, 0f),  // Stepped platform (X: 25..35)
                new Vector3(66f, 4f, 0f),  // Mid-level island (X: 62..71)
                new Vector3(102f, 4f, 0f)  // Final approach stretch (X: 96..118)
            };

            for (int i = 0; i < patrollerPositions.Length; i++)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(patrollerPrefab, scene);
                p.name = $"Enemy_Patroller_{i + 1}";
                p.transform.position = patrollerPositions[i];
                p.transform.SetParent(enemiesRoot.transform);
            }

            // 10. Spawn 2 Chasers in strategic challenge zones
            Vector3[] chaserPositions = new Vector3[]
            {
                new Vector3(47f, 4f, 0f),  // Platform before cavern pit (X: 41..53)
                new Vector3(83f, 4f, 0f)   // Upper platform before rhythm jumps (X: 78..88)
            };

            for (int i = 0; i < chaserPositions.Length; i++)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(chaserPrefab, scene);
                c.name = $"Enemy_Chaser_{i + 1}";
                c.transform.position = chaserPositions[i];
                c.transform.SetParent(enemiesRoot.transform);
            }

            // 11. Ensure player in scene has up-to-date combat components
            EnsureScenePlayerConfigured(attackSlashSprite);

            // 12. Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#2ecc71>[Platformer]</color> Successfully placed 4 Patrollers and 2 Chasers into Level1!");
        }

        private static void UpdatePlayerPrefab(Sprite slashSprite)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                TestSceneBuilder.CreateTestScene();
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);

            // Ensure Health
            Health health = instance.GetComponent<Health>();
            if (health == null) health = instance.AddComponent<Health>();

            // Ensure PlayerCombat
            PlayerCombat combat = instance.GetComponent<PlayerCombat>();
            if (combat == null) combat = instance.AddComponent<PlayerCombat>();

            SerializedObject combatSo = new SerializedObject(combat);
            combatSo.FindProperty("attackCooldown").floatValue = 0.4f;
            combatSo.FindProperty("attackRadius").floatValue = 0.85f;
            combatSo.FindProperty("attackDamage").intValue = 1;
            combatSo.FindProperty("enemyKnockbackForce").floatValue = 8.5f;
            combatSo.FindProperty("stompDamage").intValue = 1;
            combatSo.FindProperty("bounceForce").floatValue = 13.5f;
            combatSo.FindProperty("sideKnockbackForce").floatValue = 9.5f;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer == -1) enemyLayer = 8;
            combatSo.FindProperty("enemyLayer").intValue = 1 << enemyLayer;
            if (slashSprite != null)
            {
                combatSo.FindProperty("attackSlashSprite").objectReferenceValue = slashSprite;
            }
            combatSo.ApplyModifiedProperties();

            // Ensure PlayerDeath
            PlayerDeath death = instance.GetComponent<PlayerDeath>();
            if (death == null) death = instance.AddComponent<PlayerDeath>();

            PrefabUtility.SaveAsPrefabAsset(instance, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void EnsureScenePlayerConfigured(Sprite slashSprite)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Health health = player.GetComponent<Health>();
            if (health == null) health = player.AddComponent<Health>();

            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat == null) combat = player.AddComponent<PlayerCombat>();

            SerializedObject combatSo = new SerializedObject(combat);
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer == -1) enemyLayer = 8;
            combatSo.FindProperty("enemyLayer").intValue = 1 << enemyLayer;
            if (slashSprite != null)
            {
                combatSo.FindProperty("attackSlashSprite").objectReferenceValue = slashSprite;
            }
            combatSo.ApplyModifiedProperties();

            PlayerDeath death = player.GetComponent<PlayerDeath>();
            if (death == null) death = player.AddComponent<PlayerDeath>();
        }

        private static GameObject BuildPatrollerPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Enemy_Patroller");

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.85f);
            col.offset = Vector2.zero;

            Health health = go.AddComponent<Health>();
            SerializedObject hSo = new SerializedObject(health);
            hSo.FindProperty("maxHealth").intValue = 1;
            hSo.FindProperty("invincibilityDuration").floatValue = 0f;
            hSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            hSo.FindProperty("rb").objectReferenceValue = rb;
            hSo.ApplyModifiedProperties();

            EnemyPatroller patroller = go.AddComponent<EnemyPatroller>();
            SerializedObject pSo = new SerializedObject(patroller);
            pSo.FindProperty("moveSpeed").floatValue = 2.4f;
            pSo.FindProperty("initialDirection").intValue = -1;
            pSo.FindProperty("edgeCheckForwardOffset").floatValue = 0.55f;
            pSo.FindProperty("edgeCheckDistance").floatValue = 0.8f;
            pSo.FindProperty("wallCheckDistance").floatValue = 0.6f;
            pSo.FindProperty("rayOriginHeight").floatValue = -0.2f;

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 6;
            pSo.FindProperty("groundLayer").intValue = 1 << groundLayer;
            pSo.FindProperty("rb").objectReferenceValue = rb;
            pSo.FindProperty("health").objectReferenceValue = health;
            pSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PatrollerPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildChaserPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Enemy_Chaser");

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) go.layer = enemyLayer;
            go.tag = "Enemy";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.85f);
            col.offset = Vector2.zero;

            Health health = go.AddComponent<Health>();
            SerializedObject hSo = new SerializedObject(health);
            hSo.FindProperty("maxHealth").intValue = 2;
            hSo.FindProperty("invincibilityDuration").floatValue = 0.2f;
            hSo.FindProperty("blinkInterval").floatValue = 0.05f;
            hSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            hSo.FindProperty("rb").objectReferenceValue = rb;
            hSo.ApplyModifiedProperties();

            EnemyChaser chaser = go.AddComponent<EnemyChaser>();
            SerializedObject cSo = new SerializedObject(chaser);
            cSo.FindProperty("chaseSpeed").floatValue = 3.6f;
            cSo.FindProperty("detectionRange").floatValue = 7.5f;
            cSo.FindProperty("edgeCheckForwardOffset").floatValue = 0.55f;
            cSo.FindProperty("edgeCheckDistance").floatValue = 0.8f;
            cSo.FindProperty("wallCheckDistance").floatValue = 0.6f;
            cSo.FindProperty("rayOriginHeight").floatValue = -0.2f;

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 6;
            cSo.FindProperty("groundLayer").intValue = 1 << groundLayer;
            cSo.FindProperty("rb").objectReferenceValue = rb;
            cSo.FindProperty("health").objectReferenceValue = health;
            cSo.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ChaserPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite GeneratePatrollerSprite()
        {
            int width = 32;
            int height = 32;

            Color bodyColor = new Color(0.90f, 0.28f, 0.24f, 1f); // Vibrant crimson
            Color borderColor = new Color(0.48f, 0.14f, 0.12f, 1f); // Deep maroon
            Color spikeColor = new Color(0.95f, 0.70f, 0.20f, 1f); // Amber spikes
            Color eyeWhite = Color.white;
            Color eyePupil = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color legColor = new Color(0.25f, 0.15f, 0.15f, 1f);

            return CreateSpriteAsset(PatrollerSpritePath, width, height, (x, y) =>
            {
                // Inactive boundary
                if (x < 3 || x > 28 || y < 2 || y > 27) return Color.clear;

                // Crawler feet at bottom
                if (y <= 5)
                {
                    if ((x >= 5 && x <= 10) || (x >= 14 && x <= 18) || (x >= 22 && x <= 26))
                        return legColor;
                    return Color.clear;
                }

                // Top spikes
                if (y >= 23)
                {
                    if ((x >= 7 && x <= 9) || (x >= 15 && x <= 17) || (x >= 23 && x <= 25))
                    {
                        if (y == 27) return Color.white;
                        return spikeColor;
                    }
                    return Color.clear;
                }

                // Outer shell border
                if (x == 3 || x == 28 || y == 6 || y == 22 ||
                    ((x == 4 || x == 27) && (y <= 8 || y >= 20)))
                {
                    return borderColor;
                }

                // Angry eye (looking left by default)
                if (x >= 6 && x <= 12 && y >= 12 && y <= 17)
                {
                    if (x <= 8 && y >= 13 && y <= 16) return eyePupil;
                    return eyeWhite;
                }

                return bodyColor;
            });
        }

        private static Sprite GenerateChaserSprite()
        {
            int width = 32;
            int height = 32;

            Color bodyColor = new Color(0.56f, 0.24f, 0.72f, 1f); // Deep spectral purple
            Color borderColor = new Color(0.28f, 0.10f, 0.38f, 1f); // Midnight violet
            Color hornColor = new Color(0.85f, 0.20f, 0.40f, 1f); // Crimson horns
            Color eyeGlow = new Color(0.98f, 0.82f, 0.15f, 1f); // Glowing yellow eyes
            Color eyePupil = new Color(0.85f, 0.10f, 0.10f, 1f); // Red pupil

            return CreateSpriteAsset(ChaserSpritePath, width, height, (x, y) =>
            {
                if (x < 4 || x > 27 || y < 2 || y > 29) return Color.clear;

                // Horns at top
                if (y >= 24)
                {
                    if ((x >= 6 && x <= 9) || (x >= 22 && x <= 25))
                        return hornColor;
                    return Color.clear;
                }

                // Bottom claws/phantom tail
                if (y <= 5)
                {
                    if ((x >= 6 && x <= 11) || (x >= 20 && x <= 25))
                        return borderColor;
                    return Color.clear;
                }

                // Outer border
                if (x == 4 || x == 27 || y == 6 || y == 23 ||
                    ((x == 5 || x == 26) && (y <= 8 || y >= 21)))
                {
                    return borderColor;
                }

                // Two glowing eyes
                if (y >= 13 && y <= 18)
                {
                    if (x >= 7 && x <= 11)
                    {
                        if (x == 8 && y == 15) return eyePupil;
                        return eyeGlow;
                    }
                    if (x >= 15 && x <= 19)
                    {
                        if (x == 16 && y == 15) return eyePupil;
                        return eyeGlow;
                    }
                }

                return bodyColor;
            });
        }

        private static Sprite GenerateAttackSlashSprite()
        {
            int width = 32;
            int height = 32;

            Color slashBright = new Color(1f, 1f, 1f, 0.95f);
            Color slashGlow = new Color(0.35f, 0.85f, 1f, 0.8f);

            return CreateSpriteAsset(AttackSlashSpritePath, width, height, (x, y) =>
            {
                // Crescent slash arc centered on the left
                float cx = 6f;
                float cy = 16f;
                float distSq = (x - cx) * (x - cx) + (y - cy) * (y - cy);

                // Arc between radius 10 and 16
                if (distSq >= 90f && distSq <= 240f && x >= cx)
                {
                    if (distSq >= 130f && distSq <= 190f)
                    {
                        return slashBright;
                    }
                    return slashGlow;
                }

                return Color.clear;
            });
        }

        private static Sprite CreateSpriteAsset(string relativePath, int width, int height, Func<int, int, Color> pixelColor)
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

