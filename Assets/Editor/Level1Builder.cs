using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Platformer.Common;

namespace Platformer.Editor
{
    /// <summary>
    /// Editor automation tool under Tools/Platformer/2 - Create Level 1.
    /// Procedurally generates tile sprites and tile assets in Assets/Art/Tiles/,
    /// builds Assets/Scenes/Level1.unity with a Grid, Ground Tilemap (TilemapCollider2D +
    /// CompositeCollider2D, Rigidbody2D Static), decorative Background Tilemap,
    /// a 120-tile-wide level layout in a readable string array, player spawn, camera tracking with bounds,
    /// and a KillZone trigger below the level.
    /// </summary>
    public static class Level1Builder
    {
        private const string TileArtDirectory = "Assets/Art/Tiles";
        private const string LevelScenePath = "Assets/Scenes/Level1.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        private const string GroundTopSpritePath = "Assets/Art/Tiles/GroundTop.png";
        private const string GroundFillSpritePath = "Assets/Art/Tiles/GroundFill.png";
        private const string BrickPlatformSpritePath = "Assets/Art/Tiles/BrickPlatform.png";
        private const string BgWallSpritePath = "Assets/Art/Tiles/BgWall.png";
        private const string GoalMarkerSpritePath = "Assets/Art/Tiles/GoalMarker.png";

        private const string GroundTopTilePath = "Assets/Art/Tiles/GroundTopTile.asset";
        private const string GroundFillTilePath = "Assets/Art/Tiles/GroundFillTile.asset";
        private const string BrickPlatformTilePath = "Assets/Art/Tiles/BrickPlatformTile.asset";
        private const string BgWallTilePath = "Assets/Art/Tiles/BgWallTile.asset";

        /// <summary>
        /// Readable ASCII Level Layout (16 rows high x 120 columns wide).
        /// Row index 0 represents the highest Y coordinate (Y = 15);
        /// Row index 15 represents the lowest Y coordinate (Y = 0).
        /// 
        /// Legend:
        ///   '.' = Empty Air
        ///   'T' = Ground Top (Grass on top, dirt underneath)
        ///   'G' = Ground Fill (Solid underground dirt)
        ///   'B' = Brick Platform (Collidable floating bricks)
        ///   'W' = Boundary Wall (Solid boundary pillar)
        ///   '#' = Decorative Background Wall (Non-collidable)
        ///   'H' = Brick Platform + Background Wall behind it
        ///   'S' = Player Spawn Location
        ///   'E' = Level Goal / End Marker Location
        /// </summary>
        private static readonly string[] LevelLayout = new string[]
        {
            // Row 0 (Y=15 - Top boundary ceiling & sky)
            "W......................................................................................................................W",
            // Row 1 (Y=14)
            "W......................................................................................................................W",
            // Row 2 (Y=13)
            "W......................................................................................................................W",
            // Row 3 (Y=12)
            "W......................................................................................................................W",
            // Row 4 (Y=11 - High route challenge platforms)
            "W................................................................BBBB...................BBBB...........................W",
            // Row 5 (Y=10)
            "W......................................................................................................................W",
            // Row 6 (Y=9 - Mid-air stepping platforms)
            "W......................................BBBB..................................BBBB......................................W",
            // Row 7 (Y=8 - Staggered climbing ledge)
            "W...................................................BBBBB.........................................BBBB.................W",
            // Row 8 (Y=7 - Lower platforms over pit 1 & pit 3)
            "W.......................BBBB...................................BBBB..................BBBB..............................W",
            // Row 9 (Y=6 - Starting gate archway & mid platforms)
            "W..#####....................................BBBBB.......................................................BBBB...........W",
            // Row 10 (Y=5 - Archway continuation & goal dais peak)
            "W..#####.......BBBB.............BBBB.......................BBBB..............................................E.........W",
            // Row 11 (Y=4 - Player spawn 'S' & brick dais base)
            "W..##S##....................BBBB...............................................BBBB.......................BBBBBBB......W",
            // Row 12 (Y=3 - Main ground surface with pits & gaps)
            "WTTTTTTTTTTTTTTTTTTTT....TTTTTTTTTTT.....TTTTTTTTTTTTT........TTTTTTTTTT......TTTTTTTTTTT.......TTTTTTTTTTTTTTTTTTTTTTTW",
            // Row 13 (Y=2 - Soil sublayer)
            "WGGGGGGGGGGGGGGGGGGGG....GGGGGGGGGGG.....GGGGGGGGGGGGG........GGGGGGGGGG......GGGGGGGGGGG.......GGGGGGGGGGGGGGGGGGGGGGGW",
            // Row 14 (Y=1 - Deep soil)
            "WGGGGGGGGGGGGGGGGGGGG....GGGGGGGGGGG.....GGGGGGGGGGGGG........GGGGGGGGGG......GGGGGGGGGGG.......GGGGGGGGGGGGGGGGGGGGGGGW",
            // Row 15 (Y=0 - Bottom bedrock)
            "WGGGGGGGGGGGGGGGGGGGG....GGGGGGGGGGG.....GGGGGGGGGGGGG........GGGGGGGGGG......GGGGGGGGGGG.......GGGGGGGGGGGGGGGGGGGGGGGW"
        };

