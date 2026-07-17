using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleTransition : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private Image cellPrefab;
    [SerializeField] private int rows = 10;
    [SerializeField] private int cols = 18;
    [SerializeField] private float ringDelay = 0.03f;
    [SerializeField] private float cellFadeTime = 0.08f;
    [SerializeField] private bool useStartFlash = true;
    [SerializeField] private float startFlashDuration = 0.08f;
    [SerializeField] private float startFlashMaxAlpha = 0.35f;

    private readonly List<Image> cells = new();
    private bool built;
    private Image flashOverlay;

    private void BuildGridIfNeeded()
    {
        if (built) return;
        built = true;

        if (container == null || cellPrefab == null)
        {
            Debug.LogError("[BattleTransition] Missing container or cellPrefab.");
            enabled = false;
            return;
        }

        if (rows <= 0 || cols <= 0)
        {
            Debug.LogError("[BattleTransition] rows and cols must be greater than zero.");
            enabled = false;
            return;
        }

        Canvas.ForceUpdateCanvases();

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        cells.Clear();

        var width = Mathf.Max(1f, container.rect.width);
        var height = Mathf.Max(1f, container.rect.height);
        var cellW = width / cols;
        var cellH = height / rows;

        if (cellW <= 0.01f || cellH <= 0.01f)
        {
            Debug.LogWarning($"[BattleTransition] Container size is too small for grid: {width}x{height}. Check that Container is full-screen and under a Canvas.");
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var c = Instantiate(cellPrefab, container);
                var rt = c.rectTransform;
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                rt.sizeDelta = new Vector2(cellW + 1f, cellH + 1f);
                rt.anchoredPosition = new Vector2(x * cellW, y * cellH);
                rt.localScale = Vector3.one;

                var col = c.color; col.a = 0f; c.color = col;
                cells.Add(c);
            }
        }

        if (cells.Count != rows * cols)
            Debug.LogWarning($"[BattleTransition] Expected {rows * cols} cells but created {cells.Count}.");

        BuildFlashOverlay();
    }

    private void BuildFlashOverlay()
    {
        var flashGo = new GameObject("BattleStartFlash");
        flashGo.transform.SetParent(container, false);
        flashGo.transform.SetAsLastSibling();

        var rt = flashGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        flashOverlay = flashGo.AddComponent<Image>();
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        flashOverlay.raycastTarget = false;
    }

    public IEnumerator PlayClose()
    {
        BuildGridIfNeeded();
        gameObject.SetActive(true);

        if (cells.Count == 0)
            yield break;

        if (useStartFlash)
            yield return PlayStartFlash();

        var rings = BuildRings(out int maxRing);

        for (int r = maxRing; r >= 0; r--)
        {
            if (rings.TryGetValue(r, out var ringCells))
            {
                foreach (var c in ringCells) StartCoroutine(FadeCell(c, 1f, cellFadeTime));
            }
            yield return new WaitForSecondsRealtime(ringDelay);
        }

        yield return new WaitForSecondsRealtime(cellFadeTime);
    }

    private IEnumerator PlayStartFlash()
    {
        if (flashOverlay == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, startFlashDuration);
        float half = safeDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, startFlashMaxAlpha, Mathf.Clamp01(t / half));
            flashOverlay.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startFlashMaxAlpha, 0f, Mathf.Clamp01(t / half));
            flashOverlay.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
    }

    public IEnumerator PlayOpen()
    {
        if (!built)
            BuildGridIfNeeded();

        if (cells.Count == 0)
            yield break;

        var rings = BuildRings(out int maxRing);

        for (int r = 0; r <= maxRing; r++)
        {
            if (rings.TryGetValue(r, out var ringCells))
            {
                foreach (var c in ringCells) StartCoroutine(FadeCell(c, 0f, cellFadeTime));
            }
            yield return new WaitForSecondsRealtime(ringDelay);
        }

        yield return new WaitForSecondsRealtime(cellFadeTime);
        gameObject.SetActive(false);
    }

    private Dictionary<int, List<Image>> BuildRings(out int maxRing)
    {
        var rings = new Dictionary<int, List<Image>>();
        maxRing = 0;

        float cx = (cols - 1) * 0.5f;
        float cy = (rows - 1) * 0.5f;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int idx = y * cols + x;
                if (idx < 0 || idx >= cells.Count) continue;

                int ring = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy)));
                if (!rings.ContainsKey(ring)) rings[ring] = new List<Image>();
                rings[ring].Add(cells[idx]);

                if (ring > maxRing) maxRing = ring;
            }
        }

        return rings;
    }

    private IEnumerator FadeCell(Image img, float targetAlpha, float duration)
    {
        float t = 0f;
        float start = img.color.a;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
            var col = img.color; col.a = a; img.color = col;
            yield return null;
        }

        var final = img.color; final.a = targetAlpha; img.color = final;
    }
}
