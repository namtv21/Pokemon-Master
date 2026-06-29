using System.Collections.Generic;
using UnityEngine;

// Capability check (Has*) và các hành động NPC có thể thực hiện:
// shop, hiển thị party, trainer battle, quest, storage, heal.
public partial class NPC
{
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
            ResolveToast()?.Show("Hệ thống cửa hàng không khả dụng.", Color.yellow);
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
            ShowDialogThenReturnToOverworld("Nhiệm vụ chính được kích hoạt tự động theo cốt truyện.");
            return;
        }

        if (targetQuest == null)
        {
            ShowDialogThenReturnToOverworld("Tôi không có nhiệm vụ cho bạn.");
            return;
        }

        if (questManager == null)
        {
            ShowDialogThenReturnToOverworld("Hệ thống nhiệm vụ không khả dụng.");
            return;
        }

        if (questManager.CanTurnInQuest(targetQuest, npcId))
        {
            bool turnedIn = questManager.TurnInQuest(targetQuest, npcId);
            ShowDialogThenReturnToOverworld(
                turnedIn
                    ? $"Hoàn thành nhiệm vụ: {targetQuest.Title}"
                    : "Không thể nộp nhiệm vụ lúc này."
            );
            return;
        }

        if (questManager.IsQuestActive(targetQuest))
        {
            ShowDialogThenReturnToOverworld("Nhiệm vụ chưa hoàn thành.");
            return;
        }

        if (!questManager.CanAcceptQuest(targetQuest, giveQuestOnce))
        {
            ShowDialogThenReturnToOverworld("Bạn đã hoàn thành nhiệm vụ này rồi.");
            return;
        }

        bool added = questManager.AddQuest(targetQuest, giveQuestOnce);
        ShowDialogThenReturnToOverworld(
            added
                ? $"Đã nhận nhiệm vụ: {targetQuest.Title}"
                : "Không thể nhận nhiệm vụ này."
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
            ResolveToast()?.Show("Hệ thống kho không khả dụng.", Color.yellow);
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
            ResolveToast()?.Show("Hệ thống kho không khả dụng.", Color.yellow);
            FinishInteraction();
            return;
        }

        if (!playerParty.Pokemons.Contains(pokemon))
        {
            ResolveToast()?.Show($"{pokemonName} không có trong đội của bạn.", Color.yellow);
            FinishInteraction();
            return;
        }

        if (playerParty.Pokemons.Count <= 1)
        {
            ResolveToast()?.Show("Không thể gửi Pokemon cuối cùng vào kho!", Color.yellow);
            FinishInteraction();
            return;
        }

        playerParty.RemovePokemon(pokemon);
        storage.AddPokemon(pokemon);
        ResolveToast()?.Show($"{pokemonName} đã được gửi vào kho!");
        FinishInteraction();
    }

    public void HealPlayerPokemon()
    {
        if (!canHeal) return;

        var gc = GameController.Instance;
        if (gc == null || gc.PlayerParty == null) return;

        gc.PlayerParty.HealAll();
        ResolveToast()?.Show("Pokemon của bạn đã được hồi phục hoàn toàn!");
        FinishInteraction();
    }
}
