using System;
using System.Collections.Generic;
using UnityEngine;

public enum MainStoryActionType
{
    ShowDialog = 0,
    AcceptQuest = 1,
    SubmitEvent = 2,
    Wait = 3,
    MoveNpc = 4,
    PlayAnimationTrigger = 5,
    SetStoryFlag = 6,
    GivePokemon = 7,
    ShowChoice = 11,
    StartBattle = 12,
    FadeNpc = 13,
    GiveItem = 14,
    TakeItem = 15
}

public enum MainStoryBattleType
{
    Wild,
    Trainer
}

[Serializable]
public class TrainerPokemonSpec
{
    [SerializeField] private string pokemonResourceId;
    [SerializeField] private int level = 5;

    public string PokemonResourceId => pokemonResourceId;
    public int Level => Mathf.Max(1, level);
}

[Serializable]
public class MainStoryChoiceOption
{
    [SerializeField] private string optionLabel;

    [Header("Optional Reward")]
    [SerializeField] private bool givePokemon;
    [SerializeField] private string pokemonResourceId;
    [SerializeField] private int pokemonLevel = 5;

    [Header("Optional Event")]
    [SerializeField] private bool submitQuestEvent;
    [SerializeField] private QuestEventType eventType = QuestEventType.Custom;
    [SerializeField] private string targetId;
    [SerializeField] private int amount = 1;

    [Header("Optional Story Flag")]
    [SerializeField] private bool setStoryFlag;
    [SerializeField] private StoryFlagKey storyFlag = StoryFlagKey.StarterChosen;
    [SerializeField] private bool storyFlagValue = true;
    [SerializeField] private string starterPokemonId;

    public string OptionLabel => optionLabel;
    public bool GivePokemon => givePokemon;
    public string PokemonResourceId => pokemonResourceId;
    public int PokemonLevel => Mathf.Max(1, pokemonLevel);
    public bool SubmitQuestEvent => submitQuestEvent;
    public QuestEventType EventType => eventType;
    public string TargetId => targetId;
    public int Amount => Mathf.Max(1, amount);
    public bool SetStoryFlag => setStoryFlag;
    public StoryFlagKey StoryFlag => storyFlag;
    public bool StoryFlagValue => storyFlagValue;
    public string StarterPokemonId => starterPokemonId;
}

[Serializable]
public class MainStoryAction
{
    [SerializeField] private MainStoryActionType type = MainStoryActionType.ShowDialog;

    [Header("Dialog")]
    [TextArea(2, 10)] [SerializeField] private string dialogText;
    [SerializeField] private string speakerName = "Narrator";
    [SerializeField] private Sprite portrait;

    [Header("Quest")]
    [SerializeField] private Quest questToAccept;
    [SerializeField] private bool useCurrentMainStoryQuest = true;
    [SerializeField] private bool acceptOnceOnly = true;

    [Header("Event")]
    [SerializeField] private bool submitQuestEvent;
    [SerializeField] private QuestEventType eventType = QuestEventType.Custom;
    [SerializeField] private string targetId;
    [SerializeField] private int amount = 1;

    [Header("Battle")]
    [SerializeField] private MainStoryBattleType battleType = MainStoryBattleType.Wild;
    [SerializeField] private string wildPokemonResourceId;
    [SerializeField] private int wildPokemonLevel = 5;
    [SerializeField] private string trainerNpcId;
    [SerializeField] private bool waitForBattleEnd = true;
    [SerializeField] private bool continueOnlyIfWon;
    [Header("Trainer Team (optional)")]
    [SerializeField] private List<TrainerPokemonSpec> trainerTeam = new();

    [Header("Wait")]
    [SerializeField] private float waitSeconds = 0.5f;
    [SerializeField] private bool freezePlayerInput;

    [Header("NPC Move")]
    [SerializeField] private string npcId;
    [SerializeField] private string moveTargetId;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private bool faceTargetOnArrive = true;

    [Header("NPC Fade")]
    [SerializeField] private float npcFadeDuration = 0.5f;
    [SerializeField] private bool disableNpcAfterFade = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationTrigger;

    [Header("Story Flag")]
    [SerializeField] private StoryFlagKey storyFlag = StoryFlagKey.FirstMainQuestAccepted;
    [SerializeField] private bool storyFlagValue = true;
    [SerializeField] private string starterPokemonId;

