using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePartyMenu : MonoBehaviour
{
    [SerializeField] private PartySlotUI slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private List<PartySlotUI> slotUIs = new List<PartySlotUI>();
    private int currentIndex = 0;
    private Action<Pokemon> onSelected;
    private Action onCancel;

    /// Mở PartyMenu với danh sách Pokémon
    public void Open(List<Pokemon> pokemons, Action<Pokemon> onSelected, Action onCancel)
    {
        if (slotPrefab == null)
        {
            Debug.LogError("BattlePartyMenu: slotPrefab missing. Assign a Prefab asset, not a scene object.");
            return;
        }

        gameObject.SetActive(true);
        this.onSelected = onSelected;
        this.onCancel = onCancel;

        // Chỉ hủy các slot từng Instantiate từ slotPrefab
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
                Destroy(slotUIs[i].gameObject);
        }
        slotUIs.Clear();

        // Tạo slot mới
        foreach (var p in pokemons)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            slot.SetData(p);
            slotUIs.Add(slot);
        }

        currentIndex = 0;
        HighlightCurrent();
    }

    /// Xử lý input khi PartyMenu đang mở
    public void HandleUpdate()
    {
        if (slotUIs == null || slotUIs.Count == 0) return;

        // đảm bảo index hợp lệ
        if (currentIndex < 0 || currentIndex >= slotUIs.Count)
            currentIndex = 0;

        // nếu slot hiện tại đã bị destroy thì return
        if (slotUIs[currentIndex] == null) return;

        // di chuyển con trỏ
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            int newIndex = currentIndex + 2;
            if (newIndex < slotUIs.Count)
                currentIndex = newIndex;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int newIndex = currentIndex - 2;
            if (newIndex >= 0)
                currentIndex = newIndex;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            int newIndex = currentIndex + 1;
            if (newIndex < slotUIs.Count && (currentIndex % 2 == 0))
                currentIndex = newIndex;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int newIndex = currentIndex - 1;
            if (newIndex >= 0 && (currentIndex % 2 == 1))
                currentIndex = newIndex;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentIndex >= 0 && currentIndex < slotUIs.Count && slotUIs[currentIndex] != null)
                onSelected?.Invoke(slotUIs[currentIndex].Pokemon);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            onCancel?.Invoke();
            Close();
        }

    }

    /// Highlight slot hiện tại
   private void HighlightCurrent()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            var slot = slotUIs[i];
            var nameText = slot.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null)
                nameText.color = (i == currentIndex) ? highlightColor : normalColor;
        }
    }

    /// Cập nhật HP cho tất cả slot
    public void RefreshSlots()
    {
        foreach (var slot in slotUIs)
        {
            if (slot != null)
                slot.UpdateHp();
        }
    }


    /// Đóng PartyMenu
    public void Close()
    {
        gameObject.SetActive(false);
    }
}