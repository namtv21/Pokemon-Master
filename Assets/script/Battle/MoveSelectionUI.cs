using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class MoveSelectionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moveNameText;
    [SerializeField] private TextMeshProUGUI ppText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private UnityEngine.UI.Image typeIcon;
    [SerializeField] private TypeIconDatabase typeIcons;

    public void SetMoveData(Move move)
    {
        if (move != null )
        {
            moveNameText.text = move.Base.MoveName;
            ppText.text = $"PP {move.PP}/{move.Base.PP}";
            if (move.Base.Power > 0)
            {
                powerText.text = move.Base.Power.ToString();
            }
            else
            {
                powerText.text = "-";
            };
            if (move.Base.StatusEffect != "")
            {
                statusText.text = move.Base.StatusEffect;
            }
            else if (move.Base.StatBoosts.Count > 0)
            {
                statusText.text = "Stat Changes";
            }
            else
            {
                statusText.text = "-";
            };
            typeIcon.sprite = typeIcons.GetIcon(move.Base.Type);
            typeIcon.gameObject.SetActive(move.Base.Type != PokemonType.None);
        }
        else
        {
            moveNameText.text = "-";
            ppText.text = "";
            powerText.text = "-";
            typeIcon.gameObject.SetActive(false);
        }
    }

    // đổi màu chữ tên move
    public void SetHighlight(bool active)
    {
        moveNameText.color = active ? Color.yellow : Color.black;
    }
}