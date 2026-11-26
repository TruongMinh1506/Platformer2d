using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using TMPro;
using Platformer.Common;
using Platformer.Enemies;
using Platformer.Managers;
using Platformer.Player;
using Platformer.UI;

namespace Platformer.Editor
{
    /// <summary>
    /// Common shared level generation engine refactored for Levels 1, 2, and 3,
    /// Main Menu, and Win Scene. Eliminates duplicated level setup code.
    /// </summary>
    public static class LevelBuilder
    {
        public class LevelData
        {
            public string levelName;
            public string scenePath;
            public string nextSceneName;
            public string[] layout;
            public Color skyColor;
            public Color lightColor;
            public float lightIntensity;
            public Vector2 minBounds;
            public Vector2 maxBounds;
            public Vector3[] patrollerPositions;
            public Vector3[] chaserPositions;
            public Vector3[] flyerPositions;
            public Vector3[] shooterPositions;
            public Vector3[] coinPositions;
            public Vector3[] heartPositions;
            public Vector3[] spikePositions;
            public Vector3[] checkpointPositions;
            public Vector3[] movingPlatformPositions;
            public Vector3[] movingPlatformOffsets;
            public Vector3[] crumblingPlatformPositions;
            public Vector3[] jumpPadPositions;
            public Vector3[] secretWallPositions;
            public Vector3[] relicPositions;
        }

        public static void BuildLevel(LevelData data)
        {
            // 1. Ensure tags, layers, and TMP resources
            TestSceneBuilder.SetupLayersAndTags();
            EnsureTMPEssentials();

            // 2. Ensure sprites and tile assets
            EnsureSpritesAndTiles();

            // 3. Ensure prefabs
            EnsureAllPrefabs();

            // 4. Create new empty scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 5. Setup Camera & CameraFollow
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(8f, 5.5f, -10f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = data.skyColor;
            camGo.AddComponent<AudioListener>();

            CameraFollow camFollow = camGo.AddComponent<CameraFollow>();
            SerializedObject camSo = new SerializedObject(camFollow);
            camSo.FindProperty("offset").vector3Value = new Vector3(0f, 1.2f, -10f);
            camSo.FindProperty("smoothTime").floatValue = 0.15f;
            camSo.FindProperty("useBounds").boolValue = true;
            camSo.FindProperty("minBounds").vector2Value = data.minBounds;
            camSo.FindProperty("maxBounds").vector2Value = data.maxBounds;
            camSo.ApplyModifiedProperties();

            // 5b. Setup URP Post-Processing and Global Volume (Bloom, Vignette, Tonemapping)
            var camData = camGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null) camData = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            GameObject volumeGo = new GameObject("Global PostProcess Volume");
            UnityEngine.Rendering.Volume vol = volumeGo.AddComponent<UnityEngine.Rendering.Volume>();
            vol.isGlobal = true;
            var volProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/DefaultVolumeProfile.asset");
            if (volProfile != null) vol.profile = volProfile;

            // 6. Setup 2D Global Light
            TryAddGlobalLight2D(data.lightColor, data.lightIntensity);

            // 6b. Setup Multi-layer Parallax Background & Atmospheric Weather
            ParallaxBuilder.BuildParallaxForLevel(data.levelName);
            ParallaxBuilder.BuildWeatherForLevel(data.levelName);

            // 7. Setup Grid & Tilemaps
            GameObject gridGo = new GameObject("Grid");
            Grid grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            // Background Tilemap
            GameObject bgTilemapGo = new GameObject("Background");
            bgTilemapGo.transform.SetParent(gridGo.transform, false);
            Tilemap bgTilemap = bgTilemapGo.AddComponent<Tilemap>();
            TilemapRenderer bgRenderer = bgTilemapGo.AddComponent<TilemapRenderer>();
            bgRenderer.sortingOrder = -5;

            // Ground Tilemap
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

            // 8. Paint Level Tilemaps from ASCII Array
            Tile topTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/GroundTopTile.asset");
            Tile fillTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/GroundFillTile.asset");
            Tile brickTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/BrickPlatformTile.asset");
            Tile bgTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/BgWallTile.asset");

            Vector3Int spawnCoord = new Vector3Int(4, 4, 0);
            Vector3Int endCoord = new Vector3Int(114, 5, 0);

            int totalRows = data.layout.Length;
            for (int r = 0; r < totalRows; r++)
            {
                int y = totalRows - 1 - r;
                string rowStr = data.layout[r];
                for (int x = 0; x < rowStr.Length; x++)
                {
                    char c = rowStr[x];
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    switch (c)
                    {
                        case 'T': groundTilemap.SetTile(pos, topTile); break;
                        case 'G': groundTilemap.SetTile(pos, fillTile); break;
                        case 'B':
                        case 'W': groundTilemap.SetTile(pos, brickTile); break;
                        case '#': bgTilemap.SetTile(pos, bgTile); break;
                        case 'H':
                            groundTilemap.SetTile(pos, brickTile);
                            bgTilemap.SetTile(pos, bgTile);
                            break;
                        case 'S':
                            spawnCoord = pos;
                            bgTilemap.SetTile(pos, bgTile);
                            break;
                        case 'E':
                            endCoord = pos;
                            break;
                    }
                }
            }

            tilemapCollider.ProcessTilemapChanges();
            compositeCollider.GenerateGeometry();

            // 9. Player Spawn & Player Instance
            GameObject playerSpawn = new GameObject("PlayerSpawn");
            Vector3 spawnWorldPos = new Vector3(spawnCoord.x + 0.5f, spawnCoord.y + 0.5f, 0f);
            playerSpawn.transform.position = spawnWorldPos;

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab != null)
            {
                GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
                playerInstance.transform.position = spawnWorldPos;
                camFollow.SetTarget(playerInstance.transform);
                AddPlayerLanternLight(playerInstance);
            }

            // 10. KillZone Trigger Below Level (Deep and wide to catch all falling entities)
            GameObject killZoneGo = new GameObject("KillZone");
            killZoneGo.transform.position = new Vector3(60f, -8f, 0f);
            BoxCollider2D killCol = killZoneGo.AddComponent<BoxCollider2D>();
            killCol.isTrigger = true;
            killCol.size = new Vector2(300f, 20f);
            killZoneGo.AddComponent<KillZone>();

            // 11. GameManager & Audio & Advanced Systems
            GameObject gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();
            gmGo.AddComponent<ComboRankManager>();

            EnsureAchievementManager();
            EnsureAudioManager();

            // 12. Checkpoints
            GameObject cpRoot = new GameObject("Checkpoints");
            GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Checkpoint.prefab");
            for (int i = 0; i < data.checkpointPositions.Length; i++)
            {
                GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(cpPrefab, scene);
                cp.name = $"Checkpoint_{i + 1}";
                cp.transform.position = EnsureSafeAboveSurface(data.checkpointPositions[i], data.layout, 0.25f);
                cp.transform.SetParent(cpRoot.transform);
            }

            // 13. Collectibles
            GameObject colRoot = new GameObject("Collectibles");
            GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Collectible_Coin.prefab");
            GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Collectible_Heart.prefab");

            for (int i = 0; i < data.coinPositions.Length; i++)
            {
                GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, scene);
                coin.name = $"Coin_{i + 1}";
                coin.transform.position = EnsureSafeAboveSurface(data.coinPositions[i], data.layout, 0.6f);
                coin.transform.SetParent(colRoot.transform);
            }

