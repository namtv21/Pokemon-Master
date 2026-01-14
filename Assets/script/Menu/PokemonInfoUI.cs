using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PokemonInfoUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Transform movesParent;
    [SerializeField] private MoveSelectionUI moveSlotPrefab;
    [SerializeField] private Image type1Icon;
    [SerializeField] private Image type2Icon;
    [SerializeField] private TypeIconDatabase typeIcons;
    [SerializeField] private HpBar hpBar;
    [SerializeField] private ExpBar expBar;

    public void Show(Pokemon pokemon)
    {
        gameObject.SetActive(true);

        if (iconImage != null) 
        { 
            iconImage.sprite = pokemon.Base.FrontSprite;
            iconImage.gameObject.SetActive(true); 
        }
        nameText.text = pokemon.Base.Name;
        levelText.text = $"Lv {pokemon.Level}";
        
        type1Icon.sprite = typeIcons.GetIcon(pokemon.Base.Type1);
        type1Icon.gameObject.SetActive(pokemon.Base.Type1 != PokemonType.None);

        type2Icon.sprite = typeIcons.GetIcon(pokemon.Base.Type2);
        type2Icon.gameObject.SetActive(pokemon.Base.Type2 != PokemonType.None);

        float normalizedExp = (float)pokemon.Exp / pokemon.ExpToNextLevel;
        expBar.SetExpFraction(normalizedExp);
        expBar.SetExpNumbers(pokemon.Exp, pokemon.ExpToNextLevel);

        float fraction = (float)pokemon.CurrentHp / pokemon.MaxHp;
        hpBar.SetHpFraction(fraction);
        hpBar.SetHpNumbers(pokemon.CurrentHp, pokemon.MaxHp);

        if (pokemon.IsFainted)
        {
            statusText.text = "Fainted";
            statusText.color = Color.red;
            return;
        }

        if (pokemon.Status == StatusEffect.None)
        {
            statusText.text = "";
            statusText.color = Color.white;
        }
        else
        {
            statusText.text = pokemon.Status.ToString();
            statusText.color = Color.black;
        }

        statsText.text =
            $"Atk: {pokemon.Attack}\n" +
            $"Def: {pokemon.Defense}\n" +
            $"SpAtk: {pokemon.SpAttack}\n" +
            $"SpDef: {pokemon.SpDefense}\n" +
            $"Speed: {pokemon.Speed}";

        foreach (Transform child in movesParent)
            Destroy(child.gameObject);

        foreach (var move in pokemon.Moves)
        {
            var slot = Instantiate(moveSlotPrefab, movesParent);
            slot.SetMoveData(move);
            slot.SetHighlight(false);
        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
