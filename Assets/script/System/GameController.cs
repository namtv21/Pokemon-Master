using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    Overworld,
    Battle,
    Dialog,
    Menu,
    NPCInteraction,
    Shop,
    Storage,
    Cutscene,
}

public enum BattleOutcome
{
    None,
    Win,
    Lose,
    Escape,
    Capture
}

public partial class GameController : MonoBehaviour
{
    [Header("Core systems")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MenuController menuController;

    [Header("Battle scene")]
    [SerializeField] private string battleSceneName = "BattleScene";
    private BattleTransition battleTransition; // runtime bind
    private BattleSystem battleSystem;         // runtime bind

    [Header("Battle FX")]
    [SerializeField] private float battleFallbackFadeDuration = 0.2f;
    [SerializeField] private float battleEndRevealDuration = 0.25f;

    public PlayerParty PlayerParty => PlayerParty.Instance;
    public StorageSystem StorageSystem => StorageSystem.Instance;
    public ShopUI ShopUI => ShopUI.Instance;
    public PartyMenuUI PartyMenuUI => PartyMenuUI.Instance;
    public BattleSystem BattleSystem => battleSystem;
    public BattleUnit EnemyUnit => battleSystem?.EnemyUnit;
    public BattleOutcome LastBattleOutcome { get; private set; } = BattleOutcome.None;
    public bool WasLastBattleSuccessful => LastBattleOutcome == BattleOutcome.Win || LastBattleOutcome == BattleOutcome.Capture;

    public static GameController Instance { get; private set; }
    public GameState State { get; private set; }
    public event System.Action<GameState> OnStateChanged;

    private string cachedOverworldSceneName;
    private bool battleSceneLoaded;
    private readonly Dictionary<Camera, bool> cachedCameraEnabledStates = new();
    private readonly Dictionary<AudioListener, bool> cachedListenerEnabledStates = new();
    private readonly Dictionary<GameObject, bool> cachedOverworldRootStates = new();
    private float nextBattleCameraEnforceTime;
    private CanvasGroup sceneFadeCanvasGroup;
    // scene fade is handled by SceneFadeController
    private bool isSceneTransitioning;
    private bool pendingBattleAllowRun = true;
    private OverworldPokemon activeOverworldPokemon;
    private bool activeOverworldPokemonCaptured;

    private PlayerController ResolvePlayerController()
    {
        if (playerController != null)
            return playerController;

        playerController = PlayerController.Instance;
        if (playerController != null)
            return playerController;

        playerController = FindObjectOfType<PlayerController>();
        return playerController;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DuplicateSystemRootUtility.DestroyDuplicate(this, Instance);
            return;
        }

        Instance = this;
    }

    private BattleContext pendingBattleContext = BattleContext.Grass;

    private BattleContext DetectBattleContext()
    {
        string scene = SceneManager.GetActiveScene().name.ToLower();
        if (scene.Contains("cave")) return BattleContext.Cave;
        if (scene.Contains("gym") || scene.Contains("poke") || scene.Contains("mart") ||
            scene.Contains("lab") || scene.Contains("studio"))
            return BattleContext.Indoor;
        return BattleContext.Grass;
    }

    public void StartWildBattle(Pokemon wildPokemon)
    {
        StartWildBattle(wildPokemon, true, DetectBattleContext());
    }

    public void StartWildBattle(Pokemon wildPokemon, bool allowRun)
    {
        StartWildBattle(wildPokemon, allowRun, DetectBattleContext());
    }

    public void StartWildBattle(Pokemon wildPokemon, bool allowRun, BattleContext context)
    {
        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (State == GameState.Battle || isSceneTransitioning || battleSceneLoaded || (battleScene.IsValid() && battleScene.isLoaded))
        {
            Debug.LogWarning("[Battle] Ignored duplicate wild battle start request.");
            return;
        }

        LastBattleOutcome = BattleOutcome.None;
        pendingBattleAllowRun = allowRun;
        pendingBattleContext = context;
        StartCoroutine(StartWildBattleRoutine(wildPokemon));
    }

