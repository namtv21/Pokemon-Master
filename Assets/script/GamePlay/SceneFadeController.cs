using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFadeController : MonoBehaviour
{
    public static SceneFadeController Instance { get; private set; }

    private CanvasGroup sceneFadeCanvasGroup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            EnsureSceneFadeOverlay();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureSceneFadeOverlay()
    {
        if (sceneFadeCanvasGroup != null)
            return;

        var canvasGO = new GameObject("SceneFadeOverlay");
        canvasGO.transform.SetParent(this.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        sceneFadeCanvasGroup = panel.AddComponent<CanvasGroup>();
        sceneFadeCanvasGroup.alpha = 0f;
    }

    public IEnumerator Fade(float targetAlpha, float duration)
    {
        if (sceneFadeCanvasGroup == null)
            EnsureSceneFadeOverlay();

        float startAlpha = sceneFadeCanvasGroup.alpha;
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            sceneFadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        sceneFadeCanvasGroup.alpha = targetAlpha;
    }

    public void SetImmediate(float alpha)
    {
        if (sceneFadeCanvasGroup == null)
            EnsureSceneFadeOverlay();

        sceneFadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    // Gọi trước SceneManager.LoadScene — tự động fade in sau khi scene mới load xong
    public void FadeInAfterLoad(float duration)
    {
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnLoaded;
            StartCoroutine(Fade(0f, duration));
        }
        SceneManager.sceneLoaded += OnLoaded;
    }
}