        [MenuItem("Tools/Platformer/2 - Create Level 1", priority = 2)]
        public static void CreateLevel1()
        {
            // 1. Ensure required project layers & tags exist
            TestSceneBuilder.SetupLayersAndTags();

            // 2. Ensure Tile Art directories exist
            EnsureDirectories();

            // 3. Generate procedural tile sprites
            Sprite groundTopSprite = GenerateGroundTopSprite();
            Sprite groundFillSprite = GenerateGroundFillSprite();
            Sprite brickPlatformSprite = GenerateBrickPlatformSprite();
            Sprite bgWallSprite = GenerateBgWallSprite();
            Sprite goalMarkerSprite = GenerateGoalMarkerSprite();

            // 4. Generate or update Tile ScriptableObject assets
            Tile groundTopTile = GetOrCreateTile(GroundTopTilePath, groundTopSprite, Tile.ColliderType.Grid);
            Tile groundFillTile = GetOrCreateTile(GroundFillTilePath, groundFillSprite, Tile.ColliderType.Grid);
            Tile brickPlatformTile = GetOrCreateTile(BrickPlatformTilePath, brickPlatformSprite, Tile.ColliderType.Grid);
            Tile bgWallTile = GetOrCreateTile(BgWallTilePath, bgWallSprite, Tile.ColliderType.None);

            // 5. Ensure Player Prefab exists
            EnsurePlayerPrefabExists();

            // 6. Create new clean scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 7. Configure Main Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(8f, 5.5f, -10f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.14f, 0.19f, 1f); // Deep night sky

            camGo.AddComponent<AudioListener>();

            CameraFollow camFollow = camGo.AddComponent<CameraFollow>();
            SerializedObject camSo = new SerializedObject(camFollow);
            camSo.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
            camSo.FindProperty("smoothTime").floatValue = 0.15f;
            camSo.FindProperty("useBounds").boolValue = true;
            camSo.FindProperty("minBounds").vector2Value = new Vector2(10.5f, 5.5f);
            camSo.FindProperty("maxBounds").vector2Value = new Vector2(109.5f, 10.5f);
            camSo.ApplyModifiedProperties();

            // 8. Add 2D Universal Global Light
            TryAddGlobalLight2D();

            // 9. Create Grid & Tilemaps
            GameObject gridGo = new GameObject("Grid");
            Grid grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            // Decorative Background Tilemap (Sorting order -5, no collider)
            GameObject bgTilemapGo = new GameObject("Background");
            bgTilemapGo.transform.SetParent(gridGo.transform, false);
            Tilemap bgTilemap = bgTilemapGo.AddComponent<Tilemap>();
            TilemapRenderer bgRenderer = bgTilemapGo.AddComponent<TilemapRenderer>();
            bgRenderer.sortingOrder = -5;

            // Collidable Ground Tilemap (TilemapCollider2D + CompositeCollider2D, Rigidbody2D Static, layer Ground)
            GameObject groundTilemapGo = new GameObject("Ground");
            groundTilemapGo.transform.SetParent(gridGo.transform, false);

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 6;
            groundTilemapGo.layer = groundLayer;

            Tilemap groundTilemap = groundTilemapGo.AddComponent<Tilemap>();
            TilemapRenderer groundRenderer = groundTilemapGo.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = 0;

            TilemapCollider2D tilemapCollider = groundTilemapGo.AddComponent<TilemapCollider2D>();
            CompositeCollider2D compositeCollider = groundTilemapGo.AddComponent<CompositeCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;

            Rigidbody2D tilemapRb = groundTilemapGo.GetComponent<Rigidbody2D>();
            if (tilemapRb == null) tilemapRb = groundTilemapGo.AddComponent<Rigidbody2D>();
            tilemapRb.bodyType = RigidbodyType2D.Static;

            // 10. Paint level layout from ASCII array
            Vector3Int spawnCoord = new Vector3Int(4, 4, 0);
            Vector3Int endCoord = new Vector3Int(114, 5, 0);

            int totalRows = LevelLayout.Length;
            for (int r = 0; r < totalRows; r++)
            {
                int y = totalRows - 1 - r;
                string rowStr = LevelLayout[r];

                for (int x = 0; x < rowStr.Length; x++)
                {
                    char c = rowStr[x];
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    switch (c)
                    {
                        case 'T':
                            groundTilemap.SetTile(pos, groundTopTile);
                            break;
                        case 'G':
                            groundTilemap.SetTile(pos, groundFillTile);
                            break;
                        case 'B':
                        case 'W':
                            groundTilemap.SetTile(pos, brickPlatformTile);
                            break;
                        case '#':
                            bgTilemap.SetTile(pos, bgWallTile);
                            break;
                        case 'H':
                            groundTilemap.SetTile(pos, brickPlatformTile);
                            bgTilemap.SetTile(pos, bgWallTile);
                            break;
                        case 'S':
                            spawnCoord = pos;
                            bgTilemap.SetTile(pos, bgWallTile);
                            break;
                        case 'E':
                            endCoord = pos;
                            break;
                        case '.':
                        default:
                            break;
                    }
                }
            }

            // Refresh tilemap collider geometry
            tilemapCollider.ProcessTilemapChanges();
            compositeCollider.GenerateGeometry();

            // 11. Create PlayerSpawn empty object and instantiate Player prefab
            GameObject playerSpawn = new GameObject("PlayerSpawn");
            Vector3 spawnWorldPos = new Vector3(spawnCoord.x + 0.5f, spawnCoord.y + 0.5f, 0f);
            playerSpawn.transform.position = spawnWorldPos;

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab != null)
            {
                GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInstance.transform.position = spawnWorldPos;
                camFollow.SetTarget(playerInstance.transform);
            }
            else
            {
                Debug.LogWarning("[Platformer] Player prefab not found at " + PlayerPrefabPath);
            }

