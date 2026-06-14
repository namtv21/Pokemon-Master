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
    private int _battleParticipationCount;
    private int _friendshipLevel;
    private readonly Queue<MoveBase> pendingMoveLearnQueue = new Queue<MoveBase>();
    private MoveBase currentPendingMoveLearn;

    // Properties
    public PokemonBase Base => _baseData;
    public int Level => _level;
    public int CurrentHp => _currentHp;
    public int Exp => _exp;
    public int ExpToNextLevel => PokemonBase.GetExpForLevel(Level);
    public int BattleParticipationCount => _battleParticipationCount;
    public int FriendshipLevel => _friendshipLevel;
    public int FriendshipProgress => _battleParticipationCount;

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
    public MoveBase CurrentPendingMoveLearn => currentPendingMoveLearn;
    public bool HasPendingMoveLearn => currentPendingMoveLearn != null || pendingMoveLearnQueue.Count > 0;

    // Constructor khởi tạo mới
    public Pokemon(PokemonBase data, int level)
    {
        _baseData = data;
        _level = level;
        _currentHp = MaxHp;
        _exp = 0;
        _battleParticipationCount = 0;
        _friendshipLevel = 0;
        Status = StatusEffect.None;
        Moves = new List<Move>();

        RecalculateStats();

        InitializeMovesForLevel(level);
    }

    // Constructor khởi tạo từ dữ liệu save
    public Pokemon(PokemonData data)
    {
        _baseData = PokemonDB.Instance.GetPokemonByName(
            string.IsNullOrWhiteSpace(data.resourceId) ? data.name : data.resourceId);

        if (_baseData == null && !string.IsNullOrWhiteSpace(data.name))
            _baseData = PokemonDB.Instance.GetPokemonByName(data.name);

        if (_baseData == null)
            throw new InvalidOperationException($"Pokemon save data could not be resolved: '{data.name}'");

        _level = data.level;
        _currentHp = data.currentHP;
        _exp = data.exp;
        _battleParticipationCount = Mathf.Max(0, data.battleParticipationCount);
        _friendshipLevel = Mathf.Max(0, data.friendshipLevel);

        Moves = new List<Move>();
        foreach (var moveName in data.moves ?? Enumerable.Empty<string>())
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
    public bool IsHealthy => !IsFainted && Status == StatusEffect.None;

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
        GainExp(amount, true);
    }

    public void GainExp(int amount, bool awardBonusExp)
    {
        GainExp(amount, awardBonusExp, true);
    }

    public void GainExp(int amount, bool awardBonusExp, bool autoEvolveWhenUnobserved)
    {
        if (amount <= 0)
            return;

        if (awardBonusExp)
        {
            var inventory = Inventory.Instance;
            if (inventory != null)
                inventory.AddExperienceBottleExp(Mathf.FloorToInt(amount * inventory.BonusExpRatio));
        }

        _exp += amount;
        while (_exp >= ExpToNextLevel)
        {
            _exp -= ExpToNextLevel;
            LevelUp();

            if (OnMoveLearnRequested == null)
            {
                AutoResolvePendingMoveLearns();

                while (autoEvolveWhenUnobserved && CanEvolveNow())
                    TryEvolve();
            }
        }
    }

    public void AddBattleParticipation(int count = 1)
    {
        if (count <= 0)
            return;

        _battleParticipationCount += count;
        while (_battleParticipationCount >= 10)
        {
            _battleParticipationCount -= 10;
            _friendshipLevel++;
            Debug.Log($"{Base.Name}'s friendship increased to {_friendshipLevel}!");
        }
    }

    public bool CanEvolveNow()
    {
        return _baseData != null && _baseData.Evolvable && _baseData.EvolvesTo != null && _level >= _baseData.EvolutionLevel;
    }

    public string GetEvolutionTargetName()
    {
        return _baseData != null && _baseData.EvolvesTo != null ? _baseData.EvolvesTo.Name : string.Empty;
    }

    public bool TryEvolve()
    {
        if (!CanEvolveNow())
            return false;

        var targetBase = _baseData.EvolvesTo;
        if (targetBase == null)
            return false;

        string oldName = _baseData.Name;
        float hpRatio = MaxHp > 0 ? (float)_currentHp / MaxHp : 1f;

        _baseData = targetBase;
        RecalculateStats();
        _currentHp = Mathf.Clamp(Mathf.RoundToInt(MaxHp * hpRatio), 1, MaxHp);

        Debug.Log($"{oldName} evolved into {_baseData.Name}!");
        return true;
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
                TryLearnMove(learnableMove.move);
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
        if (newMoveBase == null || KnowsMove(newMoveBase))
            return;

        if (Moves.Count < 4 && currentPendingMoveLearn == null && pendingMoveLearnQueue.Count == 0)
        {
            Moves.Add(new Move(newMoveBase));
            Debug.Log($"{Base.Name} learned {newMoveBase.MoveName}!");
        }
        else
        {
            if (!pendingMoveLearnQueue.Contains(newMoveBase) && currentPendingMoveLearn != newMoveBase)
                pendingMoveLearnQueue.Enqueue(newMoveBase);

            DispatchNextPendingMoveLearn();
        }
    }

    public event Action<Pokemon, MoveBase> OnMoveLearnRequested;

    public void DispatchNextPendingMoveLearn()
    {
        if (currentPendingMoveLearn != null || pendingMoveLearnQueue.Count == 0 || OnMoveLearnRequested == null)
            return;

        currentPendingMoveLearn = pendingMoveLearnQueue.Dequeue();
        OnMoveLearnRequested?.Invoke(this, currentPendingMoveLearn);
    }

    public string ResolvePendingMoveLearn(int selectedIndex)
    {
        if (currentPendingMoveLearn == null)
            return string.Empty;

        string message;
        MoveBase newMoveBase = currentPendingMoveLearn;

        if (selectedIndex >= 0 && selectedIndex < Moves.Count)
        {
            Moves[selectedIndex] = new Move(newMoveBase);
            message = $"{Base.Name} forgot a move and learned {newMoveBase.MoveName}!";
        }
        else if (Moves.Count < 4)
        {
            Moves.Add(new Move(newMoveBase));
            message = $"{Base.Name} learned {newMoveBase.MoveName}!";
        }
        else
        {
            message = $"{Base.Name} chose not to learn {newMoveBase.MoveName}.";
        }

        currentPendingMoveLearn = null;
        return message;
    }

    public PokemonData GetSaveData()
    {
        var data = new PokemonData();
        data.name = Base.Name;
        data.level = Level;
        data.currentHP = CurrentHp;
        data.maxHP = MaxHp;
        data.exp = Exp;
        data.battleParticipationCount = BattleParticipationCount;
        data.friendshipLevel = FriendshipLevel;

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

    private void InitializeMovesForLevel(int level)
    {
        if (Base?.LearnableMoves == null || Base.LearnableMoves.Length == 0)
            return;

        var selectedMoves = Base.LearnableMoves
            .Where(lm => lm.move != null && lm.level <= level)
            .GroupBy(lm => lm.move.MoveName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(lm => lm.level).First())
            .OrderByDescending(lm => lm.move.GetStartingMovePriorityScore(Base))
            .ThenByDescending(lm => lm.move.Power)
            .ThenByDescending(lm => lm.level)
            .ThenBy(lm => lm.move.MoveName)
            .Take(4)
            .OrderBy(lm => lm.level)
            .ThenBy(lm => lm.move.MoveName);

        foreach (var learnableMove in selectedMoves)
            Moves.Add(new Move(learnableMove.move));
    }

    private bool KnowsMove(MoveBase moveBase)
    {
        if (moveBase == null)
            return false;

        if (Moves.Any(m => m?.Base == moveBase || string.Equals(m?.Base?.MoveName, moveBase.MoveName, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (currentPendingMoveLearn == moveBase || string.Equals(currentPendingMoveLearn?.MoveName, moveBase.MoveName, StringComparison.OrdinalIgnoreCase))
            return true;

        return pendingMoveLearnQueue.Any(m => m == moveBase || string.Equals(m?.MoveName, moveBase.MoveName, StringComparison.OrdinalIgnoreCase));
    }

    private void AutoResolvePendingMoveLearns()
    {
        while (currentPendingMoveLearn != null || pendingMoveLearnQueue.Count > 0)
        {
            if (currentPendingMoveLearn == null)
                currentPendingMoveLearn = pendingMoveLearnQueue.Dequeue();

            AutoResolveCurrentPendingMoveLearn();
        }
    }

    private void AutoResolveCurrentPendingMoveLearn()
    {
        if (currentPendingMoveLearn == null)
            return;

        MoveBase newMoveBase = currentPendingMoveLearn;
        currentPendingMoveLearn = null;

        if (KnowsMove(newMoveBase))
            return;

        if (Moves.Count < 4)
        {
            Moves.Add(new Move(newMoveBase));
            Debug.Log($"{Base.Name} learned {newMoveBase.MoveName}!");
            return;
        }

        int weakestIndex = GetWeakestMoveIndex();
        if (weakestIndex < 0)
            return;

        var weakestMove = Moves[weakestIndex]?.Base;
        int weakestScore = weakestMove != null ? weakestMove.GetStartingMovePriorityScore(Base) : int.MinValue;
        int newScore = newMoveBase.GetStartingMovePriorityScore(Base);

        if (newScore > weakestScore)
        {
            string forgottenMoveName = weakestMove != null ? weakestMove.MoveName : "a move";
            Moves[weakestIndex] = new Move(newMoveBase);
            Debug.Log($"{Base.Name} forgot {forgottenMoveName} and learned {newMoveBase.MoveName}!");
        }
    }

    private int GetWeakestMoveIndex()
    {
        int weakestIndex = -1;
        int weakestScore = int.MaxValue;

        for (int i = 0; i < Moves.Count; i++)
        {
            var moveBase = Moves[i]?.Base;
            if (moveBase == null)
                continue;

            int score = moveBase.GetStartingMovePriorityScore(Base);
            if (score < weakestScore)
            {
                weakestScore = score;
                weakestIndex = i;
            }
        }

        return weakestIndex;
    }
}
