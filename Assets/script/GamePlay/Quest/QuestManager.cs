using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum QuestEventType
{
    PokemonCaught,
    PokemonOwned,
    NPCTalked,
    TrainerDefeated,
    ItemCollected,
    LocationReached,
    Custom
}

public struct QuestEvent
{
    public QuestEventType Type;
    public string TargetId;
    public int Amount;

    public QuestEvent(QuestEventType type, string targetId, int amount = 1)
    {
        Type = type;
        TargetId = targetId;
        Amount = amount;
    }
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Bootstrap")]
    [SerializeField] private Quest tutorialQuest;

    [Header("Main Story (optional, ordered)")]
    [SerializeField] private List<Quest> mainStoryOrder = new();

    [Header("Main Story Auto Accept")]
    [SerializeField] private bool autoAcceptMainStory = true;
    [SerializeField] private bool requirePrologueDoneForMainStory = false;
    [SerializeField] private bool mainStoryOnceOnly = true;
    [SerializeField] private bool restrictMainStoryAutoAcceptToScene = true;
    [SerializeField] private string mainStoryAutoAcceptSceneName = "Town1";

    [Header("Main Story Auto Turn-In")]
    [SerializeField] private bool autoTurnInMainStoryWhenReady = true;
    [SerializeField] private bool ignoreNpcTurnInRestrictionForMainStory = true;

