using UnityEngine;

public enum StoryFlagKey
{
    PrologueDone,
    FirstMainQuestAccepted,
    StarterChosen,
    InCave,
    AfterGrassGym,
    AfterWaterGym,
    AfterFireGym
}

public class StoryFlags : MonoBehaviour
{
    public static StoryFlags Instance { get; private set; }

    private static bool runtimeStateInitialized;
    private static bool runtimePrologueDone;
    private static bool runtimeFirstMainQuestAccepted;
    private static bool runtimeStarterChosen;
    private static bool runtimeInCave;
    private static string runtimeStarterPokemonId;
    private static bool runtimeAfterGrassGym;
    private static bool runtimeAfterWaterGym;
    private static bool runtimeAfterFireGym;
    private static int runtimeMainStorySequenceIndex;
    private static int runtimeMainStoryStepIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRuntimeState()
    {
        runtimeStateInitialized = false;
        Instance = null;
        runtimePrologueDone = false;
        runtimeFirstMainQuestAccepted = false;
        runtimeStarterChosen = false;
        runtimeInCave = false;
        runtimeStarterPokemonId = string.Empty;
        runtimeAfterGrassGym = false;
        runtimeAfterWaterGym = false;
        runtimeAfterFireGym = false;
        runtimeMainStorySequenceIndex = 0;
        runtimeMainStoryStepIndex = 0;
    }

