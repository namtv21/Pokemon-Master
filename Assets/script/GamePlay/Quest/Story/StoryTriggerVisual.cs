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

        spriteRenderer.color = triggerColor;
    }

    private void Update()
    {
        if (isTriggered)
            return;

        // Blink effect: sin wave
        elapsedTime += Time.deltaTime * blinkSpeed;
        float alpha = Mathf.Abs(Mathf.Sin(elapsedTime)) * triggerColor.a;

        Color newColor = spriteRenderer.color;
        newColor.a = alpha;
        spriteRenderer.color = newColor;
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

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Tạo default sprite hình tròn (nếu cần)
    /// </summary>
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
