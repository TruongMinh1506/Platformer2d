using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Platformer.Editor
{
    /// <summary>
    /// Generates sprites and prefabs for Falling Stalactites, Thermal Geysers, and Campfire NPC.
    /// </summary>
    public static class InteractiveObjectsBuilder
    {
        public const string StalactiteSpritePath = "Assets/Art/Stalactite.png";
        public const string WandererSpritePath = "Assets/Art/OldWanderer.png";
        public const string GeyserSpritePath = "Assets/Art/ThermalGeyser.png";

        public static void GenerateAllInteractiveSprites()
        {
            GenerateStalactiteSprite();
            GenerateWandererSprite();
            GenerateGeyserSprite();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateStalactiteSprite()
        {
            int w = 16;
            int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clears = new Color[w * h];
            for (int i = 0; i < clears.Length; i++) clears[i] = Color.clear;
            tex.SetPixels(clears);

            Color rockBase = new Color(0.45f, 0.42f, 0.48f, 1f);
            Color rockDark = new Color(0.28f, 0.25f, 0.32f, 1f);
            Color rockLight = new Color(0.68f, 0.65f, 0.72f, 1f);
            Color rockHighlight = new Color(0.85f, 0.85f, 0.92f, 1f);

            // Inverted triangle / stalactite pointing DOWN
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1); // 0 at tip (bottom), 1 at base (top)
                float widthAtY = (ny * 6.5f) + 0.8f;
                int centerX = w / 2;

                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - centerX);
                    if (dx <= widthAtY)
                    {
                        if (x < centerX - 1) tex.SetPixel(x, y, rockLight);
                        else if (x == centerX - 1) tex.SetPixel(x, y, rockHighlight);
                        else if (x == centerX) tex.SetPixel(x, y, rockBase);
                        else tex.SetPixel(x, y, rockDark);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(StalactiteSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            ImportAsSprite(StalactiteSpritePath, 16f, new Vector2(0.5f, 1.0f));
        }

        private static void GenerateWandererSprite()
        {
            int w = 24;
            int h = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clears = new Color[w * h];
            for (int i = 0; i < clears.Length; i++) clears[i] = Color.clear;
            tex.SetPixels(clears);

            Color cloakMain = new Color(0.35f, 0.28f, 0.22f, 1f);
            Color cloakDark = new Color(0.22f, 0.16f, 0.12f, 1f);
            Color cowlLight = new Color(0.55f, 0.42f, 0.30f, 1f);
            Color skinShadow = new Color(0.15f, 0.12f, 0.15f, 0.95f);
            Color eyeGlint = new Color(0.3f, 0.9f, 1f, 1f);

            // Draw seated hooded figure
            for (int y = 2; y < 14; y++) // Body robe
            {
                for (int x = 4; x < 20; x++)
                {
                    tex.SetPixel(x, y, (x < 11) ? cowlLight : (x > 14 ? cloakDark : cloakMain));
                }
            }

            for (int y = 14; y < 24; y++) // Hood
            {
                for (int x = 7; x < 17; x++)
                {
                    tex.SetPixel(x, y, (x < 11) ? cowlLight : (x > 13 ? cloakDark : cloakMain));
                }
            }

            // Face shadow & glowing eye glint
            for (int y = 16; y < 20; y++)
            {
                for (int x = 10; x < 14; x++)
                {
                    tex.SetPixel(x, y, skinShadow);
                }
            }
            tex.SetPixel(11, 18, eyeGlint);
            tex.SetPixel(13, 18, eyeGlint);

            tex.Apply();
            File.WriteAllBytes(WandererSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            ImportAsSprite(WandererSpritePath, 16f, new Vector2(0.5f, 0f));
        }

        private static void GenerateGeyserSprite()
        {
            int w = 24;
            int h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clears = new Color[w * h];
            for (int i = 0; i < clears.Length; i++) clears[i] = Color.clear;
            tex.SetPixels(clears);

            Color rimDark = new Color(0.22f, 0.15f, 0.12f, 1f);
            Color rimBase = new Color(0.42f, 0.25f, 0.18f, 1f);
            Color lavaGlow = new Color(1.0f, 0.45f, 0.1f, 1f);
            Color lavaCore = new Color(1.0f, 0.85f, 0.3f, 1f);

            for (int y = 0; y < 8; y++)
            {
                for (int x = 2; x < 22; x++)
                {
                    tex.SetPixel(x, y, rimBase);
                }
            }

            for (int y = 4; y < 12; y++)
            {
                for (int x = 6; x < 18; x++)
                {
                    tex.SetPixel(x, y, y > 7 ? lavaCore : lavaGlow);
                }
            }

            tex.Apply();
            File.WriteAllBytes(GeyserSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            ImportAsSprite(GeyserSpritePath, 16f, new Vector2(0.5f, 0f));
        }

        private static void ImportAsSprite(string path, float ppu, Vector2 pivot)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = ppu;
                importer.spritePivot = pivot;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}
