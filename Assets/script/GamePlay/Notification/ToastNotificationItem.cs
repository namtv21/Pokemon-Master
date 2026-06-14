using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToastNotificationItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private bool textOnly = true;

    [Header("Animation")]
    [SerializeField] private float fadeInTime = 0.15f;
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float fadeOutTime = 0.4f;
    [SerializeField] private float moveUpDistance = 28f;

    public event Action OnFinished;

    private Vector2 startPos;
    private Vector2 endPos;

    private TextMeshProUGUI EnsureMessageText()
    {
        if (messageText != null)
        {
            if (messageText.font == null && TMP_Settings.defaultFontAsset != null)
                messageText.font = TMP_Settings.defaultFontAsset;
            return messageText;
        }

        messageText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (messageText != null)
            return messageText;

        var go = new GameObject("ToastMessage", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(16f, 8f);
        rt.offsetMax = new Vector2(-16f, -8f);

        messageText = go.AddComponent<TextMeshProUGUI>();
        messageText.enableWordWrapping = true;
        messageText.overflowMode = TextOverflowModes.Overflow;
        messageText.alignment = TextAlignmentOptions.Midline;
        messageText.fontSize = 28;
        messageText.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
            messageText.font = TMP_Settings.defaultFontAsset;

        return messageText;
    }

    private void ApplyVisualMode()
    {
        if (!textOnly)
            return;

        var images = GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image == null)
                continue;

            image.enabled = false;
        }
    }

    public void Play(string message, Color color, float? customHoldTime = null)
    {
        EnsureMessageText();
        ApplyVisualMode();
        color.a = 1f;

        var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.enabled = true;
            messageText.text = message;
            messageText.color = color;
            if (messageText.font == null && TMP_Settings.defaultFontAsset != null)
                messageText.font = TMP_Settings.defaultFontAsset;
            messageText.transform.SetAsLastSibling();
            messageText.ForceMeshUpdate();
        }

        if (allTexts != null && allTexts.Length > 0)
        {
            foreach (var t in allTexts)
            {
                if (t == null) continue;
                t.gameObject.SetActive(true);
                t.enabled = true;
                t.text = message;
                t.color = color;
                if (t.font == null && TMP_Settings.defaultFontAsset != null)
                    t.font = TMP_Settings.defaultFontAsset;
                t.ForceMeshUpdate();
            }
        }

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
            Debug.LogWarning("ToastNotificationItem missing TextMeshProUGUI reference.");

        startPos = rectTransform.anchoredPosition;
        endPos = startPos + new Vector2(0f, moveUpDistance);

        rectTransform.SetAsLastSibling();

        if (customHoldTime.HasValue)
            holdTime = Mathf.Max(0.1f, customHoldTime.Value);

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (rectTransform != null) rectTransform.anchoredPosition = startPos;

        // Fade in
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeInTime);
            if (canvasGroup != null) canvasGroup.alpha = p;
            if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, p * 0.35f);
            yield return null;
        }

        // Hold
        float hold = 0f;
        while (hold < holdTime)
        {
            hold += Time.deltaTime;
            yield return null;
        }

        // Fade out + move
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeOutTime);
            if (canvasGroup != null) canvasGroup.alpha = 1f - p;
            if (rectTransform != null) rectTransform.anchoredPosition = Vector2.Lerp(endPos, endPos + new Vector2(0f, moveUpDistance), p);
            yield return null;
        }

        OnFinished?.Invoke();
        Destroy(gameObject);
    }
}