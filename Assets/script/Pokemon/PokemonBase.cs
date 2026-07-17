using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LearnableMove
{
    public MoveBase move;
    public int level; // cấp độ học chiêu
}

[System.Serializable]
public class EvolutionOption
{
    [SerializeField] private PokemonBase evolvesTo;
    [SerializeField] private int evolutionLevel = 0;
    [SerializeField] private string label;

    public EvolutionOption() { }

    public EvolutionOption(PokemonBase target, int level, string optionLabel = "")
    {
        evolvesTo = target;
        evolutionLevel = level;
        label = optionLabel;
    }

    public PokemonBase EvolvesTo => evolvesTo;
    public int EvolutionLevel => evolutionLevel;
    public string Label => string.IsNullOrWhiteSpace(label)
        ? (evolvesTo != null ? evolvesTo.Name : string.Empty)
        : label;
}


[CreateAssetMenu(fileName = "Pokemon", menuName = "Pokemon/New Pokemon")]
public class PokemonBase : ScriptableObject
{
    [SerializeField] int num;
    [SerializeField] string pokemonName;

    [TextArea]
    [SerializeField] string description;
    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;
    [SerializeField] PokemonType type1;
    [SerializeField] PokemonType type2;
    public int MaxHp;
    public int Attack;
    public int Defense;
    public int SpAttack;
    public int SpDefense;
    public int Speed;
    [Header("Evolution")]
    [SerializeField] private bool evolvable;
    [SerializeField] private int evolutionLevel = 0;
    [SerializeField] private PokemonBase evolvesTo;
    [SerializeField] private List<EvolutionOption> evolutionOptions = new List<EvolutionOption>();

    [Header("Personality (AI companion — không ảnh hưởng battle)")]
    [Tooltip("Bật để loài này THIÊN về một tính cách. Tắt = hoàn toàn ngẫu nhiên.")]
    [SerializeField] private bool biasPersonality = false;
    [Tooltip("Tính cách ưu tiên (vd Snorlax → Lazy, huyền thoại → Proud).")]
    [SerializeField] private PokemonPersonality preferredPersonality = PokemonPersonality.Playful;
    [Range(0, 100)]
    [Tooltip("Xác suất (%) cá thể nhận đúng tính cách ưu tiên; còn lại ngẫu nhiên. 100 = luôn (huyền thoại).")]
    [SerializeField] private int preferredPersonalityChance = 70;
    public int Num => num;
    public string Name => pokemonName;
    public Sprite FrontSprite => frontSprite;
    public Sprite BackSprite => backSprite;
    //public string Description => description;
    public PokemonType Type1 => type1;
    public PokemonType Type2 => type2;
    public bool Evolvable => evolvable;
    public int EvolutionLevel => evolutionLevel;
    public PokemonBase EvolvesTo => evolvesTo;
    public IReadOnlyList<EvolutionOption> EvolutionOptions => evolutionOptions;

    public List<EvolutionOption> GetValidEvolutionOptions()
    {
        var result = new List<EvolutionOption>();

        if (evolutionOptions != null)
        {
            foreach (var option in evolutionOptions)
            {
                if (option != null && option.EvolvesTo != null && option.EvolutionLevel > 0)
                    result.Add(option);
            }
        }

        if (result.Count == 0 && evolvable && evolvesTo != null && evolutionLevel > 0)
            result.Add(new EvolutionOption(evolvesTo, evolutionLevel));

        return result;
    }

    /// Chọn tính cách cho một cá thể MỚI: nếu bật bias thì có preferredPersonalityChance%
    /// ra tính cách ưu tiên, còn lại ngẫu nhiên → giữ sắc thái loài mà không mất đa dạng.
    public PokemonPersonality RollPersonality()
    {
        if (biasPersonality && Random.Range(0, 100) < preferredPersonalityChance)
            return preferredPersonality;
        return PokemonPersonalityUtil.RandomPersonality();
    }
    public void LoadFromJson(PokemonJson data)
    {
        num = data.num;
        pokemonName = data.name;
        type1 = ParseType(data.types[0]);
        type2 = data.types.Length > 1 ? ParseType(data.types[1]) : PokemonType.None;

        MaxHp = data.baseStats.hp;
        Attack = data.baseStats.atk;
        Defense = data.baseStats.def;
        SpAttack = data.baseStats.spa;
        SpDefense = data.baseStats.spd;
        Speed = data.baseStats.spe;

        string spriteName = GetSpriteResourceName(pokemonName);
        frontSprite = Resources.Load<Sprite>($"Sprites/Front/{spriteName}");
        backSprite  = Resources.Load<Sprite>($"Sprites/Back/{spriteName}");

        if (frontSprite == null || backSprite == null)
            Debug.LogError($"[PokemonImporter] Missing battle sprite for '{pokemonName}' (resource name '{spriteName}').");
    }

    private static string GetSpriteResourceName(string displayName)
    {
        string normalized = System.Text.RegularExpressions.Regex
            .Replace(displayName ?? string.Empty, @"[^a-zA-Z0-9]", string.Empty)
            .ToUpperInvariant();

        // The source sprite pack encodes the Nidoran gender symbols as FE/MA.
        return normalized switch
        {
            "NIDORANF" => "NIDORANfE",
            "NIDORANM" => "NIDORANmA",
            _ => normalized
        };
    }

    private PokemonType ParseType(string type)
    {
        return (PokemonType)System.Enum.Parse(typeof(PokemonType), type, true);
    }
    [Header("Encounter")]
    [SerializeField] private string[] encounterLocations = new string[0];
    public string[] EncounterLocations => encounterLocations;

    [SerializeField] LearnableMove[] learnableMoves;
    public LearnableMove[] LearnableMoves => learnableMoves;
    public static int GetExpForLevel(int level)
    {
        // Công thức kinh nghiệm bậc trung bình
        return Mathf.FloorToInt(Mathf.Pow(level, 2));
    }
}

public enum PokemonType
{
    None,
    Normal,
    Fire,
    Water,
    Electric,
    Grass,
    Ice,
    Fighting,
    Poison,
    Ground,
    Flying,
    Psychic,
    Bug,
    Rock,
    Ghost,
    Dragon,
    Dark,
    Steel,
    Fairy,
}
