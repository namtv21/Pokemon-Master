using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PartySlotUI : MonoBehaviour
{
    [Header("UI Tham chiếu")]
    [SerializeField] private UnityEngine.UI.Image iconImage;   // icon Pokémon
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statusText;              // trạng thái Pokémon
    [SerializeField] private Image type1Icon;
    [SerializeField] private Image type2Icon;
    [SerializeField] private TypeIconDatabase typeIcons;

    [Header("HP & EXP Bar")]
    [SerializeField] private HpBar hpBar;   // tham chiếu tới component HpBar
    [SerializeField] private ExpBar expBar; // tham chiếu tới component ExpBar

    public Pokemon Pokemon { get; private set; }

    /// Khởi tạo dữ liệu slot khi mở PartyMenu
    public void SetData(Pokemon p)
    {
        Pokemon = p;
        if (Pokemon == null) return;

        if (nameText != null) nameText.text = p.Base.Name;
        if (levelText != null) levelText.text = "Lv." + p.Level;

        if (iconImage != null && p.Base != null && p.Base.FrontSprite != null)
            iconImage.sprite = p.Base.FrontSprite;
        
        type1Icon.sprite = typeIcons.GetIcon(p.Base.Type1);
        type1Icon.gameObject.SetActive(p.Base.Type1 != PokemonType.None);

        type2Icon.sprite = typeIcons.GetIcon(p.Base.Type2);
        type2Icon.gameObject.SetActive(p.Base.Type2 != PokemonType.None);
        UpdateHp();
        UpdateExp();
        UpdateStatus();
    }

    /// Cập nhật ExpBar ngay lập tức
    public void UpdateExp()
    {
        if (Pokemon == null || expBar == null) return;

        float normalizedExp = (float)Pokemon.Exp / Pokemon.ExpToNextLevel;
        expBar.SetExpFraction(normalizedExp);
        expBar.SetExpNumbers(Pokemon.Exp, Pokemon.ExpToNextLevel);
    }

    /// Cập nhật HP ngay lập tức
    public void UpdateHp()
    {
        if (Pokemon == null || hpBar == null) return;

        float fraction = (float)Pokemon.CurrentHp / Pokemon.MaxHp;
        hpBar.SetHpFraction(fraction);
        hpBar.SetHpNumbers(Pokemon.CurrentHp, Pokemon.MaxHp);
    }

    /// Cập nhật trạng thái hiển thị
    private void UpdateStatus()
    {
        if (statusText == null || Pokemon == null) return;

        if (Pokemon.IsFainted)
        {
            statusText.text = "Fainted";
            statusText.color = Color.red;
            return;
        }

        if (Pokemon.Status == StatusEffect.None)
        {
            statusText.text = "";
            statusText.color = Color.white;
        }
        else
        {
            statusText.text = Pokemon.Status.ToString();
            statusText.color = Color.black;
        }
    }

    public Pokemon GetPokemon() => Pokemon;
}