    [Header("Give Pokemon")]
    [SerializeField] private string pokemonResourceId;
    [SerializeField] private int pokemonLevel = 5;

    [Header("Story Items")]
    [SerializeField] private ItemBase item;
    [SerializeField] private int itemCount = 1;

    [Header("Choice")]
    [TextArea] [SerializeField] private string choicePrompt = "Choose an option:";
    [SerializeField] private List<MainStoryChoiceOption> choiceOptions = new();

    public MainStoryActionType Type => type;
    public string DialogText => dialogText;
    public string SpeakerName => speakerName;
    public Sprite Portrait => portrait;
    public Quest QuestToAccept => questToAccept;
    public bool UseCurrentMainStoryQuest => useCurrentMainStoryQuest;
    public bool AcceptOnceOnly => acceptOnceOnly;
    public bool SubmitQuestEvent => submitQuestEvent;
    public QuestEventType EventType => eventType;
    public string TargetId => targetId;
    public int Amount => amount;
    public MainStoryBattleType BattleType => battleType;
    public string WildPokemonResourceId => wildPokemonResourceId;
    public int WildPokemonLevel => Mathf.Max(1, wildPokemonLevel);
    public string TrainerNpcId => trainerNpcId;
    public bool WaitForBattleEnd => waitForBattleEnd;
    public bool ContinueOnlyIfWon => continueOnlyIfWon;
    public IReadOnlyList<TrainerPokemonSpec> TrainerTeam => trainerTeam;
    public float WaitSeconds => Mathf.Max(0f, waitSeconds);
    public bool FreezePlayerInput => freezePlayerInput;
    public string NpcId => npcId;
    public string MoveTargetId => moveTargetId;
    public float MoveSpeed => Mathf.Max(0.01f, moveSpeed);
    public bool FaceTargetOnArrive => faceTargetOnArrive;
    public float NpcFadeDuration => Mathf.Max(0.01f, npcFadeDuration);
    public bool DisableNpcAfterFade => disableNpcAfterFade;
    public Animator Animator => animator;
    public string AnimationTrigger => animationTrigger;
    public StoryFlagKey StoryFlag => storyFlag;
    public bool StoryFlagValue => storyFlagValue;
    public string StarterPokemonId => starterPokemonId;
    public string PokemonResourceId => pokemonResourceId;
    public int PokemonLevel => Mathf.Max(1, pokemonLevel);
    public ItemBase Item => item;
    public int ItemCount => Mathf.Max(1, itemCount);
    public string ChoicePrompt => choicePrompt;
    public IReadOnlyList<MainStoryChoiceOption> ChoiceOptions => choiceOptions;
}

[Serializable]
public class MainStoryStep
{
    [SerializeField] private string stepId;
    [TextArea(2, 6)]
    [SerializeField] private string description;
    [SerializeField] private string sceneName;
    [SerializeField] private string triggerId;
    [SerializeField] private bool triggerOnSceneLoad = true;
    [SerializeField] private bool requirePrologueDone = false;
    [Header("Optional Story Flag Requirement")]
    [SerializeField] private bool requireStoryFlag;
    [SerializeField] private StoryFlagKey requiredStoryFlag = StoryFlagKey.StarterChosen;
    [SerializeField] private bool requiredStoryFlagValue = true;
    [SerializeField] private bool oneShot = true;
    [SerializeField] private List<MainStoryAction> actions = new();

    public string StepId => stepId;
    public string Description => description;
    public string SceneName => sceneName;
    public string TriggerId => triggerId;
    public bool TriggerOnSceneLoad => triggerOnSceneLoad;
    public bool RequirePrologueDone => requirePrologueDone;
    public bool RequireStoryFlag => requireStoryFlag;
    public StoryFlagKey RequiredStoryFlag => requiredStoryFlag;
    public bool RequiredStoryFlagValue => requiredStoryFlagValue;
    public bool OneShot => oneShot;
    public IReadOnlyList<MainStoryAction> Actions => actions;
}

[CreateAssetMenu(menuName = "Quest/Main Story Sequence")]
public class MainStorySequence : ScriptableObject
{
    [SerializeField] private List<MainStoryStep> steps = new();

    public IReadOnlyList<MainStoryStep> Steps => steps;
}
