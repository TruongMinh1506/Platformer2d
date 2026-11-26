using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Platformer.Common;

namespace Platformer.Editor
{
    public static class DynamicPlatformBuilder
    {
        private const string MovingPlatformSpritePath = "Assets/Art/Tiles/MovingPlatform.png";
        private const string CrumblingPlatformSpritePath = "Assets/Art/Tiles/CrumblingPlatform.png";
        private const string JumpPadSpritePath = "Assets/Art/Tiles/JumpPad.png";

        private const string MovingPlatformPrefabPath = "Assets/Prefabs/Platform_Moving.prefab";
        private const string CrumblingPlatformPrefabPath = "Assets/Prefabs/Platform_Crumbling.prefab";
        private const string JumpPadPrefabPath = "Assets/Prefabs/Interactable_JumpPad.prefab";

        public static void EnsureAllDynamicPlatformPrefabs()
        {
            Sprite movingSprite = GenerateMovingPlatformSprite();
            Sprite crumblingSprite = GenerateCrumblingPlatformSprite();
            Sprite jumpPadSprite = GenerateJumpPadSprite();

            BuildMovingPlatformPrefab(movingSprite);
            BuildCrumblingPlatformPrefab(crumblingSprite);
            BuildJumpPadPrefab(jumpPadSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject BuildMovingPlatformPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Platform_Moving");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1) go.layer = groundLayer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.0f, 0.5f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            go.AddComponent<MovingPlatform>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, MovingPlatformPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildCrumblingPlatformPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Platform_Crumbling");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1) go.layer = groundLayer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.0f, 0.5f);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            go.AddComponent<CrumblingPlatform>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, CrumblingPlatformPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildJumpPadPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Interactable_JumpPad");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.0f, 0.45f);
            col.offset = new Vector2(0f, 0.1f);
            col.isTrigger = true;

            JumpPad jp = go.AddComponent<JumpPad>();
            GameObject dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DustParticle.prefab");
            if (dustPrefab != null)
            {
                SerializedObject so = new SerializedObject(jp);
                so.FindProperty("bounceDustPrefab").objectReferenceValue = dustPrefab;
                so.ApplyModifiedProperties();
            }

            GameObject labelGo = new GameObject("JumpPad_Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var tmp = labelGo.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "<color=#FFD700>[NHẢY]</color>";
            tmp.fontSize = 2.8f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.sortingOrder = 20;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, JumpPadPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite GenerateMovingPlatformSprite()
        {
            int w = 64; int h = 16;
            Color border = new Color(0.12f, 0.16f, 0.22f, 1f);
            Color body = new Color(0.24f, 0.32f, 0.42f, 1f);
            Color highlight = new Color(0.45f, 0.65f, 0.85f, 1f);
            Color neonCyan = new Color(0.25f, 0.95f, 1f, 1f);
            Color bolt = new Color(0.85f, 0.9f, 0.95f, 1f);

            return CreateSprite(MovingPlatformSpritePath, w, h, (x, y) =>
            {
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1) return border;
                if (y == h - 2) return highlight;

                // Center glowing power bar
                if (y >= 6 && y <= 8 && x >= 12 && x <= w - 13) return neonCyan;

                // Corner bolts
                if ((x >= 3 && x <= 5 || x >= w - 6 && x <= w - 4) && (y >= 4 && y <= 6 || y >= 9 && y <= 11))
                    return bolt;

                return body;
            });
        }

        private static Sprite GenerateCrumblingPlatformSprite()
        {
            int w = 64; int h = 16;
            Color border = new Color(0.18f, 0.14f, 0.12f, 1f);
            Color stone = new Color(0.48f, 0.42f, 0.36f, 1f);
            Color stoneDark = new Color(0.32f, 0.27f, 0.22f, 1f);
            Color crack = new Color(0.12f, 0.08f, 0.06f, 1f);

            return CreateSprite(CrumblingPlatformSpritePath, w, h, (x, y) =>
            {
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1) return border;

                // Jagged crack lines
                if ((Mathf.Abs(x - 22) <= 1 && y >= 3 && y <= 13) ||
                    (Mathf.Abs(x - (22 + (y - 5))) <= 1 && y >= 5 && y <= 11) ||
                    (Mathf.Abs(x - 44) <= 1 && y >= 2 && y <= 12) ||
                    (Mathf.Abs(x - (44 - (y - 4))) <= 1 && y >= 4 && y <= 10))
                {
                    return crack;
                }

                if ((x + y * 3) % 7 == 0) return stoneDark;
                return stone;
            });
        }

        private static Sprite GenerateJumpPadSprite()
        {
            int w = 32; int h = 20;
            Color plateTop = new Color(0.98f, 0.78f, 0.15f, 1f);
            Color plateRed = new Color(0.88f, 0.22f, 0.22f, 1f);
            Color border = new Color(0.15f, 0.15f, 0.18f, 1f);
            Color springMetal = new Color(0.68f, 0.72f, 0.78f, 1f);
            Color basePlate = new Color(0.28f, 0.32f, 0.38f, 1f);

            return CreateSprite(JumpPadSpritePath, w, h, (x, y) =>
            {
                // Base at bottom
                if (y <= 3)
                {
                    if (x < 4 || x > w - 5) return Color.clear;
                    if (y == 0 || x == 4 || x == w - 5) return border;
                    return basePlate;
                }

                // Coiled spring in middle
                if (y >= 4 && y <= 12)
                {
                    if (x < 10 || x > w - 11) return Color.clear;
                    int coilStep = (y + (x % 4)) % 4;
                    if (coilStep <= 1) return springMetal;
                    return Color.clear;
                }

                // Launch pad plate on top
                if (y >= 13)
                {
                    if (x < 2 || x > w - 3) return Color.clear;
                    if (y == 13 || y == h - 1 || x == 2 || x == w - 3) return border;
                    // Alternating yellow/red stripes
                    return ((x / 4) % 2 == 0) ? plateTop : plateRed;
                }

                return Color.clear;
            });
        }

        private static Sprite CreateSprite(string relativePath, int width, int height, Func<int, int, Color> pixelColor)
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, pixelColor(x, y));
                }
            }
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(fullPath, bytes);

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
