using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ItemMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;     // Prefab cho mỗi item slot
    [SerializeField] private Transform slotParent;      // Container chứa các slot
    [SerializeField] private ItemInfoUI itemInfoUI;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private ScrollRect scrollRect;

    private List<ItemSlotUI> slotUIs = new List<ItemSlotUI>();
    private int currentIndex = 0;
    private Action<ItemBase> onItemSelected;
    private Action onClose;

    private void Awake()
    {
        EnsureScrollSetup();
    }

    /// Mở menu item ngoài battle
    public void OpenMenu(List<ItemSlot> slots, Action<ItemBase> onSelectedCallback, Action onCloseCallback)
    {
        gameObject.SetActive(true);
        currentIndex = 0;

        EnsureScrollSetup();

        onItemSelected = onSelectedCallback;
        onClose = onCloseCallback;

        RefreshUI(slots);
        HighlightCurrent(currentIndex);
        Canvas.ForceUpdateCanvases();
        UpdateMoneyText();
        UpdateItemInfo();
    }

    /// Đóng menu item
    public void CloseMenu()
    {
        gameObject.SetActive(false);
        slotUIs.Clear();
        currentIndex = 0;

        InvokeItemInfoMethod("Hide");
        UpdateMoneyText();

        onItemSelected = null;
        onClose = null;
    }

    /// Cập nhật UI theo inventory
    private void RefreshUI(List<ItemSlot> slots)
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        slotUIs.Clear();
        EnsureTopDownListLayout();

        var orderedSlots = slots == null
            ? Enumerable.Empty<ItemSlot>()
            : slots
                .Where(slot => slot != null && slot.item != null && slot.count > 0)
                .OrderByDescending(slot => slot.item.isExperienceBottle)
                .ThenBy(slot => slot.item.itemName);

        foreach (ItemSlot slot in orderedSlots)
        {
            GameObject obj = Instantiate(slotPrefab, slotParent);
            ItemSlotUI ui = obj.GetComponent<ItemSlotUI>();
            ui.SetData(slot);
            slotUIs.Add(ui);
        }

        if (scrollRect != null && scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
    }

    /// Xử lý input ngoài battle
    public void HandleUpdate()
    {
        if (!gameObject.activeSelf || slotUIs.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = WrapIndex(currentIndex - 1, slotUIs.Count);
            HighlightCurrent(currentIndex);
            UpdateItemInfo();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = WrapIndex(currentIndex + 1, slotUIs.Count);
            HighlightCurrent(currentIndex);
            UpdateItemInfo();
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
    private void HighlightCurrent(int index)
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].SetHighlight(i == index, highlightColor, normalColor);
        }

        UpdateScrollPosition();
        UpdateItemInfo();
    }

    private void UpdateMoneyText()
    {
        if (moneyText == null)
            moneyText = FindMoneyText();

        if (moneyText == null)
            return;

        if (Inventory.Instance != null)
            moneyText.text = $"Tiền: {Inventory.Instance.Money} Đồng";
    }

    private TMP_Text FindMoneyText()
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (text == null)
                continue;

            string objectName = text.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("money") || objectName.Contains("coin") || objectName.Contains("currency"))
                return text;
        }

        return null;
    }

    private void UpdateItemInfo()
    {
        if (currentIndex < 0 || currentIndex >= slotUIs.Count)
            return;

        var slot = slotUIs[currentIndex].GetSlot();
        if (slot == null || slot.item == null)
        {
            InvokeItemInfoMethod("Hide");
            return;
        }

        int amount = slot.item.isExperienceBottle ? Mathf.Max(0, slot.storedExp) : Mathf.Max(0, slot.count);
        InvokeItemInfoMethod("ShowForBag", slot.item, amount);
    }

    private void InvokeItemInfoMethod(string methodName, params object[] args)
    {
        if (itemInfoUI == null)
        {
            Debug.LogWarning($"ItemMenuUI: ItemInfoUI is not assigned (method={methodName}). Assign the ItemInfoUI component from your panel in the inspector.");
            return;
        }

        try
        {
            if (methodName == "Hide")
            {
                itemInfoUI.Hide();
                //Debug.Log("ItemMenuUI: Called Hide() on assigned ItemInfoUI.");
                return;
            }

            if (methodName == "ShowForBag" && args != null && args.Length == 2 && args[0] is ItemBase && args[1] is int)
            {
                itemInfoUI.ShowForBag((ItemBase)args[0], (int)args[1]);
                //Debug.Log("ItemMenuUI: Called ShowForBag on assigned ItemInfoUI.");
                return;
            }

            Debug.LogWarning($"ItemMenuUI: Unsupported ItemInfoUI method request: {methodName}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ItemMenuUI: Exception while calling {methodName} on ItemInfoUI: {ex}");
        }
    }

    private void UpdateScrollPosition()
    {
        if (scrollRect == null || slotUIs.Count <= 1)
            return;

        if (scrollRect.content == null)
        {
            Debug.LogWarning("ItemMenuUI: ScrollRect.content is not assigned. Assign the Content RectTransform in the ScrollRect inspector to enable auto-scrolling.");
            return;
        }

        var selectedSlot = slotUIs[currentIndex];
        if (selectedSlot == null)
            return;

        var selectedRect = selectedSlot.transform as RectTransform;
        var viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        var content = scrollRect.content;
        if (selectedRect == null || viewport == null)
            return;

        try
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selectedRect);
            float viewportTop = viewport.rect.yMax;
            float viewportBottom = viewport.rect.yMin;

            float offset = 0f;
            if (itemBounds.max.y > viewportTop)
                offset = itemBounds.max.y - viewportTop;
            else if (itemBounds.min.y < viewportBottom)
                offset = itemBounds.min.y - viewportBottom;

            if (Mathf.Abs(offset) > 0.01f)
            {
                Vector2 pos = content.anchoredPosition;
                pos.y -= offset;

                float maxY = Mathf.Max(0f, content.rect.height - viewport.rect.height);
                pos.y = Mathf.Clamp(pos.y, 0f, maxY);
                content.anchoredPosition = pos;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ItemMenuUI: Exception while setting scroll position: {ex}");
        }
    }

    private void EnsureScrollSetup()
    {
        if (slotParent == null)
            return;

        EnsureTopDownListLayout();

        if (scrollRect == null)
            scrollRect = slotParent.GetComponentInParent<ScrollRect>(true);

        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (scrollRect == null)
        {
            Debug.LogWarning("ItemMenuUI: ScrollRect was not found. Assign an existing ScrollRect in inspector to preserve current UI layout.");
            return;
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = Mathf.Max(scrollRect.scrollSensitivity, 25f);

        var slotParentRect = slotParent as RectTransform;
        if (slotParentRect == null)
            return;

        if (scrollRect.viewport == null)
        {
            var viewportFromHierarchy = scrollRect.transform.Find("Viewport") as RectTransform;
            scrollRect.viewport = viewportFromHierarchy != null ? viewportFromHierarchy : slotParentRect.parent as RectTransform;
        }

        if (scrollRect.content == null)
        {
            var contentFromHierarchy = scrollRect.transform.Find("Viewport/Content") as RectTransform;
            scrollRect.content = contentFromHierarchy != null ? contentFromHierarchy : slotParentRect;
        }

        if (slotParent != scrollRect.content)
            slotParent = scrollRect.content;
    }

    private void EnsureTopDownListLayout()
    {
        if (slotParent == null)
            return;

        var slotParentRect = slotParent as RectTransform;
        if (slotParentRect != null)
        {
            slotParentRect.anchorMin = new Vector2(0f, 1f);
            slotParentRect.anchorMax = new Vector2(1f, 1f);
            slotParentRect.pivot = new Vector2(0.5f, 1f);
        }

        var grid = slotParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.childAlignment = TextAnchor.UpperLeft;

            if (grid.constraint == GridLayoutGroup.Constraint.Flexible)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1;
            }

            var fitter = slotParent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = slotParent.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return;
        }

        var vertical = slotParent.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = false;

            var fitter = slotParent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = slotParent.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private int WrapIndex(int value, int count)
    {
        if (count <= 0)
            return 0;

        value %= count;
        if (value < 0)
            value += count;

        return value;
    }
}
