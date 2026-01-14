using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform contentParent;       // nơi chứa các item UI
    [SerializeField] private ShopItemUI shopItemPrefab;     // prefab UI cho item
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Default Items")]
    [SerializeField] private List<ItemBase> defaultItems = new List<ItemBase>();

    private List<ShopItemUI> slotUIs = new List<ShopItemUI>();
    private List<ItemBase> items = new List<ItemBase>();
    private int currentSelection;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        shopPanel.SetActive(false);
    }

    public void Open(List<ItemBase> shopItems = null)
    {
        items = (shopItems != null && shopItems.Count > 0) ? shopItems : defaultItems;
        currentSelection = 0;

        // Xóa các slot cũ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
        slotUIs.Clear();

        // Tạo slot mới
        foreach (var item in items)
        {
            var ui = Instantiate(shopItemPrefab, contentParent);
            ui.SetData(item);
            slotUIs.Add(ui);
        }

        shopPanel.SetActive(true);
        UpdateUI();
        GameController.Instance.SetState(GameState.Shop);
    }

    public void Close()
    {
        shopPanel.SetActive(false);
        GameController.Instance.SetState(GameState.Overworld);
    }

    private void UpdateUI()
    {
        // highlight slot hiện tại
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].SetHighlight(i == currentSelection);
        }

        moneyText.text = $"Money: {Inventory.Instance.Money} Yen";
    }

    public void HandleUpdate()
    {
        if (!shopPanel.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSelection = (currentSelection - 1 + items.Count) % items.Count;
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSelection = (currentSelection + 1) % items.Count;
            UpdateUI();
        }

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            BuyItem(items[currentSelection]);
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void BuyItem(ItemBase item)
    {
        if (Inventory.Instance.SpendMoney(item.price))
        {
            Inventory.Instance.AddItem(item);
            DialogManager.Instance.ShowDialog(
                $"You bought {item.itemName}!",
                () => {
                    // callback sau khi dialog đóng
                    GameController.Instance.SetState(GameState.Shop);
                    UpdateUI();
                });
        }
        else
        {
            DialogManager.Instance.ShowDialog(
                "Not enough money!",
                () => {
                    GameController.Instance.SetState(GameState.Shop);
                    UpdateUI();
                });
        }
    }
}