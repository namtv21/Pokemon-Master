using UnityEngine;

public enum MenuState { None, Main, Party, Item, Storage, Save, Load, Option, Quest }

public class MenuController : MonoBehaviour
{
    public static MenuController Instance { get; private set; }

    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private PartyMenuUI partyMenuUI;
    [SerializeField] private ItemMenuUI itemMenuUI;
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerParty playerParty;
    [SerializeField] private ItemHandler itemHandler;
    [SerializeField] private PokemonInfoUI pokemonInfoUI;
    [SerializeField] private StorageSystem storageSystem;
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private SaveLoadMenuUI saveLoadMenuUI;
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private QuestMenuUI questMenuUI;

    public Inventory Inventory => inventory;

    private MenuState currentState = MenuState.None;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        //DontDestroyOnLoad(gameObject);
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
        mainMenuUI.Open(OnMenuSelected, CloseAll);
    }

    public void HandleUpdate(System.Action onClose)
    {
        switch (currentState)
        {
            case MenuState.Main:
                mainMenuUI.HandleUpdate();
                break;
            case MenuState.Item:
                itemMenuUI.HandleUpdate();
                break;
            case MenuState.Party:
                partyMenuUI.HandleUpdate();
                break;
            case MenuState.Storage:
                storageSystem.HandleUpdate();
                break;
            case MenuState.Save:
                saveLoadMenuUI.HandleUpdate(() => 
                { 
                    currentState = MenuState.Main; 
                    mainMenuUI.Open(OnMenuSelected, CloseAll); 
                });
                break;
            case MenuState.Load:
                saveLoadMenuUI.HandleUpdate(() => 
                { 
                    currentState = MenuState.Main; 
                    mainMenuUI.Open(OnMenuSelected, CloseAll); 
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
                questMenuUI.HandleUpdate();
                if (Input.GetKeyDown(KeyCode.X))
                {
                    questMenuUI.Close();
                    currentState = MenuState.Main;
                    mainMenuUI.Open(OnMenuSelected, CloseAll);
                }
                break;


        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            CloseAll();
            onClose?.Invoke();
        }
    }

    /// Callback khi chọn option trong MainMenu
    private void OnMenuSelected(MainMenuOption option)
    {
        switch (option)
        {
            case MainMenuOption.Party:
                mainMenuUI.Close();
                SetState(MenuState.Party);
                partyMenuUI.Open(playerParty.Pokemons, PartyMenuMode.Switch,
                    null,
                    CloseAll);

                break;

            case MainMenuOption.Item:
                mainMenuUI.Close();
                SetState(MenuState.Item);
                itemMenuUI.OpenMenu(inventory.GetSlots(),
                    (itemBase) =>
                    {
                        if (itemBase.itemType == ItemType.Pokeball || itemBase.itemType == ItemType.KeyItem)
                        {
                            DialogManager.Instance.ShowDialogCoroutine($"{itemBase.itemName} can't be used here.");
                            CloseAll();
                            return;
                        }

                        // Sau khi chọn item, mở PartyMenu để chọn Pokémon target
                        currentState = MenuState.Party;
                        partyMenuUI.Open(playerParty.Pokemons, PartyMenuMode.Selection,
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
                storageSystem.OpenStorage();
                break;

            case MainMenuOption.Save:
                mainMenuUI.Close();
                SetState(MenuState.Save);
                saveLoadMenuUI.Open(true, true); // mở Save menu, inGame = true
                break;

            case MainMenuOption.Load:
                mainMenuUI.Close();
                SetState(MenuState.Load);
                saveLoadMenuUI.Open(false, true); // mở Load menu, inGame = true
                break;

            case MainMenuOption.Option:
                mainMenuUI.Close();
                SetState(MenuState.Option);
                audioSettings.Open();
                break;

            case MainMenuOption.Quest:
                mainMenuUI.Close();
                SetState(MenuState.Quest);
                questMenuUI.Open();
                break;
        }
    }

    /// Đóng tất cả menu
    public void CloseAll()
    {
        currentState = MenuState.None;
        mainMenuUI.Close();
        itemMenuUI.CloseMenu();
        partyMenuUI.Close();
        storageSystem.CloseStorage();
        saveLoadMenuUI.Close();
        audioSettings.Close();
        pokemonInfoUI.Hide();
        // Nếu có StorageMenu, SaveMenu, LoadMenu, OptionMenu thì Close ở đây
    }
}
