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
    [SerializeField] private TMP_Text bottomPromptText;

    private List<PartySlotUI> slotUIs = new List<PartySlotUI>();
    private int currentIndex = 0;
    private Action<Pokemon> onSelected;
    private Action onCancel;
    private List<Pokemon> pokemons; // giữ danh sách hiện tại
    private PartyMenuMode mode;
    private int firstSelectedIndex = -1; // cho chế độ Switch
    public static PartyMenuUI Instance { get; private set; }
    private void Awake() // thêm
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open(List<Pokemon> pokemons, PartyMenuMode mode, Action<Pokemon> onSelected, Action onCancel, string promptText = null)
    {
        gameObject.SetActive(true);
        this.mode = mode;
        this.onSelected = onSelected;
        this.onCancel = onCancel;
        this.pokemons = pokemons;
        firstSelectedIndex = -1;

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
        string hint = (mode == PartyMenuMode.Switch)
            ? "[Z] Swap  [C] Vào kho"
            : (string.IsNullOrWhiteSpace(promptText) ? "Info" : promptText);
        UpdateBottomPrompt(hint);
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
                        UpdateBottomPrompt("[Z] Xác nhận  [X] Thoát");
                    }
                    else
                    {
                        // switch the two selected Pokémon
                        var partyHandler = new PartyHandler(pokemons);
                        partyHandler.SwitchPokemon(firstSelectedIndex, currentIndex);
                        pokemons = partyHandler.GetPokemons();
                        RefreshSlots();
                        firstSelectedIndex = -1;
                        UpdateBottomPrompt("[Z] Swap  [C] Vào kho");
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            if (mode == PartyMenuMode.Switch && firstSelectedIndex >= 0)
            {
                firstSelectedIndex = -1;
                UpdateBottomPrompt("[Z] Swap  [C] Vào kho");
            }
            else
            {
                onCancel?.Invoke();
                infoUI.Hide();
                Close();
            }
        }
        else if (Input.GetKeyDown(KeyCode.C) && mode == PartyMenuMode.Switch)
        {
            var storage = StorageSystem.Instance;
            if (storage == null)
            {
                ToastNotificationManager.Instance?.Show("Kho không khả dụng.", Color.yellow);
                return;
            }
            var party = PlayerParty.Instance;
            if (party == null || party.Pokemons.Count <= 1)
            {
                ToastNotificationManager.Instance?.Show("Không thể gửi Pokemon cuối cùng vào kho!", Color.yellow);
                return;
            }
            var selected = slotUIs[currentIndex].Pokemon;
            party.RemovePokemon(selected);
            storage.AddPokemon(selected);
            ToastNotificationManager.Instance?.Show($"Đã gửi {selected.Base.Name} vào kho!", Color.white);
            pokemons = party.Pokemons;
            currentIndex = Mathf.Clamp(currentIndex, 0, pokemons.Count - 1);
            RefreshSlots();
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
        if (bottomPromptText != null)
            bottomPromptText.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    private void UpdateBottomPrompt(string promptText)
    {
        if (bottomPromptText == null)
        {
            bottomPromptText = EnsureBottomPromptText();
        }

        if (bottomPromptText == null)
            return;

        if (string.IsNullOrWhiteSpace(promptText))
        {
            bottomPromptText.gameObject.SetActive(false);
            return;
        }

        bottomPromptText.gameObject.SetActive(true);
        bottomPromptText.text = promptText;
    }

    private TMP_Text EnsureBottomPromptText()
    {
        var existing = transform.Find("BottomPrompt");
        if (existing != null)
            return existing.GetComponent<TMP_Text>();

        var go = new GameObject("BottomPrompt");
        go.transform.SetParent(transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 18f);
        rect.sizeDelta = new Vector2(400f, 40f);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 28;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