    public void StartOverworldPokemonBattle(OverworldPokemon source, bool allowRun = true)
    {
        if (source == null)
            return;

        var wildPokemon = source.CreateBattlePokemon();
        if (wildPokemon == null)
            return;

        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (State == GameState.Battle || isSceneTransitioning || battleSceneLoaded || (battleScene.IsValid() && battleScene.isLoaded))
        {
            Debug.LogWarning("[Battle] Ignored duplicate overworld pokemon battle start request.");
            return;
        }

        activeOverworldPokemon = source;
        activeOverworldPokemonCaptured = false;
        StartWildBattle(wildPokemon, allowRun);
    }

    public void NotifyActiveOverworldPokemonCaptured()
    {
        activeOverworldPokemonCaptured = true;
    }

    public bool TryReceivePokemon(Pokemon sourcePokemon, bool submitCaughtEvent = true)
    {
        if (sourcePokemon == null || sourcePokemon.Base == null)
            return false;

        var playerParty = PlayerParty.Instance;
        if (playerParty == null)
            return false;

        bool sentToStorage = playerParty.Pokemons.Count >= 6;
        if (sentToStorage && StorageSystem.Instance == null)
            return false;

        var ownedPokemon = sourcePokemon.CloneAsOwned();

        // MarkCaught trước để CatchAllPokemon quest đọc đúng count
        PokedexManager.GetOrCreate().MarkCaught(sourcePokemon);

        if (submitCaughtEvent)
        {
            QuestManager.Instance?.SubmitEvent(
                new QuestEvent(QuestEventType.PokemonCaught, sourcePokemon.Base.Name, 1)
            );
        }

        if (sentToStorage)
            StorageSystem.Instance?.AddPokemon(ownedPokemon);
        else
            playerParty.AddPokemon(ownedPokemon);

        QuestManager.Instance?.SubmitEvent(
            new QuestEvent(QuestEventType.PokemonOwned, sourcePokemon.Base.Name, 1)
        );

        if (sentToStorage)
            ToastNotificationManager.Instance?.Show($"{sourcePokemon.Base.Name} was sent to storage!");
        else
            ToastNotificationManager.Instance?.Show($"{sourcePokemon.Base.Name} was added to your party!");

        return true;
    }

    private IEnumerator StartWildBattleRoutine(Pokemon wildPokemon)
    {
        cachedOverworldSceneName = SceneManager.GetActiveScene().name;
        SetState(GameState.Battle);

        yield return EnsureBattleSceneLoadedAndBound();
        if (battleSystem == null)
        {
            yield return RecoverFromBattleStartupFailure();
            yield break;
        }

        SetOverworldSceneVisibility(false);
        SetBattleCameraIsolation(true);

        if (battleTransition != null)
        {
            yield return battleTransition.PlayClose();
        }
        else
        {
            var fadeCtrl = GetOrCreateSceneFadeController();
            if (fadeCtrl != null)
                yield return fadeCtrl.Fade(1f, battleFallbackFadeDuration);
        }

        battleSystem.SetBackground(pendingBattleContext);
        battleSystem.StartWildBattle(wildPokemon, pendingBattleAllowRun);
        pendingBattleAllowRun = true;

        if (battleSystem == null || battleSystem.IsEndingBattle)
            yield break;

        if (battleTransition != null)
        {
            yield return battleTransition.PlayOpen();
        }
        else
        {
            var fadeCtrl = GetOrCreateSceneFadeController();
            if (fadeCtrl != null)
                yield return fadeCtrl.Fade(0f, battleFallbackFadeDuration);
        }
    }

    private IEnumerator RecoverFromBattleStartupFailure()
    {
        Debug.LogError("[Battle] BattleScene could not be initialized. Returning to the overworld.");
        SetBattleCameraIsolation(false);
        SetOverworldSceneVisibility(true);
        RestoreOverworldAsActiveScene();

        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (battleScene.IsValid() && battleScene.isLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(battleSceneName);
            if (unload != null)
                yield return unload;
        }

        battleSceneLoaded = false;
        battleSystem = null;
        battleTransition = null;
        pendingBattleAllowRun = true;
        SetState(GameState.Overworld);

        activeOverworldPokemon?.HandleBattleFinished(false);
        activeOverworldPokemon = null;
        activeOverworldPokemonCaptured = false;
        yield return FadeSceneOverlay(0f, battleEndRevealDuration);
    }

