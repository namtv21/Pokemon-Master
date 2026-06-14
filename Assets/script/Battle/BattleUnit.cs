using System.Collections;
using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    [Header("Tham chiếu UI")]
    [SerializeField] private BattleHud hud;
    public BattleHud Hud => hud;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Cấu hình")]
    [SerializeField] private bool isPlayerUnit;

    [Header("Animation")]
    [SerializeField] private float enterOffsetX = 8f;
    [SerializeField] private float enterDuration = 0.4f;
    [SerializeField] private float attackLungeX = 1.5f;
    [SerializeField] private float attackDuration = 0.15f;

    public Pokemon Pokemon { get; private set; }

    private Vector3 originalLocalPosition;

    public void Setup(PokemonBase baseData, int level)
    {
        Pokemon = new Pokemon(baseData, level);
        ApplyPokemonVisuals();
    }

    public void Setup(Pokemon pokemon)
    {
        Pokemon = pokemon;
        ApplyPokemonVisuals();
    }

    private void ApplyPokemonVisuals()
    {
        if (Pokemon == null || Pokemon.Base == null)
        {
            Debug.LogError("BattleUnit: Pokemon or Base is null");
            return;
        }
        if (spriteRenderer == null)
        {
            Debug.LogError("BattleUnit: spriteRenderer is null");
            return;
        }
        var sprite = isPlayerUnit ? Pokemon.Base.BackSprite : Pokemon.Base.FrontSprite;
        if (sprite == null)
        {
            Debug.LogError($"BattleUnit: Missing sprite on {(isPlayerUnit ? "BackSprite" : "FrontSprite")} for {Pokemon.Base.Name}");
            return;
        }
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.enabled = false; // ẩn cho đến khi PlayEnterAnimation
        spriteRenderer.transform.localPosition = Vector3.zero;
        originalLocalPosition = Vector3.zero;

        if (hud == null)
        {
            Debug.LogError("BattleUnit: hud is null");
            return;
        }
        hud.SetData(Pokemon);
    }

    public void UpdateHud() => hud.SetData(Pokemon);

    public void UpdateHp() => hud.SetHP(Pokemon.CurrentHp, Pokemon.MaxHp);

    public void SetSprite(Sprite newSprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = newSprite;
    }

    // ─── Animations ──────────────────────────────────────────────────────────

    // Trượt vào từ ngoài màn hình
    public IEnumerator PlayEnterAnimation()
    {
        float offset = isPlayerUnit ? -enterOffsetX : enterOffsetX;
        Vector3 startPos = originalLocalPosition + new Vector3(offset, 0f, 0f);
        spriteRenderer.transform.localPosition = startPos;
        spriteRenderer.enabled = true;

        float t = 0f;
        while (t < enterDuration)
        {
            t += Time.deltaTime;
            spriteRenderer.transform.localPosition = Vector3.Lerp(startPos, originalLocalPosition, Mathf.Clamp01(t / enterDuration));
            yield return null;
        }
        spriteRenderer.transform.localPosition = originalLocalPosition;
    }

    // Lao về phía đối thủ rồi rút về
    public IEnumerator PlayAttackAnimation()
    {
        float dir = isPlayerUnit ? attackLungeX : -attackLungeX;
        Vector3 lungePos = originalLocalPosition + new Vector3(dir, 0f, 0f);

        float t = 0f;
        while (t < attackDuration)
        {
            t += Time.deltaTime;
            spriteRenderer.transform.localPosition = Vector3.Lerp(originalLocalPosition, lungePos, Mathf.Clamp01(t / attackDuration));
            yield return null;
        }
        t = 0f;
        while (t < attackDuration)
        {
            t += Time.deltaTime;
            spriteRenderer.transform.localPosition = Vector3.Lerp(lungePos, originalLocalPosition, Mathf.Clamp01(t / attackDuration));
            yield return null;
        }
        spriteRenderer.transform.localPosition = originalLocalPosition;
    }

    // Nhấp nháy khi nhận sát thương
    public IEnumerator PlayHitAnimation()
    {
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(0.06f);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(0.06f);
        }
    }

    // Rung khi đòn không hiệu quả / trượt
    public IEnumerator PlayNoDamageAnimation()
    {
        float[] offsets = { 0.2f, -0.2f, 0.2f, -0.2f, 0f };
        foreach (float offset in offsets)
        {
            spriteRenderer.transform.localPosition = originalLocalPosition + new Vector3(offset, 0f, 0f);
            yield return new WaitForSeconds(0.05f);
        }
        spriteRenderer.transform.localPosition = originalLocalPosition;
    }

    // Rơi xuống và mờ dần khi faint
    public IEnumerator PlayFaintAnimation()
    {
        float duration = 0.5f;
        float t = 0f;
        Vector3 startPos = spriteRenderer.transform.localPosition;
        Color startColor = spriteRenderer.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            spriteRenderer.transform.localPosition = Vector3.Lerp(startPos, startPos + Vector3.down * 2f, p);
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, p);
            spriteRenderer.color = c;
            yield return null;
        }
    }
}
