using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NPC được tách thành nhiều file partial theo concern:
//   NPC.cs          — fields, properties, lifecycle, luồng tương tác chính
//   NPC.Movement.cs — facing, patrol, các coroutine di chuyển
//   NPC.Actions.cs  — capability check + hành động (shop, quest, storage, heal, battle)
//   NPC.State.cs    — fade away và lưu/khôi phục trạng thái runtime
public partial class NPC : MonoBehaviour, Interactable
{
    [Header("Thông tin NPC")]
    [SerializeField] private string npcId;
    public string npcName;
    [SerializeField] private Sprite portrait; // thêm
    [TextArea] public string introDialog;
    [TextArea] [SerializeField] private string outroDialog;
    public TrainerParty Party;

    [Header("Chức năng (tích chọn những gì NPC có)")]
    [SerializeField] private bool canHeal;
    [SerializeField] private bool canShop;
    [SerializeField] private bool canUseStorage;
    [SerializeField] private bool canGiveQuest;
    [SerializeField] private bool canBattle;

    [Header("Dialog Fade (optional)")]
    [SerializeField] private bool fadeAwayAfterDialog;
    [SerializeField] private float fadeAfterDialogDuration = 0.5f;
    [SerializeField] private bool disableNpcAfterDialogFade = true;

    [Header("Shop Items")]
    [SerializeField] private List<ItemBase> shopItems = new List<ItemBase>();

    [Header("Dialog")]
    [SerializeField] private Dialog dialog;

    [Header("Animation")]
    [SerializeField] private NPCDirectionalAnimator directionalAnimator;

    [Header("Quest")]
    [SerializeField] private Quest quest;
    [SerializeField] private bool giveQuestOnce = true;

    [Header("Battle Rewards")]
    [SerializeField] private int rewardMoney = 20;
    [SerializeField] private bool isGymLeader;
    [SerializeField] private string badgeName;
    [SerializeField] private ItemBase badgeItem;
    [SerializeField] private bool setStoryFlagAfterBadge;
    [SerializeField] private StoryFlagKey storyFlagAfterBadge = StoryFlagKey.AfterGrassGym;
    [SerializeField] private bool storyFlagAfterBadgeValue = true;
    [SerializeField] private bool canBattleOnce = false;

    [Header("Movement")]
    [SerializeField] private bool enableMovement = false;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private bool loopPatrol = true;
    [SerializeField] private bool startPatrolOnAwake = false;
    [SerializeField] private float patrolMoveSpeed = 2f;
    [SerializeField] private float waitAtPatrolPoint = 0.25f;

    [Header("Post Battle Move")]
    [SerializeField] private bool moveAfterBattle = false;
    [SerializeField] private Transform postBattleMoveTarget;
    [SerializeField] private float postBattleMoveSpeed = 2f;
    [SerializeField] private bool faceTargetAfterMove = true;

    [Header("Dialog Move")]
    [SerializeField] private bool moveAfterDialog = false;
    [SerializeField] private Transform dialogMoveTarget;
    [SerializeField] private float dialogMoveSpeed = 2f;
    [SerializeField] private bool faceTargetAfterDialog = true;

    private Rigidbody2D cachedRigidbody;
    private Coroutine patrolRoutine;
    private bool isFadingAway;
    private string runtimeStateKey;

    public string NPCId => npcId;
    public string RuntimeStateKey => runtimeStateKey;
    public Sprite Portrait => portrait;
    public int RewardMoney => rewardMoney;
    public bool IsGymLeader => isGymLeader;
    public string BadgeName => badgeName;
    public ItemBase BadgeItem => badgeItem;
    public bool SetStoryFlagAfterBadge => setStoryFlagAfterBadge;
    public StoryFlagKey StoryFlagAfterBadge => storyFlagAfterBadge;
    public bool StoryFlagAfterBadgeValue => storyFlagAfterBadgeValue;
    public bool CanBattleOnce => canBattleOnce;
    public bool CanBattle
    {
        get => canBattle;
        set
        {
            canBattle = value;
            SaveLoadSystem.RegisterRuntimeNpcBattleState(runtimeStateKey, value);
        }
    }
    public string OutroDialog => outroDialog;

    private void Awake()
    {
        CacheComponents();
        runtimeStateKey = SaveLoadSystem.BuildNpcStateKey(gameObject.scene.name, npcId, transform.position);

        if (SaveLoadSystem.TryGetRuntimeNpcBattleState(runtimeStateKey, out bool savedCanBattle))
            canBattle = savedCanBattle;

        if (SaveLoadSystem.TryGetRuntimeNpcTransformState(runtimeStateKey, out var runtimeState) && runtimeState != null)
        {
            bool sameScene = string.IsNullOrWhiteSpace(runtimeState.sceneName) ||
                             string.Equals(runtimeState.sceneName, gameObject.scene.name, System.StringComparison.OrdinalIgnoreCase);
            if (sameScene)
            {
                transform.position = new Vector3(runtimeState.posX, runtimeState.posY, runtimeState.posZ);
                if (gameObject.activeSelf != runtimeState.isActive)
                    gameObject.SetActive(runtimeState.isActive);
            }
        }
    }

