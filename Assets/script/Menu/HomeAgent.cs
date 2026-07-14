using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một Pokemon sống trong Nhà: đi lang thang trong RoomArea, né nội thất,
/// nhún (bob) khi di chuyển, nhịp sinh hoạt theo TÍNH CÁCH.
/// Hỗ trợ tương tác: bong bóng thoại/emote, nhảy (hop), highlight khi được chọn.
/// Được PokemonHomeUI sinh ra lúc mở nhà — không tự tồn tại.
/// </summary>
public class HomeAgent : MonoBehaviour
{
    private const float HopDuration = 0.35f;
    private const int AgentSortingOrder = 20;

    public Pokemon Pokemon { get; private set; }
    public Vector2 AnchoredPosition => rt != null ? rt.anchoredPosition : Vector2.zero;

    private PokemonHomeUI home;
    private RectTransform rt;
    private RectTransform spriteRt;   // con — chỉ để bob/hop, không ảnh hưởng vị trí logic
    private Image image;

    // Bong bóng thoại (tạo lười)
    private CanvasGroup bubbleGroup;
    private TextMeshProUGUI bubbleText;
    private Coroutine bubbleRoutine;

    private Vector2 target;
    private bool moving;
    private float restUntil;
    private float bobPhase;
    private float hopUntil;
    private float nextZzzTime;

    // Tham số theo tính cách
    private float speed;
    private Vector2 restRange;
    private float maxWanderDistance;

    public void Init(PokemonHomeUI owner, Pokemon pokemon, Vector2 startPos, float agentSize)
    {
        home = owner;
        Pokemon = pokemon;

        // UI draw order is controlled by canvases, not Physics layers.
        var agentCanvas = GetComponent<Canvas>();
        if (agentCanvas == null)
            agentCanvas = gameObject.AddComponent<Canvas>();
        agentCanvas.overrideSorting = true;
        agentCanvas.sortingOrder = AgentSortingOrder;
        transform.SetAsLastSibling();

        rt = transform as RectTransform;
        if (rt == null)
            rt = gameObject.AddComponent<RectTransform>();   // phòng khi bị tạo không kèm RectTransform
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(agentSize, agentSize);
        rt.anchoredPosition = startPos;

        var spriteGo = new GameObject("Sprite");
        spriteGo.transform.SetParent(transform, false);
        spriteRt = spriteGo.AddComponent<RectTransform>();
        spriteRt.anchorMin = Vector2.zero;
        spriteRt.anchorMax = Vector2.one;
        spriteRt.offsetMin = spriteRt.offsetMax = Vector2.zero;

        image = spriteGo.AddComponent<Image>();
        image.sprite = pokemon.Base != null ? pokemon.Base.FrontSprite : null;
        image.preserveAspect = true;
        image.raycastTarget = false;

        ApplyPersonalityParams(pokemon.Personality);
        bobPhase = Random.value * Mathf.PI * 2f;
        restUntil = Time.time + Random.Range(0.3f, restRange.y);   // khởi động lệch nhịp nhau
        nextZzzTime = Time.time + Random.Range(3f, 8f);
    }

    // Nhịp sống mỗi tính cách một kiểu — đây là chỗ hệ tính cách "nhìn thấy được".
    private void ApplyPersonalityParams(PokemonPersonality p)
    {
        switch (p)
        {
            case PokemonPersonality.Playful: speed = 170f; restRange = new Vector2(0.3f, 1.2f); maxWanderDistance = 9999f; break;
            case PokemonPersonality.Brave:   speed = 120f; restRange = new Vector2(1f, 2.5f);   maxWanderDistance = 9999f; break;
            case PokemonPersonality.Curious: speed = 110f; restRange = new Vector2(1f, 2f);     maxWanderDistance = 9999f; break;
            case PokemonPersonality.Timid:   speed = 90f;  restRange = new Vector2(2f, 4f);     maxWanderDistance = 350f;  break;
            case PokemonPersonality.Gentle:  speed = 70f;  restRange = new Vector2(2f, 5f);     maxWanderDistance = 400f;  break;
            case PokemonPersonality.Proud:   speed = 70f;  restRange = new Vector2(3f, 6f);     maxWanderDistance = 300f;  break;
            case PokemonPersonality.Lazy:    speed = 45f;  restRange = new Vector2(4f, 9f);     maxWanderDistance = 150f;  break;
            default:                         speed = 100f; restRange = new Vector2(1.5f, 3f);   maxWanderDistance = 9999f; break;
        }
    }

