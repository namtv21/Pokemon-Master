using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
    Cutscene,
    HealingCenter,
    Quest
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
        if (scene.Contains("cave") || scene.Contains("mountain")) return BattleContext.Cave;
        if (scene.Contains("gym") || scene.Contains("poke") || scene.Contains("mart") ||
            scene.Contains("lab") || scene.Contains("studio") || scene.Contains("champion"))
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

    public void LoadSceneWithFade(string sceneName, string spawnPointId, float fadeOutDuration = 0.5f, float fadeInDuration = 0.25f)
    {
        if (isSceneTransitioning)
            return;

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName, spawnPointId, fadeOutDuration, fadeInDuration));
    }

    private IEnumerator StartWildBattleRoutine(Pokemon wildPokemon)
    {
        cachedOverworldSceneName = SceneManager.GetActiveScene().name;
        SetState(GameState.Battle);

        yield return EnsureBattleSceneLoadedAndBound();
        if (battleSystem == null) yield break;

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
        if (battleSystem == null) yield break;

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

        if (battleSceneLoaded)
        {
            var unload = SceneManager.UnloadSceneAsync(battleSceneName);
            if (unload != null) yield return unload;
            battleSceneLoaded = false;
        }
        battleSystem = null;
        battleTransition = null;

        if (!string.IsNullOrEmpty(cachedOverworldSceneName))
        {
            var scene = SceneManager.GetSceneByName(cachedOverworldSceneName);
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.SetActiveScene(scene);
        }

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
            var oldBase = pokemon.Base;
            var newBase = oldBase?.EvolvesTo;
            string oldName = oldBase != null ? oldBase.Name : pokemon.Base.Name;
            string targetName = newBase != null ? newBase.Name : pokemon.GetEvolutionTargetName();

            yield return ShowDialogAndWait($"{oldName} is evolving!");
            yield return FadeSceneOverlay(1f, 0.35f);

            Sprite beforeSprite = oldBase != null ? oldBase.FrontSprite : null;
            Sprite afterSprite = newBase != null ? newBase.FrontSprite : null;
            yield return StartCoroutine(PlayEvolutionVisual(beforeSprite, afterSprite));

            bool evolved = pokemon.TryEvolve();
            yield return FadeSceneOverlay(0f, 0.35f);

            if (!evolved)
                break;

            PokedexManager.GetOrCreate().MarkCaught(pokemon);
            yield return ShowDialogAndWait($"Congratulations! {oldName} evolved into {targetName}!");
        }
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

    private IEnumerator LoadSceneWithFadeRoutine(string sceneName, string spawnPointId, float fadeOutDuration, float fadeInDuration)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            yield break;

        isSceneTransitioning = true;

        yield return FadeSceneOverlay(1f, fadeOutDuration);

        SpawnManager.SetNextSpawnPoint(spawnPointId);
        SceneManager.LoadScene(sceneName);

        // Wait one frame so the new scene is initialized before fade-in.
        yield return null;

        yield return FadeSceneOverlay(0f, fadeInDuration);

        isSceneTransitioning = false;
    }

    private void EnsureSceneFadeOverlay()
    {
        if (sceneFadeCanvasGroup != null)
            return;

        var canvasGO = new GameObject("SceneFadeOverlay");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        sceneFadeCanvasGroup = panel.AddComponent<CanvasGroup>();
        sceneFadeCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeSceneOverlay(float targetAlpha, float duration)
    {
        var fadeCtrl = GetOrCreateSceneFadeController();
        if (fadeCtrl != null)
        {
            yield return fadeCtrl.Fade(targetAlpha, duration);
            yield break;
        }

        EnsureSceneFadeOverlay();

        float startAlpha = sceneFadeCanvasGroup.alpha;
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            sceneFadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        sceneFadeCanvasGroup.alpha = targetAlpha;
    }

    private void SetSceneFadeImmediate(float alpha)
    {
        var fadeCtrl = GetOrCreateSceneFadeController();
        if (fadeCtrl != null)
            fadeCtrl.SetImmediate(alpha);

        if (sceneFadeCanvasGroup != null)
            sceneFadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private SceneFadeController GetOrCreateSceneFadeController()
    {
        if (SceneFadeController.Instance != null)
            return SceneFadeController.Instance;

        var existing = FindObjectOfType<SceneFadeController>(true);
        if (existing != null)
            return existing;

        var go = new GameObject("SceneFadeController");
        DontDestroyOnLoad(go);
        return go.AddComponent<SceneFadeController>();
    }

    private void SetBattleCameraIsolation(bool inBattle)
    {
        var cameras = FindObjectsOfType<Camera>(true);
        var listeners = FindObjectsOfType<AudioListener>(true);
        var battleScene = SceneManager.GetSceneByName(battleSceneName);

        if (inBattle)
        {
            cachedCameraEnabledStates.Clear();
            cachedListenerEnabledStates.Clear();

            for (int i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null) continue;

                bool belongsToBattleScene = battleScene.IsValid() && cam.gameObject.scene == battleScene;
                cachedCameraEnabledStates[cam] = cam.enabled;
                cam.enabled = belongsToBattleScene;
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                var al = listeners[i];
                if (al == null) continue;

                bool belongsToBattleScene = battleScene.IsValid() && al.gameObject.scene == battleScene;
                cachedListenerEnabledStates[al] = al.enabled;
                al.enabled = belongsToBattleScene;
            }

            return;
        }

        foreach (var kv in cachedCameraEnabledStates)
        {
            if (kv.Key != null)
                kv.Key.enabled = kv.Value;
        }

        foreach (var kv in cachedListenerEnabledStates)
        {
            if (kv.Key != null)
                kv.Key.enabled = kv.Value;
        }

        cachedCameraEnabledStates.Clear();
        cachedListenerEnabledStates.Clear();
    }

    private void SetOverworldSceneVisibility(bool visible)
    {
        if (string.IsNullOrWhiteSpace(cachedOverworldSceneName))
            return;

        var scene = SceneManager.GetSceneByName(cachedOverworldSceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var roots = scene.GetRootGameObjects();
        if (!visible)
        {
            cachedOverworldRootStates.Clear();
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                    continue;

                // Never hide the root that contains this GameController.
                if (transform.IsChildOf(root.transform))
                    continue;

                // MusicManager phải luôn active để có thể restore nhạc khi battle kết thúc.
                if (root.GetComponentInChildren<MusicManager>(true) != null)
                    continue;

                cachedOverworldRootStates[root] = root.activeSelf;
                root.SetActive(false);
            }

            return;
        }

        foreach (var kv in cachedOverworldRootStates)
        {
            if (kv.Key != null)
                kv.Key.SetActive(kv.Value);
        }

        cachedOverworldRootStates.Clear();
    }

    private void EnforceBattleCameraSettings()
    {
        if (State != GameState.Battle)
            return;

        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (!battleScene.IsValid() || !battleScene.isLoaded)
            return;

        var cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            if (cam == null)
                continue;

            bool belongsToBattleScene = cam.gameObject.scene == battleScene;
            if (belongsToBattleScene)
            {
                cam.enabled = true;
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.depth = 100f;
            }
            else
            {
                cam.enabled = false;
            }
        }
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
            battleSceneLoaded = true;
        }

        if (battleScene.IsValid() && battleScene.isLoaded)
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
        SaveLoadSystem.ApplyLoadedData();

        if (scene.name == battleSceneName)
            BindBattleReferencesFromScene(scene);
    }
}
