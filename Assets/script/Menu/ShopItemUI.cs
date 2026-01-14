using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image highlight; // khung highlight

    private ItemBase item;

    public void SetData(ItemBase itemBase)
    {
        item = itemBase;
        icon.sprite = itemBase.icon;
        nameText.text = itemBase.itemName;
        priceText.text = $"{itemBase.price} Yen";
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (highlight != null)
            highlight.enabled = active;
    }

    public ItemBase GetItem() => item;
}