using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class PokemonDexMenuUI : MonoBehaviour
{
    private enum DexTab
    {
        PokemonDb = 0,
        MoveDb = 1,
        MainStory = 2,
        Tutorial = 3
    }

    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("Tabs")]
    [SerializeField] private TMP_Text[] tabTexts;
    [SerializeField] private GameObject[] tabPanels;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("Pokemon DB")]
    [SerializeField] private TMP_Text[] pokemonDbLines;
    [SerializeField] private TMP_Text pokemonDbDetailText;
    [SerializeField] private Image pokemonDbDetailImage;
    [SerializeField] private float holdScrollStartDelay = 0.35f;
    [SerializeField] private float holdScrollRepeatInterval = 0.06f;

    [Header("Move DB")]
    [SerializeField] private TMP_Text[] moveDbLines;
    [SerializeField] private TMP_Text moveDbDetailText;
    [SerializeField] private Color[] moveTypeColors;

    [Header("Main Story")]
    [SerializeField] private TMP_Text[] storySummaryLines;
    [SerializeField] private TMP_Text storySummaryDetailText;

    [Header("Tutorial")]
    [SerializeField] private TMP_Text[] tutorialTopicLines;
    [SerializeField] private GameObject[] tutorialPages;
    [SerializeField] private TMP_Text tutorialPageText;

    private Action onClose;
    private DexTab currentTab = DexTab.PokemonDb;
    private int dbIndex;
    private int dbScrollOffset;
    private int moveIndex;
    private int moveScrollOffset;
    private int storyIndex;
    private int tutorialTopicIndex;
    private int tutorialPageIndex;
    private int repeatDirection;
    private float nextRepeatTime;
    private GameObject moveSearchPanel;
    private TMP_InputField moveSearchInput;
    private TMP_Text moveSearchHint;
    private TMP_Text moveDbHeaderText;
    private TMP_Text tabNavigationHintText;

    private const int PokemonRowsPerPage = 15;
    private const int StoryRowsPerPage = 17;

    private readonly List<PokemonBase> pokemonDb = new();
    private readonly List<MoveBase> moveDb = new();
    private readonly List<Quest> mainStoryQuests = new();
    private readonly List<MainStoryStepEntry> mainStorySteps = new();
    private readonly List<TMP_Text> autoPokemonDbTexts = new();
    private readonly List<TMP_Text> autoMoveDbTexts = new();
    private readonly List<TMP_Text> autoStoryTexts = new();
    private readonly List<TMP_Text> autoTutorialTexts = new();
    private readonly Dictionary<PokemonBase, int> pokemonEvolutionDepth = new();
    private readonly Dictionary<PokemonBase, string> pokemonEvolutionPath = new();
    private Sprite dexCircleSprite;
    private Quest tutorialQuest;

    private class MainStoryStepEntry
    {
        public int SequenceIndex;
        public int StepIndex;
        public MainStorySequence Sequence;
        public MainStoryStep Step;
    }

    private void Awake()
    {
        // Avoid startup overlay: Dex UI should only open via MenuController.
        if (rootPanel != null)
            rootPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void Open(Action onCloseCallback = null)
    {
        onClose = onCloseCallback;
        if (rootPanel != null)
            rootPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        BuildData();

        currentTab = DexTab.PokemonDb;
        dbIndex = 0;
        dbScrollOffset = 0;
        moveIndex = 0;
        moveScrollOffset = 0;
        storyIndex = 0;
        tutorialTopicIndex = 0;
        tutorialPageIndex = 0;

        dbIndex = ClampIndex(dbIndex, pokemonDb.Count);
        moveIndex = ClampIndex(moveIndex, moveDb.Count);
        storyIndex = ClampIndex(storyIndex, mainStorySteps.Count);
        tutorialTopicIndex = ClampIndex(tutorialTopicIndex, tutorialTopicLines != null ? tutorialTopicLines.Length : 0);
        if (tutorialPages != null && tutorialPages.Length > 0)
            tutorialPageIndex = Mathf.Clamp(tutorialPageIndex, 0, tutorialPages.Length - 1);
        else
            tutorialPageIndex = 0;

        dbScrollOffset = Mathf.Clamp(dbScrollOffset, 0, Mathf.Max(0, pokemonDb.Count - PokemonRowsPerPage));
        moveScrollOffset = Mathf.Clamp(moveScrollOffset, 0, Mathf.Max(0, moveDb.Count - PokemonRowsPerPage));
        StopRepeatScroll();
        RefreshAll();
    }

    public void Close()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        onClose = null;
    }

    public void HandleUpdate(Action onClosed = null)
    {
        if (!(rootPanel != null ? rootPanel.activeSelf : gameObject.activeSelf))
            return;

        bool textInputFocused = currentTab == DexTab.MoveDb && IsMoveSearchFocused();

        if (!textInputFocused && Input.GetKeyDown(KeyCode.Q))
        {
            StopRepeatScroll();
            currentTab = (DexTab)(((int)currentTab - 1 + 4) % 4);
            RefreshAll();
            return;
        }

        if (!textInputFocused && Input.GetKeyDown(KeyCode.E))
        {
            StopRepeatScroll();
            currentTab = (DexTab)(((int)currentTab + 1) % 4);
            RefreshAll();
            return;
        }

        int verticalStep = ReadVerticalStep();
        if (verticalStep != 0)
        {
            if (currentTab == DexTab.MoveDb && IsMoveSearchFocused())
                return;

            MoveVertical(verticalStep);
            RefreshAll();
            return;
        }

        if (currentTab == DexTab.MoveDb && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !IsMoveSearchFocused())
        {
            FocusMoveSearch();
            return;
        }

        if (currentTab == DexTab.MoveDb && IsMoveSearchFocused())
        {
            HandleMoveSearchInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            StopRepeatScroll();
            if (moveSearchInput != null && moveSearchInput.isFocused)
                moveSearchInput.DeactivateInputField();
            Close();
            (onClosed ?? onClose)?.Invoke();
        }
    }

    private void BuildData()
    {
        pokemonDb.Clear();
        moveDb.Clear();
        mainStoryQuests.Clear();
        mainStorySteps.Clear();
        pokemonEvolutionDepth.Clear();
        pokemonEvolutionPath.Clear();
        tutorialQuest = null;

        var db = PokemonDB.Instance != null ? PokemonDB.Instance.GetAllPokemons() : Resources.LoadAll<PokemonBase>("PokemonData");
        BuildPokemonDexEvolutionOrder(db);

        var moveEntries = MoveDB.Instance != null
            ? MoveDB.Instance.GetAllMoves()
            : Resources.LoadAll<MoveBase>("MoveData");
        moveDb.AddRange(moveEntries.Where(m => m != null).OrderBy(m => m.MoveName));

        var storyDirector = MainStoryDirector.Instance != null
            ? MainStoryDirector.Instance
            : UnityEngine.Object.FindObjectOfType<MainStoryDirector>(true);

        if (storyDirector == null)
        {
            var loadedDirector = Resources.FindObjectsOfTypeAll<MainStoryDirector>().FirstOrDefault(d => d != null);
            if (loadedDirector != null)
                storyDirector = loadedDirector;
        }

        var sequences = new List<MainStorySequence>();
        if (storyDirector != null)
        {
            var directorSequences = storyDirector.GetStorySequences();
            if (directorSequences != null)
                sequences.AddRange(directorSequences.Where(sequence => sequence != null));
        }

        if (sequences.Count == 0)
        {
            var loadedSequences = Resources.FindObjectsOfTypeAll<MainStorySequence>()
                .Where(sequence => sequence != null)
                .ToList();

            sequences.AddRange(loadedSequences);
        }

        for (int sequenceIndex = 0; sequenceIndex < sequences.Count; sequenceIndex++)
        {
            var sequence = sequences[sequenceIndex];
            if (sequence == null || sequence.Steps == null)
                continue;

            for (int stepIndex = 0; stepIndex < sequence.Steps.Count; stepIndex++)
            {
                var step = sequence.Steps[stepIndex];
                if (step == null)
                    continue;

                mainStorySteps.Add(new MainStoryStepEntry
                {
                    SequenceIndex = sequenceIndex,
                    StepIndex = stepIndex,
                    Sequence = sequence,
                    Step = step
                });
            }
        }

        var questManager = QuestManager.Instance;
        if (questManager != null)
        {
            var mainStoryOrder = questManager.GetMainStoryOrder();
            if (mainStoryOrder != null)
                mainStoryQuests.AddRange(mainStoryOrder.Where(q => q != null));

            tutorialQuest = questManager.GetTutorialQuest();
        }

        EnsurePokemonDbRuntimeUI();
        EnsureMoveRuntimeUI();
        EnsureStoryRuntimeUI();
        EnsureTabNavigationHint();
        ResolveAutoTextCollections();
    }

    private void BuildPokemonDexEvolutionOrder(IEnumerable<PokemonBase> source)
    {
        if (source == null)
            return;

        var allPokemon = source
            .Where(pokemon => pokemon != null)
            .Distinct()
            .OrderBy(pokemon => pokemon.Num)
            .ThenBy(pokemon => pokemon.Name)
            .ToList();

        var evolutionTargets = new HashSet<PokemonBase>();
        foreach (var pokemon in allPokemon)
        {
            foreach (var option in pokemon.GetValidEvolutionOptions())
            {
                if (option?.EvolvesTo != null)
                    evolutionTargets.Add(option.EvolvesTo);
            }
        }

        var visited = new HashSet<PokemonBase>();
        foreach (var root in allPokemon.Where(pokemon => !evolutionTargets.Contains(pokemon)))
            AddPokemonEvolutionTrace(root, 0, root.Name, visited);

        foreach (var pokemon in allPokemon)
        {
            if (!visited.Contains(pokemon))
                AddPokemonEvolutionTrace(pokemon, 0, pokemon.Name, visited);
        }
    }

    private void AddPokemonEvolutionTrace(PokemonBase pokemon, int depth, string path, HashSet<PokemonBase> visited)
    {
        if (pokemon == null || !visited.Add(pokemon))
            return;

        pokemonDb.Add(pokemon);
        pokemonEvolutionDepth[pokemon] = Mathf.Max(0, depth);
        pokemonEvolutionPath[pokemon] = string.IsNullOrWhiteSpace(path) ? pokemon.Name : path;

        var options = pokemon.GetValidEvolutionOptions()
            .Where(option => option != null && option.EvolvesTo != null)
            .OrderBy(option => option.EvolutionLevel)
            .ThenBy(option => option.EvolvesTo.Num)
            .ThenBy(option => option.EvolvesTo.Name);

        foreach (var option in options)
            AddPokemonEvolutionTrace(option.EvolvesTo, depth + 1, $"{pokemonEvolutionPath[pokemon]} -> {option.EvolvesTo.Name}", visited);
    }

    private void MoveVertical(int delta)
    {
        switch (currentTab)
        {
            case DexTab.PokemonDb:
                dbIndex = WrapIndex(dbIndex + delta, pokemonDb.Count);
                break;
            case DexTab.MoveDb:
                moveIndex = WrapIndex(moveIndex + delta, moveDb.Count);
                EnsureMoveSelectionVisible(PokemonRowsPerPage);
                break;
            case DexTab.MainStory:
                storyIndex = WrapIndex(storyIndex + delta, mainStorySteps.Count);
                break;
            case DexTab.Tutorial:
                tutorialPageIndex = WrapIndex(tutorialPageIndex + delta, TutorialPageCount);
                break;
        }
    }

    private int ClampIndex(int value, int count)
    {
        if (count <= 0)
            return 0;

        return Mathf.Clamp(value, 0, count - 1);
    }

    private int WrapIndex(int value, int count)
    {
        if (count <= 0)
            return 0;

        value %= count;
        if (value < 0)
            value += count;

        return value;
    }

    private int ReadVerticalStep()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            StartRepeatScroll(-1);
            return -1;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            StartRepeatScroll(1);
            return 1;
        }

        if (repeatDirection != 0)
        {
            bool holding = repeatDirection < 0 ? Input.GetKey(KeyCode.UpArrow) : Input.GetKey(KeyCode.DownArrow);
            if (!holding)
            {
                StopRepeatScroll();
                return 0;
            }

            if (Time.unscaledTime >= nextRepeatTime)
            {
                nextRepeatTime = Time.unscaledTime + Mathf.Max(0.01f, holdScrollRepeatInterval);
                return repeatDirection;
            }
        }

        return 0;
    }

    private void StartRepeatScroll(int direction)
    {
        repeatDirection = direction;
        nextRepeatTime = Time.unscaledTime + Mathf.Max(0.01f, holdScrollStartDelay);
    }

    private void StopRepeatScroll()
    {
        repeatDirection = 0;
        nextRepeatTime = 0f;
    }

    private void EnsureDbSelectionVisible(int visibleRows)
    {
        if (visibleRows <= 0 || pokemonDb.Count <= 0)
        {
            dbScrollOffset = 0;
            return;
        }

        dbScrollOffset = Mathf.Clamp(dbScrollOffset, 0, Mathf.Max(0, pokemonDb.Count - visibleRows));

        if (dbIndex < dbScrollOffset)
            dbScrollOffset = dbIndex;
        else if (dbIndex >= dbScrollOffset + visibleRows)
            dbScrollOffset = dbIndex - visibleRows + 1;
    }

    private void RefreshAll()
    {
        EnsureTabNavigationHint();
        RefreshTabs();
        RefreshPokemonDb();
        RefreshMoveDb();
        RefreshStorySummary();
        RefreshTutorialPage();
    }

    private void RefreshTabs()
    {
        if (tabTexts != null)
        {
            for (int i = 0; i < tabTexts.Length; i++)
                tabTexts[i].color = i == (int)currentTab ? highlightColor : normalColor;
        }

        if (tabPanels != null)
        {
            for (int i = 0; i < tabPanels.Length; i++)
                tabPanels[i].SetActive(i == (int)currentTab);
        }

        if (tabNavigationHintText != null)
            tabNavigationHintText.text = "<- Q   E ->";
    }

    private void EnsureTabNavigationHint()
    {
        if (tabNavigationHintText != null)
            return;

        var parent = rootPanel != null ? rootPanel.transform : transform;
        var hintTransform = parent.Find("PokemonDexTabHint") as RectTransform;
        if (hintTransform == null)
        {
            var hintGo = new GameObject("PokemonDexTabHint", typeof(RectTransform));
            hintTransform = hintGo.GetComponent<RectTransform>();
            hintTransform.SetParent(parent, false);
            hintTransform.anchorMin = new Vector2(0.5f, 1f);
            hintTransform.anchorMax = new Vector2(0.5f, 1f);
            hintTransform.pivot = new Vector2(0.5f, 1f);
            hintTransform.anchoredPosition = new Vector2(0f, -12f);
            hintTransform.sizeDelta = new Vector2(220f, 32f);

            var tmp = hintGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.fontSize = 22f;
            tmp.color = highlightColor;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tabNavigationHintText = tmp;
        }
        else
        {
            tabNavigationHintText = hintTransform.GetComponent<TextMeshProUGUI>();
            if (tabNavigationHintText == null)
                tabNavigationHintText = hintTransform.gameObject.AddComponent<TextMeshProUGUI>();
        }

        tabNavigationHintText.text = "<- Q   E ->";
    }

    private void ResolveAutoTextCollections()
    {
        autoPokemonDbTexts.Clear();
        autoStoryTexts.Clear();
        autoTutorialTexts.Clear();

        EnsurePokemonDbRuntimeUI();
        if (autoPokemonDbTexts.Count == 0)
            autoPokemonDbTexts.AddRange(ResolveTexts(pokemonDbLines, DexTab.PokemonDb, pokemonDbDetailText));
        EnsureMoveRuntimeUI();
        if (autoMoveDbTexts.Count == 0)
            autoMoveDbTexts.AddRange(ResolveTexts(moveDbLines, DexTab.MoveDb, moveDbDetailText));
        autoStoryTexts.AddRange(ResolveTexts(storySummaryLines, DexTab.MainStory, storySummaryDetailText));
        autoTutorialTexts.AddRange(ResolveTexts(tutorialTopicLines, DexTab.Tutorial, tutorialPageText));
    }

    private List<TMP_Text> ResolveTexts(TMP_Text[] manualTexts, DexTab tab, params TMP_Text[] exclude)
    {
        if (manualTexts != null && manualTexts.Length > 0)
            return manualTexts.Where(t => t != null).ToList();

        GameObject panel = null;
        if (tabPanels != null && (int)tab < tabPanels.Length)
            panel = tabPanels[(int)tab];
        else if (rootPanel != null)
            panel = rootPanel;

        if (panel == null)
            return new List<TMP_Text>();

        var textList = panel.GetComponentsInChildren<TMP_Text>(true)
            .Where(t => t != null && !exclude.Contains(t))
            .ToList();

        return textList;
    }

    private Color BuildRowColor(bool selected, bool unlocked)
    {
        if (selected)
            return highlightColor;

        if (!unlocked)
            return new Color(normalColor.r, normalColor.g, normalColor.b, 0.35f);

        return normalColor;
    }

    private string FormatOrUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "???" : value;
    }

}
