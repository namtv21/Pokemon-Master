using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleItemHandler : MonoBehaviour
{
    [SerializeField] private BattleDialogBox dialogBox;
    [SerializeField] private GameObject battleUI;
    private Inventory inventory;

    private bool isWildBattle;
    private Pokemon wildPokemon;
    private BattleSystem battleSystem;

    public void Init(BattleDialogBox dialog, GameObject ui, Inventory inv, bool wild, Pokemon wildTarget, BattleSystem system)
    {
        dialogBox = dialog;
        battleUI = ui;
        inventory = inv;
        isWildBattle = wild;
        wildPokemon = wildTarget;
        battleSystem = system;
    }

    public IEnumerator UseItemOnPokemon(ItemBase item, Pokemon targetPokemon, BattleUnit targetUnit = null)
    {
        // kiểm tra số lượng item
        ItemSlot slot = inventory.GetSlots().Find(s => s.item == item);
        if (slot == null || slot.count <= 0)
        {
            battleSystem.SetState(BattleState.Busy);
            if (dialogBox != null)
                dialogBox.ShowDialog($"You don't have any {item.itemName}.");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        // xử lý Pokéball riêng
        if (!isWildBattle && item.itemType == ItemType.Pokeball)
        {
            battleSystem.SetState(BattleState.Busy);
            dialogBox.ShowDialog("You can't catch a Trainer's Pokémon!");
            yield return new WaitForSeconds(1.2f);
            battleSystem.PlayerAction();
            yield break;
        }
        else if (item.itemType == ItemType.Pokeball)
        {
            battleSystem.SetState(BattleState.Busy);
            yield return AttemptCatch(item, wildPokemon);
            yield break;
        }

        // Key item không dùng được trong chiến đấu
        if (item.itemType == ItemType.KeyItem)
        {
            dialogBox.ShowDialog($"{item.itemName} can't be used in battle.");
            yield return new WaitForSeconds(1.2f);
            battleSystem.PlayerAction();
            yield break;
        }

        battleSystem.SetState(BattleState.Busy);


        string msg = "";
        bool success = false;

        switch (item.itemType)
        {
            case ItemType.Healing:
                if (item.healToFull)
                {
                    targetPokemon.FullHeal();
                    msg = $"{targetPokemon.Base.Name}'s HP was fully restored!";
                    success = true;
                }
                else if (item.healAmount > 0)
                {
                    int prevHp = targetPokemon.CurrentHp;
                    targetPokemon.Heal(item.healAmount);
                    int healed = targetPokemon.CurrentHp - prevHp;

                    msg = healed > 0
                        ? $"{targetPokemon.Base.Name} recovered {healed} HP!"
                        : $"{targetPokemon.Base.Name}'s HP is already full!";
                    success = healed > 0;
                }

                if (targetUnit != null) targetUnit.UpdateHp();
                break;

            case ItemType.Revive:
                if (targetPokemon.IsFainted)
                {
                    targetPokemon.Revive(item.revivePercent);
                    msg = $"{targetPokemon.Base.Name} was revived!";
                    success = true;
                    if (targetUnit != null) targetUnit.UpdateHp();
                }
                else
                {
                    msg = $"{targetPokemon.Base.Name} is not fainted!";
                }
                break;

            case ItemType.StatusHeal:
                if (item.curesAllStatus)
                {
                    if (targetPokemon.Status != StatusEffect.None)
                    {
                        targetPokemon.CureStatus();
                        msg = $"{targetPokemon.Base.Name}'s status was cured!";
                        success = true;
                        if (targetUnit != null) targetUnit.UpdateHud();
                    }
                    else
                    {
                        msg = $"{targetPokemon.Base.Name} doesn't have a status condition!";
                    }
                }
                else if (item.curesSpecific != null && item.curesSpecific.Length > 0)
                {
                    foreach (var eff in item.curesSpecific)
                    {
                        if (targetPokemon.Status == eff)
                        {
                            targetPokemon.CureStatus();
                            msg = $"{targetPokemon.Base.Name}'s {eff} was cured!";
                            success = true;
                            if (targetUnit != null) targetUnit.UpdateHud();
                            break;
                        }
                    }

                    if (!success) msg = $"{item.itemName} had no effect.";
                }
                else
                {
                    msg = $"{item.itemName} had no effect.";
                }
                break;

        }

        if (dialogBox != null)
            dialogBox.ShowDialog(msg);
        yield return new WaitForSeconds(1.2f);

        if (success)
        {
            if (item.consumable)
                inventory.RemoveItem(item, 1);
            // Dùng item có tác dụng = tiêu 1 lượt → chuyển sang lượt địch.
            yield return ProceedAfterPlayerAction();
        }
        else
        {
            // Item không có tác dụng (đầy máu, chưa ngất, không có trạng thái…) → KHÔNG tiêu lượt,
            // trả người chơi về màn chọn hành động để chọn lại (giống nhánh KeyItem/ball-vào-trainer).
            battleSystem.PlayerAction();
        }
    }

    private IEnumerator AttemptCatch(ItemBase ball, Pokemon target)
    {
        dialogBox?.ShowDialog($"You used {ball.itemName}!");
        yield return new WaitForSeconds(0.6f);
        if (ball.consumable)
            inventory.RemoveItem(ball, 1);

        var enemyUnit = battleSystem.EnemyUnit;
        enemyUnit.SetSprite(ball.icon);
        yield return new WaitForSeconds(0.4f);

        // Tính kết quả trước để quyết định số lần rung
        float hpFactor = Mathf.Clamp01(1f - (target.CurrentHp / (float)target.MaxHp));
        float chance   = Mathf.Clamp01((0.3f + hpFactor * 0.7f) * ball.catchRateMultiplier);
        bool caught    = Random.value < chance;

        // Số rung: 3 lần = bắt được, 1-2 lần = thoát (gần hay xa tuỳ chance)
        int shakes = caught ? 3 : (chance >= 0.5f ? 2 : 1);
        yield return StartCoroutine(enemyUnit.PlayShakeBallAnimation(shakes));

        if (caught)
        {
            dialogBox.ShowDialog($"Gotcha! {target.Base.Name} was caught!");
            yield return new WaitForSeconds(1.4f);
            CaptureSuccess(target);
        }
        else
        {
            // Flash sáng → hiện lại Pokemon
            yield return StartCoroutine(enemyUnit.PlayBreakFreeFlash());
            enemyUnit.Setup(target);
            enemyUnit.ShowSprite();
            dialogBox.ShowDialog($"{target.Base.Name} broke free!");
            yield return new WaitForSeconds(1f);
            yield return ProceedAfterPlayerAction();
        }
    }

    private void CaptureSuccess(Pokemon target)
    {
        GameController.Instance?.TryReceivePokemon(target);
        EndBattleWithCapture();
    }

    private void EndBattleWithCapture()
    {
        battleSystem.SetState(BattleState.BattleOver);
        battleSystem.SetBattleOutcome(BattleOutcome.Capture);
        GameController.Instance?.NotifyActiveOverworldPokemonCaptured();
        GameController.Instance.EndBattle();
        battleUI.SetActive(false);
    }

    private IEnumerator ProceedAfterPlayerAction()
    {
        yield return new WaitForSeconds(0.5f);
        battleSystem.ProceedTurn();
    }

}
