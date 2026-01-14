using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TypeIconDatabase", menuName = "Pokemon/Type Icon Database")]
public class TypeIconDatabase : ScriptableObject
{
    [System.Serializable]
    public class TypeIconEntry
    {
        public PokemonType type;
        public Sprite icon;
    }

    public List<TypeIconEntry> icons;

    private Dictionary<PokemonType, Sprite> iconDict;

    public void Init()
    {
        iconDict = new Dictionary<PokemonType, Sprite>();
        foreach (var entry in icons)
        {
            if (!iconDict.ContainsKey(entry.type))
                iconDict.Add(entry.type, entry.icon);
        }
    }

    public Sprite GetIcon(PokemonType type)
    {
        if (iconDict == null) Init();
        return iconDict.ContainsKey(type) ? iconDict[type] : null;
    }
}