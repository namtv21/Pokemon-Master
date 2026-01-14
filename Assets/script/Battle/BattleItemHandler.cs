using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BattleItemHandler : MonoBehaviour
{
    [SerializeField] private BattleDialogBox dialogBox;
    [SerializeField] private GameObject battleUI;
    [SerializeField] private Inventory inventory;
    [SerializeField] private StorageSystem storage;

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
        battleSystem.SetState(BattleState.Busy);

        // kiểm tra số lượng item
        ItemSlot slot = inventory.GetSlots().Find(s => s.item == item);
        if (slot == null || slot.count <= 0)
        {
            dialogBox.ShowDialog($"You don't have any {item.itemName}.");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        // xử lý Pokéball riêng
        if (!isWildBattle && item.itemType == ItemType.Pokeball)
        {
            dialogBox.ShowDialog("You can't catch a Trainer's Pokémon!");
            yield return new WaitForSeconds(1.2f);

            // 👉 Quay lại PlayerActionSelection
            battleSystem.PlayerAction();
            yield break;
        }
        else if (item.itemType == ItemType.Pokeball)
        {
            yield return AttemptCatch(item, wildPokemon);
            yield break;
        }


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
                    targetPokemon.CureStatus();
                    msg = $"{targetPokemon.Base.Name}'s status was cured!";
                    success = true;
                    if (targetUnit != null) targetUnit.UpdateHud();
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
                break;

            case ItemType.KeyItem:
                msg = $"{item.itemName} can't be used in battle.";
                success = false;
                break;
        }

        dialogBox.ShowDialog(msg);
        yield return new WaitForSeconds(1.2f);

        if (success && item.consumable)
            inventory.RemoveItem(item, 1);

        yield return ProceedAfterPlayerAction();
    }

    private IEnumerator AttemptCatch(ItemBase ball, Pokemon target)
    {
        dialogBox.ShowDialog($"You used {ball.itemName}!");
        yield return new WaitForSeconds(0.8f);
        if (ball.consumable)
            inventory.RemoveItem(ball, 1);

        var enemyUnit = battleSystem.EnemyUnit; // cần tham chiếu từ Init
        enemyUnit.SetSprite(ball.icon);   // ballSprite là Sprite của Pokéball

        yield return new WaitForSeconds(1f);


        float hpFactor = Mathf.Clamp01(1f - (target.CurrentHp / (float)target.MaxHp));
        float baseRate = 1f;
        float ballMod = ball.catchRateMultiplier;
        float chance = Mathf.Clamp01(baseRate * (0.3f + hpFactor * 0.7f) * ballMod);

        if (Random.value < chance)
        {
            dialogBox.ShowDialog($"Gotcha! {target.Base.Name} was caught!");
            yield return new WaitForSeconds(1.4f);
            CaptureSuccess(target);
        }
        else
        {
            dialogBox.ShowDialog($"{target.Base.Name} broke free!");
            yield return new WaitForSeconds(1f);
            enemyUnit.Setup(target);
            yield return ProceedAfterPlayerAction();
        }
    }

    private void CaptureSuccess(Pokemon target)
    {
        var playerParty = FindObjectOfType<PlayerParty>();
        if (playerParty.Pokemons.Count < 6)
        {
            playerParty.Pokemons.Add(target.CloneAsOwned());
            dialogBox.ShowDialog($"{target.Base.Name} was added to your party!");
        }
        else
        {
            storage.AddPokemon(target.CloneAsOwned());
            dialogBox.ShowDialog($"{target.Base.Name} was sent to the Box!");
        }
        EndBattleWithCapture();
    }

    private void EndBattleWithCapture()
    {
        battleSystem.SetState(BattleState.BattleOver);
        GameController.Instance.EndBattle();
        battleUI.SetActive(false);
    }

    private IEnumerator ProceedAfterPlayerAction()
    {
        yield return new WaitForSeconds(0.5f);
        battleSystem.ProceedTurn();
    }

}