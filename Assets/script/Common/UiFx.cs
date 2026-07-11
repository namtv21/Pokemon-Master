using System.Collections;
using UnityEngine;

/// <summary>
/// Hiệu ứng UI dùng chung, không phụ thuộc DOTween (convention của project: coroutine lerp).
/// Gọi UiFx.PopIn(panel) NGAY SAU khi SetActive(true) — panel nảy nhẹ (scale 0.94→1 + fade 0→1).
/// An toàn: tự thêm CanvasGroup nếu thiếu, gọi lặp thì restart, kết thúc luôn về đúng trạng thái chuẩn.
/// </summary>
public static class UiFx
{
    public static void PopIn(GameObject panel, float duration = 0.12f)
    {
        if (panel == null || !panel.activeInHierarchy)
            return;

        var runner = panel.GetComponent<UiFxRunner>();
        if (runner == null)
            runner = panel.AddComponent<UiFxRunner>();

        runner.PlayPopIn(duration);
    }

    /// Runner ẩn gắn trên panel để chạy coroutine (panel tắt thì coroutine tự dừng — an toàn).
    [DisallowMultipleComponent]
    public class UiFxRunner : MonoBehaviour
    {
        private Coroutine current;

        public void PlayPopIn(float duration)
        {
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(PopInRoutine(duration));
        }

        private IEnumerator PopInRoutine(float duration)
        {
            var rect = transform as RectTransform;
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();

            Vector3 baseScale = Vector3.one;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;   // menu vẫn mượt kể cả khi timescale đổi
                float p = Mathf.Clamp01(t / duration);
                float ease = 1f - (1f - p) * (1f - p);   // ease-out
                if (rect != null) rect.localScale = Vector3.Lerp(baseScale * 0.94f, baseScale, ease);
                group.alpha = ease;
                yield return null;
            }

            if (rect != null) rect.localScale = baseScale;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            current = null;
        }

        // Panel bị tắt giữa chừng → trả trạng thái chuẩn để lần mở sau không kẹt alpha/scale.
        private void OnDisable()
        {
            var rect = transform as RectTransform;
            if (rect != null) rect.localScale = Vector3.one;
            var group = GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
            current = null;
        }
    }
}
