using UnityEngine;
using TMPro;
using System.IO;

public class SaveLoadSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text slotText;

    private string slotName;

    public void SetData(string slotName, SaveData data, string path, bool isSelected, bool isModeSelecting, bool isSaveMode)
    {
        this.slotName = slotName;
        // default to white text
        if (slotText != null)
            slotText.color = Color.white;

        var lastWrite = File.Exists(path) ? File.GetLastWriteTime(path) : System.DateTime.MinValue;
        string dateStr = lastWrite == System.DateTime.MinValue ? "(no save)" : lastWrite.ToString("yyyy-MM-dd HH:mm");

        // Line 1: include SAVE/LOAD labels and slot label with scene name and datetime
        string scenePart = data != null && !string.IsNullOrWhiteSpace(data.sceneName) ? data.sceneName : "NoScene";
        string saveLabel = isModeSelecting && isSelected && isSaveMode ? "<color=#FFFF66>SAVE</color>" : "<color=#FFFFFF>SAVE</color>";
        string loadLabel = isModeSelecting && isSelected && !isSaveMode ? "<color=#FFFF66>LOAD</color>" : "<color=#FFFFFF>LOAD</color>";
        string line1 = $"{saveLabel} {loadLabel}  {slotName} ({scenePart}, {dateStr})";

        // Prepare lines 2 and 3 with up to 3 pokemons each
        string line2 = "";
        string line3 = "";

        if (data != null && data.partyPokemons != null && data.partyPokemons.Count > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                if (i < data.partyPokemons.Count)
                {
                    var p = data.partyPokemons[i];
                    if (!string.IsNullOrWhiteSpace(line2)) line2 += "  ";
                    line2 += $"{p.name} Lv.{p.level}";
                }
            }

            for (int i = 3; i < 6; i++)
            {
                int j = i - 3;
                if (i < data.partyPokemons.Count)
                {
                    var p = data.partyPokemons[i];
                    if (!string.IsNullOrWhiteSpace(line3)) line3 += "  ";
                    line3 += $"{p.name} Lv.{p.level}";
                }
            }
        }

        if (string.IsNullOrWhiteSpace(line2)) line2 = "Empty";
        if (string.IsNullOrWhiteSpace(line3)) line3 = ""; // optional third line empty if no more

        // Compose final text
        slotText.text = string.IsNullOrWhiteSpace(line3)
            ? $"{line1}\n{line2}"
            : $"{line1}\n{line2}\n{line3}";
    }

    public void SetHighlighted(bool highlighted, Color highlightColor, Color normalColor)
    {
        if (slotText == null) return;
        slotText.color = highlighted ? highlightColor : normalColor;
    }

    public string GetSlotName() => slotName;
}
