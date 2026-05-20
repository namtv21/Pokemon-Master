using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PokemonDexMenuUI : MonoBehaviour
{
    private enum DexTab
    {
        PokemonDb = 0,
        MainStory = 1,
        Tutorial = 2
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
    private int storyIndex;
    private int tutorialTopicIndex;
    private int tutorialPageIndex;
    private int repeatDirection;
    private float nextRepeatTime;

    private const int PokemonRowsPerPage = 15;

    private readonly List<PokemonBase> pokemonDb = new();
    private readonly List<Quest> mainStoryQuests = new();
    private readonly List<MainStoryStepEntry> mainStorySteps = new();
    private readonly List<TMP_Text> autoPokemonDbTexts = new();
    private readonly List<TMP_Text> autoStoryTexts = new();
    private readonly List<TMP_Text> autoTutorialTexts = new();
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
        storyIndex = 0;
        tutorialTopicIndex = 0;
        tutorialPageIndex = 0;

        dbIndex = ClampIndex(dbIndex, pokemonDb.Count);
        storyIndex = ClampIndex(storyIndex, mainStorySteps.Count);
        tutorialTopicIndex = ClampIndex(tutorialTopicIndex, tutorialTopicLines != null ? tutorialTopicLines.Length : 0);
        if (tutorialPages != null && tutorialPages.Length > 0)
            tutorialPageIndex = Mathf.Clamp(tutorialPageIndex, 0, tutorialPages.Length - 1);
        else
            tutorialPageIndex = 0;

        dbScrollOffset = Mathf.Clamp(dbScrollOffset, 0, Mathf.Max(0, pokemonDb.Count - PokemonRowsPerPage));
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

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StopRepeatScroll();
            currentTab = (DexTab)(((int)currentTab - 1 + 3) % 3);
            RefreshAll();
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StopRepeatScroll();
            currentTab = (DexTab)(((int)currentTab + 1) % 3);
            RefreshAll();
            return;
        }

        int verticalStep = ReadVerticalStep();
        if (verticalStep != 0)
        {
            MoveVertical(verticalStep);
            RefreshAll();
            return;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            StopRepeatScroll();
            Close();
            (onClosed ?? onClose)?.Invoke();
        }
    }

    private void BuildData()
    {
        pokemonDb.Clear();
        mainStoryQuests.Clear();
        mainStorySteps.Clear();
        tutorialQuest = null;

        var db = PokemonDB.Instance != null ? PokemonDB.Instance.GetAllPokemons() : Resources.LoadAll<PokemonBase>("PokemonData");
        pokemonDb.AddRange(db.OrderBy(p => p.Name));

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
        EnsureStoryRuntimeUI();
        ResolveAutoTextCollections();
    }

    private void MoveVertical(int delta)
    {
        switch (currentTab)
        {
            case DexTab.PokemonDb:
                dbIndex = WrapIndex(dbIndex + delta, pokemonDb.Count);
                break;
            case DexTab.MainStory:
                storyIndex = WrapIndex(storyIndex + delta, mainStorySteps.Count);
                break;
            case DexTab.Tutorial:
                tutorialPageIndex = WrapIndex(tutorialPageIndex + delta, tutorialPages != null ? tutorialPages.Length : 0);
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

    private void AdvanceTutorialPage()
    {
        if (tutorialPages == null || tutorialPages.Length == 0)
            return;

        tutorialPageIndex = (tutorialPageIndex + 1) % tutorialPages.Length;
    }

    private void RefreshAll()
    {
        RefreshTabs();
        RefreshPokemonDb();
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
    }

    private void RefreshPokemonDb()
    {
        var lines = GetPokemonDbTexts();
        int visibleRows = Mathf.Min(PokemonRowsPerPage, Mathf.Max(1, lines.Count));
        EnsureDbSelectionVisible(visibleRows);

        for (int i = 0; i < lines.Count; i++)
        {
            var text = lines[i];

            if (i >= visibleRows)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            int dataIndex = dbScrollOffset + i;
            if (dataIndex < 0 || dataIndex >= pokemonDb.Count)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            var pokemon = pokemonDb[dataIndex];
            bool caught = PokedexManager.GetOrCreate().HasCaught(pokemon.Name);
            text.text = FormatDexEntry(dataIndex, pokemon);
            text.color = BuildRowColor(dataIndex == dbIndex, true);
            EnsureDexIndicator(text, caught);
        }

        if (pokemonDbDetailText == null) return;

        if (pokemonDb.Count == 0)
        {
            pokemonDbDetailText.text = "No Pokemon data found.";
            if (pokemonDbDetailImage != null)
            {
                pokemonDbDetailImage.sprite = null;
                pokemonDbDetailImage.enabled = false;
            }
            return;
        }

        var selected = pokemonDb[Mathf.Clamp(dbIndex, 0, pokemonDb.Count - 1)];
        if (pokemonDbDetailImage != null)
        {
            pokemonDbDetailImage.sprite = selected.FrontSprite;
            pokemonDbDetailImage.enabled = selected.FrontSprite != null;
        }

        string learnsetText = BuildLearnsetPreview(selected);

        pokemonDbDetailText.text =
            $"Name: {selected.Name}\n" +
            $"Type: {selected.Type1}/{selected.Type2}\n" +
            $"HP: {selected.MaxHp}  Atk: {selected.Attack}  Def: {selected.Defense}\n" +
            $"SpA: {selected.SpAttack}  SpD: {selected.SpDefense}  Spe: {selected.Speed}\n" +
            $"Learnset:\n{learnsetText}";
    }

    private string BuildLearnsetPreview(PokemonBase pokemon)
    {
        if (pokemon == null || pokemon.LearnableMoves == null || pokemon.LearnableMoves.Length == 0)
            return "(No learnset data)";

        var sortedMoves = pokemon.LearnableMoves
            .Where(lm => lm != null && lm.move != null)
            .OrderBy(lm => lm.level)
            .ToList();

        if (sortedMoves.Count == 0)
            return "(No learnset data)";

        const int maxRowsPerColumn = 10;
        const int maxTotalRows = maxRowsPerColumn * 2;
        int rows = Mathf.Min(maxTotalRows, sortedMoves.Count);

        var leftCol = new List<string>(maxRowsPerColumn);
        var rightCol = new List<string>(maxRowsPerColumn);

        for (int i = 0; i < rows; i++)
        {
            var lm = sortedMoves[i];
            string moveName = string.IsNullOrWhiteSpace(lm.move.MoveName) ? lm.move.name : lm.move.MoveName;
            string entry = $"Lv {Mathf.Max(1, lm.level):00} - {moveName}";
            if (i < maxRowsPerColumn)
                leftCol.Add(entry);
            else
                rightCol.Add(entry);
        }

        int rowCount = Mathf.Max(leftCol.Count, rightCol.Count);
        int leftWidth = 0;
        for (int i = 0; i < leftCol.Count; i++)
            leftWidth = Mathf.Max(leftWidth, leftCol[i].Length);

        var lines = new List<string>(rowCount + 1);
        for (int row = 0; row < rowCount; row++)
        {
            string left = row < leftCol.Count ? leftCol[row] : string.Empty;
            string right = row < rightCol.Count ? rightCol[row] : string.Empty;

            if (!string.IsNullOrEmpty(right))
                lines.Add($"{left.PadRight(leftWidth + 3)}{right}");
            else
                lines.Add(left);
        }

        if (sortedMoves.Count > maxTotalRows)
            lines.Add($"... +{sortedMoves.Count - maxTotalRows} more moves");

        return string.Join("\n", lines);
    }

    private void RefreshStorySummary()
    {
        var lines = GetStoryTexts();
        var storyFlags = StoryFlags.Instance;
        int currentSequenceIndex = storyFlags != null ? storyFlags.MainStorySequenceIndex : 0;
        int currentStepIndex = storyFlags != null ? storyFlags.MainStoryStepIndex : 0;

        if (mainStorySteps.Count == 0)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].text = string.Empty;
                lines[i].color = normalColor;
            }

            if (storySummaryDetailText != null)
                storySummaryDetailText.text = "No main story data found.";
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            var text = lines[i];
            if (i >= mainStorySteps.Count)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            var entry = mainStorySteps[i];
            bool unlocked = IsStoryStepUnlocked(entry, currentSequenceIndex, currentStepIndex);

            text.text = BuildStoryStepRowText(i + 1, entry, unlocked);
            text.color = BuildRowColor(i == storyIndex, unlocked);
            EnsureDexIndicator(text, unlocked);
        }

        if (storySummaryDetailText == null) return;

        var selected = mainStorySteps[Mathf.Clamp(storyIndex, 0, mainStorySteps.Count - 1)];
        bool selectedUnlocked = IsStoryStepUnlocked(selected, currentSequenceIndex, currentStepIndex);
        storySummaryDetailText.text = BuildStoryStepDetailText(selected, selectedUnlocked);
    }

    private void ResolveAutoTextCollections()
    {
        autoPokemonDbTexts.Clear();
        autoStoryTexts.Clear();
        autoTutorialTexts.Clear();

        EnsurePokemonDbRuntimeUI();
        if (autoPokemonDbTexts.Count == 0)
            autoPokemonDbTexts.AddRange(ResolveTexts(pokemonDbLines, DexTab.PokemonDb, pokemonDbDetailText));
        autoStoryTexts.AddRange(ResolveTexts(storySummaryLines, DexTab.MainStory, storySummaryDetailText));
        autoTutorialTexts.AddRange(ResolveTexts(tutorialTopicLines, DexTab.Tutorial, tutorialPageText));
    }

    private void EnsurePokemonDbRuntimeUI()
    {
        if (pokemonDbLines != null && pokemonDbLines.Any(t => t != null))
            return;

        GameObject panel = null;
        if (tabPanels != null && tabPanels.Length > (int)DexTab.PokemonDb)
            panel = tabPanels[(int)DexTab.PokemonDb];
        else if (rootPanel != null)
            panel = rootPanel;
        else
            panel = gameObject;

        if (panel == null)
            return;

        var listRoot = panel.transform.Find("PokemonDbAutoList") as RectTransform;
        if (listRoot == null)
        {
            var listGo = new GameObject("PokemonDbAutoList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listRoot = listGo.GetComponent<RectTransform>();
            listRoot.SetParent(panel.transform, false);
            listRoot.anchorMin = new Vector2(0.02f, 0.08f);
            listRoot.anchorMax = new Vector2(0.48f, 0.92f);
            listRoot.offsetMin = Vector2.zero;
            listRoot.offsetMax = Vector2.zero;

            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
        }

        var infoPanel = panel.transform.Find("PokemonDbAutoInfoPanel") as RectTransform;
        if (infoPanel == null)
        {
            var infoGo = new GameObject("PokemonDbAutoInfoPanel", typeof(RectTransform), typeof(Image));
            infoPanel = infoGo.GetComponent<RectTransform>();
            infoPanel.SetParent(panel.transform, false);
            infoPanel.anchorMin = new Vector2(0.52f, 0.08f);
            infoPanel.anchorMax = new Vector2(0.98f, 0.92f);
            infoPanel.offsetMin = Vector2.zero;
            infoPanel.offsetMax = Vector2.zero;

            var bg = infoGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.2f);
        }

        if (pokemonDbDetailText == null)
        {
            var detailTf = infoPanel.Find("InfoText") as RectTransform;
            if (detailTf == null)
            {
                var detailGo = new GameObject("InfoText", typeof(RectTransform));
                detailTf = detailGo.GetComponent<RectTransform>();
                detailTf.SetParent(infoPanel, false);
                detailTf.anchorMin = Vector2.zero;
                detailTf.anchorMax = Vector2.one;
                detailTf.offsetMin = new Vector2(10f, 10f);
                detailTf.offsetMax = new Vector2(-10f, -10f);

                var tmp = detailGo.AddComponent<TextMeshProUGUI>();
                tmp.enableWordWrapping = true;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.fontSize = 22f;
                tmp.color = normalColor;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
                pokemonDbDetailText = tmp;

                // Leave space for sprite preview above text.
                detailTf.offsetMin = new Vector2(10f, 10f);
                detailTf.offsetMax = new Vector2(-10f, -150f);
            }
            else
            {
                pokemonDbDetailText = detailTf.GetComponent<TextMeshProUGUI>();
                if (pokemonDbDetailText == null)
                    pokemonDbDetailText = detailTf.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        if (pokemonDbDetailImage == null)
        {
            var imageTf = infoPanel.Find("InfoImage") as RectTransform;
            if (imageTf == null)
            {
                var imageGo = new GameObject("InfoImage", typeof(RectTransform), typeof(Image));
                imageTf = imageGo.GetComponent<RectTransform>();
                imageTf.SetParent(infoPanel, false);
                imageTf.anchorMin = new Vector2(0.5f, 1f);
                imageTf.anchorMax = new Vector2(0.5f, 1f);
                imageTf.pivot = new Vector2(0.5f, 1f);
                imageTf.anchoredPosition = new Vector2(0f, -10f);
                imageTf.sizeDelta = new Vector2(128f, 128f);
            }

            pokemonDbDetailImage = imageTf.GetComponent<Image>();
            if (pokemonDbDetailImage == null)
                pokemonDbDetailImage = imageTf.gameObject.AddComponent<Image>();

            pokemonDbDetailImage.preserveAspect = true;
        }

        autoPokemonDbTexts.Clear();
        int rowCount = PokemonRowsPerPage;
        for (int i = 0; i < rowCount; i++)
        {
            var rowName = $"Row_{i + 1:00}";
            var rowTf = listRoot.Find(rowName) as RectTransform;
            TextMeshProUGUI rowText;

            if (rowTf == null)
            {
                var rowGo = new GameObject(rowName, typeof(RectTransform));
                rowTf = rowGo.GetComponent<RectTransform>();
                rowTf.SetParent(listRoot, false);
                rowTf.sizeDelta = new Vector2(0f, 28f);

                rowText = rowGo.AddComponent<TextMeshProUGUI>();
                rowText.enableWordWrapping = false;
                rowText.overflowMode = TextOverflowModes.Ellipsis;
                rowText.alignment = TextAlignmentOptions.MidlineLeft;
                rowText.fontSize = 22f;
                rowText.color = normalColor;
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
                if (TMP_Settings.defaultFontAsset != null)
                    rowText.font = TMP_Settings.defaultFontAsset;
            }
            else
            {
                rowText = rowTf.GetComponent<TextMeshProUGUI>();
                if (rowText == null)
                    rowText = rowTf.gameObject.AddComponent<TextMeshProUGUI>();
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
            }

            autoPokemonDbTexts.Add(rowText);
        }
    }

    private void EnsureStoryRuntimeUI()
    {
        if (storySummaryLines != null && storySummaryLines.Any(t => t != null) && storySummaryDetailText != null)
            return;

        GameObject panel = null;
        if (tabPanels != null && tabPanels.Length > (int)DexTab.MainStory)
            panel = tabPanels[(int)DexTab.MainStory];
        else if (rootPanel != null)
            panel = rootPanel;
        else
            panel = gameObject;

        if (panel == null)
            return;

        var listRoot = panel.transform.Find("MainStoryAutoList") as RectTransform;
        if (listRoot == null)
        {
            var listGo = new GameObject("MainStoryAutoList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listRoot = listGo.GetComponent<RectTransform>();
            listRoot.SetParent(panel.transform, false);
            listRoot.anchorMin = new Vector2(0.02f, 0.08f);
            listRoot.anchorMax = new Vector2(0.48f, 0.92f);
            listRoot.offsetMin = Vector2.zero;
            listRoot.offsetMax = Vector2.zero;

            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
        }

        var infoPanel = panel.transform.Find("MainStoryAutoInfoPanel") as RectTransform;
        if (infoPanel == null)
        {
            var infoGo = new GameObject("MainStoryAutoInfoPanel", typeof(RectTransform), typeof(Image));
            infoPanel = infoGo.GetComponent<RectTransform>();
            infoPanel.SetParent(panel.transform, false);
            infoPanel.anchorMin = new Vector2(0.52f, 0.08f);
            infoPanel.anchorMax = new Vector2(0.98f, 0.92f);
            infoPanel.offsetMin = Vector2.zero;
            infoPanel.offsetMax = Vector2.zero;

            var bg = infoGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.2f);
        }

        if (storySummaryDetailText == null)
        {
            var detailTf = infoPanel.Find("StoryInfoText") as RectTransform;
            if (detailTf == null)
            {
                var detailGo = new GameObject("StoryInfoText", typeof(RectTransform));
                detailTf = detailGo.GetComponent<RectTransform>();
                detailTf.SetParent(infoPanel, false);
                detailTf.anchorMin = Vector2.zero;
                detailTf.anchorMax = Vector2.one;
                detailTf.offsetMin = new Vector2(10f, 10f);
                detailTf.offsetMax = new Vector2(-10f, -10f);

                var tmp = detailGo.AddComponent<TextMeshProUGUI>();
                tmp.enableWordWrapping = true;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.fontSize = 22f;
                tmp.color = normalColor;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
                storySummaryDetailText = tmp;
            }
            else
            {
                storySummaryDetailText = detailTf.GetComponent<TextMeshProUGUI>();
                if (storySummaryDetailText == null)
                    storySummaryDetailText = detailTf.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        autoStoryTexts.Clear();
        int rowCount = Mathf.Max(1, PokemonRowsPerPage);
        for (int i = 0; i < rowCount; i++)
        {
            var rowName = $"Row_{i + 1:00}";
            var rowTf = listRoot.Find(rowName) as RectTransform;
            TextMeshProUGUI rowText;

            if (rowTf == null)
            {
                var rowGo = new GameObject(rowName, typeof(RectTransform));
                rowTf = rowGo.GetComponent<RectTransform>();
                rowTf.SetParent(listRoot, false);
                rowTf.sizeDelta = new Vector2(0f, 28f);

                rowText = rowGo.AddComponent<TextMeshProUGUI>();
                rowText.enableWordWrapping = false;
                rowText.overflowMode = TextOverflowModes.Ellipsis;
                rowText.alignment = TextAlignmentOptions.MidlineLeft;
                rowText.fontSize = 22f;
                rowText.color = normalColor;
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
                if (TMP_Settings.defaultFontAsset != null)
                    rowText.font = TMP_Settings.defaultFontAsset;
            }
            else
            {
                rowText = rowTf.GetComponent<TextMeshProUGUI>();
                if (rowText == null)
                    rowText = rowTf.gameObject.AddComponent<TextMeshProUGUI>();
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
            }

            autoStoryTexts.Add(rowText);
        }
    }

    private string FormatDexEntry(int dataIndex, PokemonBase pokemon)
    {
        if (pokemon == null)
            return string.Empty;

        return $"No.{dataIndex + 1:000}  {pokemon.Name}";
    }

    private void EnsureDexIndicator(TMP_Text lineText, bool caught)
    {
        if (lineText == null)
            return;

        var indicatorRoot = lineText.transform.Find("DexIndicator");
        if (indicatorRoot == null)
        {
            var iconGo = new GameObject("DexIndicator", typeof(RectTransform), typeof(Image));
            indicatorRoot = iconGo.transform;
            indicatorRoot.SetParent(lineText.transform, false);

            var rt = iconGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(4f, 0f);
            rt.sizeDelta = new Vector2(16f, 16f);

            var ring = iconGo.GetComponent<Image>();
            ring.sprite = GetDexCircleSprite();
            ring.type = Image.Type.Simple;
            ring.color = Color.black;
            ring.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(indicatorRoot, false);

            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.5f, 0.5f);
            fillRt.anchorMax = new Vector2(0.5f, 0.5f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(9f, 9f);

            var fill = fillGo.GetComponent<Image>();
            fill.sprite = GetDexCircleSprite();
            fill.type = Image.Type.Simple;
            fill.color = Color.green;
            fill.raycastTarget = false;
        }

        var fillTransform = indicatorRoot.Find("Fill");
        if (fillTransform != null)
            fillTransform.gameObject.SetActive(caught);
    }

    private Sprite GetDexCircleSprite()
    {
        if (dexCircleSprite != null)
            return dexCircleSprite;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "DexCircleSpriteTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center - 0.5f;
        float radiusSqr = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                bool inside = dx * dx + dy * dy <= radiusSqr;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        dexCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);

        return dexCircleSprite;
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

    private List<TMP_Text> GetPokemonDbTexts()
    {
        if (pokemonDbLines != null && pokemonDbLines.Any(t => t != null))
            return pokemonDbLines.Where(t => t != null).ToList();

        if (autoPokemonDbTexts.Count == 0)
        {
            EnsurePokemonDbRuntimeUI();
            ResolveAutoTextCollections();
        }

        return autoPokemonDbTexts;
    }

    private List<TMP_Text> GetStoryTexts()
    {
        if (storySummaryLines != null && storySummaryLines.Any(t => t != null))
            return storySummaryLines.Where(t => t != null).ToList();

        if (autoStoryTexts.Count == 0)
            ResolveAutoTextCollections();

        return autoStoryTexts;
    }

    private List<TMP_Text> GetTutorialTexts()
    {
        if (tutorialTopicLines != null && tutorialTopicLines.Any(t => t != null))
            return tutorialTopicLines.Where(t => t != null).ToList();

        if (autoTutorialTexts.Count == 0)
            ResolveAutoTextCollections();

        return autoTutorialTexts;
    }

    private Color BuildRowColor(bool selected, bool unlocked)
    {
        if (selected)
            return highlightColor;

        if (!unlocked)
            return new Color(normalColor.r, normalColor.g, normalColor.b, 0.35f);

        return normalColor;
    }

    private bool IsStoryStepUnlocked(MainStoryStepEntry entry, int currentSequenceIndex, int currentStepIndex)
    {
        if (entry == null || entry.Step == null)
            return false;

        if (entry.SequenceIndex < currentSequenceIndex)
            return true;

        if (entry.SequenceIndex > currentSequenceIndex)
            return false;

        return entry.StepIndex <= currentStepIndex;
    }

    private string BuildStoryStepRowText(int stepNumber, MainStoryStepEntry entry, bool unlocked)
    {
        if (entry == null || entry.Step == null)
            return $"Step {stepNumber:00}  ???";

        if (!unlocked)
            return $"Step {stepNumber:00}  ???";

        return $"Step {stepNumber:00}  {BuildStepSummary(entry.Step)}";
    }

    private string BuildStoryStepDetailText(MainStoryStepEntry entry, bool unlocked)
    {
        if (entry == null || entry.Step == null)
            return string.Empty;

        if (!unlocked)
            return "Requirement: ???";

        return string.IsNullOrWhiteSpace(entry.Step.Description)
            ? "- No description."
            : entry.Step.Description.Trim();
    }

    private string BuildStepSummary(MainStoryStep step)
    {
        if (step == null)
            return "???";

        return FormatOrUnknown(step.StepId);
    }

    private string BuildStepActionText(MainStoryStep step)
    {
        if (step == null || step.Actions == null || step.Actions.Count == 0)
            return "- None";

        var sb = new StringBuilder();
        int shown = 0;

        for (int i = 0; i < step.Actions.Count; i++)
        {
            var action = step.Actions[i];
            if (action == null)
                continue;

            string line = BuildActionText(action);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            sb.AppendLine($"- {line}");
            shown++;

            if (shown >= 4)
                break;
        }

        if (shown == 0)
            return "- None";

        return sb.ToString().TrimEnd();
    }

    private string BuildActionText(MainStoryAction action)
    {
        if (action == null)
            return string.Empty;

        switch (action.Type)
        {
            case MainStoryActionType.ShowDialog:
                return BuildFirstDialogLine(action.SpeakerName, action.DialogText);
            case MainStoryActionType.ShowChoice:
                return string.IsNullOrWhiteSpace(action.ChoicePrompt) ? "Choice" : action.ChoicePrompt;
            case MainStoryActionType.AcceptQuest:
                return "Accept quest";
            case MainStoryActionType.SubmitEvent:
                return string.IsNullOrWhiteSpace(action.TargetId) ? "Submit event" : $"Event: {action.TargetId}";
            case MainStoryActionType.Wait:
                return $"Wait {action.WaitSeconds:0.##}s";
            case MainStoryActionType.PlayAnimationTrigger:
                return string.IsNullOrWhiteSpace(action.AnimationTrigger) ? "Play animation" : $"Anim: {action.AnimationTrigger}";
            case MainStoryActionType.SetStoryFlag:
                return $"Flag: {action.StoryFlag}";
            case MainStoryActionType.GivePokemon:
                return string.IsNullOrWhiteSpace(action.PokemonResourceId) ? "Give Pokémon" : $"Give {action.PokemonResourceId}";
            case MainStoryActionType.StartBattle:
                return action.BattleType == MainStoryBattleType.Wild
                    ? $"Battle: {action.WildPokemonResourceId}"
                    : $"Battle: {action.TrainerNpcId}";
            default:
                return action.Type.ToString();
        }
    }

    private string BuildFirstDialogLine(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.IsNullOrWhiteSpace(speaker) ? "Dialog" : speaker;

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string first = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (string.IsNullOrWhiteSpace(first))
            return string.IsNullOrWhiteSpace(speaker) ? "Dialog" : speaker;

        return string.IsNullOrWhiteSpace(speaker) ? first.Trim() : $"{speaker}: {first.Trim()}";
    }

    private string FormatOrUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "???" : value;
    }

    private void RefreshTutorialPage()
    {
        if (tutorialPages == null || tutorialPages.Length == 0)
        {
            var lines = GetTutorialTexts();
            if (lines.Count == 0)
                return;

            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].text = string.Empty;
                lines[i].color = normalColor;
            }

            if (tutorialPageText != null)
                tutorialPageText.text = "No tutorial pages configured.";
            return;
        }

        tutorialPageIndex = ClampIndex(tutorialPageIndex, tutorialPages.Length);

        for (int i = 0; i < tutorialPages.Length; i++)
            tutorialPages[i].SetActive(i == tutorialPageIndex);

        if (tutorialPageText != null)
            tutorialPageText.text = $"Page {tutorialPageIndex + 1}/{tutorialPages.Length} (Up/Down) - {tutorialPages[tutorialPageIndex].name}";
    }
}
