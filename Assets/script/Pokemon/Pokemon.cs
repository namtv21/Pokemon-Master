using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class Pokemon
{
    // Backing fields
    private PokemonBase _baseData;
    private int _level;
    private int _currentHp;
    private int _exp;

    // Properties
    public PokemonBase Base => _baseData;
    public int Level => _level;
    public int CurrentHp => _currentHp;
    public int Exp => _exp;
    public int ExpToNextLevel => PokemonBase.GetExpForLevel(Level);

    // Stat gốc (tính từ baseData + level)
    public int MaxHp => Mathf.FloorToInt((_baseData.MaxHp * _level) / 50f + 10);
    public int Attack => Mathf.FloorToInt((_baseData.Attack * _level) / 50f + 5);
    public int Defense => Mathf.FloorToInt((_baseData.Defense * _level) / 50f + 5);
    public int SpAttack => Mathf.FloorToInt((_baseData.SpAttack * _level) / 50f + 5);
    public int SpDefense => Mathf.FloorToInt((_baseData.SpDefense * _level) / 50f + 5);
    public int Speed => Mathf.FloorToInt((_baseData.Speed * _level) / 50f + 5);

    // Stat hiện tại trong trận
    public int CurrentAttack { get; private set; }
    public int CurrentDefense { get; private set; }
    public int CurrentSpAttack { get; private set; }
    public int CurrentSpDefense { get; private set; }
    public int CurrentSpeed { get; private set; }
    public float AccuracyModifier { get; private set; } = 1f;

    public StatusEffect Status { get; private set; } = StatusEffect.None;
    public List<Move> Moves { get; private set; }

    // Constructor khởi tạo mới
    public Pokemon(PokemonBase data, int level)
    {
        _baseData = data;
        _level = level;
        _currentHp = MaxHp;
        _exp = 0;
        Status = StatusEffect.None;
        Moves = new List<Move>();

        RecalculateStats();

        var sortedMoves = Base.LearnableMoves.OrderBy(lm => lm.level);
        foreach (var learnableMove in sortedMoves)
        {
            if (learnableMove.level <= level)
            {
                Moves.Add(new Move(learnableMove.move));
                if (Moves.Count >= 4)
                    break;
            }
        }
    }

    // Constructor khởi tạo từ dữ liệu save
    public Pokemon(PokemonData data)
    {
        _baseData = PokemonDB.Instance.GetPokemonByName(data.name);
        _level = data.level;
        _currentHp = data.currentHP;
        _exp = data.exp;

        Moves = new List<Move>();
        foreach (var moveName in data.moves)
        {
            var moveBase = MoveDB.Instance.GetMoveByName(moveName);
            if (moveBase != null)
                Moves.Add(new Move(moveBase));
        }

        RecalculateStats();
    }
    // Quản lý stat boost stacks
    private Dictionary<string, int> statBoostStacks = new Dictionary<string, int>();

    public int GetCurrentStacks(string stat)
    {
        return statBoostStacks.ContainsKey(stat) ? statBoostStacks[stat] : 0;
    }

    public void IncrementStacks(string stat)
    {
        if (!statBoostStacks.ContainsKey(stat)) statBoostStacks[stat] = 0;
        statBoostStacks[stat]++;
    }

    public void ResetAllStacks()
    {
        statBoostStacks.Clear();
    }

    // Các hàm thao tác
    public void HealAll()
    {
        _currentHp = MaxHp;
        Status = StatusEffect.None;
        ResetAllStacks();
        foreach (var move in Moves)
        {
            move.ResetPP();
        }
        RecalculateStats();
    }

    public Pokemon CloneAsOwned()
    {
        return new Pokemon(this.Base, this.Level);
    }

    public void TakeDamage(int damage)
    {
        _currentHp = Mathf.Max(_currentHp - damage, 0);
    }

    public bool IsFainted => _currentHp <= 0;
    public bool IsHealthy => !IsFainted && Status != StatusEffect.None;

    public void ReviveToOneHP()
    {
        if (IsFainted)
            _currentHp = 1;
    }

    public void Heal(int amount)
    {
        _currentHp = Mathf.Min(_currentHp + amount, MaxHp);
    }

    public void FullHeal()
    {
        _currentHp = MaxHp;
    }

    public void Revive(int percent)
    {
        if (IsFainted)
        {
            int reviveHp = Mathf.FloorToInt(MaxHp * (percent / 100f));
            _currentHp = Mathf.Max(reviveHp, 1);
        }
    }


    public void ApplyStatus(StatusEffect effect)
    {
        Status = effect;
        Debug.Log($"{Base.Name} is now {effect}!");
    }

    public void CureStatus()
    {
        Status = StatusEffect.None;
        Debug.Log($"{Base.Name} is cured of its status condition!");
    }

    public void ModifyStat(string stat, float multiplier)
    {
        switch (stat.ToLower())
        {
            case "attack":
                CurrentAttack = (int)(CurrentAttack * (1 + multiplier));
                break;
            case "defense":
                CurrentDefense = (int)(CurrentDefense * (1 + multiplier));
                break;
            case "spattack":
                CurrentSpAttack = (int)(CurrentSpAttack * (1 + multiplier));
                break;
            case "spdefense":
                CurrentSpDefense = (int)(CurrentSpDefense * (1 + multiplier));
                break;
            case "speed":
                CurrentSpeed = (int)(CurrentSpeed * (1 + multiplier));
                break;
            case "accuracy":
                AccuracyModifier *= (1 + multiplier);
                break;
        }
    }

    public void GainExp(int amount)
    {
        _exp += amount;
        while (_exp >= ExpToNextLevel)
        {
            _exp -= ExpToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        int oldMaxHp = MaxHp;
        _level++;
        RecalculateStats();
        int newMaxHp = MaxHp;
        int hpGain = newMaxHp - oldMaxHp;
        _currentHp += hpGain;
        _currentHp = Mathf.Min(_currentHp, newMaxHp);
        Debug.Log($"{Base.Name} leveled up to {_level}!");

        LearnableMove[] sortedMoves = Base.LearnableMoves.OrderBy(lm => lm.level).ToArray();
        foreach (var learnableMove in sortedMoves)
        {
            if (learnableMove.level == _level)
            {
                TryLearnMove(learnableMove.move);
                break;
            }
        }
    }

    private void RecalculateStats()
    {
        CurrentAttack = Attack;
        CurrentDefense = Defense;
        CurrentSpAttack = SpAttack;
        CurrentSpDefense = SpDefense;
        CurrentSpeed = Speed;
        AccuracyModifier = 1f;
    }
    public void ResetStatBoosts()
    {
        // Reset các chỉ số về base
        CurrentAttack = Attack;
        CurrentDefense = Defense;
        CurrentSpAttack = SpAttack;
        CurrentSpDefense = SpDefense;
        CurrentSpeed = Speed;
        AccuracyModifier = 1f;

        // Reset tất cả stack boost
        ResetAllStacks();
    }

    private void TryLearnMove(MoveBase newMoveBase)
    {
        if (Moves.Count < 4)
        {
            Moves.Add(new Move(newMoveBase));
            Debug.Log($"{Base.Name} learned {newMoveBase.MoveName}!");
        }
        else
        {
            OnMoveLearnRequested?.Invoke(this, newMoveBase);
        }
    }

    public event Action<Pokemon, MoveBase> OnMoveLearnRequested;

    public PokemonData GetSaveData()
    {
        var data = new PokemonData();
        data.name = Base.Name;
        data.level = Level;
        data.currentHP = CurrentHp;
        data.maxHP = MaxHp;
        data.exp = Exp;

        data.moves = new List<string>();
        foreach (var m in Moves)
            data.moves.Add(m.Base.MoveName);

        data.attack = Attack;
        data.defense = Defense;
        data.spAttack = SpAttack;
        data.spDefense = SpDefense;
        data.speed = Speed;

        return data;
    }
}
