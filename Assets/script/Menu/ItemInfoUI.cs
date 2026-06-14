using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoUI : MonoBehaviour
{
    [Header("Optional Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text footerText;

    [Header("Style")]
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color detailColor = new Color(1f, 0.92f, 0.55f);
    [SerializeField] private Color footerColor = Color.white;

    private void Awake()
    {
        CacheAssignedChildren();
        Hide();
    }

    public void ShowForBag(ItemBase item, int count)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        ShowInternal(item, count, isShop: false);
    }

    public void ShowForShop(ItemBase item, bool isSelling)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        ShowInternal(item, item.price, isShop: true, isSelling: isSelling);
    }

    [ContextMenu("Cache Assigned Children")]
    private void CacheAssignedChildren()
    {
        if (rootPanel == null)
            return;

        if (iconImage == null)
            iconImage = FindChildComponent<Image>("Image");

        if (nameText == null)
            nameText = FindChildComponent<TMP_Text>("Text (TMP)");

        if (descriptionText == null)
            descriptionText = FindChildComponent<TMP_Text>("Text (TMP) (1)");

        if (detailText == null)
            detailText = FindChildComponent<TMP_Text>("Text (TMP) (2)");

        if (footerText == null)
            footerText = FindChildComponent<TMP_Text>("Text (TMP) (3)");
    }

    public void Hide()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void ShowInternal(ItemBase item, int amount, bool isShop, bool isSelling = false)
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = item.icon != null;
        }

        if (nameText != null)
        {
            nameText.text = item.itemName;
            nameText.color = titleColor;
        }

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrWhiteSpace(item.description) ? "No description." : item.description.Trim();

        if (detailText != null)
        {
            if (isShop)
            {
                detailText.gameObject.SetActive(true);
                if (isSelling)
                {
                    detailText.text = item.itemType == ItemType.KeyItem
                        ? "Không thể bán"
                        : $"Giá bán: {Mathf.Max(0, amount / 2)} Đồng";
                }
                else
                {
                    detailText.text = $"Giá mua: {Mathf.Max(0, amount)} Đồng";
                }
            }
            else
            {
                detailText.gameObject.SetActive(false);
                detailText.text = string.Empty;
            }
        }

        if (footerText != null)
            footerText.text = isShop
                ? (isSelling ? "Z: Sell    ← Buy Mode    X: Close" : "Z: Buy    → Sell Mode    X: Close")
                : "Press Z to use";

        if (detailText != null)
            detailText.color = detailColor;

        if (footerText != null)
            footerText.color = footerColor;
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        if (rootPanel == null)
            return null;

        var found = rootPanel.transform.Find(childName);
        if (found == null)
            return null;

        return found.GetComponent<T>();
    }
}