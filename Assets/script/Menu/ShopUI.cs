using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    private enum ShopMode
    {
        Buy,
        Sell
    }

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform contentParent;       // nơi chứa các item UI
    [SerializeField] private ShopItemUI shopItemPrefab;     // prefab UI cho item
    [SerializeField] private TextMeshProUGUI buyTitleText;
    [SerializeField] private TextMeshProUGUI sellTitleText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private ItemInfoUI itemInfoUI;

    [Header("Default Items")]
    [SerializeField] private List<ItemBase> defaultItems = new List<ItemBase>();

    private List<ShopItemUI> slotUIs = new List<ShopItemUI>();
    private List<ItemBase> buyItems = new List<ItemBase>();
    private HashSet<ItemBase> buyItemSet = new HashSet<ItemBase>();
    private List<ItemSlot> sellSlots = new List<ItemSlot>();
    private int currentSelection;
    private ShopMode currentMode = ShopMode.Buy;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open(List<ItemBase> shopItems = null)
    {
        if (shopPanel == null || contentParent == null || shopItemPrefab == null || moneyText == null)
        {
            Debug.LogWarning("Shop UI references are missing.");
            return;
        }

        Debug.Log("ShopUI: Open() called.");

        gameObject.SetActive(true);
        shopPanel.SetActive(true);
        currentMode = ShopMode.Buy;
        buyItems = (shopItems != null && shopItems.Count > 0) ? shopItems : defaultItems;
        buyItemSet = new HashSet<ItemBase>(buyItems.Where(item => item != null));
        currentSelection = 0;

        RebuildList();
        UpdateTitle();
        UpdateUI();
        UpdateItemInfo();
        GameController.Instance.SetState(GameState.Shop);
    }

    public void Close()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (itemInfoUI == null)
            itemInfoUI = FindObjectOfType<ItemInfoUI>(true);

        if (itemInfoUI == null)
        {
            Debug.LogWarning("ShopUI: ItemInfoUI is not assigned. Assign the ItemInfoUI component from your panel in the inspector.");
            GameController.Instance.SetState(GameState.Overworld);
            return;
        }

        itemInfoUI?.Hide();
        GameController.Instance.SetState(GameState.Overworld);
    }

    private void UpdateUI()
    {
        UpdateTitle();

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].SetHighlight(i == currentSelection);
        }

        moneyText.text = $"Tiền: {Inventory.Instance.Money} Đồng";
    }

    private void UpdateItemInfo()
    {
        if (itemInfoUI == null)
            itemInfoUI = FindObjectOfType<ItemInfoUI>(true);

        if (itemInfoUI == null)
        {
            Debug.LogWarning("ShopUI: ItemInfoUI is not assigned. Assign the ItemInfoUI component from your panel in the inspector.");
            return;
        }

        Debug.Log($"ShopUI: Showing item info for selection {currentSelection}.");

        if (slotUIs == null || slotUIs.Count == 0)
        {
            itemInfoUI.Hide();
            return;
        }

        int index = Mathf.Clamp(currentSelection, 0, slotUIs.Count - 1);
        var ui = slotUIs[index];
        if (currentMode == ShopMode.Buy)
        {
            var item = ui.GetItem();
            if (item != null)
                itemInfoUI.ShowForShop(item, false);
            else
                itemInfoUI.Hide();
        }
        else
        {
            var slot = ui.GetItemSlot();
            if (slot != null && slot.item != null)
                itemInfoUI.ShowForShop(slot.item, true);
            else
                itemInfoUI.Hide();
        }
    }

    private void RebuildList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        slotUIs.Clear();

        if (currentMode == ShopMode.Buy)
        {
            foreach (var item in buyItems)
            {
                if (item == null)
                    continue;

                var ui = Instantiate(shopItemPrefab, contentParent);
                ui.SetData(item);
                slotUIs.Add(ui);
            }
        }
        else
        {
            sellSlots = Inventory.Instance != null
                ? Inventory.Instance.GetSlots().Where(slot =>
                    slot != null &&
                    slot.item != null &&
                    buyItemSet.Contains(slot.item) &&
                    (slot.count > 0 || (slot.item.isExperienceBottle && slot.storedExp > 0))).ToList()
                : new List<ItemSlot>();

            foreach (var slot in sellSlots)
            {
                var ui = Instantiate(shopItemPrefab, contentParent);
                ui.SetSellData(slot);
                slotUIs.Add(ui);
            }
        }

        if (slotUIs.Count == 0)
            currentSelection = 0;
        else
            currentSelection = Mathf.Clamp(currentSelection, 0, slotUIs.Count - 1);
    }

    private void SwitchToBuyMode()
    {
        currentMode = ShopMode.Buy;
        currentSelection = 0;
        RebuildList();
        UpdateTitle();
        UpdateUI();
        UpdateItemInfo();
    }

    private void SwitchToSellMode()
    {
        currentMode = ShopMode.Sell;
        currentSelection = 0;
        RebuildList();
        UpdateTitle();
        UpdateUI();
        UpdateItemInfo();
    }

    private void UpdateTitle()
    {
        if (buyTitleText != null)
        {
            buyTitleText.text = "BUY";
            buyTitleText.color = currentMode == ShopMode.Buy ? Color.white : new Color(0.75f, 0.75f, 0.75f);
        }

        if (sellTitleText != null)
        {
            sellTitleText.text = "SELL";
            sellTitleText.color = currentMode == ShopMode.Sell ? Color.white : new Color(0.75f, 0.75f, 0.75f);
        }
    }

    public void HandleUpdate()
    {
        if (!shopPanel.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) && currentMode == ShopMode.Buy)
        {
            SwitchToSellMode();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentMode == ShopMode.Sell)
        {
            SwitchToBuyMode();
            return;
        }

        if (slotUIs == null || slotUIs.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSelection = (currentSelection - 1 + slotUIs.Count) % slotUIs.Count;
            UpdateUI();
            UpdateItemInfo();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSelection = (currentSelection + 1) % slotUIs.Count;
            UpdateUI();
            UpdateItemInfo();
        }

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            if (currentMode == ShopMode.Buy)
                BuyItem(slotUIs[currentSelection].GetItem());
            else
                SellItem(slotUIs[currentSelection].GetItemSlot());
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void BuyItem(ItemBase item)
    {
        if (item == null)
            return;

        if (item.price <= 0)
        {
            ToastNotificationManager.Instance?.Show("This item cannot be bought.", Color.yellow);
            GameController.Instance.SetState(GameState.Shop);
            UpdateUI();
            UpdateItemInfo();
            return;
        }

        if (Inventory.Instance.SpendMoney(item.price))
        {
            Inventory.Instance.AddItem(item);
            ToastNotificationManager.Instance?.Show($"You bought {item.itemName}!");
            GameController.Instance.SetState(GameState.Shop);
            UpdateUI();
            UpdateItemInfo();
        }
        else
        {
            ToastNotificationManager.Instance?.Show("Not enough money!", Color.yellow);
            GameController.Instance.SetState(GameState.Shop);
            UpdateUI();
            UpdateItemInfo();
        }
    }

    private void SellItem(ItemSlot slot)
    {
        if (slot == null || slot.item == null || Inventory.Instance == null)
            return;

        var item = slot.item;
        if (!buyItemSet.Contains(item))
        {
            ToastNotificationManager.Instance?.Show("This item cannot be sold.", Color.yellow);
            UpdateUI();
            UpdateItemInfo();
            return;
        }

        int sellPrice = Mathf.Max(0, item.price / 2);
        if (item.isExperienceBottle)
            Inventory.Instance.RemoveExperienceBottle(item);
        else
            Inventory.Instance.RemoveItem(item, 1);

        Inventory.Instance.AddMoney(sellPrice);
        ToastNotificationManager.Instance?.Show($"Đã bán {item.itemName} với giá {sellPrice} Đồng!");

        RebuildList();
        UpdateUI();
        UpdateItemInfo();
    }
}