    private void RestoreOverworldAsActiveScene()
    {
        if (string.IsNullOrWhiteSpace(cachedOverworldSceneName))
            return;

        var overworldScene = SceneManager.GetSceneByName(cachedOverworldSceneName);
        if (overworldScene.IsValid() && overworldScene.isLoaded)
            SceneManager.SetActiveScene(overworldScene);
    }

    public void StartTrainerBattle(NPC trainer)
    {
        StartTrainerBattle(trainer, false);
    }

    public void StartTrainerBattle(NPC trainer, bool allowRun)
    {
        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (State == GameState.Battle || isSceneTransitioning || battleSceneLoaded || (battleScene.IsValid() && battleScene.isLoaded))
        {
            Debug.LogWarning("[Battle] Ignored duplicate trainer battle start request.");
            return;
        }

        LastBattleOutcome = BattleOutcome.None;
        pendingBattleAllowRun = allowRun;
        pendingBattleContext = DetectBattleContext();
        StartCoroutine(StartTrainerBattleRoutine(trainer));
    }

    private IEnumerator StartTrainerBattleRoutine(NPC trainer)
    {
        cachedOverworldSceneName = SceneManager.GetActiveScene().name;
        SetState(GameState.Battle);

        yield return EnsureBattleSceneLoadedAndBound();
        if (battleSystem == null)
        {
            yield return RecoverFromBattleStartupFailure();
            yield break;
        }

        SetOverworldSceneVisibility(false);
        SetBattleCameraIsolation(true);

        if (battleTransition != null)
        {
            yield return battleTransition.PlayClose();
        }
        else
        {
            var fadeCtrl = GetOrCreateSceneFadeController();
            if (fadeCtrl != null)
                yield return fadeCtrl.Fade(1f, battleFallbackFadeDuration);
        }

        battleSystem.SetBackground(pendingBattleContext);
        battleSystem.StartTrainerBattle(trainer, pendingBattleAllowRun);
        pendingBattleAllowRun = true;

        if (battleSystem == null || battleSystem.IsEndingBattle)
            yield break;

        if (battleTransition != null)
        {
            yield return battleTransition.PlayOpen();
        }
        else
        {
            var fadeCtrl = GetOrCreateSceneFadeController();
            if (fadeCtrl != null)
                yield return fadeCtrl.Fade(0f, battleFallbackFadeDuration);
        }
    }

    public void EndBattle()
    {
        if (!isActiveAndEnabled)
        {
            var root = transform.root;
            if (root != null && !root.gameObject.activeSelf)
                root.gameObject.SetActive(true);

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogError("GameController is still inactive, cannot start EndBattle coroutine.");
            return;
        }

        StartCoroutine(EndBattleRoutine());
    }

    private IEnumerator EndBattleRoutine()
    {
        var defeatedTrainer = battleSystem != null ? battleSystem.CurrentTrainer : null;
        var defeatedWildPokemon = battleSystem != null && battleSystem.EnemyUnit != null
            ? battleSystem.EnemyUnit.Pokemon
            : null;
        LastBattleOutcome = battleSystem != null ? battleSystem.Outcome : BattleOutcome.None;
        bool shouldAutoCaptureOverworldPokemon =
            activeOverworldPokemon != null &&
            !activeOverworldPokemonCaptured &&
            defeatedWildPokemon != null &&
            defeatedWildPokemon.IsFainted;

        if (shouldAutoCaptureOverworldPokemon && TryReceivePokemon(defeatedWildPokemon))
            activeOverworldPokemonCaptured = true;

        if (battleTransition != null)
        {
            yield return battleTransition.PlayClose();
            SetSceneFadeImmediate(1f);
        }
        else
        {
            yield return FadeSceneOverlay(1f, battleFallbackFadeDuration);
        }

        SetBattleCameraIsolation(false);
        SetOverworldSceneVisibility(true);
        nextBattleCameraEnforceTime = 0f;
        RestoreOverworldAsActiveScene();

        if (battleSceneLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(battleSceneName);
            if (unload != null) yield return unload;
            battleSceneLoaded = false;
        }
        battleSystem = null;
        battleTransition = null;

        SetState(GameState.Overworld);
        if (playerController != null) playerController.enabled = true;

        yield return FadeSceneOverlay(0f, battleEndRevealDuration);

        if (PlayerParty.Instance != null)
        {
            PlayerParty.Instance.RecordBattleParticipation();
            yield return StartCoroutine(ProcessPostBattleEvolution());
        }

        if (LastBattleOutcome == BattleOutcome.Win || LastBattleOutcome == BattleOutcome.Capture)
        {
            defeatedTrainer?.OnBattleEnded();
            // Flag có thể vừa được set bởi NPC (setStoryFlagAfterBadge) → check story step mới trong cùng scene
            if (MainStoryDirector.Instance != null) MainStoryDirector.Instance.TryPlayAfterBattle();
        }

        activeOverworldPokemon?.HandleBattleFinished(activeOverworldPokemonCaptured);
        activeOverworldPokemon = null;
        activeOverworldPokemonCaptured = false;
    }

