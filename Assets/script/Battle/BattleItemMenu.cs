using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class BattleItemMenu : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float slotHeight = 60f;   // chiều cao cố định mỗi slot

    private List<ItemSlotUI> slotUIs = new List<ItemSlotUI>();
    private int currentIndex = 0;
    private Action<ItemBase> onItemSelected;
    private Action onClose;

    /// Mở menu item trong battle
    public void OpenMenu(List<ItemSlot> slots, Action<ItemBase> onSelectedCallback, Action onCloseCallback)
    {
        gameObject.SetActive(true);
        currentIndex = 0;
        EnsureLayoutSetup();

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

            // Gán chiều cao cố định — tránh slot giãn vô tội vạ
            var le = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            le.preferredHeight = slotHeight;
            le.flexibleHeight  = 0f;

            ItemSlotUI ui = obj.GetComponent<ItemSlotUI>();
            ui.SetData(slot);
            slotUIs.Add(ui);
        }

        if (scrollRect != null && scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
    }

    private void EnsureLayoutSetup()
    {
        if (slotParent == null) return;

        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (scrollRect != null)
        {
            // Gán viewport nếu chưa có
            if (scrollRect.viewport == null)
            {
                var vp = scrollRect.transform.Find("Viewport") as RectTransform;
                scrollRect.viewport = vp != null ? vp : (RectTransform)scrollRect.transform;
            }
            // Gán content nếu chưa có
            if (scrollRect.content == null)
            {
                var ct = scrollRect.transform.Find("Viewport/Content") as RectTransform;
                scrollRect.content = ct != null ? ct : slotParent as RectTransform;
            }
            // Đồng bộ slotParent với content
            if (slotParent != scrollRect.content)
                slotParent = scrollRect.content;
        }

        // Anchor Content bám top để list xếp từ trên xuống
        var rect = slotParent as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot     = new Vector2(0.5f, 1f);
        }

        // VerticalLayoutGroup: không giãn chiều cao từng slot
        var vlg = slotParent.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.childControlHeight     = true;
            vlg.childControlWidth      = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = true;
        }

        // ContentSizeFitter: Content tự giãn theo tổng chiều cao slot
        var fitter = slotParent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = slotParent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
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
            slotUIs[i].SetHighlight(i == currentIndex, highlightColor, normalColor);

        ScrollToItem(currentIndex);
    }

    /// Cuộn scroll rect để item được chọn luôn nằm trong vùng nhìn thấy
    private void ScrollToItem(int index)
    {
        if (scrollRect == null || slotUIs.Count == 0) return;

        Canvas.ForceUpdateCanvases();

        RectTransform content  = scrollRect.content;
        RectTransform viewport = scrollRect.viewport != null
                               ? scrollRect.viewport
                               : (RectTransform)scrollRect.transform;
        RectTransform item     = slotUIs[index].GetComponent<RectTransform>();

        if (content == null || item == null) return;

        float contentH    = content.rect.height;
        float viewportH   = viewport.rect.height;
        float scrollableH = contentH - viewportH;

        if (scrollableH <= 0f) return;

        // Vị trí top/bottom của item tính từ đỉnh content (pixel)
        float itemTop    = -item.localPosition.y - item.rect.height * (1f - item.pivot.y);
        float itemBottom = itemTop + item.rect.height;

        // Vị trí viewport hiện tại (pixel từ đỉnh content)
        float viewTop    = (1f - scrollRect.verticalNormalizedPosition) * scrollableH;
        float viewBottom = viewTop + viewportH;

        float target = viewTop;
        if (itemTop < viewTop)           target = itemTop;           // item bị che trên
        else if (itemBottom > viewBottom) target = itemBottom - viewportH; // item bị che dưới

        scrollRect.verticalNormalizedPosition =
            1f - Mathf.Clamp01(target / scrollableH);
    }

}