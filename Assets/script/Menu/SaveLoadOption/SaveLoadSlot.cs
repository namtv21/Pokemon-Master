using UnityEngine;
using TMPro;
using System.IO;

public class SaveLoadSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text slotText;

    private string slotName;

    public void SetData(string slotName, SaveData data, string path)
    {
        this.slotName = slotName;

        if (data != null && data.partyPokemons.Count > 0)
        {
            var firstPokemon = data.partyPokemons[0];
            var lastWrite = File.GetLastWriteTime(path);
            slotText.text = $"{slotName}: {firstPokemon.name} Lv.{firstPokemon.level} ({lastWrite})";
        }
        else
        {
            slotText.text = $"{slotName}: Empty";
        }
    }

    public string GetSlotName() => slotName;
}
