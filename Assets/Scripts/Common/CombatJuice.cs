using System.Collections;
using UnityEngine;
using Platformer.Managers;

namespace Platformer.Common
{
    /// <summary>
    /// Central manager for high-impact game feel (Juice):
    /// - Micro hit-stops / frame freezes
    /// - Directional camera impulses
    /// - Bullet-time slow motion on boss finishers / critical parries
    /// - Smooth camera zooms
    /// </summary>
    public class CombatJuice : MonoBehaviour
    {
        private static CombatJuice instance;

        public static CombatJuice Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = Object.FindFirstObjectByType<CombatJuice>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        GameObject go = new GameObject("[CombatJuice]");
                        instance = go.AddComponent<CombatJuice>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private Coroutine slowMoRoutine;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnDisable()
        {
            if (slowMoRoutine != null)
            {
                StopCoroutine(slowMoRoutine);
                slowMoRoutine = null;
            }
            Time.timeScale = 1.0f;
        }

        public static void HitStop(float duration = 0.05f)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerHitStop(duration);
            }
        }

        public static void DirectionalShake(Vector2 direction, float duration = 0.16f, float intensity = 0.35f)
        {
            CameraFollow.DirectionalShake(direction, duration, intensity);
        }

        public static void Shake(float duration = 0.2f, float intensity = 0.3f)
        {
            CameraFollow.Shake(duration, intensity);
        }

        public static void SlowMotion(float timeScale = 0.25f, float duration = 1.0f)
        {
            Instance.TriggerSlowMotion(timeScale, duration);
        }

        public static void CinematicZoom(float targetSize = 5.2f, float duration = 0.4f, float holdTime = 0.6f)
        {
            CameraFollow.CinematicZoom(targetSize, duration, holdTime);
        }

        public void TriggerSlowMotion(float targetScale, float duration)
        {
            if (slowMoRoutine != null)
            {
                StopCoroutine(slowMoRoutine);
            }
            slowMoRoutine = StartCoroutine(SlowMotionRoutine(targetScale, duration));
        }

        private IEnumerator SlowMotionRoutine(float targetScale, float duration)
        {
            Time.timeScale = targetScale;
            yield return new WaitForSecondsRealtime(duration);
            if (!UI.PauseMenuController.IsGamePaused)
            {
                Time.timeScale = 1.0f;
            }
            slowMoRoutine = null;
        }
    }
}