    private readonly List<QuestRuntimeState> activeStates = new();
    private readonly HashSet<Quest> acceptedHistory = new();
    private readonly HashSet<Quest> completedQuests = new();
    private readonly HashSet<Quest> readyToTurnInQuests = new();
    public event Action<Quest> OnQuestReadyToTurnIn;
    public event Action<Quest> OnQuestAdded;
    public event Action<Quest> OnQuestUpdated;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DuplicateSystemRootUtility.DestroyDuplicate(this, Instance);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TryAutoAcceptCurrentMainStoryQuest();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAutoAcceptCurrentMainStoryQuest();
    }


    // ===== Query =====
    public List<Quest> GetActiveQuests()
    {
        var result = new List<Quest>(activeStates.Count);
        for (int i = 0; i < activeStates.Count; i++)
            result.Add(activeStates[i].Definition);
        return result;
    }

    public List<Quest> GetCompletedSideQuests()
    {
        var result = new List<Quest>();
        foreach (var q in completedQuests)
            if (q != null && q.Category != QuestCategory.MainStory)
                result.Add(q);
        return result;
    }

    public QuestRuntimeState GetState(Quest quest)
    {
        return FindState(quest);
    }

    public bool IsQuestActive(Quest quest) => FindState(quest) != null;
    public bool IsQuestCompleted(Quest quest) => quest != null && completedQuests.Contains(quest);
    public bool HasAcceptedBefore(Quest quest) => quest != null && acceptedHistory.Contains(quest);

    public List<Quest> GetVisibleMainStoryQuests()
    {
        var result = new List<Quest>();
        for (int i = 0; i < mainStoryOrder.Count; i++)
        {
            var quest = mainStoryOrder[i];
            if (quest == null) continue;
            if (!IsQuestActive(quest) && !IsQuestCompleted(quest)) continue;
            result.Add(quest);
        }
        return result;
    }

    public IReadOnlyList<Quest> GetMainStoryOrder()
    {
        return mainStoryOrder;
    }

    public Quest GetTutorialQuest()
    {
        return tutorialQuest;
    }

    public bool IsMainStoryQuestVisible(Quest quest)
    {
        return quest != null && (IsQuestActive(quest) || IsQuestCompleted(quest));
    }

    public QuestSaveSnapshot ExportSaveSnapshot()
    {
        var snapshot = new QuestSaveSnapshot();

        foreach (var quest in completedQuests)
        {
            if (!IsValidQuestId(quest)) continue;
            snapshot.completedQuestIds.Add(quest.QuestId);
        }

        foreach (var quest in readyToTurnInQuests)
        {
            if (!IsValidQuestId(quest)) continue;
            snapshot.readyToTurnInQuestIds.Add(quest.QuestId);
        }

        foreach (var state in activeStates)
        {
            var quest = state.Definition;
            if (!IsValidQuestId(quest)) continue;

            var stateData = new QuestStateSaveData
            {
                questId = quest.QuestId,
                objectives = new List<ObjectiveProgressSaveData>()
            };

            for (int i = 0; i < state.ObjectiveCount; i++)
            {
                stateData.objectives.Add(new ObjectiveProgressSaveData
                {
                    current = state.GetObjectiveCurrent(i),
                    completed = state.IsObjectiveCompleted(i)
                });
            }

            snapshot.activeQuests.Add(stateData);
        }

        return snapshot;
    }

    public void ImportSaveSnapshot(QuestSaveSnapshot snapshot)
    {
        activeStates.Clear();
        acceptedHistory.Clear();
        completedQuests.Clear();
        readyToTurnInQuests.Clear();

        if (snapshot == null)
            return;

        if (snapshot.completedQuestIds != null)
        {
            foreach (var questId in snapshot.completedQuestIds)
            {
                if (!TryResolveQuestById(questId, out var quest)) continue;
                completedQuests.Add(quest);
                acceptedHistory.Add(quest);
            }
        }

        if (snapshot.activeQuests != null)
        {
            foreach (var stateData in snapshot.activeQuests)
            {
                if (stateData == null || !TryResolveQuestById(stateData.questId, out var quest)) continue;
                if (completedQuests.Contains(quest)) continue;

                var state = new QuestRuntimeState(quest);
                var objectiveSaves = stateData.objectives ?? new List<ObjectiveProgressSaveData>();
                for (int i = 0; i < objectiveSaves.Count && i < state.ObjectiveCount; i++)
                {
                    var objective = objectiveSaves[i];
                    if (objective == null) continue;
                    state.SetObjectiveProgress(i, objective.current, objective.completed);
                }

                activeStates.Add(state);
                acceptedHistory.Add(quest);
            }
        }

        if (snapshot.readyToTurnInQuestIds != null)
        {
            foreach (var questId in snapshot.readyToTurnInQuestIds)
            {
                if (!TryResolveQuestById(questId, out var quest)) continue;
                if (activeStates.Any(s => s.Definition == quest))
                    readyToTurnInQuests.Add(quest);
            }
        }
    }

    public Quest GetCurrentMainStoryQuest()
    {
        for (int i = 0; i < mainStoryOrder.Count; i++)
        {
            var q = mainStoryOrder[i];
            if (q == null) continue;
            if (completedQuests.Contains(q)) continue;
            return q;
        }
        return null;
    }

    // ===== Accept =====
    public bool CanAcceptQuest(Quest quest, bool onceOnly = false)
    {
        if (quest == null) return false;
        if (IsQuestActive(quest)) return false;
        if (onceOnly && acceptedHistory.Contains(quest)) return false;
        if (!ArePrerequisitesMet(quest)) return false;
        return true;
    }

    public bool AcceptQuest(Quest quest, bool onceOnly = false)
    {
        return AddQuest(quest, onceOnly);
    }

    public bool AddQuest(Quest quest, bool onceOnly)
    {
        if (!CanAcceptQuest(quest, onceOnly))
            return false;

        activeStates.Add(new QuestRuntimeState(quest));
        acceptedHistory.Add(quest);

        OnQuestAdded?.Invoke(quest);
        return true;
    }

    // ===== Manual progress (API cũ) =====
    public bool CompleteObjective(Quest quest, int objectiveIndex)
    {
        var state = FindState(quest);
        if (state == null) return false;

        int required = 1;
        if (quest != null && quest.Objectives != null && objectiveIndex >= 0 && objectiveIndex < quest.Objectives.Count)
            required = quest.Objectives[objectiveIndex].RequiredCount;

        bool changed = state.MarkObjectiveCompleted(objectiveIndex, required);
        if (!changed) return false;

        OnQuestUpdated?.Invoke(quest);

        if (state.Status == QuestStatus.Completed)
            TryCompleteByObjectives(state);

        return true;
    }

    public void CompleteQuest(Quest quest)
    {
        var state = FindState(quest);
        if (state == null) return;

        state.MarkAllCompleted();
        OnQuestUpdated?.Invoke(quest);
        TryCompleteByObjectives(state);
    }

    // ===== Event-driven progress =====
    public void SubmitEvent(QuestEvent questEvent)
    {
        for (int i = activeStates.Count - 1; i >= 0; i--)
        {
            var state = activeStates[i];
            var quest = state.Definition;
            if (quest == null) continue;

            bool changed = false;

            for (int objIndex = 0; objIndex < quest.Objectives.Count; objIndex++)
            {
                var obj = quest.Objectives[objIndex];
                if (obj == null) continue;
                if (state.IsObjectiveCompleted(objIndex)) continue;
                if (!IsObjectiveMatch(obj, questEvent)) continue;

                changed |= state.AddProgress(
                    objIndex,
                    questEvent.Amount <= 0 ? 1 : questEvent.Amount,
                    obj.RequiredCount
                );
            }

            if (changed)
            {
                OnQuestUpdated?.Invoke(quest);

                if (state.Status == QuestStatus.Completed)
                    TryCompleteByObjectives(state);
            }
        }
    }

    // ===== Internal =====
    private QuestRuntimeState FindState(Quest quest)
    {
        if (quest == null) return null;
        for (int i = 0; i < activeStates.Count; i++)
        {
            if (activeStates[i].Definition == quest)
                return activeStates[i];
        }
        return null;
    }

    private bool IsObjectiveMatch(QuestObjective obj, QuestEvent e)
    {
        if (obj == null) return false;
        if (!EventTypeMatches(obj.Type, e.Type)) return false;

        if (string.IsNullOrWhiteSpace(obj.TargetId))
            return true;

        return string.Equals(obj.TargetId, e.TargetId, StringComparison.OrdinalIgnoreCase);
    }

    private bool EventTypeMatches(QuestObjectiveType objectiveType, QuestEventType eventType)
    {
        return objectiveType switch
        {
            QuestObjectiveType.CatchPokemon => eventType == QuestEventType.PokemonCaught,
            QuestObjectiveType.OwnPokemon => eventType == QuestEventType.PokemonOwned,
            QuestObjectiveType.TalkToNPC => eventType == QuestEventType.NPCTalked,
            QuestObjectiveType.DefeatTrainer => eventType == QuestEventType.TrainerDefeated,
            QuestObjectiveType.CollectItem => eventType == QuestEventType.ItemCollected,
            QuestObjectiveType.ReachLocation => eventType == QuestEventType.LocationReached,
            QuestObjectiveType.Custom => eventType == QuestEventType.Custom,
            _ => false
        };
    }

    private bool ArePrerequisitesMet(Quest quest)
    {
        if (quest == null) return false;
        if (quest.PrerequisiteQuestIds == null || quest.PrerequisiteQuestIds.Count == 0) return true;

        for (int i = 0; i < quest.PrerequisiteQuestIds.Count; i++)
        {
            string requiredId = quest.PrerequisiteQuestIds[i];
            if (string.IsNullOrWhiteSpace(requiredId)) continue;

            bool found = false;
            foreach (var done in completedQuests)
            {
                if (done != null && string.Equals(done.QuestId, requiredId, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found) return false;
        }

        return true;
    }

    private bool TryResolveQuestById(string questId, out Quest resolved)
    {
        resolved = null;
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        foreach (var quest in EnumerateKnownQuests())
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId)) continue;
            if (!string.Equals(quest.QuestId, questId, StringComparison.OrdinalIgnoreCase)) continue;

            resolved = quest;
            return true;
        }

        return false;
    }

    private IEnumerable<Quest> EnumerateKnownQuests()
    {
        if (tutorialQuest != null)
            yield return tutorialQuest;

        foreach (var quest in mainStoryOrder)
            yield return quest;

        foreach (var state in activeStates)
            yield return state?.Definition;

        foreach (var quest in completedQuests)
            yield return quest;

        foreach (var quest in Resources.LoadAll<Quest>(string.Empty))
            yield return quest;
    }

    private bool IsValidQuestId(Quest quest)
    {
        return quest != null && !string.IsNullOrWhiteSpace(quest.QuestId);
    }

    private void FinalizeQuestCompletion(Quest quest)
    {
        if (quest == null) return;

        var state = FindState(quest);
        if (state == null) return;

        activeStates.Remove(state);
        completedQuests.Add(quest);

        OnQuestCompleted?.Invoke(quest);

        var nextMain = GetCurrentMainStoryQuest();
        if (nextMain != null && !IsQuestActive(nextMain) && ArePrerequisitesMet(nextMain))
            OnQuestUnlocked?.Invoke(nextMain);

        // Tự nhận main story quest kế tiếp nếu đủ điều kiện.
        TryAutoAcceptCurrentMainStoryQuest();
    }

    public bool IsQuestReadyToTurnIn(Quest quest)
    {
        return quest != null && readyToTurnInQuests.Contains(quest);
    }

    public Quest GetReadyToTurnInQuestForNpc(string npcId, Quest preferredQuest = null)
    {
        if (preferredQuest != null && CanTurnInQuest(preferredQuest, npcId))
            return preferredQuest;

        foreach (var quest in readyToTurnInQuests)
        {
            if (CanTurnInQuest(quest, npcId))
                return quest;
        }

        return null;
    }

    public bool CanTurnInQuest(Quest quest, string npcId = "")
    {
        if (quest == null) return false;
        if (!readyToTurnInQuests.Contains(quest)) return false;

        if (string.IsNullOrWhiteSpace(quest.TurnInNpcId)) return true;
        return string.Equals(quest.TurnInNpcId, npcId, StringComparison.OrdinalIgnoreCase);
    }

    public bool TurnInQuest(Quest quest, string npcId = "")
    {
        if (!CanTurnInQuest(quest, npcId)) return false;

        ApplyRewards(quest);
        readyToTurnInQuests.Remove(quest);

        FinalizeQuestCompletion(quest); // dùng chung để có unlock main story
        return true;
    }

    private void MarkQuestReadyToTurnIn(Quest quest)
    {
        if (quest == null) return;
        if (readyToTurnInQuests.Contains(quest)) return;

        readyToTurnInQuests.Add(quest);
        OnQuestReadyToTurnIn?.Invoke(quest);
    }

    // chỗ đang finalize ngay khi objective đủ -> đổi thành "ready to turn in"
    private void TryCompleteByObjectives(QuestRuntimeState state)
    {
        if (state == null || state.Definition == null) return;
        if (state.Status != QuestStatus.Completed)
            return;

        var quest = state.Definition;
        MarkQuestReadyToTurnIn(quest);

        if (autoTurnInMainStoryWhenReady && quest.Category == QuestCategory.MainStory)
        {
            string npcIdForAutoTurnIn = string.Empty;
            if (ignoreNpcTurnInRestrictionForMainStory)
                npcIdForAutoTurnIn = quest.TurnInNpcId;

            TurnInQuest(quest, npcIdForAutoTurnIn);
        }
    }

    private void ApplyRewards(Quest quest)
    {
        var inventory = Inventory.Instance != null ? Inventory.Instance : FindObjectOfType<Inventory>();

        if (inventory != null && quest.RewardMoney > 0)
            inventory.AddMoney(quest.RewardMoney);

        if (inventory != null)
        {
            foreach (var r in quest.RewardItems)
            {
                if (r == null || r.item == null) continue;
                inventory.AddItem(r.item, Mathf.Max(1, r.amount));
            }
        }

        var party = PlayerParty.Instance;
        var storage = StorageSystem.Instance;

        foreach (var p in quest.RewardPokemons)
        {
            if (p == null || p.pokemonBase == null) continue;
            var rewardPokemon = new Pokemon(p.pokemonBase, Mathf.Max(1, p.level)); // kiểm tra constructor project bạn

            if (party != null && party.Pokemons.Count < 6) party.AddPokemon(rewardPokemon);
            else storage?.AddPokemon(rewardPokemon);
        }
    }

    private void TryAutoAcceptCurrentMainStoryQuest()
    {
        if (!autoAcceptMainStory)
            return;

        if (restrictMainStoryAutoAcceptToScene && !string.IsNullOrWhiteSpace(mainStoryAutoAcceptSceneName))
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (!string.Equals(sceneName, mainStoryAutoAcceptSceneName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        Quest targetQuest = GetCurrentMainStoryQuest();

        // fallback cho project cũ chưa set mainStoryOrder
        if (targetQuest == null)
            targetQuest = tutorialQuest;

        if (targetQuest == null)
            return;

        if (!ArePrerequisitesMet(targetQuest))
            return;

        if (IsQuestCompleted(targetQuest) || IsQuestActive(targetQuest))
            return;

        // main story thường chỉ nhận 1 lần
        bool accepted = AddQuest(targetQuest, onceOnly: mainStoryOnceOnly);
        if (accepted)
            Debug.Log($"[Quest] Auto accepted main story quest: {targetQuest.Title}");
    }
}
