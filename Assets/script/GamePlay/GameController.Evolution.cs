using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class GameController
{
    private IEnumerator PlayEvolutionVisual(Sprite beforeSprite, Sprite afterSprite)
    {
        var canvasGO = new GameObject("EvolutionCanvas");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imageGO = new GameObject("EvoImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        var rect = imageGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 320);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        var image = imageGO.AddComponent<Image>();
        image.preserveAspect = true;
        image.sprite = beforeSprite;

        float t = 0f;
        float showDur = 0.6f;
        while (t < showDur)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0.8f, 1.05f, Mathf.Clamp01(t / showDur));
            rect.localScale = Vector3.one * s;
            yield return null;
        }

        var pulseGO = new GameObject("EvoPulse");
        pulseGO.transform.SetParent(canvasGO.transform, false);
        var pr = pulseGO.AddComponent<RectTransform>();
        pr.sizeDelta = new Vector2(600, 600);
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero;
        var pulseImage = pulseGO.AddComponent<Image>();
        pulseImage.color = Color.white;
        pulseImage.canvasRenderer.SetAlpha(0f);

        float pulseDur = 0.25f;
        t = 0f;
        while (t < pulseDur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / pulseDur));
            pulseImage.canvasRenderer.SetAlpha(a);
            yield return null;
        }

        image.sprite = afterSprite ?? image.sprite;

        t = 0f;
        while (t < pulseDur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / pulseDur));
            pulseImage.canvasRenderer.SetAlpha(a);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.6f);
        Object.Destroy(canvasGO);
    }
}
