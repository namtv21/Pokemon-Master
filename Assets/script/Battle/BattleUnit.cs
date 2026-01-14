using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    [Header("Tham chiếu UI")]
    [SerializeField] private BattleHud hud;
    public BattleHud Hud => hud;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Cấu hình")]
    [SerializeField] private bool isPlayerUnit;

    public Pokemon Pokemon { get; private set; }

    /// Setup bằng cách tạo mới Pokemon từ Base + Level
    public void Setup(PokemonBase baseData, int level)
    {
        Pokemon = new Pokemon(baseData, level);
        ApplyPokemonVisuals();
    }

    /// Setup bằng cách nhận thẳng một Pokemon đã có (ví dụ từ PlayerParty)
    public void Setup(Pokemon pokemon)
    {
        Pokemon = pokemon;
        ApplyPokemonVisuals();
    }

    /// Cập nhật sprite hiển thị theo unit (player hay enemy)
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
        spriteRenderer.transform.localPosition = Vector3.zero;

        if (hud == null)
        {
            Debug.LogError("BattleUnit: hud is null");
            return;
        }
        hud.SetData(Pokemon);
    }

    /// Cập nhật toàn bộ HUD (HP, status, level…)
    public void UpdateHud()
    {
        hud.SetData(Pokemon);
    }
    
    /// Chỉ cập nhật thanh HP
    public void UpdateHp()
    {
        hud.SetHP(Pokemon.CurrentHp, Pokemon.MaxHp);
    }
    /// Thay đổi sprite để hiển thị pokeball khi bắt pokemon
    public void SetSprite(Sprite newSprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = newSprite;
    }

}