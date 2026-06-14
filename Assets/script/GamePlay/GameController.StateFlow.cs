using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameController
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        SetState(GameState.Overworld);
        SaveLoadSystem.ApplyLoadedData();

        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.OnDialogStarted += OnDialogStarted;
            DialogManager.Instance.OnDialogFinished += OnDialogFinished;
        }
    }

    private void OnDestroy()
    {
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.OnDialogStarted -= OnDialogStarted;
            DialogManager.Instance.OnDialogFinished -= OnDialogFinished;
        }
    }

    private void OnDialogStarted()
    {
        if (State != GameState.Battle && State != GameState.Shop && State != GameState.Storage && State != GameState.Cutscene)
            SetState(GameState.Dialog);
    }

    private void OnDialogFinished()
    {
        if (State == GameState.Dialog)
            SetState(GameState.Overworld);
    }

    public void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }

    private void Update()
    {
        switch (State)
        {
            case GameState.Overworld:
                HandleOverworldUpdate();
                break;

            case GameState.Menu:
                menuController?.HandleUpdate(() => SetState(GameState.Overworld));
                break;

            case GameState.Dialog:
                DialogManager.Instance?.HandleUpdate();
                break;

            case GameState.NPCInteraction:
                OptionUI.Instance?.HandleUpdate();
                break;

            case GameState.Shop:
                ShopUI.Instance?.HandleUpdate();
                break;

            case GameState.Storage:
                HandleStorageUpdate();
                break;

            case GameState.Cutscene:
                break;

            case GameState.Battle:
                HandleBattleUpdate();
                break;
        }
    }

    private void HandleOverworldUpdate()
    {
        if (MainStoryDirector.Instance != null && MainStoryDirector.Instance.IsPlayingStep)
            return;

        var livePlayerController = ResolvePlayerController();
        if (livePlayerController != null)
            livePlayerController.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.X))
            OpenMenu();
    }

    private void HandleStorageUpdate()
    {
        if (PartyMenuUI != null && PartyMenuUI.gameObject.activeInHierarchy)
            PartyMenuUI.HandleUpdate();
        else if (StorageSystem != null && StorageSystem.gameObject.activeInHierarchy)
            StorageSystem.HandleUpdate();
    }

    private void HandleBattleUpdate()
    {
        if (Time.unscaledTime < nextBattleCameraEnforceTime)
            return;

        EnforceBattleCameraSettings();
        nextBattleCameraEnforceTime = Time.unscaledTime + 0.1f;
    }

    private void OpenMenu()
    {
        SetState(GameState.Menu);
        menuController?.OpenMainMenu();
    }

    public void HealAllPlayerPokemon()
    {
        PlayerParty.HealAll();
        ToastNotificationManager.Instance?.Show("Your Pokemon have been fully healed!");
        SetState(GameState.Overworld);
    }

    public void OpenShop()
    {
        ShopUI.Open();
        SetState(GameState.Shop);
    }

    public void OpenStorageParty(NPC currentNPC)
    {
        var partyPokemons = PlayerParty.Pokemons;
        SetState(GameState.Storage);

        PartyMenuUI.Open(
            partyPokemons,
            PartyMenuMode.Selection,
            onSelected: (pokemon) =>
            {
                currentNPC.SendPokemonToStorage(pokemon);
                PartyMenuUI.Close();
            },
            onCancel: () => OptionUI.Instance.ShowOptions(currentNPC)
        );
    }
}
