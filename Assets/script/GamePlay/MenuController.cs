using UnityEngine;

public enum MenuState { None, Main, Party, Item, Storage, Quest, Pokedex, SaveLoad, Option }

public class MenuController : MonoBehaviour
{
    public static MenuController Instance { get; private set; }

    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private PartyMenuUI partyMenuUI;
    [SerializeField] private ItemMenuUI itemMenuUI;
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemHandler itemHandler;
    [SerializeField] private PokemonInfoUI pokemonInfoUI;
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private SaveLoadMenuUI saveLoadMenuUI;
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private QuestMenuUI questMenuUI;
    [SerializeField] private PokemonDexMenuUI pokemonDexMenuUI;

    public Inventory Inventory => inventory;

    private MenuState currentState = MenuState.None;

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
                saveLoadMenuUI?.HandleUpdate(() =>
                {
                    currentState = MenuState.Main;
                    mainMenuUI?.Open(OnMenuSelected, CloseAll);
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
                mainMenuUI.Close();
                SetState(MenuState.Item);
                if (itemMenuUI == null || inventory == null || itemHandler == null)
                {
                    ToastNotificationManager.Instance?.Show("Item menu is unavailable.", Color.yellow);
                    CloseAll();
                    return;
                }
                itemMenuUI.OpenMenu(inventory.GetSlots(),
                    (itemBase) =>
                    {
                        if (itemBase.itemType == ItemType.Pokeball || itemBase.itemType == ItemType.KeyItem)
                        {
                            ToastNotificationManager.Instance?.Show($"{itemBase.itemName} can't be used here.", Color.yellow);
                            CloseAll();
                            return;
                        }

                        // Sau khi chọn item, mở PartyMenu để chọn Pokémon target
                        currentState = MenuState.Party;
                        partyMenuUI.Open(PlayerParty.Instance.Pokemons, PartyMenuMode.Selection,
                            (pokemon) =>
                            {
                                StartCoroutine(itemHandler.UseItemOnPokemon(itemBase, pokemon));
                            },
                            CloseAll);
                    },
                    CloseAll);
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
        // Nếu có StorageMenu, SaveMenu, LoadMenu, OptionMenu thì Close ở đây
    }
}
