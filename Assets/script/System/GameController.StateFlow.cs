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

    private System.Collections.IEnumerator Start()
    {
        SetState(GameState.Overworld);
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.OnDialogStarted += OnDialogStarted;
            DialogManager.Instance.OnDialogFinished += OnDialogFinished;
        }

        yield return SaveLoadSystem.ApplyLoadedDataWhenReady();
        // Prologue đã bị bỏ — luôn coi là đã hoàn thành
        if (StoryFlags.Instance != null) StoryFlags.Instance.PrologueDone = true;
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
        if (State != GameState.Battle && State != GameState.Shop && State != GameState.Storage && State != GameState.Cutscene && State != GameState.NPCInteraction)
            SetState(GameState.Dialog);
    }

    private void OnDialogFinished()
    {
        if (State == GameState.Dialog)
            SetState(GameState.Overworld);
    }

    public void SetState(GameState newState)
    {
        if (battleSceneLoaded && battleCameraIsolationActive && State == GameState.Battle && newState != GameState.Battle)
        {
            Debug.LogWarning($"[Battle] Ignored state change from Battle to {newState} while BattleScene is active.");
            return;
        }

        State = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void PrepareForSaveLoad()
    {
        StopAllCoroutines();

        SetBattleCameraIsolation(false);
        SetOverworldSceneVisibility(true);

        battleSceneLoaded = false;
        battleSystem = null;
        battleTransition = null;
        isSceneTransitioning = false;
        pendingBattleAllowRun = true;
        activeOverworldPokemon = null;
        activeOverworldPokemonCaptured = false;
        cachedOverworldSceneName = null;
        LastBattleOutcome = BattleOutcome.None;

        SetState(GameState.Overworld);
    }

    private void Update()
    {
        if (SaveLoadSystem.IsLoadInProgress)
            return;

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
                DialogManager.Instance?.HandleUpdate();
                break;

            case GameState.Battle:
                HandleBattleUpdate();
                break;
        }
    }

    private void LateUpdate()
    {
        if (battleSceneLoaded && battleCameraIsolationActive)
            EnforceBattleCameraSettings();
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
        // Battle input is handled by BattleSystem. Camera isolation is maintained in LateUpdate.
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