    private IEnumerator ProcessPostBattleEvolution()
    {
        if (PlayerParty.Instance == null || PlayerParty.Instance.Pokemons == null)
            yield break;

        var partySnapshot = new List<Pokemon>(PlayerParty.Instance.Pokemons);
        foreach (var pokemon in partySnapshot)
            yield return StartCoroutine(ProcessPokemonEvolution(pokemon));
    }

    public IEnumerator GainExpAndProcessEvolution(Pokemon pokemon, int expAmount, bool awardBonusExp = false)
    {
        if (pokemon == null || expAmount <= 0)
            yield break;

        var previousState = State;
        if (State != GameState.Battle)
            SetState(GameState.Cutscene);

        pokemon.GainExp(expAmount, awardBonusExp, autoEvolveWhenUnobserved: false);
        yield return StartCoroutine(ProcessPokemonEvolution(pokemon));

        if (State == GameState.Cutscene)
            SetState(previousState == GameState.Battle ? GameState.Overworld : previousState);
    }

    public IEnumerator ProcessPokemonEvolution(Pokemon pokemon)
    {
        if (pokemon == null)
            yield break;

        while (pokemon.CanEvolveNow())
        {
            var options = pokemon.GetAvailableEvolutionOptions();
            if (options.Count == 0)
                yield break;

            EvolutionOption selectedOption = options[0];
            if (options.Count > 1)
            {
                yield return StartCoroutine(ShowEvolutionChoice(pokemon, options, option => selectedOption = option));
                if (selectedOption == null)
                    yield break;
            }

            var oldBase = pokemon.Base;
            var newBase = selectedOption.EvolvesTo;
            string oldName = oldBase != null ? oldBase.Name : pokemon.Base.Name;
            string targetName = newBase != null ? newBase.Name : selectedOption.Label;

            yield return ShowDialogAndWait($"{oldName} is evolving!");
            yield return FadeSceneOverlay(1f, 0.35f);

            Sprite beforeSprite = oldBase != null ? oldBase.FrontSprite : null;
            Sprite afterSprite = newBase != null ? newBase.FrontSprite : null;
            yield return StartCoroutine(PlayEvolutionVisual(beforeSprite, afterSprite));

            bool evolved = pokemon.TryEvolve(selectedOption);
            yield return FadeSceneOverlay(0f, 0.35f);

            if (!evolved)
                break;

            PokedexManager.GetOrCreate().MarkCaught(pokemon);
            yield return ShowDialogAndWait($"Congratulations! {oldName} evolved into {targetName}!");
        }
    }

