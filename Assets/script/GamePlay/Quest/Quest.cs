using System.Collections.Generic;
using UnityEngine;

public enum QuestStatus
{
    Ongoing,
    Completed
}

public enum QuestCategory
{
    MainStory,
    SideQuest
}

public enum QuestObjectiveType
{
    CatchPokemon,
    OwnPokemon,
    TalkToNPC,
    DefeatTrainer,
    CollectItem,
    ReachLocation,
    Custom
}

[System.Serializable]
public class QuestObjective
{
    [TextArea] [SerializeField] private string text;
    [SerializeField] private QuestObjectiveType type = QuestObjectiveType.Custom;
    [SerializeField] private string targetId;
    [Min(1)] [SerializeField] private int requiredCount = 1;

    public string Text => text;
    public QuestObjectiveType Type => type;
    public string TargetId => targetId;
    public int RequiredCount => Mathf.Max(1, requiredCount);
}

[System.Serializable]
public class QuestItemReward
{
    public ItemBase item;
    [Min(1)] public int amount = 1;
}

[System.Serializable]
public class QuestPokemonReward
{
    public PokemonBase pokemonBase;
    [Min(1)] public int level = 5;
}

[CreateAssetMenu(menuName = "Quest")]
public class Quest : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string questId;
    [SerializeField] private string title;
    [TextArea] [SerializeField] private string description;
    [SerializeField] private QuestCategory category = QuestCategory.SideQuest;

    [Header("Chain / Unlock")]
    [SerializeField] private List<string> prerequisiteQuestIds = new();

    [Header("Objectives")]
    [SerializeField] private List<QuestObjective> objectives = new();
    
    [Header("Turn-in")]
    [SerializeField] private string turnInNpcId; // NPC nào trả nhiệm vụ (để trống = NPC nào cũng trả được)

    [Header("Rewards")]
    [Min(0)] [SerializeField] private int rewardMoney = 0;
    [SerializeField] private List<QuestItemReward> rewardItems = new();
    [SerializeField] private List<QuestPokemonReward> rewardPokemons = new();

    public string TurnInNpcId => turnInNpcId;
    public int RewardMoney => rewardMoney;
    public IReadOnlyList<QuestItemReward> RewardItems => rewardItems;
    public IReadOnlyList<QuestPokemonReward> RewardPokemons => rewardPokemons;
    public string QuestId => questId;
    public string Title => title;
    public string Description => description;
    public QuestCategory Category => category;
    public IReadOnlyList<string> PrerequisiteQuestIds => prerequisiteQuestIds;
    public IReadOnlyList<QuestObjective> Objectives => objectives;

    // Tương thích code UI cũ đang gọi hàm này
    public string GetDisplayTitle()
    {
        if (Application.isPlaying && QuestManager.Instance != null && QuestManager.Instance.IsQuestCompleted(this))
            return $"{title} (Done)";
        return title;
    }
}