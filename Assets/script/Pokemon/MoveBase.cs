using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum MoveTarget
{
    Self,
    Normal
}

[CreateAssetMenu(fileName = "Move", menuName = "Pokemon/New Move")]
public class MoveBase : ScriptableObject
{
    private static readonly HashSet<string> AlwaysPreferredMoveNames = new HashSet<string>
    {
        "tackle",
        "scratch",
        "pound"
    };
    [Header("Thông tin cơ bản")]
    [SerializeField] string moveName;
    [SerializeField] PokemonType type;
    [SerializeField] string category;
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] int pp;
    [FormerlySerializedAs("priority")]
    [SerializeField] int learnPriority;
    [SerializeField] MoveTarget target;

    [Header("Hiệu ứng đặc biệt")]
    [SerializeField] int drainNumerator;
    [SerializeField] int drainDenominator;
    [SerializeField] string statusEffect;
    [SerializeField] List<StatBoost> statBoosts;

    // Properties
    public string MoveName => moveName;
    public PokemonType Type => type;
    public string Category => category;
    public int Power => power;
    public int Accuracy => accuracy;
    public int PP => pp;
    public int LearnPriority => learnPriority;
    public bool IsAlwaysPreferredForStartingMoves => AlwaysPreferredMoveNames.Contains(TextKeyUtility.NormalizeKey(moveName));
    public MoveTarget Target => target;
    public float DrainRatio => drainDenominator > 0 ? (float)drainNumerator / drainDenominator : 0f;
    public string StatusEffect => statusEffect;
    public List<StatBoost> StatBoosts => statBoosts;
    public bool HasStatusEffect => !string.IsNullOrWhiteSpace(statusEffect);
    public bool HasStatBoosts => statBoosts != null && statBoosts.Count > 0;
    public bool IsDamagingMove => power > 0;

    public void SetTarget(MoveTarget newTarget) => target = newTarget;

    public int GetStartingMovePriorityScore(PokemonBase owner = null)
    {
        int score = learnPriority * 1000;

        if (IsAlwaysPreferredForStartingMoves)
            score += 2_000_000;

        if (owner != null && IsSignatureMoveFor(owner))
            score += 1_000_000;

        // Slightly prefer lower PP, then stronger damaging moves, then explicit move priority if present.
        score += Mathf.Clamp(100 - pp, -100, 100);
        score += Mathf.Clamp(power, 0, 999);

        return score;
    }

    public int GetStartingMovePriorityTier(PokemonBase owner = null)
    {
        if (IsAlwaysPreferredForStartingMoves)
            return 600;

        if (IsSignatureMoveFor(owner))
            return 500;

        if (IsLowPPMove())
            return 400;

        if (HasStatusEffect)
            return 300;

        if (IsLowPowerHighPPMove())
            return 200;

        if (HasStatBoosts)
            return 100;

        return 0;
    }

    public static int CalculateLearnPriority(MoveJson data)
    {
        if (data == null)
            return 0;

        string normalizedName = TextKeyUtility.NormalizeKey(data.name);

        if (AlwaysPreferredMoveNames.Contains(normalizedName))
            return 600;

        bool hasStatusEffect = !string.IsNullOrWhiteSpace(data.status);
        bool hasStatBoosts = data.boosts != null && data.boosts.Count > 0;
        bool isDamagingMove = data.basePower > 0;

        if (data.pp > 0 && data.pp <= 15)
            return 400;

        if (hasStatusEffect)
            return 300;

        if (isDamagingMove && data.basePower <= 60 && data.pp >= 20)
            return 200;

        if (hasStatBoosts)
            return 100;

        return 0;
    }

    public void SetLearnPriority(int value) => learnPriority = value;

    public bool IsSignatureMoveFor(PokemonBase owner)
    {
        if (owner == null || !IsDamagingMove)
            return false;

        if (HasStatusEffect || HasStatBoosts)
            return false;

        return type == owner.Type1 || type == owner.Type2;
    }

    public bool IsLowPPMove()
    {
        return pp > 0 && pp <= 15;
    }

    public bool IsLowPowerHighPPMove()
    {
        return IsDamagingMove && power <= 60 && pp >= 20;
    }

    // Load từ JSON
    public void LoadFromJson(MoveJson data)
    {
        moveName = data.name;
        type = data.type;
        category = data.category;
        power = data.basePower;
        accuracy = data.accuracy;
        pp = data.pp;
        learnPriority = data.learnPriority;

        if (System.Enum.TryParse(data.target, true, out MoveTarget parsedTarget))
            target = parsedTarget;
        else
            target = MoveTarget.Normal;

        if (data.drain != null && data.drain.Length == 2)
        {
            drainNumerator = data.drain[0];
            drainDenominator = data.drain[1];
        }
        else
        {
            drainNumerator = 0;
            drainDenominator = 0;
        }

        statusEffect = data.status;

        statBoosts = new List<StatBoost>();
        if (data.boosts != null)
        {
            foreach (var kv in data.boosts)
            {
                statBoosts.Add(new StatBoost(kv.Key, kv.Value));
            }
        }
    }
}

[System.Serializable]
public class StatBoost
{
    public string stat;          // attack, defense, accuracy, speed...
    public int amount;           // từ JSON (ví dụ 2 hoặc -1)
    public float multiplier;     // % mỗi cấp (amount * 0.1f)
    public int maxStacks = 3;    // tối đa 3 lần
    public int currentStacks = 0;

    // Constructor mặc định để có thể dùng object initializer
    public StatBoost() {}

    // Constructor tham số để load từ JSON
    public StatBoost(string stat, int amount)
    {
        this.stat = stat;
        this.amount = amount;
        this.multiplier = amount * 0.1f;
        this.currentStacks = 0;
    }

    public float Apply(float baseValue)
    {
        if (currentStacks >= maxStacks)
            return baseValue;

        currentStacks++;
        return baseValue * (1 + multiplier);
    }

    // Reset stacks cho từng Pokémon khi bắt đầu battle
    public void ResetStacks()
    {
        currentStacks = 0;
    }
}
