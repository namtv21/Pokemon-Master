using System.Collections.Generic;
using UnityEngine;

public enum MoveTarget
{
    Self,
    Normal
}

[CreateAssetMenu(fileName = "Move", menuName = "Pokemon/New Move")]
public class MoveBase : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    [SerializeField] string moveName;
    [SerializeField] PokemonType type;
    [SerializeField] string category;
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] int pp;
    [SerializeField] int priority;
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
    public int Priority => priority;
    public MoveTarget Target => target;
    public float DrainRatio => drainDenominator > 0 ? (float)drainNumerator / drainDenominator : 0f;
    public string StatusEffect => statusEffect;
    public List<StatBoost> StatBoosts => statBoosts;

    public void SetTarget(MoveTarget newTarget) => target = newTarget;

    // Load từ JSON
    public void LoadFromJson(MoveJson data)
    {
        moveName = data.name;
        type = data.type;
        category = data.category;
        power = data.basePower;
        accuracy = data.accuracy;
        pp = data.pp;
        priority = data.priority;

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
