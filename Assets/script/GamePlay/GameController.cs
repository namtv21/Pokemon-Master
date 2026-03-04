using UnityEngine;

public enum GameState
{
    Overworld,
    Battle,
    Dialog,
    Noti,
    Menu,
    NPCInteraction,
    Shop,
    Storage,
    HealingCenter
}

public class GameController : MonoBehaviour
{
    [Header("Core systems")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MenuController menuController;
    [SerializeField] private DialogManager dialogManager;

     [Header("Player systems")]
    [SerializeField] private PlayerParty playerParty;
    [SerializeField] private StorageSystem storageSystem;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private PartyMenuUI partyMenuUI;
    public PlayerParty PlayerParty => playerParty;
    public StorageSystem StorageSystem => storageSystem;
    public ShopUI ShopUI => shopUI;

    public static GameController Instance { get; private set; }
    public GameState State { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetState(GameState.Overworld);
        SaveLoadSystem.ApplyLoadedData();
        // Đăng ký sự kiện từ DialogManager
        DialogManager.Instance.OnDialogStarted += () => SetState(GameState.Dialog);
        DialogManager.Instance.OnDialogFinished += () => SetState(GameState.Overworld);
    }

    public event System.Action<GameState> OnStateChanged;

    public void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
        // Debug.Log("Game State changed to: " + State);
    }

    private void Update()
    {
        switch (State)
        {
            case GameState.Overworld:
                playerController.HandleUpdate();
                if (Input.GetKeyDown(KeyCode.X))
                {
                    OpenMenu();
                }
                break;

            case GameState.Menu:
                menuController.HandleUpdate(() => SetState(GameState.Overworld));
                break;

            case GameState.Dialog:
                DialogManager.Instance.HandleUpdate();
                break;

            case GameState.Battle:
                // BattleSystem tự xử lý update
                break;

            case GameState.NPCInteraction:
                OptionUI.Instance.HandleUpdate();
                break;

            case GameState.Shop:
                    ShopUI.Instance.HandleUpdate();

                break;

            case GameState.Storage:
                partyMenuUI.HandleUpdate();
                break;
        }
    }

    // ----------------- Battle -----------------
    public void StartWildBattle(Pokemon wildPokemon)
    {
        SetState(GameState.Battle);
        var battleSystem = FindObjectOfType<BattleSystem>();
        playerController.enabled = false;
        battleSystem.gameObject.SetActive(true);
        battleSystem.StartWildBattle(wildPokemon);
    }

    public void StartTrainerBattle(NPC trainer)
    {
        SetState(GameState.Battle);
        var battleSystem = FindObjectOfType<BattleSystem>();
        playerController.enabled = false;
        battleSystem.gameObject.SetActive(true);
        battleSystem.StartTrainerBattle(trainer);
    }

    public void EndBattle()
    {
        SetState(GameState.Overworld);
        playerController.enabled = true;
    }

    // ----------------- Menu -----------------
    private void OpenMenu()
    {
        SetState(GameState.Menu);
        menuController.OpenMainMenu();
    }

    // ----------------- Player Systems -----------------
    public void HealAllPlayerPokemon()
    {
        playerParty.HealAll();
        DialogManager.Instance.ShowDialog("Your Pokémon have been fully healed!");
        SetState(GameState.Overworld);
    }

    public void OpenShop()
    {
        shopUI.Open();
        SetState(GameState.Shop);
    }

    public void OpenStorageParty(NPC currentNPC)
    {
        // Lấy danh sách Pokémon trong party
        var partyPokemons = PlayerParty.Pokemons;
        SetState(GameState.Storage);
        // Mở PartyMenu
        partyMenuUI.Open(
            partyPokemons,
            PartyMenuMode.Selection,
            onSelected: (pokemon) =>
            {
                // Gửi Pokémon vào storage
                currentNPC.SendPokemonToStorage(pokemon);
                partyMenuUI.Close();
            },
            onCancel: () =>
            {
                // Nếu hủy, quay lại OptionUI
                OptionUI.Instance.ShowOptions(currentNPC);
            }
        );
    }

}