    private void Update()
    {
        if (home == null || spriteRt == null || rt == null)   // chưa Init xong thì đứng yên
            return;

        if (!moving)
        {
            // Đứng yên: chỉ còn hiệu ứng hop (nếu vừa được vuốt ve) + Lazy ngáy
            float hopOffset = 0f;
            if (Time.time < hopUntil)
            {
                float t = 1f - (hopUntil - Time.time) / HopDuration;
                hopOffset = Mathf.Sin(t * Mathf.PI) * 16f;
            }
            spriteRt.anchoredPosition = new Vector2(0f, hopOffset);

            if (Pokemon != null && Pokemon.Personality == PokemonPersonality.Lazy && Time.time >= nextZzzTime)
            {
                nextZzzTime = Time.time + Random.Range(6f, 11f);
                Emote("zZ");
            }

            if (Time.time >= restUntil)
                TryStartWandering();
            return;
        }

        Vector2 pos = rt.anchoredPosition;
        Vector2 next = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);
        rt.anchoredPosition = next;

        // Bob khi di chuyển + quay mặt theo hướng đi
        bobPhase += Time.deltaTime * 10f;
        spriteRt.anchoredPosition = new Vector2(0f, Mathf.Abs(Mathf.Sin(bobPhase)) * 6f);
        float dx = target.x - pos.x;
        if (Mathf.Abs(dx) > 1f)
            spriteRt.localScale = new Vector3(dx < 0f ? 1f : -1f, 1f, 1f);   // FrontSprite mặc định nhìn trái

        if ((next - target).sqrMagnitude < 4f)
        {
            moving = false;
            restUntil = Time.time + Random.Range(restRange.x, restRange.y);
        }
    }

    private void TryStartWandering()
    {
        // Proud thích khu trung tâm; các tính cách khác đi tự do trong tầm của mình
        bool preferCenter = Pokemon != null && Pokemon.Personality == PokemonPersonality.Proud;
        if (home.TryGetWanderTarget(rt.anchoredPosition, maxWanderDistance, preferCenter, out target))
            moving = true;
        else
            restUntil = Time.time + 1f;   // phòng chật/kẹt — thử lại sau
    }

    // ===== Tương tác =====

    /// Được chọn (từ list bên phải): phóng to nhẹ + sáng màu.
    public void SetHighlighted(bool on)
    {
        if (rt != null)
            rt.localScale = on ? Vector3.one * 1.12f : Vector3.one;
        if (image != null)
            image.color = on ? new Color(1f, 1f, 0.85f) : Color.white;
    }

    /// Nhảy một nhịp (phản ứng khi được vuốt ve).
    public void Hop() => hopUntil = Time.time + HopDuration;

    /// Bong bóng thoại nhỏ trên đầu.
    public void Say(string text, float duration = 2.4f, float fontSize = 19f)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        EnsureBubble();
        bubbleText.text = text;
        bubbleText.fontSize = fontSize;

        if (bubbleRoutine != null) StopCoroutine(bubbleRoutine);
        bubbleRoutine = StartCoroutine(BubbleRoutine(duration));
    }

    /// Emote ngắn (ký hiệu to): "~", "!", "?", "zZ", "<3"...
    public void Emote(string symbol) => Say(symbol, 1.3f, 30f);

    private IEnumerator BubbleRoutine(float duration)
    {
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            bubbleGroup.alpha = t / 0.12f;
            yield return null;
        }
        bubbleGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            bubbleGroup.alpha = 1f - t / 0.25f;
            yield return null;
        }
        bubbleGroup.alpha = 0f;
        bubbleRoutine = null;
    }

    private void EnsureBubble()
    {
        if (bubbleGroup != null)
            return;

        var go = new GameObject("Bubble");
        go.transform.SetParent(transform, false);
        var bubbleRt = go.AddComponent<RectTransform>();
        bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 1f);
        bubbleRt.pivot = new Vector2(0.5f, 0f);
        bubbleRt.anchoredPosition = new Vector2(0f, 8f);
        bubbleRt.sizeDelta = new Vector2(170f, 46f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);
        bg.raycastTarget = false;

        bubbleGroup = go.AddComponent<CanvasGroup>();
        bubbleGroup.alpha = 0f;
        bubbleGroup.interactable = false;
        bubbleGroup.blocksRaycasts = false;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(6f, 3f);
        textRt.offsetMax = new Vector2(-6f, -3f);

        bubbleText = textGo.AddComponent<TextMeshProUGUI>();
        bubbleText.font = TMP_Settings.defaultFontAsset;
        bubbleText.alignment = TextAlignmentOptions.Center;
        bubbleText.color = Color.white;
        bubbleText.enableWordWrapping = true;
        bubbleText.overflowMode = TextOverflowModes.Ellipsis;
        bubbleText.raycastTarget = false;
    }
}
