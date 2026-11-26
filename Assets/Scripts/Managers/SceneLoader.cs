using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Platformer.Managers
{
    /// <summary>
    /// Persistent singleton managing smooth scene transitions with a black fade overlay.
    /// Auto-initializes if not already present in the scene.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader instance;

        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject loaderGo = new GameObject("[SceneLoader]");
                    instance = loaderGo.AddComponent<SceneLoader>();
                    DontDestroyOnLoad(loaderGo);
                    instance.InitializeOverlay();
                }
                return instance;
            }
        }

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 0.35f;

        private CanvasGroup overlayCanvasGroup;
        private bool isTransitioning;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeOverlay();
        }

        private void InitializeOverlay()
        {
            if (overlayCanvasGroup != null) return;

            // Create ScreenSpace overlay canvas
            GameObject canvasGo = new GameObject("Transition_Canvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Topmost layer

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            overlayCanvasGroup = canvasGo.AddComponent<CanvasGroup>();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;

            // Fullscreen solid black fade image
            GameObject imageGo = new GameObject("Fade_Image");
            imageGo.transform.SetParent(canvasGo.transform, false);

            RectTransform rect = imageGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = imageGo.AddComponent<Image>();
            img.color = Color.black;
        }

        public static void LoadScene(string sceneName)
        {
            Instance.StartCoroutine(Instance.FadeAndLoad(sceneName));
        }

        public static void ReloadCurrentScene()
        {
            Scene active = SceneManager.GetActiveScene();
            string sName = !string.IsNullOrEmpty(active.name) ? active.name : $"Level{active.buildIndex}";
            Instance.StartCoroutine(Instance.FadeAndLoad(sName, active.buildIndex));
        }

        private IEnumerator FadeAndLoad(string sceneName, int buildIndex = -1)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;

            try
            {
                // Ensure overlay exists
                InitializeOverlay();

                // 1. Fade Out (transparent -> solid black)
                if (overlayCanvasGroup != null)
                {
                    overlayCanvasGroup.blocksRaycasts = true;
                    float elapsed = 0f;
                    while (elapsed < fadeDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        overlayCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                        yield return null;
                    }
                    overlayCanvasGroup.alpha = 1f;
                }

                // Ensure normal time scale before and after scene load
                Time.timeScale = 1f;

                // 2. Load Scene Asynchronously
                AsyncOperation asyncOp = buildIndex >= 0
                    ? SceneManager.LoadSceneAsync(buildIndex)
                    : SceneManager.LoadSceneAsync(sceneName);

                while (asyncOp != null && !asyncOp.isDone)
                {
                    yield return null;
                }

                Time.timeScale = 1f;

                // Short frame delay for scene objects to settle
                yield return null;

                // 3. Fade In (solid black -> transparent)
                if (overlayCanvasGroup != null)
                {
                    float elapsed = 0f;
                    while (elapsed < fadeDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        overlayCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                        yield return null;
                    }
                    overlayCanvasGroup.alpha = 0f;
                    overlayCanvasGroup.blocksRaycasts = false;
                }
            }
            finally
            {
                if (overlayCanvasGroup != null)
                {
                    overlayCanvasGroup.alpha = 0f;
                    overlayCanvasGroup.blocksRaycasts = false;
                }
                isTransitioning = false;
            }
        }
    }
}

