using System.Collections;
using UnityEngine;

public class ItemHandler : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    // [SerializeField] private DialogManager dialogBox; // bỏ

    public IEnumerator UseItemOnPokemon(ItemBase item, Pokemon targetPokemon)
    {
        yield return UseItemOnPokemon(item, targetPokemon, -1);
    }

    public IEnumerator UseItemOnPokemon(ItemBase item, Pokemon targetPokemon, int requestedExpAmount)
    {
        // kiểm tra số lượng item
        ItemSlot slot = inventory.GetSlots().Find(s => s.item == item);
        if (slot == null || slot.count <= 0)
        {
            ToastNotificationManager.Instance?.Show($"Bạn không có {item.itemName}.", Color.yellow);
            yield return new WaitForSeconds(0.8f);
            yield break;
        }

        string msg = "";
        bool success = false;

        switch (item.itemType)
        {
            case ItemType.Healing:
                if (item.healToFull && !targetPokemon.IsFainted)
                {
                    targetPokemon.FullHeal();
                    msg = $"{targetPokemon.Base.Name}'s HP was fully restored!";
                    success = true;
                }
                else if (item.healAmount > 0 && !targetPokemon.IsFainted)
                {
                    int prevHp = targetPokemon.CurrentHp;
                    targetPokemon.Heal(item.healAmount);
                    int healed = targetPokemon.CurrentHp - prevHp;

                    msg = healed > 0
                        ? $"{targetPokemon.Base.Name} recovered {healed} HP!"
                        : $"{targetPokemon.Base.Name}'s HP is already full!";
                    success = healed > 0;
                }
                else
                {
                    msg = $"{targetPokemon.Base.Name} is fainted and cannot be healed!";
                }
                break;

            case ItemType.Revive:
                if (targetPokemon.IsFainted)
                {
                    targetPokemon.Revive(item.revivePercent);
                    msg = $"{targetPokemon.Base.Name} was revived!";
                    success = true;
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
                            break;
                        }
                    }

                    if (!success) msg = $"{item.itemName} had no effect.";
                }
                break;

            case ItemType.Pokeball:
                msg = $"Không thể dùng {item.itemName} ở đây.";
                success = false;
                break;

            case ItemType.KeyItem:
                if (item.isExperienceBottle)
                {
                    int availableExp = inventory.GetExperienceBottleExp(item);
                    int desiredExp = requestedExpAmount > 0 ? requestedExpAmount : availableExp;
                    int totalExp = Mathf.Clamp(desiredExp, 1, availableExp);

                    if (availableExp > 0 && totalExp > 0)
                    {
                        if (GameController.Instance != null)
                            yield return GameController.Instance.GainExpAndProcessEvolution(targetPokemon, totalExp, false);
                        else
                            targetPokemon.GainExp(totalExp, false, autoEvolveWhenUnobserved: false);

                        inventory.SpendExperienceBottleExp(item, totalExp);
                        msg = $"{targetPokemon.Base.Name} nhận được {totalExp} EXP!";
                        success = true;
                    }
                    else
                    {
                        msg = $"{item.itemName} chưa có EXP tích lũy.";
                    }
                }
                else
                {
                    msg = $"Không thể dùng {item.itemName} ở đây.";
                    success = false;
                }
                break;
        }

        ToastNotificationManager.Instance?.Show(msg, success ? Color.white : Color.yellow);
        yield return new WaitForSeconds(0.8f);

        if (success)
        {
            if (!item.isExperienceBottle && item.consumable)
                inventory.RemoveItem(item, 1);
        }

        MenuController.Instance.CloseAll();
        GameController.Instance.SetState(GameState.Overworld);
    }
}