            for (int i = 0; i < data.heartPositions.Length; i++)
            {
                GameObject heart = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab, scene);
                heart.name = $"Heart_{i + 1}";
                heart.transform.position = EnsureSafeAboveSurface(data.heartPositions[i], data.layout, 0.6f);
                heart.transform.SetParent(colRoot.transform);
            }

            // 14. Hazards (Spikes)
            GameObject hazardRoot = new GameObject("Hazards");
            GameObject spikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hazard_Spikes.prefab");
            for (int i = 0; i < data.spikePositions.Length; i++)
            {
                GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(spikePrefab, scene);
                spike.name = $"Spikes_{i + 1}";
                spike.transform.position = AlignHazardToSurface(data.spikePositions[i], data.layout, 0.47f);
                spike.transform.SetParent(hazardRoot.transform);
            }

            // 15. Enemies
            GameObject enemyRoot = new GameObject("Enemies");
            GameObject patrollerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Patroller.prefab");
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chaser.prefab");

            for (int i = 0; i < data.patrollerPositions.Length; i++)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(patrollerPrefab, scene);
                p.name = $"Enemy_Patroller_{i + 1}";
                p.transform.position = EnsureSafeAboveSurface(data.patrollerPositions[i], data.layout, 0.5f);
                p.transform.SetParent(enemyRoot.transform);
            }

            for (int i = 0; i < data.chaserPositions.Length; i++)
            {
                GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(chaserPrefab, scene);
                c.name = $"Enemy_Chaser_{i + 1}";
                c.transform.position = EnsureSafeAboveSurface(data.chaserPositions[i], data.layout, 0.5f);
                c.transform.SetParent(enemyRoot.transform);
            }

            GameObject flyerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NewEnemiesBuilder.FlyerPrefabPath);
            if (data.flyerPositions != null && flyerPrefab != null)
            {
                for (int i = 0; i < data.flyerPositions.Length; i++)
                {
                    GameObject f = (GameObject)PrefabUtility.InstantiatePrefab(flyerPrefab, scene);
                    f.name = $"Enemy_Flyer_{i + 1}";
                    f.transform.position = data.flyerPositions[i];
                    var flyerScript = f.GetComponent<EnemyFlyer>();
                    if (flyerScript != null) flyerScript.SetAnchorPosition(data.flyerPositions[i]);
                    f.transform.SetParent(enemyRoot.transform);
                }
            }

            GameObject shooterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NewEnemiesBuilder.ShooterPrefabPath);
            if (data.shooterPositions != null && shooterPrefab != null)
            {
                for (int i = 0; i < data.shooterPositions.Length; i++)
                {
                    GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(shooterPrefab, scene);
                    s.name = $"Enemy_Shooter_{i + 1}";
                    s.transform.position = EnsureSafeAboveSurface(data.shooterPositions[i], data.layout, 0.6f);
                    s.transform.SetParent(enemyRoot.transform);
                }
            }

            // 15b. Dynamic Platforms & Jump Pads
            GameObject dynamicRoot = new GameObject("DynamicPlatforms");
            GameObject movingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Platform_Moving.prefab");
            GameObject crumblingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Platform_Crumbling.prefab");
            GameObject jumpPadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactable_JumpPad.prefab");

            if (data.movingPlatformPositions != null && movingPrefab != null)
            {
                for (int i = 0; i < data.movingPlatformPositions.Length; i++)
                {
                    GameObject mp = (GameObject)PrefabUtility.InstantiatePrefab(movingPrefab, scene);
                    mp.name = $"Platform_Moving_{i + 1}";
                    mp.transform.position = data.movingPlatformPositions[i];
                    if (data.movingPlatformOffsets != null && i < data.movingPlatformOffsets.Length)
                    {
                        var script = mp.GetComponent<MovingPlatform>();
                        if (script != null) script.SetMoveOffset(data.movingPlatformOffsets[i]);
                    }
                    mp.transform.SetParent(dynamicRoot.transform);
                }
            }

            if (data.crumblingPlatformPositions != null && crumblingPrefab != null)
            {
                for (int i = 0; i < data.crumblingPlatformPositions.Length; i++)
                {
                    GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(crumblingPrefab, scene);
                    cp.name = $"Platform_Crumbling_{i + 1}";
                    cp.transform.position = data.crumblingPlatformPositions[i];
                    cp.transform.SetParent(dynamicRoot.transform);
                }
            }

            if (data.jumpPadPositions != null && jumpPadPrefab != null)
            {
                for (int i = 0; i < data.jumpPadPositions.Length; i++)
                {
                    GameObject jp = (GameObject)PrefabUtility.InstantiatePrefab(jumpPadPrefab, scene);
                    jp.name = $"JumpPad_{i + 1}";
                    jp.transform.position = data.jumpPadPositions[i];
                    jp.transform.SetParent(dynamicRoot.transform);
                }
            }

            // 15c. Secret Breakable Walls & Ancient Relics
            GameObject secretWallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SecretWallBuilder.WallPrefabPath);
            GameObject relicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SecretWallBuilder.RelicPrefabPath);

            if (data.secretWallPositions != null && secretWallPrefab != null)
            {
                for (int i = 0; i < data.secretWallPositions.Length; i++)
                {
                    GameObject sw = (GameObject)PrefabUtility.InstantiatePrefab(secretWallPrefab, scene);
                    sw.name = $"Secret_BreakableWall_{i + 1}";
                    sw.transform.position = data.secretWallPositions[i];
                    sw.transform.SetParent(dynamicRoot.transform);
                }
            }

            if (data.relicPositions != null && relicPrefab != null)
            {
                for (int i = 0; i < data.relicPositions.Length; i++)
                {
                    GameObject rel = (GameObject)PrefabUtility.InstantiatePrefab(relicPrefab, scene);
                    rel.name = $"Collectible_Relic_{i + 1}";
                    rel.transform.position = data.relicPositions[i];
                    rel.transform.SetParent(colRoot.transform);
                }
            }

            // 16. Build Unified HUD (HUDController + PauseMenuController + LevelComplete UI)
            GameObject canvasGo = BuildUnifiedHUD(data.nextSceneName);

            // 17. LevelGoal Prefab at End Marker 'E'
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LevelGoal.prefab");
            GameObject goalInstance = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab, scene);
            goalInstance.transform.position = new Vector3(endCoord.x + 0.5f, endCoord.y + 0.75f, 0f);

            LevelGoal goalScript = goalInstance.GetComponent<LevelGoal>();
            if (goalScript != null)
            {
                goalScript.SetNextSceneName(data.nextSceneName);

                // Connect to LevelComplete panel on canvas
                Transform compPanel = canvasGo.transform.Find("LevelComplete_Panel");
                if (compPanel != null)
                {
                    TextMeshProUGUI cText = compPanel.Find("Complete_Coins")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI tText = compPanel.Find("Complete_Time")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI sText = compPanel.Find("Complete_Score")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI stText = compPanel.Find("Complete_Stars")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI crText = compPanel.Find("Complete_Criteria")?.GetComponent<TextMeshProUGUI>();
                    Button nextBtn = compPanel.Find("Next_Button")?.GetComponent<Button>();
                    Button retryBtn = compPanel.Find("Retry_Button")?.GetComponent<Button>();
                    Button menuBtn = compPanel.Find("Menu_Button")?.GetComponent<Button>();
                    goalScript.SetUIReferences(compPanel.gameObject, cText, tText, sText, stText, crText, nextBtn, retryBtn, menuBtn);
                }
            }

            // 17b. Boss Arena Setup for Level 3
            if (data.levelName.Contains("Level 3"))
            {
                SetupLevel3BossArena(scene, canvasGo, goalInstance, camFollow);
            }

            // 17c. Atmospheric 2D Point Lights & Lighting Fixtures
            if (data.levelName.Contains("Level 2"))
            {
                AddCrystalCavernAtmosphericLights(scene);
            }
            else if (data.levelName.Contains("Level 3"))
            {
                AddVolcanoAtmosphericLights(scene);
            }

            // 17d. Interactive Environmental Physics & Campfire NPC
            AddLevelInteractiveHazardsAndNPCs(scene, data);

            // 17e. Dynamic Atmosphere Lighting & God Rays
            GameObject atmoGo = new GameObject("DynamicAtmosphere");
            atmoGo.AddComponent<DynamicAtmosphereController>();

            // 17f. Level 2 Mini-Boss (Crystal Guardian)
            if (data.levelName.Contains("Level 2"))
            {
                SetupLevel2MiniBoss(scene);
            }

            // 17g. Elite Champion Enemies
            SetupEliteEnemies(enemyRoot, data.levelName);

            // 18. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, data.scenePath);
            Debug.Log($"<color=#2ecc71>[Platformer]</color> Built {data.levelName} at '{data.scenePath}'!");
        }

        private static void SetupLevel3BossArena(Scene scene, GameObject canvasGo, GameObject goalInstance, CameraFollow camFollow)
        {
            // 1. Ensure Boss Assets exist
            BossBuilder.BuildAllBossAssets();

            GameObject golemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossBuilder.GolemPrefabPath);
            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossBuilder.GatePrefabPath);
            GameObject defeatFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyDefeatParticle.prefab");

            // 2. Instantiate Boss Gate at arena entrance (X: 103.5)
            GameObject gateInstance = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab, scene);
            gateInstance.transform.position = new Vector3(103.5f, 5.0f, 0f);
            gateInstance.name = "Arena_BossGate";

            // 3. Instantiate Boss Ignis (X: 112.0)
            GameObject golemInstance = (GameObject)PrefabUtility.InstantiatePrefab(golemPrefab, scene);
            golemInstance.transform.position = new Vector3(112.0f, 5.0f, 0f);
            golemInstance.name = "Boss_Ignis";

            // Position LevelGoal on ground level inside the arena
            if (goalInstance != null)
            {
                goalInstance.transform.position = new Vector3(114.5f, 5.0f, 0f);
            }

            // 4. Create Arena Entrance Trigger (Wide and tall across entire entrance zone: X: 104.0, Y: 10.0, Size: 4x24)
            GameObject triggerGo = new GameObject("Arena_Trigger");
            triggerGo.transform.position = new Vector3(104.0f, 10.0f, 0f);
            BoxCollider2D trigCol = triggerGo.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(4.0f, 24.0f);

            BossArenaController arenaController = triggerGo.AddComponent<BossArenaController>();
            BossVolcanoGolem golemScript = golemInstance.GetComponent<BossVolcanoGolem>();
            BossGate gateScript = gateInstance.GetComponent<BossGate>();
            BossHealthBarUI healthBarUI = canvasGo.GetComponent<BossHealthBarUI>();

            arenaController.SetReferences(golemScript, gateScript, goalInstance, healthBarUI, camFollow, defeatFx);

            // Add Molten Core 2D Light to Boss Ignis
            AddBossCoreLight(golemInstance);

            // 5. Hide LevelGoal until Boss is defeated
            goalInstance.SetActive(false);
        }

        private static void AddPlayerLanternLight(GameObject playerInstance)
        {
            if (playerInstance == null) return;
            GameObject lightGo = new GameObject("PlayerLanternLight2D");
            lightGo.transform.SetParent(playerInstance.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            var light2D = lightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light2D.color = new Color(1.0f, 0.94f, 0.85f, 1f);
            light2D.pointLightInnerRadius = 0.8f;
            light2D.pointLightOuterRadius = 4.2f;
            light2D.intensity = 0.95f;
            light2D.falloffIntensity = 0.6f;
        }

        private static void AddCrystalCavernAtmosphericLights(Scene scene)
        {
            GameObject lightRoot = new GameObject("CrystalLights");
            Vector3[] crystalPositions = new Vector3[]
            {
                new Vector3(22f, 7.5f, 0f),
                new Vector3(41.5f, 11f, 0f),
                new Vector3(56f, 6.5f, 0f),
                new Vector3(76.5f, 8f, 0f),
                new Vector3(94f, 7f, 0f),
                new Vector3(108f, 8.5f, 0f)
            };

            for (int i = 0; i < crystalPositions.Length; i++)
            {
                GameObject cl = new GameObject($"CrystalLight_{i + 1}");
                cl.transform.SetParent(lightRoot.transform);
                cl.transform.position = crystalPositions[i];

                var l2d = cl.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                l2d.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
                l2d.color = (i % 2 == 0) ? new Color(0.2f, 0.85f, 1f, 1f) : new Color(0.95f, 0.35f, 1f, 1f);
                l2d.pointLightInnerRadius = 1.0f;
                l2d.pointLightOuterRadius = 4.5f;
                l2d.intensity = 1.25f;

                cl.AddComponent<Platformer.Common.PulsingLight2D>();
            }
        }

        private static void AddVolcanoAtmosphericLights(Scene scene)
        {
            GameObject lightRoot = new GameObject("VolcanoLavaLights");
            Vector3[] lavaPositions = new Vector3[]
            {
                new Vector3(10.5f, 3.0f, 0f),
                new Vector3(27.5f, 6.0f, 0f),
                new Vector3(39.5f, 4.0f, 0f),
                new Vector3(53f, 3.0f, 0f),
                new Vector3(70.5f, 6.0f, 0f),
                new Vector3(87f, 4.0f, 0f),
                new Vector3(97.5f, 4.0f, 0f),
                new Vector3(112f, 6.5f, 0f) // Boss Arena Center
            };

            for (int i = 0; i < lavaPositions.Length; i++)
            {
                GameObject vl = new GameObject($"LavaLight_{i + 1}");
                vl.transform.SetParent(lightRoot.transform);
                vl.transform.position = lavaPositions[i];

                var l2d = vl.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                l2d.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
                l2d.color = new Color(1.0f, 0.38f, 0.12f, 1f);
                l2d.pointLightInnerRadius = (i == lavaPositions.Length - 1) ? 2.5f : 1.2f;
                l2d.pointLightOuterRadius = (i == lavaPositions.Length - 1) ? 9.5f : 5.0f;
                l2d.intensity = (i == lavaPositions.Length - 1) ? 1.6f : 1.35f;

                vl.AddComponent<Platformer.Common.FlickeringLight2D>();
            }
        }

        private static void AddBossCoreLight(GameObject bossInstance)
        {
            if (bossInstance == null) return;
            GameObject coreLightGo = new GameObject("BossMagmaCoreLight");
            coreLightGo.transform.SetParent(bossInstance.transform, false);
            coreLightGo.transform.localPosition = new Vector3(0f, 0.4f, 0f);

            var l2d = coreLightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            l2d.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            l2d.color = new Color(1.0f, 0.25f, 0.1f, 1f);
            l2d.pointLightInnerRadius = 1.0f;
            l2d.pointLightOuterRadius = 4.8f;
            l2d.intensity = 1.75f;

            coreLightGo.AddComponent<Platformer.Common.FlickeringLight2D>();
        }

        private static void AddLevelInteractiveHazardsAndNPCs(Scene scene, LevelData data)
        {
            Sprite stalactiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(InteractiveObjectsBuilder.StalactiteSpritePath);
            Sprite wandererSprite = AssetDatabase.LoadAssetAtPath<Sprite>(InteractiveObjectsBuilder.WandererSpritePath);
            Sprite geyserSprite = AssetDatabase.LoadAssetAtPath<Sprite>(InteractiveObjectsBuilder.GeyserSpritePath);
            GameObject dustFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DustParticle.prefab");

            GameObject hazardRoot = new GameObject("InteractiveHazardsAndNPCs");

            if (data.levelName.Contains("Level 2"))
            {
                // 1. Falling Stalactites hanging from high cavern ceilings
                Vector3[] stalactitePositions = new Vector3[]
                {
                    new Vector3(32f, 11.8f, 0f),
                    new Vector3(82f, 11.8f, 0f)
                };

                for (int i = 0; i < stalactitePositions.Length; i++)
                {
                    GameObject stGo = new GameObject($"FallingStalactite_{i + 1}");
                    stGo.transform.SetParent(hazardRoot.transform);
                    stGo.transform.position = stalactitePositions[i];

                    SpriteRenderer sr = stGo.AddComponent<SpriteRenderer>();
                    sr.sprite = stalactiteSprite;
                    sr.sortingOrder = 2;

                    PolygonCollider2D poly = stGo.AddComponent<PolygonCollider2D>();
                    FallingStalactite fs = stGo.AddComponent<FallingStalactite>();

                    SerializedObject fsSo = new SerializedObject(fs);
                    fsSo.FindProperty("triggerRayDistance").floatValue = 8.5f;
                    fsSo.FindProperty("triggerLayers").intValue = LayerMask.GetMask("Player", "Enemy");
                    fsSo.FindProperty("crushDamage").intValue = 10;
                    fsSo.FindProperty("fallSpeed").floatValue = 18f;
                    fsSo.FindProperty("shatterFxPrefab").objectReferenceValue = dustFx;
                    fsSo.ApplyModifiedProperties();
                }

                // 2. Campfire NPC resting at Checkpoint 2 (X: 50.5, Y: 5.25)
                CreateCampfireNPC(hazardRoot.transform, new Vector3(50.5f, 5.25f, 0f), wandererSprite);
            }
            else if (data.levelName.Contains("Level 3"))
            {
                // 1. Falling Volcanic Stalactites
                Vector3[] stalactitePositions = new Vector3[]
                {
                    new Vector3(45f, 12.8f, 0f),
                    new Vector3(78f, 12.8f, 0f)
                };

                for (int i = 0; i < stalactitePositions.Length; i++)
                {
                    GameObject stGo = new GameObject($"VolcanicStalactite_{i + 1}");
                    stGo.transform.SetParent(hazardRoot.transform);
                    stGo.transform.position = stalactitePositions[i];

                    SpriteRenderer sr = stGo.AddComponent<SpriteRenderer>();
                    sr.sprite = stalactiteSprite;
                    sr.color = new Color(0.95f, 0.5f, 0.45f, 1f); // Fiery tinted stalactite
                    sr.sortingOrder = 2;

                    PolygonCollider2D poly = stGo.AddComponent<PolygonCollider2D>();
                    FallingStalactite fs = stGo.AddComponent<FallingStalactite>();

                    SerializedObject fsSo = new SerializedObject(fs);
                    fsSo.FindProperty("triggerRayDistance").floatValue = 9.0f;
                    fsSo.FindProperty("triggerLayers").intValue = LayerMask.GetMask("Player", "Enemy");
                    fsSo.FindProperty("crushDamage").intValue = 10;
                    fsSo.FindProperty("fallSpeed").floatValue = 19f;
                    fsSo.FindProperty("shatterFxPrefab").objectReferenceValue = dustFx;
                    fsSo.ApplyModifiedProperties();
                }

                // 3. Campfire NPC resting at safe battlements (X: 68.5, Y: 5.25)
                CreateCampfireNPC(hazardRoot.transform, new Vector3(68.5f, 5.25f, 0f), wandererSprite);
            }
        }

        private static void CreateCampfireNPC(Transform parent, Vector3 position, Sprite wandererSprite)
        {
            GameObject npcGo = new GameObject("Campfire_OldWanderer");
            npcGo.transform.SetParent(parent);
            npcGo.transform.position = position;

            SpriteRenderer sr = npcGo.AddComponent<SpriteRenderer>();
            sr.sprite = wandererSprite;
            sr.sortingOrder = 3;

            CircleCollider2D col = npcGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.5f;

            // Prompt visual
            GameObject promptGo = new GameObject("Prompt_Visual");
            promptGo.transform.SetParent(npcGo.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var tmp = promptGo.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "<color=#FFD700>[E]</color> Trò chuyện";
            tmp.fontSize = 4.2f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.sortingOrder = 50;

            CampfireNPC npc = npcGo.AddComponent<CampfireNPC>();
            SerializedObject npcSo = new SerializedObject(npc);
            npcSo.FindProperty("promptObject").objectReferenceValue = promptGo;
            npcSo.FindProperty("interactionRadius").floatValue = 2.8f;
            npcSo.ApplyModifiedProperties();
        }

        private static Vector3 EnsureSafeAboveSurface(Vector3 position, string[] layout, float offsetAboveSurface = 0.6f)
        {
            int totalRows = layout.Length;
            int ix = Mathf.FloorToInt(position.x);
            int iy = Mathf.FloorToInt(position.y);

            Func<int, int, bool> isSolid = (col, rowY) =>
            {
                int r = totalRows - 1 - rowY;
                if (r < 0 || r >= totalRows) return false;
                if (col < 0 || col >= layout[r].Length) return false;
                char c = layout[r][col];
                return c == 'T' || c == 'G' || c == 'B' || c == 'W' || c == 'H';
            };

            // If current position is inside a solid tile, scan upwards to find the top surface
            if (isSolid(ix, iy))
            {
                for (int scanY = iy; scanY < totalRows; scanY++)
                {
                    if (!isSolid(ix, scanY))
                    {
                        return new Vector3(position.x, scanY + offsetAboveSurface, position.z);
                    }
                }
            }

            return position;
        }

        private static Vector3 AlignHazardToSurface(Vector3 position, string[] layout, float offsetAboveSurface = 0.47f)
        {
            int totalRows = layout.Length;
            int ix = Mathf.FloorToInt(position.x);
            int iy = Mathf.FloorToInt(position.y);

            Func<int, int, bool> isSolid = (col, rowY) =>
            {
                int r = totalRows - 1 - rowY;
                if (r < 0 || r >= totalRows) return false;
                if (col < 0 || col >= layout[r].Length) return false;
                char c = layout[r][col];
                return c == 'T' || c == 'G' || c == 'B' || c == 'W' || c == 'H';
            };

            // If inside a solid tile, scan upward to find the top surface
            if (isSolid(ix, iy))
            {
                for (int scanY = iy; scanY < totalRows; scanY++)
                {
                    if (!isSolid(ix, scanY))
                    {
                        return new Vector3(position.x, scanY + offsetAboveSurface, position.z);
                    }
                }
            }
            else
            {
                // If in air, scan downward to find the nearest solid tile surface below
                for (int scanY = iy; scanY >= 0; scanY--)
                {
                    if (isSolid(ix, scanY))
                    {
                        return new Vector3(position.x, (scanY + 1) + offsetAboveSurface, position.z);
                    }
                }
            }

            return position;
        }

        private static GameObject BuildUnifiedHUD(string nextSceneName)
        {
            EnsureEventSystem();

            GameObject canvasGo = new GameObject("HUD_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGo.AddComponent<GraphicRaycaster>();

            HUDController hud = canvasGo.AddComponent<HUDController>();
            PauseMenuController pause = canvasGo.AddComponent<PauseMenuController>();

            // Top Bar Texts
            TextMeshProUGUI hpText = CreateTMP(canvasGo.transform, "HP_Text", "HP: 3 / 3",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -30f), new Vector2(160f, 45f), TextAlignmentOptions.Left, 28f, new Color(1f, 0.35f, 0.35f, 1f));

            TextMeshProUGUI energyText = CreateTMP(canvasGo.transform, "Energy_Text", "MP: 100 / 100",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(210f, -30f), new Vector2(180f, 45f), TextAlignmentOptions.Left, 28f, new Color(0.2f, 0.85f, 1f, 1f));

            TextMeshProUGUI coinsText = CreateTMP(canvasGo.transform, "Coins_Text", "COINS: 00",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(400f, -30f), new Vector2(160f, 45f), TextAlignmentOptions.Left, 28f, new Color(0.98f, 0.82f, 0.15f, 1f));

            TextMeshProUGUI livesText = CreateTMP(canvasGo.transform, "Lives_Text", "LIVES: 3",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(570f, -30f), new Vector2(150f, 45f), TextAlignmentOptions.Left, 28f, new Color(0.28f, 0.86f, 0.98f, 1f));

            TextMeshProUGUI scoreText = CreateTMP(canvasGo.transform, "Score_Text", "SCORE: 00000",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -30f), new Vector2(260f, 45f), TextAlignmentOptions.Right, 28f, Color.white);

            // Game Over Panel
            GameObject goPanel = CreatePanel(canvasGo.transform, "GameOver_Panel", new Color(0.04f, 0.05f, 0.08f, 0.92f), true);
            CreateTMP(goPanel.transform, "GameOver_Title", "GAME OVER",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 90f), new Vector2(600f, 100f), TextAlignmentOptions.Center, 64f, new Color(0.95f, 0.25f, 0.25f, 1f));
            CreateTMP(goPanel.transform, "GameOver_Subtitle", "ALL LIVES LOST - TRY AGAIN?",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 25f), new Vector2(500f, 40f), TextAlignmentOptions.Center, 20f, new Color(0.85f, 0.85f, 0.85f, 0.9f));

            Button goRetryBtn = CreateButton(goPanel.transform, "GameOver_RetryBtn", "RETRY (R)", new Vector2(0f, -40f), new Vector2(260f, 52f), new Color(0.2f, 0.75f, 0.35f, 1f));
            Button goMenuBtn = CreateButton(goPanel.transform, "GameOver_MenuBtn", "MAIN MENU", new Vector2(0f, -105f), new Vector2(260f, 52f), new Color(0.22f, 0.45f, 0.70f, 1f));
            goPanel.SetActive(false);

            // Level Complete Panel
            GameObject compPanel = CreatePanel(canvasGo.transform, "LevelComplete_Panel", new Color(0.08f, 0.10f, 0.14f, 0.94f), false);
            RectTransform compRect = compPanel.GetComponent<RectTransform>();
            compRect.anchorMin = new Vector2(0.5f, 0.5f);
            compRect.anchorMax = new Vector2(0.5f, 0.5f);
            compRect.pivot = new Vector2(0.5f, 0.5f);
            compRect.sizeDelta = new Vector2(580f, 440f);

            CreateTMP(compPanel.transform, "Complete_Title", "LEVEL COMPLETE!",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(460f, 50f), TextAlignmentOptions.Center, 38f, new Color(1f, 0.82f, 0.2f, 1f));

            TextMeshProUGUI cStars = CreateTMP(compPanel.transform, "Complete_Stars", "* * *",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(400f, 45f), TextAlignmentOptions.Center, 36f, new Color(1f, 0.85f, 0.2f, 1f));

            TextMeshProUGUI cCriteria = CreateTMP(compPanel.transform, "Complete_Criteria", "Clear: Pass | Coins: Pass | Speed: Pass",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -125f), new Vector2(540f, 32f), TextAlignmentOptions.Center, 15f, new Color(0.85f, 0.85f, 0.85f, 0.9f));

            TextMeshProUGUI cCoins = CreateTMP(compPanel.transform, "Complete_Coins", "COINS: 0",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -170f), new Vector2(400f, 36f), TextAlignmentOptions.Center, 24f, Color.white);

            TextMeshProUGUI cTime = CreateTMP(compPanel.transform, "Complete_Time", "TIME: 0.0s",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -210f), new Vector2(400f, 36f), TextAlignmentOptions.Center, 24f, Color.white);

            TextMeshProUGUI cScore = CreateTMP(compPanel.transform, "Complete_Score", "SCORE: 00000",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -250f), new Vector2(400f, 36f), TextAlignmentOptions.Center, 26f, new Color(0.3f, 0.9f, 1f, 1f));

            // Action Buttons: Next Level, Retry, Menu
            Button nextBtn = CreateButton(compPanel.transform, "Next_Button", "NEXT LEVEL",
                new Vector2(170f, -155f), new Vector2(165f, 48f), new Color(0.2f, 0.75f, 0.35f, 1f));

            Button retryBtn = CreateButton(compPanel.transform, "Retry_Button", "RETRY",
                new Vector2(0f, -155f), new Vector2(130f, 48f), new Color(0.25f, 0.45f, 0.65f, 1f));

            Button compMenuBtn = CreateButton(compPanel.transform, "Menu_Button", "MAIN MENU",
                new Vector2(-170f, -155f), new Vector2(165f, 48f), new Color(0.65f, 0.22f, 0.22f, 1f));

            CreateTMP(compPanel.transform, "Complete_Advancing", "Auto-advancing in 5 seconds...",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(400f, 26f), TextAlignmentOptions.Center, 13f, new Color(0.7f, 0.7f, 0.7f, 0.75f));

            compPanel.SetActive(false);

            // Pause Menu Panel
            GameObject pausePanel = CreatePanel(canvasGo.transform, "Pause_Panel", new Color(0f, 0f, 0f, 0.82f), true);
            CreateTMP(pausePanel.transform, "Pause_Title", "GAME PAUSED",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(500f, 80f), TextAlignmentOptions.Center, 54f, Color.white);

            Button resumeBtn = CreateButton(pausePanel.transform, "Resume_Button", "RESUME", new Vector2(0f, 95f), new Vector2(240f, 44f), new Color(0.2f, 0.75f, 0.35f, 1f));
            Button restartBtn = CreateButton(pausePanel.transform, "Restart_Button", "RESTART", new Vector2(0f, 45f), new Vector2(240f, 44f), new Color(0.25f, 0.45f, 0.65f, 1f));
            Button charmsBtn = CreateButton(pausePanel.transform, "Charms_Button", "CHARMS (C)", new Vector2(0f, -5f), new Vector2(240f, 44f), new Color(0.18f, 0.65f, 0.85f, 1f));
            Button shopBtn = CreateButton(pausePanel.transform, "Upgrades_Button", "UPGRADES", new Vector2(0f, -55f), new Vector2(240f, 44f), new Color(0.92f, 0.65f, 0.15f, 1f));
            Button pauseMenuBtn = CreateButton(pausePanel.transform, "MainMenu_Button", "MAIN MENU", new Vector2(0f, -105f), new Vector2(240f, 44f), new Color(0.75f, 0.25f, 0.25f, 1f));

            Slider musicSlider = CreateSlider(pausePanel.transform, "Music_Slider", "BGM Volume", new Vector2(0f, -165f), new Vector2(360f, 30f), 0.7f);
            Slider sfxSlider = CreateSlider(pausePanel.transform, "SFX_Slider", "SFX Volume", new Vector2(0f, -210f), new Vector2(360f, 30f), 0.8f);

            CreateTMP(pausePanel.transform, "Controls_Guide",
                "<color=#FFD700><b>HƯỚNG DẪN KỸ NĂNG & PHÍM TẮT:</b></color>\n" +
                "<color=#5ce1e6>[A/D / Mũi tên]</color> Di chuyển  |  <color=#5ce1e6>[Space]</color> Nhảy / Nhảy đôi / Bám tường\n" +
                "<color=#5ce1e6>[J / Chuột Trái]</color> Chém Kiếm (Combo 3 đòn)  |  <color=#5ce1e6>[W + J]</color> Chém Hất Lên\n" +
                "<color=#5ce1e6>[S + J trên không]</color> Pogo Chém Xuống Đẩy Nảy  |  <color=#5ce1e6>[F]</color> Phản Đòn (Parry)\n" +
                "<color=#5ce1e6>[K / Chuột Phải]</color> Kiếm Khí Năng Lượng (30 MP)  |  <color=#5ce1e6>[Shift / Q]</color> Shadow Dash\n" +
                "<color=#5ce1e6>[R / X]</color> Tuyệt Kỹ Bão Kiếm (Blade Tempest)  |  <color=#5ce1e6>[C]</color> Bảng Bùa Chú\n" +
                "<color=#5ce1e6>[T]</color> Bật/Tắt Speedrun Timer  |  <color=#5ce1e6>[M / Tab]</color> Bản Đồ Minimap",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -280f), new Vector2(720f, 120f), TextAlignmentOptions.Center, 13f, new Color(0.9f, 0.95f, 1f, 0.95f));

            // Upgrade Shop Dialog in HUD
            UpgradeShopUI shopUI = BuildUpgradeShopPanel(canvasGo.transform);
            pause.SetShopReferences(shopBtn, shopUI, charmsBtn);

            pausePanel.SetActive(false);

            // Audio event binding
            canvasGo.AddComponent<AudioEventBinder>();

            // Wire HUDController
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("healthText").objectReferenceValue = hpText;
            hudSo.FindProperty("energyText").objectReferenceValue = energyText;
            hudSo.FindProperty("coinsText").objectReferenceValue = coinsText;
            hudSo.FindProperty("scoreText").objectReferenceValue = scoreText;
            hudSo.FindProperty("livesText").objectReferenceValue = livesText;
            hudSo.FindProperty("gameOverPanel").objectReferenceValue = goPanel;
            hudSo.FindProperty("retryButton").objectReferenceValue = goRetryBtn;
            hudSo.FindProperty("gameOverMenuButton").objectReferenceValue = goMenuBtn;
            hudSo.ApplyModifiedProperties();

            // Wire PauseMenuController
            SerializedObject pauseSo = new SerializedObject(pause);
            pauseSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            pauseSo.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            pauseSo.FindProperty("restartButton").objectReferenceValue = restartBtn;
            pauseSo.FindProperty("mainMenuButton").objectReferenceValue = pauseMenuBtn;
            pauseSo.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            pauseSo.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            pauseSo.ApplyModifiedProperties();

            // Boss Health Bar Panel (Centered at Top of Screen)
            GameObject bossPanel = CreatePanel(canvasGo.transform, "BossHealthBar_Panel", new Color(0.12f, 0.08f, 0.10f, 0.92f), false);
            RectTransform bRect = bossPanel.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 1f);
            bRect.anchorMax = new Vector2(0.5f, 1f);
            bRect.pivot = new Vector2(0.5f, 1f);
            bRect.anchoredPosition = new Vector2(0f, -65f);
            bRect.sizeDelta = new Vector2(560f, 44f);

            TextMeshProUGUI bTitle = CreateTMP(bossPanel.transform, "Boss_Title", "IGNIS - VOLCANIC TITAN",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(500f, 22f), TextAlignmentOptions.Center, 16f, new Color(1.0f, 0.85f, 0.25f, 1f));

            GameObject sliderGo = new GameObject("Boss_Slider");
            sliderGo.transform.SetParent(bossPanel.transform, false);
            RectTransform sRect = sliderGo.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0f);
            sRect.anchorMax = new Vector2(0.5f, 0f);
            sRect.pivot = new Vector2(0.5f, 0f);
            sRect.anchoredPosition = new Vector2(0f, 6f);
            sRect.sizeDelta = new Vector2(520f, 14f);

            Slider bSlider = sliderGo.AddComponent<Slider>();
            bSlider.interactable = false;
            bSlider.transition = Selectable.Transition.None;
            bSlider.direction = Slider.Direction.LeftToRight;
            bSlider.minValue = 0f;
            bSlider.maxValue = 10f;
            bSlider.value = 10f;

            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.12f, 0.12f, 0.85f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.96f, 0.42f, 0.12f, 1f);

            bSlider.fillRect = fillRect;

            TextMeshProUGUI bHpText = CreateTMP(bossPanel.transform, "Boss_HP_Text", "10 / 10",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 6f), new Vector2(200f, 14f), TextAlignmentOptions.Center, 12f, Color.white);

            BossHealthBarUI bossUI = canvasGo.AddComponent<BossHealthBarUI>();
            bossUI.SetReferences(bossPanel, bTitle, bHpText, bSlider, fillImg);

            bossPanel.SetActive(false);

            // 7. Combo Rank UI (Style Meter)
            GameObject comboPanel = CreatePanel(canvasGo.transform, "Combo_Panel", new Color(0f, 0f, 0f, 0f), false);
            RectTransform comboRect = comboPanel.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(1f, 0.5f);
            comboRect.anchorMax = new Vector2(1f, 0.5f);
            comboRect.pivot = new Vector2(1f, 0.5f);
            comboRect.anchoredPosition = new Vector2(-40f, 80f);
            comboRect.sizeDelta = new Vector2(160f, 180f);

            TextMeshProUGUI rankLetter = CreateTMP(comboPanel.transform, "Rank_Letter", "S",
                new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(120f, 90f), TextAlignmentOptions.Center, 68f, new Color(1f, 0.6f, 0.1f));

            TextMeshProUGUI rankTitle = CreateTMP(comboPanel.transform, "Rank_Title", "SUPREME",
                new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(160f, 30f), TextAlignmentOptions.Center, 18f, Color.white);

            Slider odSlider = CreateSlider(comboPanel.transform, "Overdrive_Slider", "NỘ KHÍ [R]",
                new Vector2(0f, -60f), new Vector2(150f, 20f), 0f);

            TextMeshProUGUI odReady = CreateTMP(comboPanel.transform, "Overdrive_Ready", "OVERDRIVE READY! [R / X]",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -85f), new Vector2(220f, 26f), TextAlignmentOptions.Center, 14f, new Color(1f, 0.9f, 0.2f));
            odReady.gameObject.SetActive(false);

            ComboRankUI comboUI = canvasGo.AddComponent<ComboRankUI>();
            SerializedObject cbSo = new SerializedObject(comboUI);
            cbSo.FindProperty("container").objectReferenceValue = comboPanel;
            cbSo.FindProperty("rankLetterText").objectReferenceValue = rankLetter;
            cbSo.FindProperty("rankTitleText").objectReferenceValue = rankTitle;
            cbSo.FindProperty("overdriveSlider").objectReferenceValue = odSlider;
            cbSo.FindProperty("overdriveReadyText").objectReferenceValue = odReady;
            cbSo.ApplyModifiedProperties();
            comboPanel.SetActive(false);

            // 8. Achievement Trophy Banner Popup UI
            GameObject achPanel = CreatePanel(canvasGo.transform, "Achievement_Banner", new Color(0.12f, 0.10f, 0.05f, 0.95f), false);
            RectTransform achRect = achPanel.GetComponent<RectTransform>();
            achRect.anchorMin = new Vector2(0.5f, 1f);
            achRect.anchorMax = new Vector2(0.5f, 1f);
            achRect.pivot = new Vector2(0.5f, 1f);
            achRect.anchoredPosition = new Vector2(0f, 150f);
            achRect.sizeDelta = new Vector2(480f, 85f);

            TextMeshProUGUI achTitle = CreateTMP(achPanel.transform, "Ach_Title", "ACHIEVEMENT UNLOCKED!",
                new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(450f, 32f), TextAlignmentOptions.Center, 20f, new Color(1f, 0.85f, 0.2f));

            TextMeshProUGUI achDesc = CreateTMP(achPanel.transform, "Ach_Desc", "Achievement Description",
                new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(450f, 30f), TextAlignmentOptions.Center, 14f, new Color(0.9f, 0.9f, 0.9f));

            AchievementPopupUI achUI = canvasGo.AddComponent<AchievementPopupUI>();
            SerializedObject aSo = new SerializedObject(achUI);
            aSo.FindProperty("bannerPanel").objectReferenceValue = achRect;
            aSo.FindProperty("titleText").objectReferenceValue = achTitle;
            aSo.FindProperty("descriptionText").objectReferenceValue = achDesc;
            aSo.ApplyModifiedProperties();
            achPanel.SetActive(false);

            // 9. Interactive Minimap & Radar HUD
            GameObject mmPanel = CreatePanel(canvasGo.transform, "Minimap_Frame", new Color(0.05f, 0.08f, 0.12f, 0.75f), false);
            RectTransform mmRect = mmPanel.GetComponent<RectTransform>();
            mmRect.anchorMin = new Vector2(1f, 1f);
            mmRect.anchorMax = new Vector2(1f, 1f);
            mmRect.pivot = new Vector2(1f, 1f);
            mmRect.anchoredPosition = new Vector2(-40f, -80f);
            mmRect.sizeDelta = new Vector2(180f, 100f);

            GameObject pMarkerGo = new GameObject("PlayerMarker");
            pMarkerGo.transform.SetParent(mmPanel.transform, false);
            RectTransform pMarkerRect = pMarkerGo.AddComponent<RectTransform>();
            pMarkerRect.sizeDelta = new Vector2(10f, 10f);
            Image pMarkerImg = pMarkerGo.AddComponent<Image>();
            pMarkerImg.color = new Color(0.2f, 0.95f, 1f, 1f);

            TextMeshProUGUI radarText = CreateTMP(mmPanel.transform, "Radar_Text", "<color=#FFD700>SECRET NEARBY!</color>",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 14f), new Vector2(170f, 20f), TextAlignmentOptions.Center, 11f, Color.yellow);
            radarText.gameObject.SetActive(false);

            MinimapController minimap = canvasGo.AddComponent<MinimapController>();
            SerializedObject mSo = new SerializedObject(minimap);
            mSo.FindProperty("mapFrame").objectReferenceValue = mmRect;
            mSo.FindProperty("playerMarker").objectReferenceValue = pMarkerRect;
            mSo.FindProperty("secretRadarText").objectReferenceValue = radarText;
            mSo.ApplyModifiedProperties();

            // 10. Dialogue Box UI
            GameObject diagPanel = CreatePanel(canvasGo.transform, "Dialogue_Panel", new Color(0.06f, 0.07f, 0.10f, 0.94f), false);
            RectTransform dRect = diagPanel.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0f);
            dRect.anchorMax = new Vector2(0.5f, 0f);
            dRect.pivot = new Vector2(0.5f, 0f);
            dRect.anchoredPosition = new Vector2(0f, 40f);
            dRect.sizeDelta = new Vector2(760f, 160f);

            TextMeshProUGUI speakerText = CreateTMP(diagPanel.transform, "Speaker_Text", "Old Wanderer",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -15f), new Vector2(300f, 35f), TextAlignmentOptions.Left, 22f, new Color(1f, 0.85f, 0.25f));

            TextMeshProUGUI bodyText = CreateTMP(diagPanel.transform, "Body_Text", "...",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(700f, 85f), TextAlignmentOptions.Left, 18f, Color.white);

            TextMeshProUGUI promptText = CreateTMP(diagPanel.transform, "Prompt_Text", "[E / Space] Tiếp tục",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-30f, 15f), new Vector2(200f, 25f), TextAlignmentOptions.Right, 13f, new Color(0.7f, 0.7f, 0.7f));

            DialogueBoxUI diagUI = canvasGo.AddComponent<DialogueBoxUI>();
            SerializedObject dSo = new SerializedObject(diagUI);
            dSo.FindProperty("dialogContainer").objectReferenceValue = diagPanel;
            dSo.FindProperty("speakerNameText").objectReferenceValue = speakerText;
            dSo.FindProperty("dialogueBodyText").objectReferenceValue = bodyText;
            dSo.FindProperty("advancePromptText").objectReferenceValue = promptText;
            dSo.ApplyModifiedProperties();
            diagPanel.SetActive(false);

            // 11. Charm Socketing UI Panel (Hollow Knight style)
            GameObject charmPanel = CreatePanel(canvasGo.transform, "CharmSocket_Panel", new Color(0.06f, 0.08f, 0.12f, 0.96f), true);
            RectTransform charmRect = charmPanel.GetComponent<RectTransform>();
            charmRect.sizeDelta = new Vector2(740f, 490f);

            CreateTMP(charmPanel.transform, "Charm_Title", "CHARM INVENTORY & SOCKETS\n<size=17><color=#a0a8b8>Bùa Chú Cổ Vật & Khảm Khe Cắm</color></size>",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(650f, 65f), TextAlignmentOptions.Center, 26f, new Color(0.98f, 0.82f, 0.15f, 1f));

            TextMeshProUGUI slotCountText = CreateTMP(charmPanel.transform, "SlotCount_Text", "CHARM SOCKETS: 0 / 3",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -88f), new Vector2(400f, 30f), TextAlignmentOptions.Center, 20f, new Color(0.3f, 0.85f, 1f, 1f));

            GameObject cardsRoot = new GameObject("Cards_Root", typeof(RectTransform));
            cardsRoot.transform.SetParent(charmPanel.transform, false);

            var allCharms = CharmManager.Instance.GetAllCharms();
            float[] cardY = new float[] { 75f, 10f, -55f, -120f };
            int cIdx = 0;
            foreach (var cInfo in allCharms)
            {
                GameObject card = CreatePanel(cardsRoot.transform, $"CharmCard_{cInfo.type}", new Color(0.12f, 0.14f, 0.18f, 0.9f), false);
                RectTransform crt = card.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(660f, 56f);
                crt.anchoredPosition = new Vector2(0f, cardY[cIdx % cardY.Length]);

                Button cardBtn = card.AddComponent<Button>();
                CharmType cType = cInfo.type;
                cardBtn.onClick.AddListener(() => CharmManager.Instance.ToggleCharm(cType));

                CreateTMP(card.transform, "Name_Text", $"<b>{cInfo.name}</b> <size=13><color=#888888>({cInfo.subtitle})</color></size>\n<size=12><color=#cccccc>{cInfo.description}</color></size>",
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(25f, 0f), new Vector2(460f, 48f), TextAlignmentOptions.Left, 16f, Color.white);

                CreateTMP(card.transform, "Status_Text", "[CLICK TO EQUIP]",
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-15f, 0f), new Vector2(160f, 40f), TextAlignmentOptions.Right, 13f, new Color(0.9f, 0.5f, 0.1f));

                cIdx++;
            }

            Button charmCloseBtn = CreateButton(charmPanel.transform, "CharmClose_Btn", "CLOSE [ESC / C]",
                new Vector2(0f, -200f), new Vector2(220f, 42f), new Color(0.65f, 0.25f, 0.25f, 1f));

            CharmSocketUI charmUI = canvasGo.AddComponent<CharmSocketUI>();
            charmUI.SetReferences(charmPanel, slotCountText, cardsRoot.transform, charmCloseBtn);
            charmPanel.SetActive(false);

            // 12. Speedrun Timer HUD (top-left)
            GameObject srGo = new GameObject("SpeedrunTimer_HUD", typeof(RectTransform));
            srGo.transform.SetParent(canvasGo.transform, false);
            RectTransform srRect = srGo.GetComponent<RectTransform>();
            srRect.anchorMin = new Vector2(0f, 1f);
            srRect.anchorMax = new Vector2(0f, 1f);
            srRect.pivot = new Vector2(0f, 1f);
            srRect.anchoredPosition = new Vector2(40f, -75f);
            srRect.sizeDelta = new Vector2(240f, 80f);

            TextMeshProUGUI srTimer = CreateTMP(srGo.transform, "Timer_Text", "00:00.000",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(220f, 28f), TextAlignmentOptions.Left, 22f, new Color(0.2f, 0.95f, 1f, 1f));

            TextMeshProUGUI srSplit = CreateTMP(srGo.transform, "Split_Text", "",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -26f), new Vector2(240f, 22f), TextAlignmentOptions.Left, 13f, Color.yellow);

            TextMeshProUGUI srPB = CreateTMP(srGo.transform, "PB_Text", "PB: --:--.---",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -48f), new Vector2(220f, 20f), TextAlignmentOptions.Left, 12f, new Color(1f, 0.85f, 0.3f, 0.85f));

            SpeedrunTimerUI timerUI = canvasGo.AddComponent<SpeedrunTimerUI>();
            timerUI.SetReferences(srGo, srTimer, srSplit, srPB);

            return canvasGo;
        }

        public static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureEventSystem();
            EnsureAudioManager();

            // Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.42f, 0.65f, 0.95f, 1f);
            camGo.AddComponent<AudioListener>();

            // Setup scenic parallax background and weather for MainMenu
            ParallaxBuilder.BuildParallaxForLevel("Level 1");
            ParallaxBuilder.BuildWeatherForLevel("Level 1");

            // Canvas
            GameObject canvasGo = new GameObject("MainMenu_Canvas");
            canvasGo.layer = 5; // UI Layer
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100; // Guaranteed to render in front of all parallax & particle effects
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            MainMenuController controller = canvasGo.AddComponent<MainMenuController>();

            // Title Shadow (for 3D depth and readability against sky/particle background)
            CreateTMP(canvasGo.transform, "Title_Shadow", "PLATFORMER 2D",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(3f, 277f), new Vector2(900f, 120f), TextAlignmentOptions.Center, 80f, new Color(0.04f, 0.06f, 0.10f, 0.95f));

            // Title Main Text
            CreateTMP(canvasGo.transform, "Title_Text", "PLATFORMER 2D",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 280f), new Vector2(900f, 120f), TextAlignmentOptions.Center, 80f, new Color(0.98f, 0.82f, 0.15f, 1f));

            // Subtitle
            CreateTMP(canvasGo.transform, "Subtitle_Text", "- MASTER OF BLADES & RUNES -",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 220f), new Vector2(600f, 35f), TextAlignmentOptions.Center, 20f, new Color(0.88f, 0.94f, 1f, 0.92f));

            // High-Contrast Stats Banner (Translucent dark panel for perfect contrast against sky)
            GameObject statsCard = CreatePanel(canvasGo.transform, "Stats_Backdrop", new Color(0.05f, 0.08f, 0.15f, 0.65f), false);
            RectTransform scRect = statsCard.GetComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 0.5f);
            scRect.anchorMax = new Vector2(0.5f, 0.5f);
            scRect.pivot = new Vector2(0.5f, 0.5f);
            scRect.anchoredPosition = new Vector2(0f, 140f);
            scRect.sizeDelta = new Vector2(460f, 92f);

            // Best Score (White label with golden value)
            TextMeshProUGUI bestScore = CreateTMP(statsCard.transform, "BestScore_Text", "BEST SCORE: <color=#FFD700>00000</color>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 18f), new Vector2(440f, 36f), TextAlignmentOptions.Center, 24f, Color.white);

            // Total Stars Display (White label with golden stars)
            TextMeshProUGUI totalStars = CreateTMP(statsCard.transform, "TotalStars_Text", "TOTAL STARS: <color=#FFD700>0 / 9</color>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f), new Vector2(440f, 34f), TextAlignmentOptions.Center, 22f, Color.white);

            // Main Menu Buttons - Cohesive visual hierarchy & standardized palette
            // PLAY = Hero Emerald Green
            // Middle Utility = Deep Slate Navy
            // QUIT = Subdued Muted Crimson
            Color heroGreen = new Color(0.16f, 0.70f, 0.36f, 1f);
            Color slateNavy = new Color(0.15f, 0.22f, 0.32f, 0.95f);
            Color mutedRed  = new Color(0.42f, 0.16f, 0.16f, 0.92f);
            Vector2 btnSize = new Vector2(340f, 54f);

            Button playBtn = CreateButton(canvasGo.transform, "Play_Button", "PLAY", new Vector2(0f, 40f), btnSize, heroGreen);
            Button levelSelectBtn = CreateButton(canvasGo.transform, "LevelSelect_Button", "LEVEL SELECT", new Vector2(0f, -26f), btnSize, slateNavy);
            Button bossRushBtn = CreateButton(canvasGo.transform, "BossRush_Button", "BOSS RUSH ARENA", new Vector2(0f, -92f), btnSize, slateNavy);
            Button shopBtn = CreateButton(canvasGo.transform, "Upgrades_Button", "UPGRADES", new Vector2(0f, -158f), btnSize, slateNavy);
            Button quitBtn = CreateButton(canvasGo.transform, "Quit_Button", "QUIT", new Vector2(0f, -224f), btnSize, mutedRed);

            controller.SetBossRushButton(bossRushBtn);

            // Upgrade Shop Panel
            UpgradeShopUI shopUI = BuildUpgradeShopPanel(canvasGo.transform);
            controller.SetShopReferences(shopBtn, shopUI);

            // Level Select Panel (FullScreen dialog)
            GameObject selectPanel = CreatePanel(canvasGo.transform, "LevelSelect_Panel", new Color(0.06f, 0.08f, 0.12f, 0.96f), true);

            CreateTMP(selectPanel.transform, "Select_Title", "SELECT LEVEL",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 350f), new Vector2(600f, 70f), TextAlignmentOptions.Center, 52f, new Color(0.98f, 0.82f, 0.15f, 1f));

            // Level Card 1
            GameObject card1 = CreatePanel(selectPanel.transform, "Card_Level1", new Color(0.12f, 0.16f, 0.22f, 0.95f), false);
            RectTransform c1Rect = card1.GetComponent<RectTransform>();
            c1Rect.anchorMin = new Vector2(0.5f, 0.5f);
            c1Rect.anchorMax = new Vector2(0.5f, 0.5f);
            c1Rect.pivot = new Vector2(0.5f, 0.5f);
            c1Rect.anchoredPosition = new Vector2(-380f, 30f);
            c1Rect.sizeDelta = new Vector2(330f, 440f);

            CreateTMP(card1.transform, "Card1_Title", "LEVEL 1\n<size=18><color=#2ecc71>Green Meadows</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(300f, 80f), TextAlignmentOptions.Center, 26f, Color.white);
            TextMeshProUGUI l1Stars = CreateTMP(card1.transform, "Card1_Stars", "* * *",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f), new Vector2(280f, 45f), TextAlignmentOptions.Center, 34f, new Color(1.0f, 0.85f, 0.25f, 1f));
            TextMeshProUGUI l1Stats = CreateTMP(card1.transform, "Card1_Stats", "Target: 10 Coins • Under 45s",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(290f, 60f), TextAlignmentOptions.Center, 17f, new Color(0.85f, 0.85f, 0.85f, 0.9f));
            Button l1Btn = CreateButton(card1.transform, "Card1_PlayBtn", "PLAY", new Vector2(0f, -140f), new Vector2(240f, 52f), new Color(0.2f, 0.75f, 0.35f, 1f));

            // Level Card 2
            GameObject card2 = CreatePanel(selectPanel.transform, "Card_Level2", new Color(0.12f, 0.16f, 0.22f, 0.95f), false);
            RectTransform c2Rect = card2.GetComponent<RectTransform>();
            c2Rect.anchorMin = new Vector2(0.5f, 0.5f);
            c2Rect.anchorMax = new Vector2(0.5f, 0.5f);
            c2Rect.pivot = new Vector2(0.5f, 0.5f);
            c2Rect.anchoredPosition = new Vector2(0f, 30f);
            c2Rect.sizeDelta = new Vector2(330f, 440f);

            CreateTMP(card2.transform, "Card2_Title", "LEVEL 2\n<size=18><color=#3498db>Crystal Caverns</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(300f, 80f), TextAlignmentOptions.Center, 26f, Color.white);
            TextMeshProUGUI l2Stars = CreateTMP(card2.transform, "Card2_Stars", "* * *",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f), new Vector2(280f, 45f), TextAlignmentOptions.Center, 34f, new Color(1.0f, 0.85f, 0.25f, 1f));
            TextMeshProUGUI l2Stats = CreateTMP(card2.transform, "Card2_Stats", "Target: 12 Coins • Under 65s",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(290f, 60f), TextAlignmentOptions.Center, 17f, new Color(0.85f, 0.85f, 0.85f, 0.9f));
            Button l2Btn = CreateButton(card2.transform, "Card2_PlayBtn", "PLAY", new Vector2(0f, -140f), new Vector2(240f, 52f), new Color(0.2f, 0.75f, 0.35f, 1f));

            // Level Card 3
            GameObject card3 = CreatePanel(selectPanel.transform, "Card_Level3", new Color(0.12f, 0.16f, 0.22f, 0.95f), false);
            RectTransform c3Rect = card3.GetComponent<RectTransform>();
            c3Rect.anchorMin = new Vector2(0.5f, 0.5f);
            c3Rect.anchorMax = new Vector2(0.5f, 0.5f);
            c3Rect.pivot = new Vector2(0.5f, 0.5f);
            c3Rect.anchoredPosition = new Vector2(380f, 30f);
            c3Rect.sizeDelta = new Vector2(330f, 440f);

            CreateTMP(card3.transform, "Card3_Title", "LEVEL 3\n<size=18><color=#e67e22>Volcanic Fortress</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), new Vector2(300f, 80f), TextAlignmentOptions.Center, 26f, Color.white);
            TextMeshProUGUI l3Stars = CreateTMP(card3.transform, "Card3_Stars", "* * *",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f), new Vector2(280f, 45f), TextAlignmentOptions.Center, 34f, new Color(1.0f, 0.85f, 0.25f, 1f));
            TextMeshProUGUI l3Stats = CreateTMP(card3.transform, "Card3_Stats", "Target: 14 Coins • Under 90s",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(290f, 60f), TextAlignmentOptions.Center, 17f, new Color(0.85f, 0.85f, 0.85f, 0.9f));
            Button l3Btn = CreateButton(card3.transform, "Card3_PlayBtn", "PLAY", new Vector2(0f, -140f), new Vector2(240f, 52f), new Color(0.2f, 0.75f, 0.35f, 1f));

            // Bottom Buttons
            Button backBtn = CreateButton(selectPanel.transform, "Back_Button", "BACK", new Vector2(-160f, -330f), new Vector2(220f, 52f), new Color(0.42f, 0.46f, 0.52f, 1f));
            Button resetBtn = CreateButton(selectPanel.transform, "Reset_Button", "RESET PROGRESS", new Vector2(160f, -330f), new Vector2(240f, 52f), new Color(0.68f, 0.24f, 0.24f, 1f));

            selectPanel.SetActive(false);

            // Wire MainMenuController
            controller.SetReferences(
                playBtn, levelSelectBtn, quitBtn,
                bestScore, totalStars,
                selectPanel, backBtn, resetBtn,
                l1Btn, l1Stars, l1Stats,
                l2Btn, l2Stars, l2Stats,
                l3Btn, l3Stars, l3Stats);

            string path = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"<color=#2ecc71>[Platformer]</color> MainMenu built at '{path}'!");
        }

        public static void BuildWinScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureEventSystem();
            EnsureAudioManager();

            // Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.42f, 0.65f, 0.95f, 1f);
            camGo.AddComponent<AudioListener>();

            // Setup scenic parallax background and weather for MainMenu
            ParallaxBuilder.BuildParallaxForLevel("Level 1");
            ParallaxBuilder.BuildWeatherForLevel("Level 1");

            // Canvas
            GameObject canvasGo = new GameObject("Win_Canvas");
            canvasGo.layer = 5; // UI Layer
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            WinSceneController controller = canvasGo.AddComponent<WinSceneController>();

            // Title
            CreateTMP(canvasGo.transform, "Win_Title", "VICTORY!",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 220f), new Vector2(900f, 120f), TextAlignmentOptions.Center, 80f, new Color(0.98f, 0.85f, 0.15f, 1f));

            CreateTMP(canvasGo.transform, "Win_Subtitle", "ALL REALMS CONQUERED!",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f), new Vector2(600f, 45f), TextAlignmentOptions.Center, 24f, Color.white);

            // High-Contrast Stats Banner
            GameObject statsCard = CreatePanel(canvasGo.transform, "Win_Stats_Backdrop", new Color(0.05f, 0.08f, 0.15f, 0.65f), false);
            RectTransform scRect = statsCard.GetComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 0.5f);
            scRect.anchorMax = new Vector2(0.5f, 0.5f);
            scRect.pivot = new Vector2(0.5f, 0.5f);
            scRect.anchoredPosition = new Vector2(0f, 75f);
            scRect.sizeDelta = new Vector2(460f, 92f);

            TextMeshProUGUI totalStars = CreateTMP(statsCard.transform, "Win_TotalStars", "TOTAL STARS: <color=#FFD700>0 / 9</color>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 18f), new Vector2(440f, 36f), TextAlignmentOptions.Center, 24f, Color.white);

            TextMeshProUGUI bestScore = CreateTMP(statsCard.transform, "Win_BestScore", "BEST SCORE: <color=#FFD700>00000</color>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f), new Vector2(440f, 34f), TextAlignmentOptions.Center, 24f, Color.white);

            Color heroGreen = new Color(0.16f, 0.70f, 0.36f, 1f);
            Color slateNavy = new Color(0.15f, 0.22f, 0.32f, 0.95f);
            Color mutedRed  = new Color(0.42f, 0.16f, 0.16f, 0.92f);
            Vector2 btnSize = new Vector2(340f, 54f);

            Button playAgainBtn = CreateButton(canvasGo.transform, "PlayAgain_Button", "PLAY AGAIN", new Vector2(0f, -25f), btnSize, heroGreen);
            Button levelSelectBtn = CreateButton(canvasGo.transform, "LevelSelect_Button", "LEVEL SELECT", new Vector2(0f, -95f), btnSize, slateNavy);
            Button menuBtn = CreateButton(canvasGo.transform, "Menu_Button", "MAIN MENU", new Vector2(0f, -165f), btnSize, mutedRed);

            controller.SetReferences(playAgainBtn, levelSelectBtn, menuBtn, bestScore, totalStars);

            string path = "Assets/Scenes/WinScene.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"<color=#2ecc71>[Platformer]</color> WinScene built at '{path}'!");
        }

        public static void BuildBossRushScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureEventSystem();
            EnsureAudioManager();
            EnsureAchievementManager();

            // Setup Volcanic visual background
            ParallaxBuilder.BuildParallaxForLevel("Level 3");
            ParallaxBuilder.BuildWeatherForLevel("Level 3");

            // Build Level 3 base data customized for Boss Rush
            LevelData data = GetLevel3Data();
            data.levelName = "Boss Rush (Volcanic Arena)";
            data.scenePath = "Assets/Scenes/BossRush.unity";
            data.nextSceneName = "WinScene";

            BuildLevel(data);

            // In BossRush scene, attach BossRushManager to GameManager
            var gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

            if (gm != null && canvas != null)
            {
                GameObject vicPanel = CreatePanel(canvas.transform, "BossRush_VictoryPanel", new Color(0.04f, 0.06f, 0.10f, 0.95f), true);
                vicPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 440f);

                CreateTMP(vicPanel.transform, "Title", "BOSS RUSH CLEARED!",
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -40f), new Vector2(500f, 50f), TextAlignmentOptions.Center, 44f, new Color(1f, 0.85f, 0.2f, 1f));

                TextMeshProUGUI rankTmp = CreateTMP(vicPanel.transform, "Rank", "RANK S (LEGENDARY)",
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -115f), new Vector2(500f, 40f), TextAlignmentOptions.Center, 32f, new Color(0.2f, 0.95f, 1f, 1f));

                TextMeshProUGUI timeTmp = CreateTMP(vicPanel.transform, "Time", "TIME: 00:00.000",
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -170f), new Vector2(400f, 35f), TextAlignmentOptions.Center, 26f, Color.white);

                Button retryBtn = CreateButton(vicPanel.transform, "RetryBtn", "RETRY (R)", new Vector2(0f, -235f), new Vector2(250f, 50f), new Color(0.2f, 0.75f, 0.35f, 1f));
                Button menuBtn = CreateButton(vicPanel.transform, "MenuBtn", "MAIN MENU", new Vector2(0f, -300f), new Vector2(250f, 50f), new Color(0.25f, 0.45f, 0.70f, 1f));

                var brm = gm.gameObject.AddComponent<BossRushManager>();
                SerializedObject bSo = new SerializedObject(brm);
                bSo.FindProperty("victoryPanel").objectReferenceValue = vicPanel;
                bSo.FindProperty("rankText").objectReferenceValue = rankTmp;
                bSo.FindProperty("timeText").objectReferenceValue = timeTmp;
                bSo.FindProperty("retryButton").objectReferenceValue = retryBtn;
                bSo.FindProperty("menuButton").objectReferenceValue = menuBtn;
                bSo.ApplyModifiedProperties();

                vicPanel.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/BossRush.unity");
            Debug.Log("<color=#2ecc71>[Platformer]</color> Boss Rush Scene successfully built at 'Assets/Scenes/BossRush.unity'!");
        }

        private static void SetupLevel2MiniBoss(Scene scene)
        {
            GameObject guardianGo = new GameObject("CrystalGuardian_MiniBoss");
            guardianGo.transform.position = new Vector3(51.5f, 5.5f, 0f);
            guardianGo.tag = "Enemy";
            guardianGo.layer = LayerMask.NameToLayer("Enemy");

            var spr = guardianGo.AddComponent<SpriteRenderer>();
            var chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chaser.prefab");
            if (chaserPrefab != null)
            {
                var origSpr = chaserPrefab.GetComponentInChildren<SpriteRenderer>();
                if (origSpr != null) spr.sprite = origSpr.sprite;
            }
            spr.color = new Color(0.2f, 0.85f, 1f, 1f); // Crystal blue
            guardianGo.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            var boxCol = guardianGo.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(0.9f, 1.1f);

            var rb = guardianGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;

            guardianGo.AddComponent<Health>();
            guardianGo.AddComponent<CrystalGuardianMiniBoss>();
        }

        private static void SetupEliteEnemies(GameObject enemyRoot, string levelName)
        {
            if (enemyRoot == null) return;
            if (levelName.Contains("Level 2") || levelName.Contains("Level 3"))
            {
                var patrollers = enemyRoot.GetComponentsInChildren<EnemyPatroller>();
                if (patrollers.Length > 1)
                {
                    if (patrollers[1].GetComponent<EliteEnemyModifier>() == null)
                    {
                        patrollers[1].gameObject.AddComponent<EliteEnemyModifier>();
                    }
                }

                var chasers = enemyRoot.GetComponentsInChildren<EnemyChaser>();
                if (chasers.Length > 0)
                {
                    if (chasers[0].GetComponent<EliteEnemyModifier>() == null)
                    {
                        chasers[0].gameObject.AddComponent<EliteEnemyModifier>();
                    }
                }
            }
        }

        public static void SetupBuildSettings()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Level1.unity",
                "Assets/Scenes/Level2.unity",
                "Assets/Scenes/Level3.unity",
                "Assets/Scenes/WinScene.unity",
                "Assets/Scenes/BossRush.unity"
            };

            var list = scenePaths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            EditorBuildSettings.scenes = list;
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#2ecc71>[Platformer]</color> Configured EditorBuildSettings with MainMenu (0), Level1 (1), Level2 (2), Level3 (3), WinScene (4), and BossRush (5).");
        }

        public static LevelData GetLevel1Data()
        {
            return new LevelData
            {
                levelName = "Level 1",
                scenePath = "Assets/Scenes/Level1.unity",
                nextSceneName = "Level2",
                layout = Level1Layout,
                skyColor = new Color(0.42f, 0.65f, 0.95f, 1f),
                lightColor = Color.white,
                lightIntensity = 1.0f,
                minBounds = new Vector2(10.5f, 5.5f),
                maxBounds = new Vector2(109.5f, 10.5f),
                patrollerPositions = new Vector3[]
                {
                    new Vector3(9f, 4.5f, 0f),
                    new Vector3(31f, 4.5f, 0f),
                    new Vector3(65f, 4.5f, 0f),
                    new Vector3(107f, 4.5f, 0f)
                },
                chaserPositions = new Vector3[]
                {
                    new Vector3(47f, 4.5f, 0f),
                    new Vector3(84f, 4.5f, 0f)
                },
                flyerPositions = new Vector3[]
                {
                    new Vector3(49f, 8.5f, 0f),
                    new Vector3(88f, 9.5f, 0f)
                },
                shooterPositions = new Vector3[]
                {
                    new Vector3(36f, 8.6f, 0f),
                    new Vector3(83f, 9.6f, 0f)
                },
                checkpointPositions = new Vector3[]
                {
                    new Vector3(26f, 4.25f, 0f),
                    new Vector3(69f, 4.25f, 0f),
                    new Vector3(97f, 4.25f, 0f)
                },
                spikePositions = new Vector3[]
                {
                    new Vector3(14.5f, 4.47f, 0f),
                    new Vector3(46.5f, 7.47f, 0f),
                    new Vector3(65.5f, 8.47f, 0f),
                    new Vector3(81.5f, 5.47f, 0f),
                    new Vector3(101.5f, 4.47f, 0f)
                },
                coinPositions = new Vector3[]
                {
                    new Vector3(8f, 4.6f, 0f), new Vector3(10f, 4.6f, 0f), new Vector3(16f, 6.5f, 0f), new Vector3(17f, 6.5f, 0f),
                    new Vector3(22f, 4.6f, 0f), new Vector3(28f, 6.5f, 0f), new Vector3(38f, 7.6f, 0f), new Vector3(45f, 7.6f, 0f),
                    new Vector3(53f, 9.6f, 0f), new Vector3(60f, 8.6f, 0f), new Vector3(64f, 12.6f, 0f), new Vector3(66f, 12.6f, 0f),
                    new Vector3(74f, 7.6f, 0f), new Vector3(83f, 8.6f, 0f), new Vector3(94f, 7.6f, 0f), new Vector3(100f, 4.6f, 0f),
                    new Vector3(105f, 4.6f, 0f), new Vector3(110f, 5.6f, 0f)
                },
                heartPositions = new Vector3[] { new Vector3(54f, 9.6f, 0f), new Vector3(84f, 12.6f, 0f) },
                movingPlatformPositions = new Vector3[] { new Vector3(55f, 4.25f, 0f) },
                movingPlatformOffsets = new Vector3[] { new Vector3(5.5f, 0f, 0f) },
                crumblingPlatformPositions = new Vector3[] { new Vector3(74.5f, 4.25f, 0f) },
                jumpPadPositions = new Vector3[] { new Vector3(69f, 4.25f, 0f) },
                secretWallPositions = new Vector3[] { new Vector3(36f, 5.0f, 0f) },
                relicPositions = new Vector3[] { new Vector3(38f, 5.0f, 0f) }
            };
        }

        public static LevelData GetLevel2Data()
        {
            return new LevelData
            {
                levelName = "Level 2 (Deep Crystal Cavern)",
                scenePath = "Assets/Scenes/Level2.unity",
                nextSceneName = "Level3",
                layout = Level2Layout,
                skyColor = new Color(0.04f, 0.05f, 0.09f, 1f),
                lightColor = new Color(0.65f, 0.85f, 1.0f, 1f),
                lightIntensity = 0.88f,
                minBounds = new Vector2(10.5f, 5.5f),
                maxBounds = new Vector2(109.5f, 10.5f),
                patrollerPositions = new Vector3[]
                {
                    new Vector3(6f, 5.5f, 0f),
                    new Vector3(25.5f, 5.5f, 0f),
                    new Vector3(42.5f, 5.5f, 0f),
                    new Vector3(62.5f, 5.5f, 0f),
                    new Vector3(82.5f, 5.5f, 0f),
                    new Vector3(102f, 5.5f, 0f)
                },
                chaserPositions = new Vector3[]
                {
                    new Vector3(17.5f, 7.5f, 0f),
                    new Vector3(33.5f, 7.5f, 0f),
                    new Vector3(72.5f, 7.5f, 0f),
                    new Vector3(111f, 5.5f, 0f)
                },
                flyerPositions = new Vector3[]
                {
                    new Vector3(34f, 9.5f, 0f),
                    new Vector3(75f, 9.5f, 0f)
                },
                shooterPositions = new Vector3[]
                {
                    new Vector3(45f, 9.6f, 0f),
                    new Vector3(88f, 9.6f, 0f)
                },
                checkpointPositions = new Vector3[]
                {
                    new Vector3(23f, 5.25f, 0f),
                    new Vector3(60f, 5.25f, 0f),
                    new Vector3(98f, 5.25f, 0f)
                },
                spikePositions = new Vector3[]
                {
                    new Vector3(9.5f, 5.47f, 0f),
                    new Vector3(28.5f, 5.47f, 0f),
                    new Vector3(44.5f, 5.47f, 0f),
                    new Vector3(54.5f, 6.47f, 0f),
                    new Vector3(64.5f, 5.47f, 0f),
                    new Vector3(73.5f, 7.47f, 0f),
                    new Vector3(89.5f, 7.47f, 0f),
                    new Vector3(101.5f, 5.47f, 0f)
                },
                coinPositions = new Vector3[]
                {
                    new Vector3(6f, 5.6f, 0f), new Vector3(8f, 5.6f, 0f), new Vector3(16f, 7.6f, 0f),
                    new Vector3(20f, 7.5f, 0f), new Vector3(29f, 6.5f, 0f), new Vector3(36f, 12.6f, 0f),
                    new Vector3(44f, 8.5f, 0f), new Vector3(50f, 6.5f, 0f), new Vector3(57f, 11.6f, 0f),
                    new Vector3(64f, 7.5f, 0f), new Vector3(72f, 7.6f, 0f), new Vector3(78f, 8.5f, 0f),
                    new Vector3(85f, 6.5f, 0f), new Vector3(90f, 12.6f, 0f), new Vector3(92f, 12.6f, 0f),
                    new Vector3(98f, 7.5f, 0f), new Vector3(104f, 5.6f, 0f), new Vector3(106f, 5.6f, 0f),
                    new Vector3(108f, 6.6f, 0f), new Vector3(112f, 6.6f, 0f)
                },
                heartPositions = new Vector3[] { new Vector3(37f, 12.6f, 0f), new Vector3(91f, 12.6f, 0f) },
                movingPlatformPositions = new Vector3[] { new Vector3(14.5f, 5.25f, 0f), new Vector3(67.5f, 5.25f, 0f) },
                movingPlatformOffsets = new Vector3[] { new Vector3(5.5f, 0f, 0f), new Vector3(3.5f, 0f, 0f) },
                crumblingPlatformPositions = new Vector3[] { new Vector3(76.5f, 6.25f, 0f), new Vector3(94f, 5.25f, 0f) },
                jumpPadPositions = new Vector3[] { new Vector3(41.5f, 5.25f, 0f) },
                secretWallPositions = new Vector3[] { new Vector3(56f, 6.0f, 0f) },
                relicPositions = new Vector3[] { new Vector3(58f, 6.0f, 0f) }
            };
        }

        public static LevelData GetLevel3Data()
        {
            return new LevelData
            {
                levelName = "Level 3 (Volcanic Sky Fortress)",
                scenePath = "Assets/Scenes/Level3.unity",
                nextSceneName = "WinScene",
                layout = Level3Layout,
                skyColor = new Color(0.12f, 0.04f, 0.06f, 1f),
                lightColor = new Color(1.0f, 0.68f, 0.42f, 1f),
                lightIntensity = 0.92f,
                minBounds = new Vector2(10.5f, 5.5f),
                maxBounds = new Vector2(109.5f, 10.5f),
                patrollerPositions = new Vector3[]
                {
                    new Vector3(4.5f, 5.5f, 0f),
                    new Vector3(26.5f, 5.5f, 0f),
                    new Vector3(47.5f, 5.5f, 0f),
                    new Vector3(70.5f, 5.5f, 0f),
                    new Vector3(92.0f, 5.5f, 0f)
                },
                chaserPositions = new Vector3[]
                {
                    new Vector3(21.0f, 8.5f, 0f),
                    new Vector3(35.0f, 7.5f, 0f),
                    new Vector3(61.0f, 7.5f, 0f),
                    new Vector3(75.0f, 8.5f, 0f)
                },
                flyerPositions = new Vector3[]
                {
                    new Vector3(28.0f, 9.5f, 0f),
                    new Vector3(64.0f, 9.5f, 0f)
                },
                shooterPositions = new Vector3[]
                {
                    new Vector3(48.0f, 9.6f, 0f),
                    new Vector3(80.0f, 9.6f, 0f)
                },
                checkpointPositions = new Vector3[]
                {
                    new Vector3(25f, 5.25f, 0f),
                    new Vector3(69.5f, 5.25f, 0f),
                    new Vector3(93f, 5.25f, 0f)
                },
                spikePositions = new Vector3[]
                {
                    new Vector3(6.5f, 5.47f, 0f),
                    new Vector3(16.5f, 6.47f, 0f),
                    new Vector3(21.0f, 8.47f, 0f),
                    new Vector3(35.0f, 7.47f, 0f),
                    new Vector3(46.0f, 5.47f, 0f),
                    new Vector3(61.0f, 7.47f, 0f),
                    new Vector3(72.5f, 5.47f, 0f),
                    new Vector3(82.0f, 6.47f, 0f),
                    new Vector3(97.5f, 9.47f, 0f)
                },
                coinPositions = new Vector3[]
                {
                    new Vector3(6f, 5.6f, 0f), new Vector3(12f, 6.5f, 0f), new Vector3(15f, 6.5f, 0f),
                    new Vector3(21f, 8.5f, 0f), new Vector3(27f, 6.5f, 0f), new Vector3(35f, 7.5f, 0f),
                    new Vector3(38f, 13.6f, 0f), new Vector3(45f, 8.5f, 0f), new Vector3(52f, 12.6f, 0f),
                    new Vector3(58f, 7.5f, 0f), new Vector3(64f, 6.5f, 0f), new Vector3(70f, 12.5f, 0f),
                    new Vector3(76f, 8.6f, 0f), new Vector3(82f, 8.5f, 0f), new Vector3(87f, 7.5f, 0f),
                    new Vector3(92f, 9.5f, 0f), new Vector3(96f, 6.5f, 0f), new Vector3(100f, 6.5f, 0f)
                },
                heartPositions = new Vector3[] { new Vector3(38f, 13.6f, 0f), new Vector3(70f, 13f, 0f) },
                movingPlatformPositions = new Vector3[] { new Vector3(10.5f, 5.25f, 0f), new Vector3(53f, 5.25f, 0f) },
                movingPlatformOffsets = new Vector3[] { new Vector3(6.5f, 0f, 0f), new Vector3(7f, 0f, 0f) },
                crumblingPlatformPositions = new Vector3[] { new Vector3(39.5f, 6.25f, 0f), new Vector3(66f, 5.25f, 0f), new Vector3(87f, 6.25f, 0f), new Vector3(97.5f, 5.25f, 0f) },
                jumpPadPositions = null,
                secretWallPositions = new Vector3[] { new Vector3(72f, 7.0f, 0f) },
                relicPositions = new Vector3[] { new Vector3(74f, 7.0f, 0f) }
            };
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

        private static void EnsureSpritesAndTiles()
        {
            if (!Directory.Exists("Assets/Art/Tiles")) Directory.CreateDirectory("Assets/Art/Tiles");
            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");

            // Ensure tile sprites
            if (!File.Exists("Assets/Art/Tiles/GroundTop.png"))
            {
                Level1Builder.CreateLevel1();
            }
        }

        private static void EnsureAllPrefabs()
        {
            // Ensure Player Prefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab") == null)
            {
                TestSceneBuilder.CreateTestScene();
            }

            // Ensure Enemies
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Patroller.prefab") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chaser.prefab") == null)
            {
                EnemyBuilder.AddEnemiesToLevel1();
            }

            // Ensure Collectibles & Checkpoints
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Collectible_Coin.prefab") == null)
            {
                GameplayObjectsBuilder.AddGameplayObjectsToLevel1();
            }

            // Ensure LevelGoal Prefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LevelGoal.prefab") == null)
            {
                BuildLevelGoalPrefab();
            }

            // Ensure Dynamic Platforms & Jump Pads
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Platform_Moving.prefab") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Platform_Crumbling.prefab") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactable_JumpPad.prefab") == null)
            {
                DynamicPlatformBuilder.EnsureAllDynamicPlatformPrefabs();
            }

            // Ensure Parallax Textures & Weather Prefabs
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ParallaxBuilder.WeatherL1PrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Parallax/BG_L1_MountainsFar.png") == null)
            {
                ParallaxBuilder.GenerateAllAssets();
            }

            // Ensure New Enemy Prefabs (Flyer & Shooter)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(NewEnemiesBuilder.FlyerPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(NewEnemiesBuilder.ShooterPrefabPath) == null)
            {
                NewEnemiesBuilder.GenerateNewEnemyAssets();
            }

            // Ensure Energy Slash Projectile & Player Combat
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnergySlashBuilder.SlashPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<Sprite>(EnergySlashBuilder.SlashSpritePath) == null)
            {
                EnergySlashBuilder.GenerateEnergySlashAssets();
            }

            // Ensure Secret Walls & Relics
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SecretWallBuilder.WallPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(SecretWallBuilder.RelicPrefabPath) == null)
            {
                SecretWallBuilder.GenerateSecretAssets();
            }
        }

        private static void BuildLevelGoalPrefab()
        {
            Sprite goalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tiles/GoalMarker.png");
            GameObject go = new GameObject("LevelGoal");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = goalSprite;
            sr.sortingOrder = 5;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1.5f);
            col.offset = new Vector2(0f, 0.75f);

            go.AddComponent<LevelGoal>();

            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/LevelGoal.prefab");
            UnityEngine.Object.DestroyImmediate(go);
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

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor, bool stretch)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }

            Image img = panel.AddComponent<Image>();
            img.color = bgColor;
            return panel;
        }

        private static Slider CreateSlider(Transform parent, string name, string labelText, Vector2 anchoredPos, Vector2 size, float initialValue)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = anchoredPos;
            rootRect.sizeDelta = size;

            // Label on left
            CreateTMP(root.transform, "Label", labelText,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(140f, 30f), TextAlignmentOptions.Left, 20f, Color.white);

            // Slider GameObject
            GameObject sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(root.transform, false);
            RectTransform sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(70f, 0f);
            sliderRect.sizeDelta = new Vector2(-150f, 20f);

            Slider slider = sliderGo.AddComponent<Slider>();

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.25f, 0.32f, 1f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.25f);
            faRect.anchorMax = new Vector2(1f, 0.75f);
            faRect.sizeDelta = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.8f, 0.95f, 1f);

            slider.targetGraphic = bgImg;
            slider.fillRect = fillRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;

            return slider;
        }

        private static UpgradeShopUI BuildUpgradeShopPanel(Transform canvasTransform)
        {
            GameObject shopPanel = CreatePanel(canvasTransform, "UpgradeShop_Panel", new Color(0.06f, 0.08f, 0.12f, 0.96f), true);

            CreateTMP(shopPanel.transform, "Shop_Title", "UPGRADE & SKILL SHOP",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 360f), new Vector2(700f, 60f), TextAlignmentOptions.Center, 52f, new Color(0.98f, 0.82f, 0.15f, 1f));

            CreateTMP(shopPanel.transform, "Shop_Subtitle", "ENHANCE YOUR HERO PERMANENTLY WITH BANKED COINS",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 310f), new Vector2(800f, 40f), TextAlignmentOptions.Center, 20f, new Color(0.85f, 0.85f, 0.85f, 0.9f));

            TextMeshProUGUI coinsText = CreateTMP(shopPanel.transform, "Shop_CoinsText", "COINS: 00",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 260f), new Vector2(400f, 45f), TextAlignmentOptions.Center, 30f, new Color(0.2f, 0.85f, 1f, 1f));

            // Card 1: Max Health
            GameObject card1 = CreatePanel(shopPanel.transform, "Card_Health", new Color(0.12f, 0.16f, 0.22f, 0.95f), false);
            RectTransform c1Rect = card1.GetComponent<RectTransform>();
            c1Rect.anchorMin = new Vector2(0.5f, 0.5f);
            c1Rect.anchorMax = new Vector2(0.5f, 0.5f);
            c1Rect.pivot = new Vector2(0.5f, 0.5f);
            c1Rect.anchoredPosition = new Vector2(-380f, 60f);
            c1Rect.sizeDelta = new Vector2(340f, 260f);

            CreateTMP(card1.transform, "C1_Title", "MAX HEALTH\n<size=16><color=#e74c3c>+1 Life Heart</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 75f), new Vector2(300f, 60f), TextAlignmentOptions.Center, 24f, Color.white);

            TextMeshProUGUI hpLvl = CreateTMP(card1.transform, "C1_Lvl", "LEVEL: 0 / 2",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 15f), new Vector2(300f, 35f), TextAlignmentOptions.Center, 18f, new Color(0.9f, 0.9f, 0.9f, 1f));

            TextMeshProUGUI hpCost = CreateTMP(card1.transform, "C1_Cost", "15 COINS",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -25f), new Vector2(300f, 35f), TextAlignmentOptions.Center, 20f, new Color(0.98f, 0.82f, 0.15f, 1f));

            Button hpBtn = CreateButton(card1.transform, "C1_Btn", "UPGRADE", new Vector2(0f, -80f), new Vector2(220f, 45f), new Color(0.2f, 0.75f, 0.35f, 1f));

            // Card 2: Dash Booster
            GameObject card2 = CreatePanel(shopPanel.transform, "Card_Dash", new Color(0.12f, 0.16f, 0.22f, 0.95f), false);
            RectTransform c2Rect = card2.GetComponent<RectTransform>();
            c2Rect.anchorMin = new Vector2(0.5f, 0.5f);
            c2Rect.anchorMax = new Vector2(0.5f, 0.5f);
            c2Rect.pivot = new Vector2(0.5f, 0.5f);
            c2Rect.anchoredPosition = new Vector2(0f, 60f);
            c2Rect.sizeDelta = new Vector2(340f, 260f);

            CreateTMP(card2.transform, "C2_Title", "DASH BOOSTER\n<size=16><color=#3498db>Faster Air Dash</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 75f), new Vector2(300f, 60f), TextAlignmentOptions.Center, 24f, Color.white);

            TextMeshProUGUI dashLvl = CreateTMP(card2.transform, "C2_Lvl", "LEVEL: 0 / 2",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 15f), new Vector2(300f, 35f), TextAlignmentOptions.Center, 18f, new Color(0.9f, 0.9f, 0.9f, 1f));

            TextMeshProUGUI dashCost = CreateTMP(card2.transform, "C2_Cost", "15 COINS",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -25f), new Vector2(300f, 35f), TextAlignmentOptions.Center, 20f, new Color(0.98f, 0.82f, 0.15f, 1f));

            Button dashBtn = CreateButton(card2.transform, "C2_Btn", "UPGRADE", new Vector2(0f, -80f), new Vector2(220f, 45f), new Color(0.2f, 0.75f, 0.35f, 1f));

            // Card 3: Coin Magnet
            GameObject card3 = CreatePanel(shopPanel.transform, "Card_Magnet", new Color(0.12f, 0.16f, 0.22f, 0.95f), false);
            RectTransform c3Rect = card3.GetComponent<RectTransform>();
            c3Rect.anchorMin = new Vector2(0.5f, 0.5f);
            c3Rect.anchorMax = new Vector2(0.5f, 0.5f);
            c3Rect.pivot = new Vector2(0.5f, 0.5f);
            c3Rect.anchoredPosition = new Vector2(380f, 60f);
            c3Rect.sizeDelta = new Vector2(340f, 260f);

            CreateTMP(card3.transform, "C3_Title", "COIN MAGNET\n<size=16><color=#f1c40f>Pulls Gold Nearby</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 75f), new Vector2(300f, 60f), TextAlignmentOptions.Center, 24f, Color.white);

            TextMeshProUGUI magLvl = CreateTMP(card3.transform, "C3_Lvl", "LEVEL: 0 / 2",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 15f), new Vector2(300f, 35f), TextAlignmentOptions.Center, 18f, new Color(0.9f, 0.9f, 0.9f, 1f));

            TextMeshProUGUI magCost = CreateTMP(card3.transform, "C3_Cost", "20 COINS",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -25f), new Vector2(300f, 35f), TextAlignmentOptions.Center, 20f, new Color(0.98f, 0.82f, 0.15f, 1f));

            Button magBtn = CreateButton(card3.transform, "C3_Btn", "UPGRADE", new Vector2(0f, -80f), new Vector2(220f, 45f), new Color(0.2f, 0.75f, 0.35f, 1f));

            // Card 4: Blade Mastery
            GameObject card4 = CreatePanel(shopPanel.transform, "Card_Blade", new Color(0.12f, 0.16f, 0.22f, 0.95f), false);
            RectTransform c4Rect = card4.GetComponent<RectTransform>();
            c4Rect.anchorMin = new Vector2(0.5f, 0.5f);
            c4Rect.anchorMax = new Vector2(0.5f, 0.5f);
            c4Rect.pivot = new Vector2(0.5f, 0.5f);
            c4Rect.anchoredPosition = new Vector2(0f, -145f);
            c4Rect.sizeDelta = new Vector2(620f, 110f);

            CreateTMP(card4.transform, "C4_Title", "BLADE MASTERY\n<size=15><color=#9b59b6>Lower Mana Cost (30->20) & Higher Damage</color></size>",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(25f, 0f), new Vector2(360f, 60f), TextAlignmentOptions.Left, 20f, Color.white);

            TextMeshProUGUI bladeLvl = CreateTMP(card4.transform, "C4_Lvl", "LVL: 0/1",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(320f, 15f), new Vector2(100f, 30f), TextAlignmentOptions.Center, 16f, new Color(0.9f, 0.9f, 0.9f, 1f));

            TextMeshProUGUI bladeCost = CreateTMP(card4.transform, "C4_Cost", "25 COINS",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(320f, -15f), new Vector2(100f, 30f), TextAlignmentOptions.Center, 18f, new Color(0.98f, 0.82f, 0.15f, 1f));

            Button bladeBtn = CreateButton(card4.transform, "C4_Btn", "UPGRADE", new Vector2(210f, 0f), new Vector2(160f, 48f), new Color(0.2f, 0.75f, 0.35f, 1f));

            // Close button
            Button closeBtn = CreateButton(shopPanel.transform, "Shop_CloseBtn", "BACK", new Vector2(0f, -260f), new Vector2(240f, 52f), new Color(0.65f, 0.22f, 0.22f, 1f));

            UpgradeShopUI shopUI = shopPanel.AddComponent<UpgradeShopUI>();
            shopUI.SetReferences(
                shopPanel, coinsText, closeBtn,
                hpLvl, hpCost, hpBtn,
                dashLvl, dashCost, dashBtn,
                magLvl, magCost, magBtn,
                bladeLvl, bladeCost, bladeBtn
            );

            shopPanel.SetActive(false);
            return shopUI;
        }

        public static GameObject EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            var uiModule = esGo.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();
