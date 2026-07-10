using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PokemonInfoUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI statsLeftText;
    [SerializeField] private TextMeshProUGUI statsRightText;
    [SerializeField] private Transform movesParent;
    [SerializeField] private MoveSelectionUI moveSlotPrefab;
    [SerializeField] private Image type1Icon;
    [SerializeField] private Image type2Icon;
    [SerializeField] private TypeIconDatabase typeIcons;
    [SerializeField] private HpBar hpBar;
    [SerializeField] private ExpBar expBar;
    [SerializeField] private FriendshipBar friendshipBar;
    [SerializeField] private int moveColumns = 2;

    private GridLayoutGroup movesGrid;

    private void Awake()
    {
        if (movesParent != null)
            movesGrid = movesParent.GetComponent<GridLayoutGroup>();

        if (movesGrid == null && movesParent != null)
            movesGrid = movesParent.gameObject.AddComponent<GridLayoutGroup>();

        if (movesGrid != null)
        {
            movesGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            movesGrid.constraintCount = Mathf.Max(1, moveColumns);
        }
    }

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

        if (friendshipBar != null)
        {
            friendshipBar.SetFriendshipFraction(
                (float)pokemon.FriendshipProgress / Pokemon.BattleParticipationsPerFriendshipLevel);
            friendshipBar.SetFriendshipNumbers(
                pokemon.FriendshipLevel,
                pokemon.FriendshipProgress,
                Pokemon.BattleParticipationsPerFriendshipLevel);
        }

        float fraction = (float)pokemon.CurrentHp / pokemon.MaxHp;
        hpBar.SetHpFraction(fraction);
        hpBar.SetHpNumbers(pokemon.CurrentHp, pokemon.MaxHp);

        SetStatsText(pokemon);

        foreach (Transform child in movesParent)
            Destroy(child.gameObject);

        if (movesGrid != null)
            movesGrid.constraintCount = Mathf.Max(1, moveColumns);

        foreach (var move in pokemon.Moves)
        {
            var slot = Instantiate(moveSlotPrefab, movesParent);
            slot.SetMoveData(move);
            slot.SetHighlight(false);
        }

        if (pokemon.IsFainted)
        {
            statusText.text = "Fainted";
            statusText.color = Color.red;
        }
        else if (pokemon.Status == StatusEffect.None)
        {
            statusText.text = string.Empty;
            statusText.color = Color.white;
        }
        else
        {
            statusText.text = pokemon.Status.ToString();
            statusText.color = Color.white;
        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetStatsText(Pokemon pokemon)
    {
        if (pokemon == null)
            return;

        string leftColumn =
            $"HP: {pokemon.MaxHp}\n" +
            $"Atk: {pokemon.Attack}\n" +
            $"SpAtk: {pokemon.SpAttack}";

        string rightColumn =
            $"Spd: {pokemon.Speed}\n" +
            $"Def: {pokemon.Defense}\n" +
            $"SpDef: {pokemon.SpDefense}" ;

        if (statsLeftText != null || statsRightText != null)
        {
            if (statsLeftText != null)
                statsLeftText.text = leftColumn;

            if (statsRightText != null)
                statsRightText.text = rightColumn;

        }
    }

}
