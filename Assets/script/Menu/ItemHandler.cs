using System.Collections;
using UnityEngine;

public class ItemHandler : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerParty playerParty;
    [SerializeField] private StorageSystem storage;
    [SerializeField] private DialogManager dialogBox; // UI hiển thị thông báo ngoài battle

    public IEnumerator UseItemOnPokemon(ItemBase item, Pokemon targetPokemon)
    {
        // kiểm tra số lượng item
        ItemSlot slot = inventory.GetSlots().Find(s => s.item == item);
        if (slot == null || slot.count <= 0)
        {
            dialogBox.ShowDialog($"You don't have any {item.itemName}.");
            yield return new WaitForSeconds(1f);
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
                msg = $"{item.itemName} can't be used here.";
                success = false;
                break;

            case ItemType.KeyItem:
                msg = $"{item.itemName} can't be used here.";
                success = false;
                break;
        }

        yield return dialogBox.ShowDialogCoroutine(msg);


        if (success && item.consumable)
            inventory.RemoveItem(item, 1);
    
        MenuController.Instance.CloseAll();
        GameController.Instance.SetState(GameState.Overworld);
    }
}
