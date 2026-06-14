using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    private ItemSlot slot;

    public void SetData(ItemSlot itemSlot)
    {
        slot = itemSlot;

        if (slot == null || slot.item == null)
        {
            nameText.text = "-";
            countText.text = "0";
            iconImage.sprite = null;
            return;
        }

        nameText.text = slot.item.itemName;
        nameText.color = Color.white;
        iconImage.sprite = slot.item.icon;
        if (slot.item.isExperienceBottle)
            countText.text = $"exp: {Mathf.Max(0, slot.storedExp)}";
        else
            countText.text = slot.count.ToString();
        countText.color = Color.white;
    }

    public void SetHighlight(bool active, Color highlightColor, Color normalColor)
    {
        if (nameText != null) nameText.color = active ? highlightColor : Color.white;
        if (countText != null) countText.color = active ? highlightColor : Color.white;
    }

    public ItemSlot GetSlot() => slot;
}