    public static StoryFlags GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        var existing = FindObjectOfType<StoryFlags>(true);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var go = new GameObject("StoryFlags");
        return go.AddComponent<StoryFlags>();
    }

    [SerializeField] private bool prologueDone;
    [SerializeField] private bool firstMainQuestAccepted;
    [SerializeField] private bool starterChosen;
    [SerializeField] private bool inCave;
    [SerializeField] private string starterPokemonId;
    [SerializeField] private bool afterGrassGym;
    [SerializeField] private bool afterWaterGym;
    [SerializeField] private bool afterFireGym;
    [SerializeField] private int mainStorySequenceIndex;
    [SerializeField] private int mainStoryStepIndex;

    public bool PrologueDone
    {
        get => runtimeStateInitialized ? runtimePrologueDone : prologueDone;
        set
        {
            runtimeStateInitialized = true;
            runtimePrologueDone = value;
            prologueDone = value;
        }
    }


    public bool FirstMainQuestAccepted
    {
        get => runtimeStateInitialized ? runtimeFirstMainQuestAccepted : firstMainQuestAccepted;
        set
        {
            runtimeStateInitialized = true;
            runtimeFirstMainQuestAccepted = value;
            firstMainQuestAccepted = value;
        }
    }

    public bool StarterChosen
    {
        get => runtimeStateInitialized ? runtimeStarterChosen : starterChosen;
        set
        {
            runtimeStateInitialized = true;
            runtimeStarterChosen = value;
            starterChosen = value;
        }
    }

    public bool InCave
    {
        get => runtimeStateInitialized ? runtimeInCave : inCave;
        set
        {
            runtimeStateInitialized = true;
            runtimeInCave = value;
            inCave = value;
        }
    }

    public bool AfterGrassGym
    {
        get => runtimeStateInitialized ? runtimeAfterGrassGym : afterGrassGym;
        set
        {
            runtimeStateInitialized = true;
            runtimeAfterGrassGym = value;
            afterGrassGym = value;
        }
    }

    public bool AfterWaterGym
    {
        get => runtimeStateInitialized ? runtimeAfterWaterGym : afterWaterGym;
        set
        {
            runtimeStateInitialized = true;
            runtimeAfterWaterGym = value;
            afterWaterGym = value;
        }
    }

    public bool AfterFireGym
    {
        get => runtimeStateInitialized ? runtimeAfterFireGym : afterFireGym;
        set
        {
            runtimeStateInitialized = true;
            runtimeAfterFireGym = value;
            afterFireGym = value;
        }
    }

    public string StarterPokemonId
    {
        get => runtimeStateInitialized ? runtimeStarterPokemonId : starterPokemonId;
        set
        {
            runtimeStateInitialized = true;
            runtimeStarterPokemonId = value;
            starterPokemonId = value;
        }
    }

    public int MainStorySequenceIndex
    {
        get => Mathf.Max(0, runtimeStateInitialized ? runtimeMainStorySequenceIndex : mainStorySequenceIndex);
        set
        {
            var nextValue = Mathf.Max(0, value);
            runtimeStateInitialized = true;
            if (runtimeMainStorySequenceIndex != nextValue)
                Debug.Log($"[StoryFlags] MainStorySequenceIndex: {runtimeMainStorySequenceIndex} -> {nextValue}");
            runtimeMainStorySequenceIndex = nextValue;
            mainStorySequenceIndex = nextValue;
        }
    }

    public int MainStoryStepIndex
    {
        get => Mathf.Max(0, runtimeStateInitialized ? runtimeMainStoryStepIndex : mainStoryStepIndex);
        set
        {
            var nextValue = Mathf.Max(0, value);
            runtimeStateInitialized = true;
            if (runtimeMainStoryStepIndex != nextValue)
                Debug.Log($"[StoryFlags] MainStoryStepIndex: {runtimeMainStoryStepIndex} -> {nextValue}");
            runtimeMainStoryStepIndex = nextValue;
            mainStoryStepIndex = nextValue;
        }
    }

    public bool GetFlag(StoryFlagKey key)
    {
        return key switch
        {
            StoryFlagKey.PrologueDone => PrologueDone,
            StoryFlagKey.FirstMainQuestAccepted => FirstMainQuestAccepted,
            StoryFlagKey.StarterChosen => StarterChosen,
            StoryFlagKey.InCave => InCave,
            StoryFlagKey.AfterGrassGym => AfterGrassGym,
            StoryFlagKey.AfterWaterGym => AfterWaterGym,
            StoryFlagKey.AfterFireGym => AfterFireGym,
            _ => false
        };
    }

    public void SetFlag(StoryFlagKey key, bool value, string optionalStarterPokemonId = null)
    {
        switch (key)
        {
            case StoryFlagKey.PrologueDone:
                PrologueDone = value;
                break;
            case StoryFlagKey.FirstMainQuestAccepted:
                FirstMainQuestAccepted = value;
                break;
            case StoryFlagKey.StarterChosen:
                StarterChosen = value;
                if (!string.IsNullOrWhiteSpace(optionalStarterPokemonId))
                    StarterPokemonId = optionalStarterPokemonId;
                break;
            case StoryFlagKey.InCave:
                InCave = value;
                break;
            case StoryFlagKey.AfterGrassGym:
                AfterGrassGym = value;
                break;
            case StoryFlagKey.AfterWaterGym:
                AfterWaterGym = value;
                break;
            case StoryFlagKey.AfterFireGym:
                AfterFireGym = value;
                break;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DuplicateSystemRootUtility.DestroyDuplicate(this, Instance);
            return;
        }

        Instance = this;

        if (!runtimeStateInitialized)
        {
            runtimeStateInitialized = true;
            runtimePrologueDone = prologueDone;
            runtimeFirstMainQuestAccepted = firstMainQuestAccepted;
            runtimeStarterChosen = starterChosen;
            runtimeInCave = inCave;
            runtimeStarterPokemonId = starterPokemonId;
            runtimeAfterGrassGym = afterGrassGym;
            runtimeAfterWaterGym = afterWaterGym;
            runtimeAfterFireGym = afterFireGym;
            runtimeMainStorySequenceIndex = Mathf.Max(0, mainStorySequenceIndex);
            runtimeMainStoryStepIndex = Mathf.Max(0, mainStoryStepIndex);
        }
        else
        {
            prologueDone = runtimePrologueDone;
            firstMainQuestAccepted = runtimeFirstMainQuestAccepted;
            starterChosen = runtimeStarterChosen;
            inCave = runtimeInCave;
            starterPokemonId = runtimeStarterPokemonId;
            afterGrassGym = runtimeAfterGrassGym;
            afterWaterGym = runtimeAfterWaterGym;
            afterFireGym = runtimeAfterFireGym;
            mainStorySequenceIndex = runtimeMainStorySequenceIndex;
            mainStoryStepIndex = runtimeMainStoryStepIndex;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
