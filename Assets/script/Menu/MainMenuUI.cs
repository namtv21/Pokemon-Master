using UnityEngine;
using UnityEngine.UI;
using System;

public enum MainMenuOption { Party, Item, Quest, PokemonDex, SaveLoad, Option, Companion, Exit }

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;       // Panel chính
    [SerializeField] private GameObject[] optionPanels;  // Các panel con, mỗi panel chứa Text hoặc Image

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private MainMenuOption[] optionOrder;

    private static readonly MainMenuOption[] DefaultOptionOrder =
    {
        MainMenuOption.Party,
        MainMenuOption.Item,
        MainMenuOption.Companion,
        MainMenuOption.PokemonDex,
        MainMenuOption.Quest,
        MainMenuOption.SaveLoad,
        MainMenuOption.Option,
        MainMenuOption.Exit
    };

    private int currentIndex = 0;
    private Action<MainMenuOption> onSelected;
    private Action onClose;

    private void Awake()
    {
        // Keep a stable, explicit top-to-bottom menu flow.
        optionOrder = (MainMenuOption[])DefaultOptionOrder.Clone();
    }

    public void Open(Action<MainMenuOption> onSelectedCallback, Action onCloseCallback)
    {
        menuPanel.SetActive(true);
        currentIndex = 0;
        RefreshLabels();
        HighlightCurrent();

        onSelected = onSelectedCallback;
        onClose = onCloseCallback;
    }

    public void Close()
    {
        menuPanel.SetActive(false);
        onSelected = null;
        onClose = null;
    }

    public void HandleUpdate()
    {
        int visibleCount = GetVisibleOptionCount();
        if (visibleCount <= 0)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentIndex == 0)
                currentIndex = visibleCount - 1;
            else
                currentIndex--;

            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentIndex == visibleCount - 1)
                currentIndex = 0;
            else
                currentIndex++;

            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            onSelected?.Invoke(GetOptionAtIndex(currentIndex));
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            var closeCallback = onClose;
            Close();
            closeCallback?.Invoke();
        }
    }

    private void HighlightCurrent()
    {
        for (int i = 0; i < optionPanels.Length; i++)
        {
            var panel = optionPanels[i];
            if (panel == null) continue;

            bool hasOption = optionOrder != null && i < optionOrder.Length;
            panel.SetActive(hasOption);
            if (!hasOption)
                continue;

            // Lấy Text hoặc Image trong panel để đổi màu
            var text = panel.GetComponentInChildren<Text>();
            if (text != null)
                text.color = (i == currentIndex) ? highlightColor : normalColor;

            var img = panel.GetComponent<Image>();
            if (img != null)
                img.color = (i == currentIndex) ? highlightColor : normalColor;
        }
    }

    private void RefreshLabels()
    {
        for (int i = 0; i < optionPanels.Length; i++)
        {
            var panel = optionPanels[i];
            if (panel == null) continue;

            bool hasOption = optionOrder != null && i < optionOrder.Length;
            panel.SetActive(hasOption);
            if (!hasOption)
                continue;

            var text = panel.GetComponentInChildren<Text>();
            if (text != null)
                text.text = GetOptionLabel(GetOptionAtIndex(i));
        }
    }

    private string GetOptionLabel(MainMenuOption option)
    {
        switch (option)
        {
            case MainMenuOption.Party: return "Party";
            case MainMenuOption.Item: return "Item";
            case MainMenuOption.Companion: return "Chat";
            case MainMenuOption.PokemonDex: return "Pokédex";
            case MainMenuOption.Quest: return "Quest";
            case MainMenuOption.SaveLoad: return "Save/Load";
            case MainMenuOption.Option: return "Setting";
            case MainMenuOption.Exit: return "Exit Game";
            default: return option.ToString();
        }
    }

    private MainMenuOption GetOptionAtIndex(int index)
    {
        if (optionOrder != null && index >= 0 && index < optionOrder.Length)
            return optionOrder[index];

        return (MainMenuOption)index;
    }

    private int GetVisibleOptionCount()
    {
        return optionOrder != null ? optionOrder.Length : optionPanels != null ? optionPanels.Length : 0;
    }
}