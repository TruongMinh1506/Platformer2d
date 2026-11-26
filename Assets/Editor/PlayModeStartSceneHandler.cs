using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Platformer.Managers;

namespace Platformer.Editor
{
    /// <summary>
    /// Configures Unity Editor Play Mode to ALWAYS start from MainMenu (Scene 0),
    /// regardless of which level scene is currently open in the Hierarchy.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartSceneHandler
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        static PlayModeStartSceneHandler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += EnsureMainMenuAsPlayStartScene;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EnsureMainMenuAsPlayStartScene();
            }
        }

        [MenuItem("Tools/Platformer/Set MainMenu as Play Mode Start Scene", priority = 90)]
        public static void EnsureMainMenuAsPlayStartScene()
        {
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (startScene != null)
            {
                EditorSceneManager.playModeStartScene = startScene;
            }
        }

        [MenuItem("Tools/Platformer/Reset All Progression (PlayerPrefs)", priority = 95)]
        public static void ResetPlayerPrefsProgression()
        {
            ProgressManager.ResetProgress();
            Debug.Log("<color=#2ecc71>[Platformer]</color> All game progression and unlocked levels reset to Level 1!");
        }
    }
}