            // 12. Create Goal marker at 'E' coordinate
            GameObject goalMarker = new GameObject("EndGoal");
            goalMarker.transform.position = new Vector3(endCoord.x + 0.5f, endCoord.y + 0.75f, 0f);
            SpriteRenderer goalSr = goalMarker.AddComponent<SpriteRenderer>();
            goalSr.sprite = goalMarkerSprite;
            goalSr.sortingOrder = 5;

            // 13. Create KillZone trigger below the level
            GameObject killZoneGo = new GameObject("KillZone");
            killZoneGo.transform.position = new Vector3(60f, -8f, 0f);

            BoxCollider2D killCol = killZoneGo.AddComponent<BoxCollider2D>();
            killCol.isTrigger = true;
            killCol.size = new Vector2(300f, 20f);
            killZoneGo.AddComponent<KillZone>();

            // 14. Save scene and register in Build Settings
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, LevelScenePath);

            var currentScenes = EditorBuildSettings.scenes.ToList();
            if (!currentScenes.Any(s => s.path == LevelScenePath))
            {
                currentScenes.Add(new EditorBuildSettingsScene(LevelScenePath, true));
                EditorBuildSettings.scenes = currentScenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#2ecc71>[Platformer]</color> Level 1 successfully generated at '{LevelScenePath}'!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs =
            {
                "Assets/Art",
                "Assets/Art/Tiles",
                "Assets/Prefabs",
                "Assets/Scenes",
                "Assets/Scripts/Common"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }

        private static void EnsurePlayerPrefabExists()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                // Trigger TestSceneBuilder to create procedural player sprite & prefab
                TestSceneBuilder.CreateTestScene();
            }
        }

        private static Tile GetOrCreateTile(string assetPath, Sprite sprite, Tile.ColliderType colliderType)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = colliderType;
                AssetDatabase.CreateAsset(tile, assetPath);
            }
            else
            {
                tile.sprite = sprite;
                tile.colliderType = colliderType;
                EditorUtility.SetDirty(tile);
            }
            return tile;
        }

        private static Sprite GenerateGroundTopSprite()
        {
            int width = 32;
            int height = 32;

            Color grassHighlight = new Color(0.55f, 0.88f, 0.35f, 1f);
            Color grassMain = new Color(0.28f, 0.72f, 0.30f, 1f);
            Color grassRoot = new Color(0.18f, 0.48f, 0.20f, 1f);
            Color soilBase = new Color(0.38f, 0.28f, 0.20f, 1f);
            Color soilSpeck = new Color(0.48f, 0.36f, 0.26f, 1f);
            Color soilDark = new Color(0.28f, 0.20f, 0.14f, 1f);

            return CreateTileSprite(GroundTopSpritePath, width, height, (x, y) =>
            {
                // Grass surface
                if (y == 31) return grassHighlight;
                if (y >= 27) return grassMain;
                if (y == 26) return ((x % 3 == 0) ? grassRoot : grassMain);

                // Soil body
                if ((x * 5 + y * 11) % 7 == 0) return soilSpeck;
                if ((x * 7 + y * 13) % 9 == 0) return soilDark;
                return soilBase;
            });
        }

        private static Sprite GenerateGroundFillSprite()
        {
            int width = 32;
            int height = 32;

            Color soilBase = new Color(0.34f, 0.25f, 0.18f, 1f);
            Color soilSpeck = new Color(0.44f, 0.33f, 0.24f, 1f);
            Color soilDark = new Color(0.25f, 0.18f, 0.13f, 1f);

            return CreateTileSprite(GroundFillSpritePath, width, height, (x, y) =>
            {
                if ((x * 7 + y * 17) % 11 == 0) return soilSpeck;
                if ((x * 13 + y * 5) % 13 == 0) return soilDark;
                return soilBase;
            });
        }

        private static Sprite GenerateBrickPlatformSprite()
        {
            int width = 32;
            int height = 32;

            Color mortar = new Color(0.12f, 0.15f, 0.18f, 1f);
            Color brickFace = new Color(0.32f, 0.42f, 0.50f, 1f);
            Color brickHighlight = new Color(0.46f, 0.58f, 0.68f, 1f);
            Color brickShadow = new Color(0.20f, 0.28f, 0.35f, 1f);

            return CreateTileSprite(BrickPlatformSpritePath, width, height, (x, y) =>
            {
                // Horizontal mortar seams
                if (y == 0 || y == 15 || y == 31) return mortar;

                // Vertical mortar seams (staggered bricks)
                if (y > 15 && (x == 0 || x == 16 || x == 31)) return mortar;
                if (y < 15 && (x == 0 || x == 8 || x == 24 || x == 31)) return mortar;

                // Brick bevel highlights & shadows
                if (y == 14 || y == 30) return brickHighlight;
                if (y == 1 || y == 16) return brickShadow;

                return brickFace;
            });
        }

        private static Sprite GenerateBgWallSprite()
        {
            int width = 32;
            int height = 32;

            Color darkMortar = new Color(0.08f, 0.10f, 0.14f, 1f);
            Color darkWall = new Color(0.14f, 0.17f, 0.22f, 1f);
            Color darkWallAlt = new Color(0.17f, 0.21f, 0.26f, 1f);

            return CreateTileSprite(BgWallSpritePath, width, height, (x, y) =>
            {
                if (y == 0 || y == 15 || y == 31) return darkMortar;
                if (y > 15 && (x == 0 || x == 16 || x == 31)) return darkMortar;
                if (y < 15 && (x == 0 || x == 8 || x == 24 || x == 31)) return darkMortar;

                return ((x + y) % 4 == 0) ? darkWallAlt : darkWall;
            });
        }

        private static Sprite GenerateGoalMarkerSprite()
        {
            int width = 32;
            int height = 48;

            Color goldBase = new Color(0.95f, 0.72f, 0.15f, 1f);
            Color goldHighlight = new Color(1f, 0.88f, 0.35f, 1f);
            Color poleSilver = new Color(0.75f, 0.78f, 0.82f, 1f);
            Color poleHighlight = new Color(0.90f, 0.92f, 0.95f, 1f);
            Color baseStone = new Color(0.35f, 0.40f, 0.48f, 1f);

            return CreateTileSprite(GoalMarkerSpritePath, width, height, (x, y) =>
            {
                // Stone pedestal at bottom
                if (y <= 4)
                {
                    if (x >= 4 && x <= 27) return baseStone;
                    return Color.clear;
                }

                // Vertical flagpole (x: 8..9)
                if (x == 8) return poleHighlight;
                if (x == 9) return poleSilver;

                // Gold sphere topper
                if (y >= 43 && y <= 46 && x >= 7 && x <= 10)
                {
                    return goldHighlight;
                }

                // Pennant flag waving right (x: 10..29, y: 24..42)
                if (x >= 10 && x <= 28 && y >= 25 && y <= 42)
                {
                    // Triangle cutout tapering at right edge
                    int relX = x - 10;
                    int inset = relX / 2;
                    if (y >= (25 + inset) && y <= (42 - inset))
                    {
                        if (y == 42 - inset || y == 25 + inset) return goldHighlight;
                        return goldBase;
                    }
                }

                return Color.clear;
            });
        }

        private static Sprite CreateTileSprite(string relativePath, int width, int height, Func<int, int, Color> pixelColor)
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
    }
}

