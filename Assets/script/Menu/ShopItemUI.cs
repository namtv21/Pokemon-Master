using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image highlight; // khung highlight
    [SerializeField] private Color highlightTextColor = Color.yellow;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color highlightImageColor = new Color(1f, 0.9f, 0.2f, 0.4f);

    private ItemBase item;
    private ItemSlot itemSlot;
    private bool sellMode;

    public void SetData(ItemBase itemBase)
    {
        sellMode = false;
        itemSlot = null;
        item = itemBase;
        icon.sprite = itemBase.icon;
        nameText.text = itemBase.itemName;
        nameText.color = Color.white;
        priceText.text = $"{itemBase.price} Đồng";
        priceText.color = Color.white;
        SetHighlight(false);
    }

    public void SetSellData(ItemSlot slot)
    {
        sellMode = true;
        itemSlot = slot;
        item = slot != null ? slot.item : null;

        if (item == null)
        {
            if (icon != null) icon.sprite = null;
            if (nameText != null) nameText.text = "-";
            if (priceText != null) priceText.text = "0 Đồng";
            SetHighlight(false);
            return;
        }

        if (icon != null) icon.sprite = item.icon;
        if (nameText != null)
        {
            int ownedAmount = item.isExperienceBottle
                ? Mathf.Max(0, slot.storedExp)
                : Mathf.Max(0, slot.count);

            nameText.text = item.isExperienceBottle
                ? $"{item.itemName} (EXP: {ownedAmount})"
                : $"{item.itemName} x{ownedAmount}";
        }

        if (priceText != null)
        {
            if (item.itemType == ItemType.KeyItem)
                priceText.text = "Không thể bán";
            else
                priceText.text = $"{Mathf.Max(0, item.price / 2)} Đồng";
        }

        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (highlight != null)
        {
            highlight.enabled = active;
            highlight.color = active ? highlightImageColor : Color.clear;
        }

        if (nameText != null) nameText.color = active ? highlightTextColor : Color.white;
        if (priceText != null) priceText.color = active ? highlightTextColor : Color.white;
    }

    public ItemBase GetItem() => item;
    public ItemSlot GetItemSlot() => itemSlot;
    public bool IsSellMode => sellMode;
}