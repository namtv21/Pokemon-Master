using UnityEngine;

/// <summary>
/// Hiển thị vòng tròn blinking để đánh dấu story trigger
/// </summary>
public class StoryTriggerVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float blinkSpeed = 1.5f;
    [SerializeField] private Color triggerColor = new Color(1f, 0.84f, 0f, 0.6f); // Vàng nhẹ
    [SerializeField] private float fadeOutDuration = 0.5f;

    private float elapsedTime = 0f;
    private bool isTriggered = false;
    private Coroutine fadeCoroutine;
    private MainStoryTrigger parentTrigger;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning("[StoryTriggerVisual] SpriteRenderer not found!");
            enabled = false;
            return;
        }

        // Đảm bảo có sprite (mặc định là tròn trắng)
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateDefaultCircle();
        }

        // Determine whether this visual should be hidden because the trigger was already triggered
        bool shouldHide = false;
        parentTrigger = GetComponentInParent<MainStoryTrigger>();
        if (parentTrigger != null)
        {
            string tid = parentTrigger.TriggerId;
            if (parentTrigger.HasTriggered)
                shouldHide = true;
            else
            {
                // Check pending load data
                try
                {
                    var pending = SaveLoadSystem.pendingLoadData;
                    if (pending != null &&
                        pending.triggeredTriggers != null &&
                        !string.IsNullOrWhiteSpace(tid) &&
                        pending.triggeredTriggers.Contains(tid) &&
                        (MainStoryDirector.Instance == null || (!MainStoryDirector.Instance.IsCurrentStep(tid) && !MainStoryDirector.Instance.CanTrigger(tid))))
                        shouldHide = true;
                }
                catch { }

                // Check runtime-registered triggers
                if (!shouldHide &&
                    !string.IsNullOrWhiteSpace(tid) &&
                    SaveLoadSystem.IsRuntimeTriggered(tid) &&
                    (MainStoryDirector.Instance == null || (!MainStoryDirector.Instance.IsCurrentStep(tid) && !MainStoryDirector.Instance.CanTrigger(tid))))
                    shouldHide = true;
            }
        }

        // Ensure renderer enabled
        spriteRenderer.enabled = true;
        if (shouldHide)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
            isTriggered = true;
        }
        else
        {
            // Initial visible color
            Color baseColor = triggerColor;
            baseColor.a = triggerColor.a;
            spriteRenderer.color = baseColor;
        }
    }

    /// Immediately hide visual without running fade coroutine (used for applying save/load state)
    public void ForceHideInstant()
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // Hide visual by making it fully transparent but keep renderer enabled
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }

        isTriggered = true;
    }

    private void Update()
    {
        if (isTriggered)
            return;

        RefreshAvailability();

        if (spriteRenderer == null || !spriteRenderer.enabled)
            return;

        // Blink effect: sin wave
        elapsedTime += Time.deltaTime * blinkSpeed;
        float alpha = Mathf.Abs(Mathf.Sin(elapsedTime)) * triggerColor.a;

        Color newColor = spriteRenderer.color;
        newColor.a = alpha;
        spriteRenderer.color = newColor;
    }

    private void RefreshAvailability()
    {
        if (spriteRenderer == null || parentTrigger == null)
            return;

        bool canShow = parentTrigger.CanTriggerNow();
        if (!canShow)
        {
            var hidden = spriteRenderer.color;
            hidden.a = 0f;
            spriteRenderer.color = hidden;
            return;
        }

        if (spriteRenderer.color.a <= 0f)
        {
            var visible = triggerColor;
            visible.a = triggerColor.a;
            spriteRenderer.color = visible;
        }
    }

    /// <summary>
    /// Gọi khi trigger được kích hoạt - tắt vòng tròn
    /// </summary>
    public void Deactivate()
    {
        if (isTriggered)
            return;

        isTriggered = true;

        // Stop any existing fade coroutine
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Fade out và ẩn
        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    private System.Collections.IEnumerator FadeOutRoutine()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError($"[StoryTriggerVisual] SpriteRenderer is null!");
            gameObject.SetActive(false);
            yield break;
        }

        float fadeTime = 0f;
        Color originalColor = spriteRenderer.color;

        while (fadeTime < fadeOutDuration)
        {
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"[StoryTriggerVisual] SpriteRenderer became null mid-fade");
                yield break;
            }

            fadeTime += Time.deltaTime;
            float t = fadeTime / fadeOutDuration;

            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(originalColor.a, 0f, t);
            spriteRenderer.color = newColor;

            yield return null;
        }

        // After fade, keep renderer enabled but fully transparent
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }
    }

    /// <summary>
    /// Ensure the visual is visible again (used when clearing saved-trigger state)
    /// </summary>
    public void EnsureVisible()
    {
        isTriggered = false;
        elapsedTime = 0f;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color c = spriteRenderer.color;
            c.a = triggerColor.a;
            spriteRenderer.color = c;
        }
    }

    /// Tạo default sprite hình tròn (nếu cần)
    private Sprite CreateDefaultCircle()
    {
        // Nếu có sẵn sprite trong Resources, load ra
        Sprite circleSprite = Resources.Load<Sprite>("Sprites/Circle");
        if (circleSprite != null)
            return circleSprite;

        Debug.LogWarning("[StoryTriggerVisual] Sprite not found. Please assign a circle sprite to SpriteRenderer manually.");
        return null;
    }
}