#else
            esGo.AddComponent<StandaloneInputModule>();
#endif
            return esGo;
        }

        private static void EnsureAudioManager()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
            if (existing == null)
            {
                GameObject audioGo = new GameObject("AudioManager");
                audioGo.AddComponent<AudioManager>();
            }
        }

        private static void EnsureAchievementManager()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<AchievementManager>();
            if (existing == null)
            {
                GameObject achGo = new GameObject("[AchievementManager]");
                achGo.AddComponent<AchievementManager>();
            }
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size, Color btnColor)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = true;

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;

            ColorBlock cb = btn.colors;
            cb.normalColor = btnColor;
            cb.highlightedColor = btnColor * 1.15f;
            cb.pressedColor = btnColor * 0.75f;
            cb.selectedColor = btnColor;
            cb.colorMultiplier = 1f;
            btn.colors = cb;

            TextMeshProUGUI tmp = CreateTMP(btnGo.transform, "Btn_Text", text,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 24f, Color.white);
            tmp.raycastTarget = false; // Prevents text from intercepting button clicks

            return btn;
        }

        private static void TryAddGlobalLight2D(Color lightColor, float intensity)
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
                if (intensityProp != null) intensityProp.SetValue(lightComp, intensity);

                var colorProp = lightType.GetProperty("color");
                if (colorProp != null) colorProp.SetValue(lightComp, lightColor);
            }
        }

        private static readonly string[] Level1Layout = new string[]
        {
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W................................................................BBBB...................BBBB...........................W",
            "W......................................................................................................................W",
            "W......................................BBBB..................................BBBB......................................W",
            "W...................................................BBBBB.........................................BBBB.................W",
            "W.......................BBBB...................................BBBB..................BBBB..............................W",
            "W..#####....................................BBBBB.......................................................BBBB...........W",
            "W..#####.......BBBB.............BBBB.......................BBBB..............................................E.........W",
            "W..##S##....................BBBB...............................................BBBB.......................BBBBBBB......W",
            "WTTTTTTTTTTTTTTTTTTTT....TTTTTTTTTTT.....TTTTTTTTTTTTT........TTTTTTTTTT......TTTTTTTTTTT.......TTTTTTTTTTTTTTTTTTTTTTTW",
            "WGGGGGGGGGGGGGGGGGGGG....GGGGGGGGGGG.....GGGGGGGGGGGGG........GGGGGGGGGG......GGGGGGGGGGG.......GGGGGGGGGGGGGGGGGGGGGGGW",
            "WGGGGGGGGGGGGGGGGGGGG....GGGGGGGGGGG.....GGGGGGGGGGGGG........GGGGGGGGGG......GGGGGGGGGGG.......GGGGGGGGGGGGGGGGGGGGGGGW",
            "WGGGGGGGGGGGGGGGGGGGG....GGGGGGGGGGG.....GGGGGGGGGGGGG........GGGGGGGGGG......GGGGGGGGGGG.......GGGGGGGGGGGGGGGGGGGGGGGW"
        };

        private static readonly string[] Level2Layout = new string[]
        {
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W..................................BBBB...................................................BBBBB........................W",
            "W........................................................BBBB..........................................................W",
            "W.......................BBBB.................................................BBBB......................................W",
            "W.................................................................................................BBBB.................W",
            "W..#####...................................BBBBB.......................................................................W",
            "W..#####........BBBB............BBBB...................................BBBB..............BBBB..................E.......W",
            "W..##S##............................................BBBB...................................................BBBBBBB.....W",
            "WTTTTTTTTTTTT.........TTTTTTTT.........TTTTTTTT............TTTTTTTT.............TTTTTT..........TTTTTTTTTTTTTTTTTTTTTTTW",
            "WGGGGGGGGGGGG.........GGGGGGGG.........GGGGGGGG............GGGGGGGG.............GGGGGG..........GGGGGGGGGGGGGGGGGGGGGGGW",
            "WGGGGGGGGGGGG.........GGGGGGGG.........GGGGGGGG............GGGGGGGG.............GGGGGG..........GGGGGGGGGGGGGGGGGGGGGGGW",
            "WGGGGGGGGGGGG.........GGGGGGGG.........GGGGGGGG............GGGGGGGG.............GGGGGG..........GGGGGGGGGGGGGGGGGGGGGGGW",
            "WGGGGGGGGGGGG.........GGGGGGGG.........GGGGGGGG............GGGGGGGG.............GGGGGG..........GGGGGGGGGGGGGGGGGGGGGGGW"
        };

        private static readonly string[] Level3Layout = new string[]
        {
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W......................................................................................................................W",
            "W...................................BBB.....................................BBB........................................W",
            "W...................................................BBB................................................................W",
            "W.........................BBB.....................................BBB...................BBB............................W",
            "W......................................................................................................................W",
            "W.............................................BBB...............................................BBB....................W",
            "W...................BBB...................................................BBB...........................BBB......BBB...W",
            "W..#####..........................BBB.......................BBB.....................................................E..W",
            "W..##S##.......BBB...............................................................BBB...................................W",
            "WTTTTTTTT...............TTTTTT...............TTTTTT.................TTTTTT...............TTTTTTT....TT.TTTTTTTTTTTTTTTTW",
            "WGGGGGGGG...............GGGGGG...............GGGGGG.................GGGGGG...............GGGGGGG....GG.GGGGGGGGGGGGGGGGW",
            "WGGGGGGGG...............GGGGGG...............GGGGGG.................GGGGGG...............GGGGGGG....GG.GGGGGGGGGGGGGGGGW",
            "WGGGGGGGG...............GGGGGG...............GGGGGG.................GGGGGG...............GGGGGGG....GG.GGGGGGGGGGGGGGGGW",
            "WGGGGGGGG...............GGGGGG...............GGGGGG.................GGGGGG...............GGGGGGG....GG.GGGGGGGGGGGGGGGGW"
        };
    }
}

