using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class BattleItemMenu : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;     // Prefab cho mỗi item slot
    [SerializeField] private Transform slotParent;      // Container chứa các slot
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private ScrollRect scrollRect;

    private List<ItemSlotUI> slotUIs = new List<ItemSlotUI>();
    private int currentIndex = 0;
    private Action<ItemBase> onItemSelected;
    private Action onClose;

    /// Mở menu item trong battle
    public void OpenMenu(List<ItemSlot> slots, Action<ItemBase> onSelectedCallback, Action onCloseCallback)
    {
        gameObject.SetActive(true);
        currentIndex = 0;

        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        onItemSelected = onSelectedCallback;
        onClose = onCloseCallback;

        RefreshUI(slots);
        HighlightCurrent(currentIndex, highlightColor, normalColor);
    }

    /// Đóng menu item
    public void CloseMenu()
    {
        gameObject.SetActive(false);
        slotUIs.Clear();
        currentIndex = 0;

        onItemSelected = null;
        onClose = null;
    }

    /// Cập nhật UI theo inventory
    private void RefreshUI(List<ItemSlot> slots)
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        slotUIs.Clear();

        foreach (ItemSlot slot in slots)
        {
            GameObject obj = Instantiate(slotPrefab, slotParent);
            ItemSlotUI ui = obj.GetComponent<ItemSlotUI>();
            ui.SetData(slot);
            slotUIs.Add(ui);
        }
    }

    /// Xử lý input trong battle
    public void HandleUpdate()
    {
        if (!gameObject.activeSelf || slotUIs.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + slotUIs.Count) % slotUIs.Count;
            HighlightCurrent(currentIndex, highlightColor, normalColor);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % slotUIs.Count;
            HighlightCurrent(currentIndex, highlightColor, normalColor);
        }
        else if (Input.GetKeyDown(KeyCode.Z)) // chọn item
        {
            var slot = slotUIs[currentIndex].GetSlot();
            if (slot != null && slot.item != null)
            {
                onItemSelected?.Invoke(slot.item);
            }
        }
        else if (Input.GetKeyDown(KeyCode.X)) // cancel
        {
            onClose?.Invoke();
            CloseMenu();
        }
    }

    /// Highlight slot hiện tại
    public void HighlightCurrent(int currentIndex, Color highlightColor, Color normalColor)
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            var text = slotUIs[i].GetComponentInChildren<TextMeshProUGUI>();

            if (i == currentIndex)
            {
                if (text != null) text.color = highlightColor;
            }
            else
            {
                if (text != null) text.color = normalColor;
            }
        }

        if (scrollRect != null && slotUIs.Count > 1)
        {
            float normalized = 1f - ((float)currentIndex / (slotUIs.Count - 1));
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalized);
        }

    }

}