using UnityEngine;

public enum StoryFlagKey
{
    PrologueDone,
    FirstMainQuestAccepted,
    StarterChosen
}

public class StoryFlags : MonoBehaviour
{
    public static StoryFlags Instance { get; private set; }

    private static bool runtimeStateInitialized;
    private static bool runtimePrologueDone;
    private static bool runtimeFirstMainQuestAccepted;
    private static bool runtimeStarterChosen;
    private static string runtimeStarterPokemonId;
    private static int runtimeMainStorySequenceIndex;
    private static int runtimeMainStoryStepIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRuntimeState()
    {
        runtimeStateInitialized = false;
        runtimePrologueDone = false;
        runtimeFirstMainQuestAccepted = false;
        runtimeStarterChosen = false;
        runtimeStarterPokemonId = string.Empty;
        runtimeMainStorySequenceIndex = 0;
        runtimeMainStoryStepIndex = 0;
    }

    public static StoryFlags GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        var existing = FindObjectOfType<StoryFlags>();
        if (existing != null)
            return existing;

        var go = new GameObject("StoryFlags");
        return go.AddComponent<StoryFlags>();
    }

    [SerializeField] private bool prologueDone;
    [SerializeField] private bool firstMainQuestAccepted;
    [SerializeField] private bool starterChosen;
    [SerializeField] private string starterPokemonId;
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
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!runtimeStateInitialized)
        {
            runtimeStateInitialized = true;
            runtimePrologueDone = prologueDone;
            runtimeFirstMainQuestAccepted = firstMainQuestAccepted;
            runtimeStarterChosen = starterChosen;
            runtimeStarterPokemonId = starterPokemonId;
            runtimeMainStorySequenceIndex = Mathf.Max(0, mainStorySequenceIndex);
            runtimeMainStoryStepIndex = Mathf.Max(0, mainStoryStepIndex);
        }
        else
        {
            prologueDone = runtimePrologueDone;
            firstMainQuestAccepted = runtimeFirstMainQuestAccepted;
            starterChosen = runtimeStarterChosen;
            starterPokemonId = runtimeStarterPokemonId;
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