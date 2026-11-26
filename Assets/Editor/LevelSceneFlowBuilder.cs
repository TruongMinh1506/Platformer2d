using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Platformer.Editor
{
    /// <summary>
    /// Editor automation tool for Task 5:
    /// Menu item Tools/Platformer/5 - Create Levels 2 and 3.
    /// Builds MainMenu, Level 1, Level 2, Level 3, and WinScene using the shared LevelBuilder engine,
    /// sets up LevelGoal transitions, Pause menu, and configures all scenes in EditorBuildSettings.
    /// </summary>
    public static class LevelSceneFlowBuilder
    {
        [MenuItem("Tools/Platformer/5 - Create Levels 2 and 3", priority = 5)]
        public static void CreateLevels2And3AndFlow()
        {
            // 0. Ensure updated combat assets & prefabs
            EnergySlashBuilder.GenerateEnergySlashAssets();
            InteractiveObjectsBuilder.GenerateAllInteractiveSprites();
            DynamicPlatformBuilder.EnsureAllDynamicPlatformPrefabs();

            // 1. Build MainMenu scene
            LevelBuilder.BuildMainMenuScene();

            // 2. Build Level 1 with LevelGoal leading to Level 2
            LevelBuilder.BuildLevel(LevelBuilder.GetLevel1Data());

            // 3. Build Level 2 (Crystal Cavern - increased difficulty)
            LevelBuilder.BuildLevel(LevelBuilder.GetLevel2Data());

            // 4. Build Level 3 (Sky Fortress - climax difficulty)
            LevelBuilder.BuildLevel(LevelBuilder.GetLevel3Data());

            // 5. Build WinScene
            LevelBuilder.BuildWinScene();

            // 6. Build BossRush Arena Scene
            LevelBuilder.BuildBossRushScene();

            // 7. Register all scenes in EditorBuildSettings in exact sequential order
            LevelBuilder.SetupBuildSettings();

            // 8. Open MainMenu scene ready for play-testing
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            Debug.Log("<color=#2ecc71>[Platformer]</color> Multiple level flow successfully built! MainMenu (0), Level1 (1), Level2 (2), Level3 (3), WinScene (4), and BossRush (5).");
        }
    }
}

