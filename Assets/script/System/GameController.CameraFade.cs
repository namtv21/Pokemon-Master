using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

// Phần trình diễn khi chuyển cảnh / battle của GameController (partial class):
// scene fade, cô lập camera & AudioListener và ẩn/hiện scene overworld khi vào battle.
public partial class GameController
{
    public void LoadSceneWithFade(string sceneName, string spawnPointId, float fadeOutDuration = 0.5f, float fadeInDuration = 0.25f)
    {
        if (isSceneTransitioning)
            return;

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName, spawnPointId, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator LoadSceneWithFadeRoutine(string sceneName, string spawnPointId, float fadeOutDuration, float fadeInDuration)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            yield break;

        isSceneTransitioning = true;

        yield return FadeSceneOverlay(1f, fadeOutDuration);

        SpawnManager.SetNextSpawnPoint(spawnPointId);
        SceneManager.LoadScene(sceneName);

        // Wait one frame so the new scene is initialized before fade-in.
        yield return null;

        yield return FadeSceneOverlay(0f, fadeInDuration);

        isSceneTransitioning = false;
    }

    private void EnsureSceneFadeOverlay()
    {
        if (sceneFadeCanvasGroup != null)
            return;

        var canvasGO = new GameObject("SceneFadeOverlay");
        DontDestroyOnLoad(canvasGO);

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

    private IEnumerator FadeSceneOverlay(float targetAlpha, float duration)
    {
        var fadeCtrl = GetOrCreateSceneFadeController();
        if (fadeCtrl != null)
        {
            yield return fadeCtrl.Fade(targetAlpha, duration);
            yield break;
        }

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

    private void SetSceneFadeImmediate(float alpha)
    {
        var fadeCtrl = GetOrCreateSceneFadeController();
        if (fadeCtrl != null)
            fadeCtrl.SetImmediate(alpha);

        if (sceneFadeCanvasGroup != null)
            sceneFadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private SceneFadeController GetOrCreateSceneFadeController()
    {
        if (SceneFadeController.Instance != null)
            return SceneFadeController.Instance;

        var existing = FindObjectOfType<SceneFadeController>(true);
        if (existing != null)
            return existing;

        var go = new GameObject("SceneFadeController");
        DontDestroyOnLoad(go);
        return go.AddComponent<SceneFadeController>();
    }

    private void SetBattleCameraIsolation(bool inBattle)
    {
        if (inBattle && battleCameraIsolationActive)
        {
            EnforceBattleCameraSettings();
            return;
        }

        if (!inBattle && !battleCameraIsolationActive)
            return;

        var cameras = FindObjectsOfType<Camera>(true);
        var listeners = FindObjectsOfType<AudioListener>(true);
        var battleScene = SceneManager.GetSceneByName(battleSceneName);

        if (inBattle)
        {
            battleCameraIsolationActive = true;
            cachedCameraEnabledStates.Clear();
            cachedListenerEnabledStates.Clear();

            for (int i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null) continue;

                bool belongsToBattleScene = battleScene.IsValid() && cam.gameObject.scene == battleScene;
                cachedCameraEnabledStates[cam] = cam.enabled;
                cam.enabled = belongsToBattleScene;
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                var al = listeners[i];
                if (al == null) continue;

                bool belongsToBattleScene = battleScene.IsValid() && al.gameObject.scene == battleScene;
                cachedListenerEnabledStates[al] = al.enabled;
                al.enabled = belongsToBattleScene;
            }

            EnforceBattleCameraSettings();
            return;
        }

        foreach (var kv in cachedCameraEnabledStates)
        {
            if (kv.Key != null)
                kv.Key.enabled = kv.Value;
        }

        foreach (var kv in cachedListenerEnabledStates)
        {
            if (kv.Key != null)
                kv.Key.enabled = kv.Value;
        }

        cachedCameraEnabledStates.Clear();
        cachedListenerEnabledStates.Clear();
        battleCameraIsolationActive = false;
    }

    private void SetOverworldSceneVisibility(bool visible)
    {
        if (!visible && overworldSceneHidden)
            return;

        if (visible && !overworldSceneHidden)
            return;

        if (string.IsNullOrWhiteSpace(cachedOverworldSceneName))
            return;

        var scene = SceneManager.GetSceneByName(cachedOverworldSceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var roots = scene.GetRootGameObjects();
        if (!visible)
        {
            cachedOverworldRootStates.Clear();
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                    continue;

                // Never hide the root that contains this GameController.
                if (transform.IsChildOf(root.transform))
                    continue;

                // MusicManager phải luôn active để có thể restore nhạc khi battle kết thúc.
                if (root.GetComponentInChildren<MusicManager>(true) != null)
                    continue;

                cachedOverworldRootStates[root] = root.activeSelf;
                root.SetActive(false);
            }

            overworldSceneHidden = true;
            return;
        }

        foreach (var kv in cachedOverworldRootStates)
        {
            if (kv.Key != null)
                kv.Key.SetActive(kv.Value);
        }

        cachedOverworldRootStates.Clear();
        overworldSceneHidden = false;
    }

    private void EnforceBattleCameraSettings()
    {
        if (!battleCameraIsolationActive)
            return;

        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (!battleScene.IsValid() || !battleScene.isLoaded)
            return;

        var cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            if (cam == null)
                continue;

            bool belongsToBattleScene = cam.gameObject.scene == battleScene;
            if (belongsToBattleScene)
            {
                cam.enabled = true;
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.depth = 100f;
            }
            else
            {
                cam.enabled = false;
            }
        }
    }
}