    private IEnumerator ShowEvolutionChoice(Pokemon pokemon, List<EvolutionOption> options, System.Action<EvolutionOption> onSelected)
    {
        var canvasGO = new GameObject("EvolutionChoiceCanvas");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10001;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720, 420);
        panelRect.anchoredPosition = Vector2.zero;
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.86f);

        var textGO = new GameObject("ChoiceText");
        textGO.transform.SetParent(panelGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(48, 36);
        textRect.offsetMax = new Vector2(-48, -36);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 34;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;

        int index = 0;
        bool done = false;
        EvolutionOption selected = null;

        void Refresh()
        {
            string pokemonName = pokemon != null && pokemon.Base != null ? pokemon.Base.Name : "Pokemon";
            var lines = new List<string> { $"{pokemonName} can evolve. Choose a form:", string.Empty };
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                string name = option != null && option.EvolvesTo != null ? option.EvolvesTo.Name : option?.Label;
                lines.Add($"{(i == index ? "> " : "  ")}{name}");
            }
            lines.Add(string.Empty);
            lines.Add("[Z] Choose   [X] Not now");
            text.text = string.Join("\n", lines);
        }

        Refresh();
        while (!done)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                index = (index - 1 + options.Count) % options.Count;
                Refresh();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                index = (index + 1) % options.Count;
                Refresh();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                selected = options[index];
                done = true;
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                selected = null;
                done = true;
            }

            yield return null;
        }

        Object.Destroy(canvasGO);
        onSelected?.Invoke(selected);
    }

    // Evolution visual was moved to EvolutionVisuals.cs

    private IEnumerator ShowDialogAndWait(string line)
    {
        var dialogManager = DialogManager.Instance;
        if (dialogManager == null)
            yield break;

        bool finished = false;

        void OnFinish()
        {
            finished = true;
        }

        dialogManager.OnDialogFinished += OnFinish;
        dialogManager.ShowDialog(line, GameState.Overworld);

        while (!finished)
            yield return null;

        dialogManager.OnDialogFinished -= OnFinish;
    }

    private IEnumerator EnsureBattleSceneLoadedAndBound()
    {
        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        bool sceneAlreadyLoaded = battleScene.IsValid() && battleScene.isLoaded;

        if (!sceneAlreadyLoaded)
        {
            var op = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"Cannot load battle scene: {battleSceneName}");
                yield break;
            }
            yield return op;
            battleScene = SceneManager.GetSceneByName(battleSceneName);
        }

        battleSceneLoaded = battleScene.IsValid() && battleScene.isLoaded;

        if (battleSceneLoaded)
            SceneManager.SetActiveScene(battleScene);

        BindBattleReferencesFromScene(battleScene);

        if (battleSystem == null)
        {
            Debug.LogError($"[Battle] FAILED: BattleSystem not found in {battleSceneName}!\n" +
                          "Check that:\n" +
                          "1. BattleScene exists in project\n" +
                          "2. BattleSystem script is attached to a GameObject in BattleScene\n" +
                          "3. That GameObject is a child of root (or nested inside)\n");
            yield break;
        }
        
    }

    private void BindBattleReferencesFromScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        var rootObjects = scene.GetRootGameObjects();
        
        foreach (var root in rootObjects)
        {
            // Method 1: GetComponentInChildren
            if (battleSystem == null)
            {
                battleSystem = root.GetComponentInChildren<BattleSystem>(true);
                if (battleSystem == null)
                {
                    // Method 2: Direct child search if Method 1 fails
                    var battleObj = root.transform.Find("Battle");
                    if (battleObj != null)
                    {
                        battleSystem = battleObj.GetComponent<BattleSystem>();
                    }
                }
            }

            if (battleTransition == null)
            {
                battleTransition = root.GetComponentInChildren<BattleTransition>(true);
            }

            if (battleSystem != null && battleTransition != null)
                break;
        }

        if (battleSystem == null)
        {
            Debug.LogError($"[Bind] âœ— BattleSystem STILL NOT FOUND after searching all roots!\n" +
                          "SOLUTION:\n" +
                          "1. Open BattleScene\n" +
                          "2. Select 'Battle' GameObject in hierarchy\n" +
                          "3. In Inspector, click 'Add Component' and search for 'BattleSystem'\n" +
                          "4. Assign all SerializeFields (playerUnit, enemyUnit, battleUI, etc.)\n" +
                          "5. Save scene (Ctrl+S)\n" +
                          "6. Run again");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerController = PlayerController.Instance != null ? PlayerController.Instance : FindObjectOfType<PlayerController>();
        StartCoroutine(SaveLoadSystem.ApplyLoadedDataWhenReady());

        if (scene.name == battleSceneName)
            BindBattleReferencesFromScene(scene);
    }
}
