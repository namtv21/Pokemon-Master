using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private List<ItemSlot> slots = new List<ItemSlot>();

    [Header("Money")]
    [SerializeField] private int money = 0;   // số tiền hiện tại

    public int Money => money;
    public static Inventory Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        //DontDestroyOnLoad(gameObject);
    }

    public void AddItem(ItemBase item, int count = 1)
    {
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
        ItemSlot slot = slots.Find(s => s.item == item);
        if (slot != null)
        {
            slot.count -= count;
            if (slot.count <= 0)
                slots.Remove(slot);
        }
    }

    public List<ItemSlot> GetSlots()
    {
        return slots;
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
}

[System.Serializable]
public class ItemSlot
{
    public ItemBase item;
    public int count;
}
