using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] private HpBar hpBar;          // tham chiếu tới HpBar
    [SerializeField] private TMP_Text nameText;    // tên Pokémon
    [SerializeField] private TMP_Text levelText;   // level Pokémon
    [SerializeField] private TMP_Text statusText;  // trạng thái hiện tại
    [SerializeField] private Image type1Icon;
    [SerializeField] private Image type2Icon;

    [SerializeField] private TypeIconDatabase typeIcons;

    private Pokemon pokemon;
    // Khởi tạo HUD với dữ liệu Pokémon
    public void SetData(Pokemon pokemon)
    {
        this.pokemon = pokemon;
        nameText.text = pokemon.Base.Name;
        levelText.text = "Lv " + pokemon.Level;

        hpBar.SetHpFraction((float)pokemon.CurrentHp / pokemon.MaxHp);
        hpBar.SetHpNumbers(pokemon.CurrentHp, pokemon.MaxHp);

        if (pokemon.Status == StatusEffect.None)
            statusText.text = "";
        else statusText.text = pokemon.Status.ToString();

        type1Icon.sprite = typeIcons.GetIcon(pokemon.Base.Type1);
        type1Icon.gameObject.SetActive(pokemon.Base.Type1 != PokemonType.None);

        type2Icon.sprite = typeIcons.GetIcon(pokemon.Base.Type2);
        type2Icon.gameObject.SetActive(pokemon.Base.Type2 != PokemonType.None);
    }

    // Cập nhật HP ngay lập tức
    public void SetHP(int currentHP, int maxHP)
    {
        float fraction = (float)currentHP / maxHP;
        hpBar.SetHpFraction(fraction);
        hpBar.SetHpNumbers(currentHP, maxHP);
    }

    // Cập nhật HP với hiệu ứng giảm máu mượt
    public void UpdateHP()
    {
        float targetFraction = (float)pokemon.CurrentHp / pokemon.MaxHp;
        StartCoroutine(hpBar.SmoothHpChange(targetFraction, pokemon.CurrentHp, pokemon.MaxHp));
    }

    // Cập nhật trạng thái
    public void SetStatus(StatusEffect status)
    {
        if (status == StatusEffect.None)
        {
            statusText.text = "";
            statusText.color = Color.white;
        }
        else
        {
            statusText.text = status.ToString();
            statusText.color = Color.black;
        }
    }
}