using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Platformer.Player;

namespace Platformer.Editor
{
    /// <summary>
    /// Builds procedural sprite and prefab for the Player's Energy Blade Slash projectile,
    /// and configures the Player prefab with PlayerEnergy and combat projectile references.
    /// </summary>
    public static class EnergySlashBuilder
    {
        public const string SlashSpritePath = "Assets/Art/EnergySlash.png";
        public const string SlashPrefabPath = "Assets/Prefabs/Player_EnergySlash.prefab";
        public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        [MenuItem("Tools/Platformer/Combat/Generate Energy Slash Assets", priority = 40)]
        public static void GenerateEnergySlashAssets()
        {
            EnsureDirectories();
            GenerateSlashSprite();
            GameObject slashPrefab = BuildSlashPrefab();
            UpdatePlayerPrefab(slashPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#2ecc71>[EnergySlashBuilder]</color> Energy Slash Sprite, Prefab, and Player combat integration successfully created!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Art")) Directory.CreateDirectory("Assets/Art");
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        }

        private static void GenerateSlashSprite()
        {
            int w = 36;
            int h = 20;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clears = new Color[w * h];
            for (int i = 0; i < clears.Length; i++) clears[i] = Color.clear;
            tex.SetPixels(clears);

            Color coreWhite = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Color brightCyan = new Color(0.35f, 0.95f, 1.0f, 0.95f);
            Color outerBlue = new Color(0.12f, 0.55f, 0.95f, 0.75f);
            Color auraFaint = new Color(0.08f, 0.35f, 0.85f, 0.35f);

            // Draw glowing crescent arc facing right
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / (w - 1);
                // Parabolic crescent curve
                float centerY = h * 0.5f;
                float arcY = Mathf.Pow(nx - 0.7f, 2f) * 18f;

                for (int y = 0; y < h; y++)
                {
                    float distFromCenter = Mathf.Abs(y - centerY);
                    float arcThickness = 3.5f * Mathf.Sin(nx * Mathf.PI);

                    if (distFromCenter <= arcThickness && nx > 0.15f)
                    {
                        float edgeT = distFromCenter / (arcThickness + 0.01f);
                        Color c;
                        if (edgeT < 0.35f && nx > 0.45f) c = coreWhite;
                        else if (edgeT < 0.65f) c = brightCyan;
                        else if (edgeT < 0.95f) c = outerBlue;
                        else c = auraFaint;

                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(SlashSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SlashSpritePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(SlashSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f; // 36 px = 2.25 world units
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static GameObject BuildSlashPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SlashSpritePath);
            GameObject go = new GameObject("Player_EnergySlash");

            // Projectile is player weapon, but targets Enemy layer
            go.tag = "Untagged";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 12;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.0f, 1.2f);
            col.isTrigger = true;

            EnergySlashProjectile proj = go.AddComponent<EnergySlashProjectile>();
            GameObject impactFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyDefeatParticle.prefab");
            SerializedObject pSo = new SerializedObject(proj);
            pSo.FindProperty("impactEffectPrefab").objectReferenceValue = impactFx;
            pSo.FindProperty("damage").intValue = 2;
            pSo.FindProperty("speed").floatValue = 14.0f;
            pSo.ApplyModifiedProperties();

            // Add electric cyan 2D Point Light to Energy Slash
            var l2d = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            l2d.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            l2d.color = new Color(0.2f, 0.85f, 1f, 1f);
            l2d.pointLightInnerRadius = 0.5f;
            l2d.pointLightOuterRadius = 3.0f;
            l2d.intensity = 1.4f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, SlashPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        public static void UpdatePlayerPrefab(GameObject slashPrefab = null)
        {
            if (slashPrefab == null)
            {
                slashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlashPrefabPath);
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null) return;

            // Load prefab contents for editing
            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            PlayerEnergy energy = contents.GetComponent<PlayerEnergy>();
            if (energy == null) energy = contents.AddComponent<PlayerEnergy>();

            PlayerCoinMagnet magnet = contents.GetComponent<PlayerCoinMagnet>();
            if (magnet == null) magnet = contents.AddComponent<PlayerCoinMagnet>();

            PlayerPerksApplier perker = contents.GetComponent<PlayerPerksApplier>();
            if (perker == null) perker = contents.AddComponent<PlayerPerksApplier>();

            PlayerCombat combat = contents.GetComponent<PlayerCombat>();
            if (combat != null)
            {
                SerializedObject cSo = new SerializedObject(combat);
                cSo.FindProperty("playerEnergy").objectReferenceValue = energy;
                cSo.FindProperty("energySlashPrefab").objectReferenceValue = slashPrefab;
                cSo.FindProperty("energySlashCost").floatValue = 30f;
                cSo.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
