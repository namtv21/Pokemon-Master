using UnityEngine;
using System;

public enum MenuState { None, Main, Party, Item, Storage, Quest, Pokedex, SaveLoad, Option }

public class MenuController : MonoBehaviour
{
    public static MenuController Instance { get; private set; }

    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private PartyMenuUI partyMenuUI;
    [SerializeField] private ItemMenuUI itemMenuUI;
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemHandler itemHandler;
    [SerializeField] private ItemAmountSelectorUI itemAmountSelectorUI;
    [SerializeField] private PokemonInfoUI pokemonInfoUI;
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private SaveLoadMenuUI saveLoadMenuUI;
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private QuestMenuUI questMenuUI;
    [SerializeField] private PokemonDexMenuUI pokemonDexMenuUI;

    public Inventory Inventory => inventory;

    private MenuState currentState = MenuState.None;
    private ItemBase selectedItemForUse;
    private int selectedExpAmountForUse;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CloseAll();
    }

    public event System.Action<MenuState> OnStateChanged;
    public void SetState(MenuState newState)
    {
        currentState = newState;
        OnStateChanged?.Invoke(newState);

    }
    /// Mở menu chính
    public void OpenMainMenu()
    {
        SetState(MenuState.Main);
        if (mainMenuUI != null)
            mainMenuUI.Open(OnMenuSelected, CloseAll);
        else
            ToastNotificationManager.Instance?.Show("Main menu is unavailable.", Color.yellow);
    }

    public void HandleUpdate(System.Action onClose)
    {
        bool wasInMenu = currentState != MenuState.None;

        switch (currentState)
        {
            case MenuState.Main:
                mainMenuUI?.HandleUpdate();
                break;
            case MenuState.Item:
                itemMenuUI?.HandleUpdate();
                break;
            case MenuState.Party:
                partyMenuUI?.HandleUpdate();
                break;
            case MenuState.Storage:
                var storageInMenu = StorageSystem.Instance;
                if (storageInMenu != null)
                    storageInMenu.HandleUpdate();
                if (Input.GetKeyDown(KeyCode.X))
                {
                    storageInMenu?.CloseStorage();
                    currentState = MenuState.Main;
                    mainMenuUI?.Open(OnMenuSelected, CloseAll);
                }
                break;
            case MenuState.SaveLoad:
                saveLoadMenuUI?.HandleUpdate(
                    onCancel: () =>
                    {
                        currentState = MenuState.Main;
                        mainMenuUI?.Open(OnMenuSelected, CloseAll);
                    },
                    onSaveCompleted: () =>
                    {
                        currentState = MenuState.Main;
                        mainMenuUI?.Open(OnMenuSelected, CloseAll);
                    },
                    onLoadCompleted: () =>
                    {
                        CloseAll();
                    });
                break;
            case MenuState.Option:
                audioSettings.HandleUpdate(() =>
                {
                    audioSettings.Close();
                    mainMenuUI.Open(OnMenuSelected, CloseAll);
                    currentState = MenuState.Main;
                });
                break;
            case MenuState.Quest:
                questMenuUI?.HandleUpdate();
                if (Input.GetKeyDown(KeyCode.X))
                {
                    questMenuUI?.Close();
                    currentState = MenuState.Main;
                    mainMenuUI?.Open(OnMenuSelected, CloseAll);
                }
                break;
            case MenuState.Pokedex:
                pokemonDexMenuUI?.HandleUpdate(() =>
                {
                    pokemonDexMenuUI?.Close();
                    currentState = MenuState.Main;
                    mainMenuUI?.Open(OnMenuSelected, CloseAll);
                });
                break;

        }

        if (wasInMenu && currentState == MenuState.None)
            onClose?.Invoke();
    }

    /// Callback khi chọn option trong MainMenu
    private void OnMenuSelected(MainMenuOption option)
    {
        switch (option)
        {
            case MainMenuOption.Party:
                mainMenuUI.Close();
                SetState(MenuState.Party);
                partyMenuUI.Open(PlayerParty.Instance.Pokemons, PartyMenuMode.Switch,
                    null,
                    CloseAll,
                    "Move");

                break;

            case MainMenuOption.Item:
                OpenItemMenu();
                break;

            case MainMenuOption.Storage:
                mainMenuUI.Close();
                SetState(MenuState.Storage);
                var storage = StorageSystem.Instance;
                if (storage != null)
                {
                    storage.OpenStorage();
                }
                else
                {
                    ToastNotificationManager.Instance?.Show("Storage system is unavailable.", Color.yellow);
                    CloseAll();
                }
                break;

            case MainMenuOption.Quest:
                mainMenuUI.Close();
                SetState(MenuState.Quest);
                if (questMenuUI != null)
                    questMenuUI.Open();
                else
                {
                    ToastNotificationManager.Instance?.Show("Quest menu is unavailable.", Color.yellow);
                    CloseAll();
                }
                break;

            case MainMenuOption.PokemonDex:
                mainMenuUI.Close();
                SetState(MenuState.Pokedex);
                if (pokemonDexMenuUI != null)
                    pokemonDexMenuUI.Open();
                else
                {
                    ToastNotificationManager.Instance?.Show("PokemonDex menu is unavailable.", Color.yellow);
                    CloseAll();
                }
                break;

            case MainMenuOption.SaveLoad:
                mainMenuUI.Close();
                SetState(MenuState.SaveLoad);
                if (saveLoadMenuUI != null)
                    saveLoadMenuUI.Open(true);
                else
                {
                    ToastNotificationManager.Instance?.Show("Save/Load menu is unavailable.", Color.yellow);
                    CloseAll();
                }
                break;

            case MainMenuOption.Option:
                mainMenuUI.Close();
                SetState(MenuState.Option);
                if (audioSettings != null)
                    audioSettings.Open();
                else
                {
                    ToastNotificationManager.Instance?.Show("Options are unavailable.", Color.yellow);
                    CloseAll();
                }
                break;
            
            case MainMenuOption.Exit:
                Application.Quit();
                break;
        }
    }

    /// Đóng tất cả menu
    public void CloseAll()
    {
        currentState = MenuState.None;
        mainMenuUI?.Close();
        itemMenuUI?.CloseMenu();
        partyMenuUI?.Close();
        var storage = StorageSystem.Instance;
        if (storage != null)
            storage.CloseStorage();
        saveLoadMenuUI?.Close();
        audioSettings?.Close();
        pokemonDexMenuUI?.Close();
        pokemonInfoUI?.Hide();
        HideAmountSelector();
        // Nếu có StorageMenu, SaveMenu, LoadMenu, OptionMenu thì Close ở đây
    }

    private void OpenItemMenu()
    {
        mainMenuUI.Close();
        SetState(MenuState.Item);
        if (itemMenuUI == null || inventory == null || itemHandler == null)
        {
            ToastNotificationManager.Instance?.Show("Item menu is unavailable.", Color.yellow);
            CloseAll();
            return;
        }

        var slotsList = inventory.GetSlots();
        if (inventory.ExperienceBottleItem != null && !slotsList.Exists(s => s.item == inventory.ExperienceBottleItem))
        {
            inventory.AddItem(inventory.ExperienceBottleItem, 1);
            var ensuredBottleExp = inventory.GetExperienceBottleExp(inventory.ExperienceBottleItem);
            if (ensuredBottleExp < 0)
                Debug.LogWarning("Experience bottle slot was created without valid stored exp.");
        }

        itemMenuUI.OpenMenu(inventory.GetSlots(),
            (itemBase) =>
            {
                if (itemBase == null)
                {
                    CloseAll();
                    return;
                }

                if (itemBase.itemType == ItemType.Pokeball || (itemBase.itemType == ItemType.KeyItem && !itemBase.isExperienceBottle))
                {
                    ToastNotificationManager.Instance?.Show($"Không thể dùng {itemBase.itemName} ở đây.", Color.yellow);
                    CloseAll();
                    return;
                }

                if (itemBase.isExperienceBottle)
                {
                    int availableExp = inventory.GetExperienceBottleExp(itemBase);
                    if (availableExp <= 0)
                    {
                        ToastNotificationManager.Instance?.Show($"{itemBase.itemName} chưa có EXP tích lũy.", Color.yellow);
                        return;
                    }

                    selectedItemForUse = itemBase;
                    itemMenuUI.CloseMenu();
                    ShowAmountSelector(
                        availableExp,
                        amount =>
                        {
                            selectedExpAmountForUse = amount;
                            OpenPokemonTargetSelectionForSelectedItem();
                        },
                        () =>
                        {
                            selectedItemForUse = null;
                            selectedExpAmountForUse = 0;
                            OpenItemMenu();
                        });
                    return;
                }

                selectedItemForUse = itemBase;
                selectedExpAmountForUse = 0;
                OpenPokemonTargetSelectionForSelectedItem();
            },
            CloseAll);
    }

    private void OpenPokemonTargetSelectionForSelectedItem()
    {
        if (selectedItemForUse == null)
            return;

        currentState = MenuState.Party;
        partyMenuUI.Open(PlayerParty.Instance.Pokemons, PartyMenuMode.Selection,
            (pokemon) =>
            {
                StartCoroutine(itemHandler.UseItemOnPokemon(selectedItemForUse, pokemon, selectedExpAmountForUse));
            },
            () =>
            {
                selectedItemForUse = null;
                selectedExpAmountForUse = 0;
                OpenItemMenu();
            });
    }

    private ItemAmountSelectorUI EnsureAmountSelector()
    {
        if (itemAmountSelectorUI != null)
            return itemAmountSelectorUI;

        itemAmountSelectorUI = ItemAmountSelectorUI.GetOrCreate();
        return itemAmountSelectorUI;
    }

    private void ShowAmountSelector(int maxAmount, Action<int> onSelected, Action onCancel)
    {
        var selector = EnsureAmountSelector();
        if (selector == null)
        {
            onSelected?.Invoke(maxAmount);
            return;
        }

        selector.Show(maxAmount, onSelected, onCancel);
    }

    private void HideAmountSelector()
    {
        if (itemAmountSelectorUI == null)
            return;

        itemAmountSelectorUI.Hide();
    }
}
