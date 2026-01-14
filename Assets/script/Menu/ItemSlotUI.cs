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
        iconImage.sprite = slot.item.icon;
        countText.text = slot.count.ToString();
    }

    public ItemSlot GetSlot() => slot;
}