using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public enum PartyMenuMode
{
    Selection,
    Summary,
    Switch
}
public class PartyMenuUI : MonoBehaviour
{
    [SerializeField] private PartySlotUI slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private PokemonInfoUI infoUI;

    private List<PartySlotUI> slotUIs = new List<PartySlotUI>();
    private int currentIndex = 0;
    private Action<Pokemon> onSelected;
    private Action onCancel;
    private List<Pokemon> pokemons; // giữ danh sách hiện tại
    private PartyMenuMode mode;
    private int firstSelectedIndex = -1; // cho chế độ Switch

    public void Open(List<Pokemon> pokemons,PartyMenuMode mode, Action<Pokemon> onSelected, Action onCancel)
    {
        gameObject.SetActive(true);
        this.mode = mode;
        this.onSelected = onSelected;
        this.onCancel = onCancel;
        this.pokemons = pokemons;

        // clear slot cũ
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);
        slotUIs.Clear();

        // tạo slot mới
        foreach (var p in pokemons)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            slot.SetData(p);
            slotUIs.Add(slot);
        }

        currentIndex = 0;
        HighlightCurrent();
    }

    public void HandleUpdate()
    {
        if (slotUIs == null || slotUIs.Count == 0) return;
        infoUI.Show(slotUIs[currentIndex].Pokemon);
        int rowCount = Mathf.CeilToInt(slotUIs.Count / 2f); // số hàng
        int colCount = 2; // 2 cột

        int row = currentIndex / colCount;
        int col = currentIndex % colCount;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (row < rowCount - 1)
            {
                row++;
                currentIndex = row * colCount + col;
                if (currentIndex >= slotUIs.Count) currentIndex = slotUIs.Count - 1;
                HighlightCurrent();
                infoUI.Show(slotUIs[currentIndex].Pokemon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (row > 0)
            {
                row--;
                currentIndex = row * colCount + col;
                HighlightCurrent();
                infoUI.Show(slotUIs[currentIndex].Pokemon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (col < colCount - 1)
            {
                col++;
                currentIndex = row * colCount + col;
                if (currentIndex >= slotUIs.Count) currentIndex = slotUIs.Count - 1;
                HighlightCurrent();
                infoUI.Show(slotUIs[currentIndex].Pokemon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (col > 0)
            {
                col--;
                currentIndex = row * colCount + col;
                HighlightCurrent();
                infoUI.Show(slotUIs[currentIndex].Pokemon);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentIndex >= 0 && currentIndex < slotUIs.Count)
            {
                if (mode == PartyMenuMode.Selection)
                {
                    // gửi Pokémon
                    onSelected?.Invoke(slotUIs[currentIndex].Pokemon);
                    infoUI.Hide();
                    Close();
                }
                else if (mode == PartyMenuMode.Switch)
                {
                    if (firstSelectedIndex < 0)
                    {
                        firstSelectedIndex = currentIndex;
                    }
                    else
                    {
                        // switch the two selected Pokémon
                        var partyHandler = new PartyHandler(pokemons);
                        partyHandler.SwitchPokemon(firstSelectedIndex, currentIndex);
                        pokemons = partyHandler.GetPokemons();
                        RefreshSlots();
                        firstSelectedIndex = -1;
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            onCancel?.Invoke();
            infoUI.Hide();
            Close();
        }
    }


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
    
    private void RefreshSlots()
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);
        slotUIs.Clear();

        foreach (var p in pokemons)
        {
            var slot = Instantiate(slotPrefab, slotParent);
            slot.SetData(p);
            slotUIs.Add(slot);
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, slotUIs.Count - 1);
        HighlightCurrent();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
