using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour, Interactable
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

    public void FacePlayer()
    {
        var player = PlayerController.Instance != null ? PlayerController.Instance.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            return;

        FaceDirection(player.position - transform.position, true);
    }

    public void FaceDirection(Vector2 worldDirection, bool idle = true)
    {
        CacheComponents();

        if (directionalAnimator != null)
            directionalAnimator.SetFacing(worldDirection, idle);
    }

    public void StartPatrol()
    {
        if (!enableMovement)
            return;

        if (patrolRoutine != null)
            StopCoroutine(patrolRoutine);

        patrolRoutine = StartCoroutine(PatrolRoutine());
    }

    public void StopPatrol()
    {
        if (patrolRoutine == null)
            return;

        StopCoroutine(patrolRoutine);
        patrolRoutine = null;
    }

    private IEnumerator PatrolRoutine()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            yield break;

        do
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var point = patrolPoints[i];
                if (point == null)
                    continue;

                yield return MoveTo(point.position, patrolMoveSpeed, true);

                if (waitAtPatrolPoint > 0f)
                    yield return new WaitForSeconds(waitAtPatrolPoint);
            }
        }
        while (loopPatrol);

        patrolRoutine = null;
    }

    public IEnumerator MoveTo(Vector3 targetPosition, float moveSpeed, bool faceTargetOnArrive)
    {
        CacheComponents();

        if (cachedRigidbody == null)
        {
            yield return MoveTransformTo(targetPosition, moveSpeed, faceTargetOnArrive);
            yield break;
        }

        Vector2 current = cachedRigidbody.position;
        Vector2 target = targetPosition;
        Vector2 lastDirection = target - current;

        // Register target position immediately so scene reload lands NPC at destination
        if (!string.IsNullOrWhiteSpace(npcId) && !string.IsNullOrWhiteSpace(runtimeStateKey))
            SaveLoadSystem.RegisterRuntimeNpcTransformState(runtimeStateKey, npcId, gameObject.scene.name, targetPosition, gameObject.activeSelf);

        if (directionalAnimator != null)
            directionalAnimator.SetMoving(true, lastDirection);

        while ((target - current).sqrMagnitude > 0.0001f)
        {
            while (GameController.Instance != null &&
                   GameController.Instance.State != GameState.Overworld &&
                   GameController.Instance.State != GameState.Cutscene)
            {
                if (directionalAnimator != null)
                    directionalAnimator.SetMoving(false, target - current);

                yield return null;
            }

            lastDirection = target - current;
            Vector2 next = Vector2.MoveTowards(current, target, Mathf.Max(0.01f, moveSpeed) * Time.fixedDeltaTime);
            cachedRigidbody.MovePosition(next);
            current = next;

            if (directionalAnimator != null)
                directionalAnimator.SetMoving(true, target - current);

            yield return new WaitForFixedUpdate();
        }

        cachedRigidbody.MovePosition(target);

        if (directionalAnimator != null)
        {
            if (faceTargetOnArrive)
                directionalAnimator.SetFacing(lastDirection, true);
            else
                directionalAnimator.SetMoving(false, lastDirection);
        }

        RegisterRuntimeState();
    }

    private IEnumerator MoveTransformTo(Vector3 targetPosition, float moveSpeed, bool faceTargetOnArrive)
    {
        Vector3 current = transform.position;
        Vector2 lastDirection = targetPosition - current;

        if (!string.IsNullOrWhiteSpace(npcId) && !string.IsNullOrWhiteSpace(runtimeStateKey))
            SaveLoadSystem.RegisterRuntimeNpcTransformState(runtimeStateKey, npcId, gameObject.scene.name, targetPosition, gameObject.activeSelf);

        if (directionalAnimator != null)
            directionalAnimator.SetMoving(true, lastDirection);

        while ((targetPosition - current).sqrMagnitude > 0.0001f)
        {
            while (GameController.Instance != null &&
                   GameController.Instance.State != GameState.Overworld &&
                   GameController.Instance.State != GameState.Cutscene)
            {
                if (directionalAnimator != null)
                    directionalAnimator.SetMoving(false, targetPosition - current);

                yield return null;
            }

            lastDirection = targetPosition - current;
            current = Vector3.MoveTowards(current, targetPosition, Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);
            transform.position = current;

            if (directionalAnimator != null)
                directionalAnimator.SetMoving(true, targetPosition - current);

            yield return null;
        }

        transform.position = targetPosition;

        if (directionalAnimator != null)
        {
            if (faceTargetOnArrive)
                directionalAnimator.SetFacing(lastDirection, true);
            else
                directionalAnimator.SetMoving(false, lastDirection);
        }

        RegisterRuntimeState();
    }

    // ===== Kiểm tra NPC có chức năng nào =====
    public bool HasHealer() => canHeal;
    public bool HasShop() => canShop;
    public bool HasStorage() => canUseStorage;
    public bool HasQuest()
    {
        if (!canGiveQuest)
            return false;

        var qm = QuestManager.Instance;
        if (qm != null && qm.GetReadyToTurnInQuestForNpc(npcId, quest) != null)
            return true;

        if (quest == null)
            return false;

        // Main story quest is driven by quest auto trigger/director, not manual NPC option.
        return quest.Category != QuestCategory.MainStory;
    }
    public bool HasBattle() => canBattle;

    // ===== Các hành động NPC có thể làm =====

    private ToastNotificationManager ResolveToast()
    {
        return ToastNotificationManager.Instance != null
            ? ToastNotificationManager.Instance
            : Object.FindObjectOfType<ToastNotificationManager>(true);
    }

    private PartyMenuUI ResolvePartyMenu()
    {
        return PartyMenuUI.Instance != null
            ? PartyMenuUI.Instance
            : Object.FindObjectOfType<PartyMenuUI>(true);
    }

    private ShopUI ResolveShopUI()
    {
        return ShopUI.Instance != null
            ? ShopUI.Instance
            : Object.FindObjectOfType<ShopUI>(true);
    }

    private StorageSystem ResolveStorageSystem()
    {
        if (GameController.Instance != null && GameController.Instance.StorageSystem != null)
            return GameController.Instance.StorageSystem;

        return StorageSystem.Instance != null
            ? StorageSystem.Instance
            : Object.FindObjectOfType<StorageSystem>(true);
    }

    public void OpenShop()
    {
        if (!canShop)
        {
            return;
        }

        var shopUi = ResolveShopUI();
        if (shopUi == null || GameController.Instance == null)
        {
            ResolveToast()?.Show("Shop system is unavailable.", Color.yellow);
            return;
        }

        shopUi.Open(shopItems);
        GameController.Instance.SetState(GameState.Shop);
    }

    public string ShowPokemon()
    {
        if (Party == null)
            return $"{npcName} has no Pokémon.";

        List<Pokemon> pokemons = Party.GetPokemons();
        if (pokemons == null || pokemons.Count == 0)
            return $"{npcName} has no Pokémon.";

        string info = $"{npcName}'s Pokémon:\n";

        for (int i = 0; i < pokemons.Count; i++)
        {
            var p = pokemons[i];
            string pName = (p != null && p.Base != null) ? p.Base.Name : "Unknown";
            int pLevel = (p != null) ? p.Level : 0;

            info += $"{pName} (Lv {pLevel})";

            if (i % 3 != 2 && i != pokemons.Count - 1)
                info += ", ";

            if (i % 3 == 2 || i == pokemons.Count - 1)
                info += "\n";
        }

        return info;
    }

    public void StartBattle()
    {
        if (!canBattle)
        {
            return;
        }

        if (GameController.Instance == null)
        {
            Debug.LogWarning("GameController.Instance is null.");
            return;
        }

        GameController.Instance.StartTrainerBattle(this);
    }

    public void OnBattleEnded()
    {
        if (canBattleOnce)
            CanBattle = false;

        if (!string.IsNullOrWhiteSpace(outroDialog))
        {
            var dialogManager = DialogManager.Instance;
            if (dialogManager == null)
            {
                ResolveToast()?.Show(outroDialog, Color.white);
            }
            else
            {
                dialogManager.ShowDialog(npcName, portrait, outroDialog, GameState.Overworld);
            }
        }

        RegisterRuntimeState();

        if (canBattleOnce && !canBattle)
        {
            var saveLoadSys = FindObjectOfType<SaveLoadSystem>();
            if (saveLoadSys != null)
                saveLoadSys.Save("AutoSave");
        }

        if (moveAfterBattle && postBattleMoveTarget != null)
        {
            StopPatrol();
            var dm = DialogManager.Instance;
            if (dm != null && dm.IsShowing)
                StartCoroutine(MoveAfterDialogFinished(postBattleMoveTarget.position, postBattleMoveSpeed, faceTargetAfterMove));
            else
                StartCoroutine(MoveTo(postBattleMoveTarget.position, postBattleMoveSpeed, faceTargetAfterMove));
        }
    }

    private IEnumerator MoveAfterDialogFinished(Vector3 targetPos, float speed, bool faceOnArrive)
    {
        yield return new WaitUntil(() => DialogManager.Instance == null || !DialogManager.Instance.IsShowing);
        StartCoroutine(MoveTo(targetPos, speed, faceOnArrive));
    }

    private void ShowDialogThenReturnToOverworld(string message)
    {
        // đổi sang toast
        ToastNotificationManager.Instance?.Show(message);
        FinishInteraction();
    }

    public void GiveQuest()
    {
        if (!canGiveQuest)
            return;

        var questManager = QuestManager.Instance;
        var targetQuest = questManager != null
            ? questManager.GetReadyToTurnInQuestForNpc(npcId, quest) ?? quest
            : quest;

        if (targetQuest != null && targetQuest.Category == QuestCategory.MainStory)
        {
            ShowDialogThenReturnToOverworld("Main story quests are handled automatically.");
            return;
        }

        if (targetQuest == null)
        {
            ShowDialogThenReturnToOverworld("I do not have a quest for you.");
            return;
        }

        if (questManager == null)
        {
            ShowDialogThenReturnToOverworld("Quest system is unavailable.");
            return;
        }

        if (questManager.CanTurnInQuest(targetQuest, npcId))
        {
            bool turnedIn = questManager.TurnInQuest(targetQuest, npcId);
            ShowDialogThenReturnToOverworld(
                turnedIn
                    ? $"Quest completed: {targetQuest.Title}"
                    : "Cannot turn in this quest right now."
            );
            return;
        }

        if (questManager.IsQuestActive(targetQuest))
        {
            ShowDialogThenReturnToOverworld("This quest is not complete yet.");
            return;
        }

        if (!questManager.CanAcceptQuest(targetQuest, giveQuestOnce))
        {
            ShowDialogThenReturnToOverworld("You have already completed this quest.");
            return;
        }

        bool added = questManager.AddQuest(targetQuest, giveQuestOnce);
        ShowDialogThenReturnToOverworld(
            added
                ? $"Da nhan nhiem vu: {targetQuest.Title}"
                : "Khong the nhan nhiem vu nay."
        );
        return;
/*
        {
        var qm = QuestManager.Instance;
        if (!canGiveQuest) return;
        var targetQuest = qm != null ? qm.GetReadyToTurnInQuestForNpc(npcId, quest) ?? quest : quest;

        if (targetQuest != null && targetQuest.Category == QuestCategory.MainStory)
        {
            ShowDialogThenReturnToOverworld("Nhiem vu chinh duoc kich hoat tu dong theo cot truyen.");
            return;
        }

        if (targetQuest == null)
        {
            ShowDialogThenReturnToOverworld("Tôi không có nhiệm vụ cho bạn.");
            return;
        }

        if (qm == null)
        {
            ShowDialogThenReturnToOverworld("Quest system is unavailable.");
            return;
        }

        // 1) Ưu tiên trả nhiệm vụ nếu đủ điều kiện
        if (qm.CanTurnInQuest(targetQuest, npcId))
        {
            bool turnedIn = qm.TurnInQuest(targetQuest, npcId);
            ShowDialogThenReturnToOverworld(
                turnedIn
                    ? $"Đã hoàn thành nhiệm vụ: {quest.Title}"
                    : "Không thể trả nhiệm vụ."
            );
            return;
        }

        // 2) Nếu đang active mà chưa thể turn-in => chưa hoàn thành
        if (qm.IsQuestActive(targetQuest))
        {
            ShowDialogThenReturnToOverworld("Nhiệm vụ chưa hoàn thành.");
            return;
        }

        // 3) Nếu không thể nhận nữa (đã làm xong trước đó / once-only)
        if (!qm.CanAcceptQuest(targetQuest, giveQuestOnce))
        {
            ShowDialogThenReturnToOverworld("Bạn đã làm nhiệm vụ này rồi.");
            return;
        }

        // 4) Hiển thị confirm nhận quest
        if (QuestInfoUI.Instance == null)
        {
            // fallback: nhận trực tiếp nếu thiếu UI confirm
            bool addedDirect = qm.AddQuest(targetQuest, giveQuestOnce);
            ShowDialogThenReturnToOverworld(
                addedDirect
                    ? $"Đã nhận nhiệm vụ: {quest.Title}"
                    : "Không thể nhận nhiệm vụ này."
            );
            return;
        }

        GameController.Instance?.SetState(GameState.NPCInteraction);

        QuestInfoUI.Instance.ShowQuestConfirm(
            targetQuest,
            accept: () =>
            {
                bool added = qm.AddQuest(targetQuest, giveQuestOnce);
                ShowDialogThenReturnToOverworld(
                    added
                        ? $"Đã nhận nhiệm vụ: {quest.Title}"
                        : "Không thể nhận nhiệm vụ này."
                );
            },
            decline: () =>
            {
                ShowDialogThenReturnToOverworld("Đã từ chối nhiệm vụ.");
            }
        );
        }
*/
    }

    private void ReturnToOverworld()
    {
        FinishInteraction();
    }

    public void OpenStorageSendMenu()
    {
        if (!canUseStorage) return;

        var gc = GameController.Instance;
        var storage = StorageSystem.Instance;
        if (gc == null || storage == null)
        {
            ResolveToast()?.Show("Storage system is unavailable.", Color.yellow);
            FinishInteraction();
            return;
        }

        gc.SetState(GameState.Storage);
        storage.OpenStorage();
    }

    public void SendPokemonToStorage(Pokemon pokemon)
    {
        if (!canUseStorage) return;

        var gc = GameController.Instance;
        if (gc == null) return;

        var playerParty = gc.PlayerParty;
        var storage = ResolveStorageSystem();
        var pokemonName = (pokemon != null && pokemon.Base != null) ? pokemon.Base.Name : "This Pokémon";

        if (pokemon == null || playerParty == null || storage == null)
        {
            ResolveToast()?.Show("Storage system is unavailable.", Color.yellow);
            FinishInteraction();
            return;
        }

        if (!playerParty.Pokemons.Contains(pokemon))
        {
            ResolveToast()?.Show($"{pokemonName} is not in your party.", Color.yellow);
            FinishInteraction();
            return;
        }

        if (playerParty.Pokemons.Count <= 1)
        {
            ResolveToast()?.Show("You cannot send your last Pokémon to storage!", Color.yellow);
            FinishInteraction();
            return;
        }

        playerParty.RemovePokemon(pokemon);
        storage.AddPokemon(pokemon);
        ResolveToast()?.Show($"{pokemonName} was sent to storage!");
        FinishInteraction();
    }

    public void HealPlayerPokemon()
    {
        if (!canHeal) return;

        var gc = GameController.Instance;
        if (gc == null || gc.PlayerParty == null) return;

        gc.PlayerParty.HealAll();
        ResolveToast()?.Show("Your Pokémon have been fully healed!");
        FinishInteraction();
    }

    public IEnumerator FadeAway(float duration, bool disableNpcAfterFade, bool returnToOverworldWhenDone)
    {
        if (isFadingAway)
            yield break;

        isFadingAway = true;
        StopPatrol();

        var colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        var originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                originalColors[i] = renderers[i].color;
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null)
                    continue;

                var c = originalColors[i];
                c.a = Mathf.Lerp(originalColors[i].a, 0f, t);
                r.color = c;
            }

            yield return null;
        }

        isFadingAway = false;

        if (disableNpcAfterFade)
        {
            RegisterRuntimeState();
            gameObject.SetActive(false);
            yield break;
        }

        RegisterRuntimeState();

        if (returnToOverworldWhenDone)
            FinishInteraction();
    }

    private void RegisterRuntimeState()
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return;

        if (string.IsNullOrWhiteSpace(runtimeStateKey))
            runtimeStateKey = SaveLoadSystem.BuildNpcStateKey(gameObject.scene.name, npcId, transform.position);

        SaveLoadSystem.RegisterRuntimeNpcBattleState(runtimeStateKey, canBattle);
        SaveLoadSystem.RegisterRuntimeNpcTransformState(runtimeStateKey, npcId, gameObject.scene.name, transform.position, gameObject.activeSelf);
    }
}
