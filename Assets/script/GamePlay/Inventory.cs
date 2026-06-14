using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private List<ItemSlot> slots = new List<ItemSlot>();
    [SerializeField] private ItemBase experienceBottleItem;
    [SerializeField, Range(0.01f, 1f)] private float bonusExpRatio = 0.5f;

    [Header("Money")]
    [SerializeField] private int money = 0;   // số tiền hiện tại

    public int Money => money;
    public float BonusExpRatio => bonusExpRatio;
    public ItemBase ExperienceBottleItem => experienceBottleItem;
    public static Inventory Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DuplicateSystemRootUtility.DestroyDuplicate(this, Instance);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    public void AddItem(ItemBase item, int count = 1)
    {
        if (item != null && item.isExperienceBottle)
        {
            ItemSlot bottleSlot = slots.Find(s => s.item == item);
            if (bottleSlot != null)
            {
                bottleSlot.count = 1;
                return;
            }

            slots.Add(new ItemSlot { item = item, count = 1, storedExp = 0 });
            return;
        }

        ItemSlot slot = slots.Find(s => s.item == item);
        if (slot != null)
        {
            slot.count += count;
        }
        else
        {
            slots.Add(new ItemSlot { item = item, count = count });
        }
    }

    public void RemoveItem(ItemBase item, int count = 1)
    {
        if (item != null && item.isExperienceBottle)
            return;

        ItemSlot slot = slots.Find(s => s.item == item);
        if (slot != null)
        {
            slot.count -= count;
            if (slot.count <= 0)
                slots.Remove(slot);
        }
    }

    public void RemoveExperienceBottle(ItemBase item)
    {
        if (item == null || !item.isExperienceBottle)
            return;

        var slot = slots.Find(s => s.item == item);
        if (slot != null)
            slots.Remove(slot);
    }

    public List<ItemSlot> GetSlots()
    {
        return slots;
    }

    public void ClearItems()
    {
        slots.Clear();
    }

    public void AddExperienceBottleExp(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0)
            return;

        if (experienceBottleItem == null)
        {
            var fallbackItems = Resources.LoadAll<ItemBase>(string.Empty);
            foreach (var item in fallbackItems)
            {
                if (item != null && item.isExperienceBottle)
                {
                    experienceBottleItem = item;
                    break;
                }
            }
        }

        if (experienceBottleItem != null)
        {
            var slot = slots.Find(s => s.item == experienceBottleItem);
            if (slot == null)
            {
                slot = new ItemSlot { item = experienceBottleItem, count = 1, storedExp = 0 };
                slots.Add(slot);
            }

            slot.count = 1;
            slot.storedExp = Mathf.Max(0, slot.storedExp + amount);
        }
    }

    public int GetExperienceBottleExp(ItemBase item)
    {
        if (item == null || !item.isExperienceBottle)
            return 0;

        var slot = slots.Find(s => s.item == item);
        return slot != null ? Mathf.Max(0, slot.storedExp) : 0;
    }

    public int SpendExperienceBottleExp(ItemBase item, int amount)
    {
        if (item == null || !item.isExperienceBottle)
            return 0;

        amount = Mathf.Max(0, amount);
        if (amount <= 0)
            return 0;

        var slot = slots.Find(s => s.item == item);
        if (slot == null)
            return 0;

        int spent = Mathf.Min(amount, Mathf.Max(0, slot.storedExp));
        slot.storedExp -= spent;
        slot.count = 1;
        if (slot.storedExp < 0)
            slot.storedExp = 0;

        return spent;
    }

    // 👉 Thêm tiền
    public void AddMoney(int amount)
    {
        money += Mathf.Max(0, amount);
        Debug.Log($"Money increased by {amount}. Current money: {money}");
    }

    // 👉 Trừ tiền
    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            Debug.Log($"Spent {amount}. Current money: {money}");
            return true;
        }
        Debug.Log("Not enough money!");
        return false;
    }

    // 👉 Set tiền trực tiếp (ví dụ khi load game)
    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
    }

    public ItemBase FindItemByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        var items = Resources.LoadAll<ItemBase>(string.Empty);
        foreach (var item in items)
        {
            if (item != null && string.Equals(item.itemName, itemName, System.StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }
}

[System.Serializable]
public class ItemSlot
{
    public ItemBase item;
    public int count;
    public int storedExp;
}
