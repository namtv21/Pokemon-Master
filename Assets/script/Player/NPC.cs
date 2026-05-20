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
    public TrainerParty Party;

    [Header("Chức năng (tích chọn những gì NPC có)")]
    [SerializeField] private bool canHeal;
    [SerializeField] private bool canShop;
    [SerializeField] private bool canUseStorage;
    [SerializeField] private bool canGiveQuest;
    [SerializeField] private bool canBattle;

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
    [SerializeField] private bool canBattleOnce = false;

    private Rigidbody2D cachedRigidbody;

    public string NPCId => npcId;
    public Sprite Portrait => portrait;
    public int RewardMoney => rewardMoney;
    public bool IsGymLeader => isGymLeader;
    public string BadgeName => badgeName;
    public ItemBase BadgeItem => badgeItem;
    public bool CanBattleOnce => canBattleOnce;
    public bool CanBattle { get => canBattle; set => canBattle = value; }

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (directionalAnimator == null)
            directionalAnimator = GetComponent<NPCDirectionalAnimator>();

        if (cachedRigidbody == null)
            cachedRigidbody = GetComponent<Rigidbody2D>();
    }

    public void Interact()
    {
        FacePlayer();

        if (DialogManager.Instance == null) return;

        DialogManager.Instance.OnDialogFinished -= AfterDialog;
        DialogManager.Instance.OnDialogFinished += AfterDialog;

        if (dialog != null)
            DialogManager.Instance.ShowDialog(npcName, portrait, dialog); // đổi
        else
            DialogManager.Instance.ShowDialog(npcName, portrait, introDialog); // đổi
    }

    private void AfterDialog()
    {
        if (DialogManager.Instance != null)
            DialogManager.Instance.OnDialogFinished -= AfterDialog;

        var talkTarget = string.IsNullOrWhiteSpace(npcId) ? npcName : npcId;
        if (!string.IsNullOrWhiteSpace(talkTarget))
        {
            QuestManager.Instance?.SubmitEvent(
                new QuestEvent(QuestEventType.NPCTalked, talkTarget, 1)
            );
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

        if (directionalAnimator != null)
            directionalAnimator.SetMoving(true, target - current);

        while ((target - current).sqrMagnitude > 0.0001f)
        {
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
                directionalAnimator.SetFacing(target - current, true);
            else
                directionalAnimator.SetMoving(false, target - current);
        }
    }

    private IEnumerator MoveTransformTo(Vector3 targetPosition, float moveSpeed, bool faceTargetOnArrive)
    {
        Vector3 current = transform.position;
        Vector2 direction = targetPosition - current;

        if (directionalAnimator != null)
            directionalAnimator.SetMoving(true, direction);

        while ((targetPosition - current).sqrMagnitude > 0.0001f)
        {
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
                directionalAnimator.SetFacing(direction, true);
            else
                directionalAnimator.SetMoving(false, direction);
        }
    }

    // ===== Kiểm tra NPC có chức năng nào =====
    public bool HasHealer() => canHeal;
    public bool HasShop() => canShop;
    public bool HasStorage() => canUseStorage;
    public bool HasQuest()
    {
        if (!canGiveQuest || quest == null)
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

    private void ShowDialogThenReturnToOverworld(string message)
    {
        // đổi sang toast
        ToastNotificationManager.Instance?.Show(message);
        FinishInteraction();
    }

    public void GiveQuest()
    {
        if (!canGiveQuest) return;

        if (quest != null && quest.Category == QuestCategory.MainStory)
        {
            ShowDialogThenReturnToOverworld("Nhiem vu chinh duoc kich hoat tu dong theo cot truyen.");
            return;
        }

        if (quest == null)
        {
            ShowDialogThenReturnToOverworld("Tôi không có nhiệm vụ cho bạn.");
            return;
        }

        var qm = QuestManager.Instance;
        if (qm == null)
        {
            ShowDialogThenReturnToOverworld("Quest system is unavailable.");
            return;
        }

        // 1) Ưu tiên trả nhiệm vụ nếu đủ điều kiện
        if (qm.CanTurnInQuest(quest, npcId))
        {
            bool turnedIn = qm.TurnInQuest(quest, npcId);
            ShowDialogThenReturnToOverworld(
                turnedIn
                    ? $"Đã hoàn thành nhiệm vụ: {quest.Title}"
                    : "Không thể trả nhiệm vụ."
            );
            return;
        }

        // 2) Nếu đang active mà chưa thể turn-in => chưa hoàn thành
        if (qm.IsQuestActive(quest))
        {
            ShowDialogThenReturnToOverworld("Nhiệm vụ chưa hoàn thành.");
            return;
        }

        // 3) Nếu không thể nhận nữa (đã làm xong trước đó / once-only)
        if (!qm.CanAcceptQuest(quest, giveQuestOnce))
        {
            ShowDialogThenReturnToOverworld("Bạn đã làm nhiệm vụ này rồi.");
            return;
        }

        // 4) Hiển thị confirm nhận quest
        if (QuestInfoUI.Instance == null)
        {
            // fallback: nhận trực tiếp nếu thiếu UI confirm
            bool addedDirect = qm.AddQuest(quest, giveQuestOnce);
            ShowDialogThenReturnToOverworld(
                addedDirect
                    ? $"Đã nhận nhiệm vụ: {quest.Title}"
                    : "Không thể nhận nhiệm vụ này."
            );
            return;
        }

        GameController.Instance?.SetState(GameState.NPCInteraction);

        QuestInfoUI.Instance.ShowQuestConfirm(
            quest,
            accept: () =>
            {
                bool added = qm.AddQuest(quest, giveQuestOnce);
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

    private void ReturnToOverworld()
    {
        FinishInteraction();
    }

    public void OpenStorageSendMenu()
    {
        if (!canUseStorage) return;

        var gc = GameController.Instance;
        var partyMenu = ResolvePartyMenu();
        if (gc == null || gc.PlayerParty == null || partyMenu == null)
        {
            ResolveToast()?.Show("Storage system is unavailable.", Color.yellow);
            FinishInteraction();
            return;
        }

        gc.SetState(GameState.Storage);

        partyMenu.Open(
            gc.PlayerParty.Pokemons,
            PartyMenuMode.Selection,
            onSelected: (pokemon) =>
            {
                partyMenu.Close();
                SendPokemonToStorage(pokemon);
            },
            onCancel: () =>
            {
                partyMenu.Close();
                FinishInteraction();
            }
        );
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
}