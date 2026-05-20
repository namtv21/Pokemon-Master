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
    HealingCenter,
    Quest
}

public class GameController : MonoBehaviour
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

    public static GameController Instance { get; private set; }
    public GameState State { get; private set; }
    public event System.Action<GameState> OnStateChanged;

    private string cachedOverworldSceneName;
    private bool battleSceneLoaded;
    private readonly Dictionary<Camera, bool> cachedCameraEnabledStates = new();
    private readonly Dictionary<GameObject, bool> cachedOverworldRootStates = new();
    private float nextBattleCameraEnforceTime;
    private CanvasGroup sceneFadeCanvasGroup;
    private bool isSceneTransitioning;

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
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
    }

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
        // DialogManager callback chỉ nên đặt Dialog state khi game chưa bị chuyển sang state khác bởi flow riêng.
        if (State != GameState.Battle && State != GameState.Shop && State != GameState.Storage)
            SetState(GameState.Dialog);
    }

    private void OnDialogFinished()
    {
        // Nếu callback riêng (NPC/Quest/Shop) đã đổi state trước đó, không ghi đè về Overworld.
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
                var livePlayerController = ResolvePlayerController();
                if (livePlayerController != null)
                    livePlayerController.HandleUpdate();
                if (Input.GetKeyDown(KeyCode.X)) OpenMenu();
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
                if (PartyMenuUI != null && PartyMenuUI.gameObject.activeInHierarchy)
                    PartyMenuUI.HandleUpdate();
                else if (StorageSystem != null && StorageSystem.gameObject.activeInHierarchy)
                    StorageSystem.HandleUpdate();
                break;

            case GameState.Battle:
                if (Time.unscaledTime >= nextBattleCameraEnforceTime)
                {
                    EnforceBattleCameraSettings();
                    nextBattleCameraEnforceTime = Time.unscaledTime + 0.1f;
                }
                break;
        }
    }

    public void StartWildBattle(Pokemon wildPokemon)
    {
        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (State == GameState.Battle || isSceneTransitioning || battleSceneLoaded || (battleScene.IsValid() && battleScene.isLoaded))
        {
            Debug.LogWarning("[Battle] Ignored duplicate wild battle start request.");
            return;
        }

        StartCoroutine(StartWildBattleRoutine(wildPokemon));
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
            yield return FadeSceneOverlay(1f, battleFallbackFadeDuration);
        }

        battleSystem.StartWildBattle(wildPokemon);

        if (battleTransition != null)
        {
            yield return battleTransition.PlayOpen();
        }
        else
        {
            yield return FadeSceneOverlay(0f, battleFallbackFadeDuration);
        }
    }

    public void StartTrainerBattle(NPC trainer)
    {
        var battleScene = SceneManager.GetSceneByName(battleSceneName);
        if (State == GameState.Battle || isSceneTransitioning || battleSceneLoaded || (battleScene.IsValid() && battleScene.isLoaded))
        {
            Debug.LogWarning("[Battle] Ignored duplicate trainer battle start request.");
            return;
        }

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
            yield return FadeSceneOverlay(1f, battleFallbackFadeDuration);
        }

        battleSystem.StartTrainerBattle(trainer);

        if (battleTransition != null)
        {
            yield return battleTransition.PlayOpen();
        }
        else
        {
            yield return FadeSceneOverlay(0f, battleFallbackFadeDuration);
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
            battleSystem = null;
            battleTransition = null;
        }

        if (!string.IsNullOrEmpty(cachedOverworldSceneName))
        {
            var scene = SceneManager.GetSceneByName(cachedOverworldSceneName);
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.SetActiveScene(scene);
        }

        SetState(GameState.Overworld);
        if (playerController != null) playerController.enabled = true;

        yield return FadeSceneOverlay(0f, battleEndRevealDuration);
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

        canvasGO.AddComponent<CanvasScaler>();
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
        EnsureSceneFadeOverlay();
        sceneFadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private void SetBattleCameraIsolation(bool inBattle)
    {
        var cameras = FindObjectsOfType<Camera>(true);
        var battleScene = SceneManager.GetSceneByName(battleSceneName);

        if (inBattle)
        {
            cachedCameraEnabledStates.Clear();

            for (int i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null) continue;

                bool belongsToBattleScene = battleScene.IsValid() && cam.gameObject.scene == battleScene;
                cachedCameraEnabledStates[cam] = cam.enabled;
                cam.enabled = belongsToBattleScene;
            }

            return;
        }

        foreach (var kv in cachedCameraEnabledStates)
        {
            if (kv.Key != null)
                kv.Key.enabled = kv.Value;
        }

        cachedCameraEnabledStates.Clear();
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

                // Keep prologue flow object active during prologue battle so its coroutine can continue.
                if (root.GetComponentInChildren<PrologueDirector>(true) != null)
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
        if (battleScene.IsValid() && battleScene.isLoaded)
            battleSceneLoaded = true;

        if (!battleSceneLoaded)
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
            Debug.LogError($"[Bind] ✗ BattleSystem STILL NOT FOUND after searching all roots!\n" +
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

    private void OpenMenu()
    {
        SetState(GameState.Menu);
        menuController?.OpenMainMenu();
    }

    public void HealAllPlayerPokemon()
    {
        PlayerParty.HealAll();
        ToastNotificationManager.Instance?.Show("Your Pokémon have been fully healed!");
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