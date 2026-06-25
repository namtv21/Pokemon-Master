using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LearnableMove
{
    public MoveBase move;
    public int level; // cấp độ học chiêu
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

        string spriteName = System.Text.RegularExpressions.Regex.Replace(pokemonName, @"[^a-zA-Z0-9]", "").ToUpper();
        frontSprite = Resources.Load<Sprite>($"Sprites/Front/{spriteName}");
        backSprite  = Resources.Load<Sprite>($"Sprites/Back/{spriteName}");
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
