using UnityEngine;
using TMPro;

public class StorageSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    private Pokemon pokemon;

    public void SetData(Pokemon p)
    {
        pokemon = p;
        nameText.text = p.Base.Name;
        levelText.text = $"Lv {p.Level}";
    }

    public void SetHighlight(bool active)
    {
        nameText.color = active ? Color.yellow : Color.white;
        levelText.color = active ? Color.yellow : Color.white;
    }
}
