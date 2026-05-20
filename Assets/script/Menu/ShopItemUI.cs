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

    public void SetData(ItemBase itemBase)
    {
        item = itemBase;
        icon.sprite = itemBase.icon;
        nameText.text = itemBase.itemName;
        nameText.color = Color.white;
        priceText.text = $"{itemBase.price} Yen";
        priceText.color = Color.white;
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (highlight != null)
        {
            highlight.enabled = active;
            highlight.color = active ? highlightImageColor : Color.clear;
        }

        if (nameText != null) nameText.color = active ? highlightTextColor : normalTextColor;
        if (priceText != null) priceText.color = active ? highlightTextColor : normalTextColor;
    }

    public ItemBase GetItem() => item;
}