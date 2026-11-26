using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Platformer.Editor
{
    /// <summary>
    /// Editor build pipeline script for Platformer 2D.
    /// Provides menu item: Tools/Platformer/Build Windows
    /// Builds a 64-bit standalone Windows executable into Builds/Platformer2D.exe.
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Tools/Platformer/Build Windows", priority = 100)]
        public static void BuildWindows()
        {
            string buildFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string exePath = Path.Combine(buildFolder, "Platformer2D.exe");

            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Level1.unity",
                "Assets/Scenes/Level2.unity",
                "Assets/Scenes/Level3.unity",
                "Assets/Scenes/BossRush.unity",
                "Assets/Scenes/WinScene.unity"
            };

            // Verify all scene files exist before starting build
            foreach (var scene in scenes)
            {
                if (!File.Exists(scene))
                {
                    Debug.LogError($"[BuildScript] Scene not found: {scene}. Run 'Tools/Platformer/5 - Create Levels 2 and 3' or '6 - Create Animations' first.");
                    if (!Application.isBatchMode)
                    {
                        EditorUtility.DisplayDialog("Build Failed", $"Missing scene: {scene}", "OK");
                    }
                    return;
                }
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"<color=#3498db>[Platformer Build]</color> Starting Windows 64-bit build to: {exePath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#2ecc71>[Platformer Build]</color> Build succeeded! Total time: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize / (1024f * 1024f):F1} MB at {exePath}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Succeeded", $"Platformer2D Windows build completed successfully!\n\nPath: {exePath}\nSize: {summary.totalSize / (1024f * 1024f):F1} MB", "OK");
                }
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"<color=#e74c3c>[Platformer Build]</color> Build failed with {summary.totalErrors} errors.");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Failed", $"Build failed with {summary.totalErrors} errors. Check Console for details.", "OK");
                }
            }
        }

        public static void FullRebuildAndTest()
        {
            Debug.Log("[BuildScript] Starting Full Rebuild...");
            ParallaxBuilder.GenerateAllAssets();
            LevelSceneFlowBuilder.CreateLevels2And3AndFlow();
            BuildWindows();
        }
    }
}