    private void Start()
    {
        ApplyStarterBasedPartyOverride();
        RegisterRuntimeState();

        if (enableMovement && startPatrolOnAwake)
            StartPatrol();
    }

    public static void RefreshStarterBasedTrainerParties()
    {
        var npcs = Object.FindObjectsOfType<NPC>(true);
        foreach (var npc in npcs)
        {
            if (npc != null)
                npc.ApplyStarterBasedPartyOverride();
        }
    }

    private void CacheComponents()
    {
        if (directionalAnimator == null)
            directionalAnimator = GetComponent<NPCDirectionalAnimator>();

        if (cachedRigidbody == null)
            cachedRigidbody = GetComponent<Rigidbody2D>();
    }

    private void ApplyStarterBasedPartyOverride()
    {
        if (Party == null)
            return;

        var flags = StoryFlags.Instance != null ? StoryFlags.Instance : StoryFlags.GetOrCreate();
        if (flags == null || !flags.StarterChosen || string.IsNullOrWhiteSpace(flags.StarterPokemonId))
            return;

        if (IsGreenRival())
        {
            Party.ApplyStarterOverride(flags.StarterPokemonId, true);
        }
        else if (IsBlueRival())
        {
            Party.ApplyStarterOverride(flags.StarterPokemonId, false);
        }
    }

    private bool IsGreenRival()
    {
        return MatchesRivalId("green");
    }

    private bool IsBlueRival()
    {
        return MatchesRivalId("blue");
    }

    private bool MatchesRivalId(string rivalId)
    {
        string normalizedNpcId = TextKeyUtility.NormalizeKey(npcId);
        string normalizedNpcName = TextKeyUtility.NormalizeKey(npcName);
        string normalizedRivalId = TextKeyUtility.NormalizeKey(rivalId);

        return normalizedNpcId == normalizedRivalId || normalizedNpcName == normalizedRivalId;
    }

    public void Interact()
    {
        FacePlayer();

        ShowPrimaryDialog(AfterDialog);
    }

    private void AfterDialog()
    {
        var talkTarget = string.IsNullOrWhiteSpace(npcId) ? npcName : npcId;
        if (!string.IsNullOrWhiteSpace(talkTarget) && QuestManager.Instance != null)
        {
            QuestManager.Instance.SubmitEvent(
                new QuestEvent(QuestEventType.NPCTalked, talkTarget, 1)
            );

            // Tự nộp quest nếu NPC này là turn-in NPC và quest đã xong objective
            var readyQuest = QuestManager.Instance.GetReadyToTurnInQuestForNpc(talkTarget);
            if (readyQuest != null && QuestManager.Instance.TurnInQuest(readyQuest, talkTarget))
            {
                if (ToastNotificationManager.Instance != null)
                    ToastNotificationManager.Instance.Show($"Hoàn thành nhiệm vụ \"{readyQuest.Title}\"!", Color.green);
            }
        }

        if (fadeAwayAfterDialog && !isFadingAway)
        {
            StartCoroutine(FadeAway(fadeAfterDialogDuration, disableNpcAfterDialogFade, true));
            return;
        }

        // Optional: move after dialog (one-off), useful for NPCs that should step aside
        if (moveAfterDialog && dialogMoveTarget != null)
        {
            StopPatrol();
            StartCoroutine(MoveTo(dialogMoveTarget.position, dialogMoveSpeed, faceTargetAfterDialog));
            GameController.Instance?.SetState(GameState.Overworld);
            return;
        }

        var hasOptions = HasBattle() || HasQuest() || HasHealer() || HasShop() || HasStorage();

        if (!hasOptions)
        {
            GameController.Instance?.SetState(GameState.Overworld);
            return;
        }

        if (GameController.Instance != null)
            GameController.Instance.SetState(GameState.NPCInteraction);

        if (OptionUI.Instance != null)
            OptionUI.Instance.ShowOptions(this);
    }

    private void ShowPrimaryDialog(System.Action onFinished)
    {
        var dialogManager = DialogManager.Instance;
        if (dialogManager == null)
        {
            onFinished?.Invoke();
            return;
        }

        bool hasScriptedDialog = dialog != null && dialog.Sentences != null && dialog.Sentences.Count > 0;
        bool hasTextDialog = !string.IsNullOrWhiteSpace(introDialog);
        if (!hasScriptedDialog && !hasTextDialog)
        {
            onFinished?.Invoke();
            return;
        }

        System.Action handler = null;
        handler = () =>
        {
            dialogManager.OnDialogFinished -= handler;
            onFinished?.Invoke();
        };

        dialogManager.OnDialogFinished -= handler;
        dialogManager.OnDialogFinished += handler;

        bool isDefeated = canBattleOnce && !canBattle && !string.IsNullOrWhiteSpace(outroDialog);
        if (isDefeated)
            dialogManager.ShowDialog(npcName, portrait, outroDialog, GameState.Overworld);
        else if (hasScriptedDialog)
            dialogManager.ShowDialog(npcName, portrait, dialog, GameState.Overworld);
        else
            dialogManager.ShowDialog(npcName, portrait, introDialog, GameState.Overworld);
    }

    private void FinishInteraction()
    {
        if (GameController.Instance != null)
            GameController.Instance.SetState(GameState.Overworld);
    }
}
