using UnityEngine;

public enum StoryFlagKey
{
    // Giữ nguyên integer value cũ để không break serialized data trong Inspector
    PrologueDone          = 0,
    FirstMainQuestAccepted = 1,
    StarterChosen         = 2,
    InCave                = 3,
    AfterGrassGym         = 4,
    AfterWaterGym         = 5,
    AfterFireGym          = 6,
    // Flag mới — value tiếp theo, không xen vào giữa
    MeetGreen             = 7,
    MeetBlue              = 8,
    MeetTeamRocket        = 9,
    OutCave               = 10,
    Champion              = 11,
}

public class StoryFlags : MonoBehaviour
{
    public static StoryFlags Instance { get; private set; }

    private static bool runtimeStateInitialized;
    private static bool runtimePrologueDone;
    private static bool runtimeFirstMainQuestAccepted;
    private static bool runtimeStarterChosen;
    private static bool runtimeMeetGreen;
    private static bool runtimeMeetBlue;
    private static bool runtimeAfterGrassGym;
    private static bool runtimeMeetTeamRocket;
    private static bool runtimeAfterWaterGym;
    private static bool runtimeInCave;
    private static bool runtimeOutCave;
    private static bool runtimeAfterFireGym;
    private static bool runtimeChampion;
    private static string runtimeStarterPokemonId;
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
        runtimeMeetGreen = false;
        runtimeMeetBlue = false;
        runtimeAfterGrassGym = false;
        runtimeMeetTeamRocket = false;
        runtimeAfterWaterGym = false;
        runtimeInCave = false;
        runtimeOutCave = false;
        runtimeAfterFireGym = false;
        runtimeChampion = false;
        runtimeStarterPokemonId = string.Empty;
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
    [SerializeField] private bool meetGreen;
    [SerializeField] private bool meetBlue;
    [SerializeField] private bool afterGrassGym;
    [SerializeField] private bool meetTeamRocket;
    [SerializeField] private bool afterWaterGym;
    [SerializeField] private bool inCave;
    [SerializeField] private bool outCave;
    [SerializeField] private bool afterFireGym;
    [SerializeField] private bool champion;
    [SerializeField] private string starterPokemonId;
    [SerializeField] private int mainStorySequenceIndex;
    [SerializeField] private int mainStoryStepIndex;

    public bool PrologueDone
    {
        get => runtimeStateInitialized ? runtimePrologueDone : prologueDone;
        set { runtimeStateInitialized = true; runtimePrologueDone = value; prologueDone = value; }
    }

    public bool FirstMainQuestAccepted
    {
        get => runtimeStateInitialized ? runtimeFirstMainQuestAccepted : firstMainQuestAccepted;
        set { runtimeStateInitialized = true; runtimeFirstMainQuestAccepted = value; firstMainQuestAccepted = value; }
    }

    public bool StarterChosen
    {
        get => runtimeStateInitialized ? runtimeStarterChosen : starterChosen;
        set { runtimeStateInitialized = true; runtimeStarterChosen = value; starterChosen = value; }
    }

    public bool MeetGreen
    {
        get => runtimeStateInitialized ? runtimeMeetGreen : meetGreen;
        set { runtimeStateInitialized = true; runtimeMeetGreen = value; meetGreen = value; }
    }

    public bool MeetBlue
    {
        get => runtimeStateInitialized ? runtimeMeetBlue : meetBlue;
        set { runtimeStateInitialized = true; runtimeMeetBlue = value; meetBlue = value; }
    }

    public bool AfterGrassGym
    {
        get => runtimeStateInitialized ? runtimeAfterGrassGym : afterGrassGym;
        set { runtimeStateInitialized = true; runtimeAfterGrassGym = value; afterGrassGym = value; }
    }

    public bool MeetTeamRocket
    {
        get => runtimeStateInitialized ? runtimeMeetTeamRocket : meetTeamRocket;
        set { runtimeStateInitialized = true; runtimeMeetTeamRocket = value; meetTeamRocket = value; }
    }

    public bool AfterWaterGym
    {
        get => runtimeStateInitialized ? runtimeAfterWaterGym : afterWaterGym;
        set { runtimeStateInitialized = true; runtimeAfterWaterGym = value; afterWaterGym = value; }
    }

    public bool InCave
    {
        get => runtimeStateInitialized ? runtimeInCave : inCave;
        set { runtimeStateInitialized = true; runtimeInCave = value; inCave = value; }
    }

    public bool OutCave
    {
        get => runtimeStateInitialized ? runtimeOutCave : outCave;
        set { runtimeStateInitialized = true; runtimeOutCave = value; outCave = value; }
    }

    public bool AfterFireGym
    {
        get => runtimeStateInitialized ? runtimeAfterFireGym : afterFireGym;
        set { runtimeStateInitialized = true; runtimeAfterFireGym = value; afterFireGym = value; }
    }

    public bool Champion
    {
        get => runtimeStateInitialized ? runtimeChampion : champion;
        set { runtimeStateInitialized = true; runtimeChampion = value; champion = value; }
    }

    public string StarterPokemonId
    {
        get => runtimeStateInitialized ? runtimeStarterPokemonId : starterPokemonId;
        set { runtimeStateInitialized = true; runtimeStarterPokemonId = value; starterPokemonId = value; }
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
            StoryFlagKey.PrologueDone           => PrologueDone,
            StoryFlagKey.FirstMainQuestAccepted => FirstMainQuestAccepted,
            StoryFlagKey.StarterChosen          => StarterChosen,
            StoryFlagKey.MeetGreen              => MeetGreen,
            StoryFlagKey.MeetBlue               => MeetBlue,
            StoryFlagKey.AfterGrassGym          => AfterGrassGym,
            StoryFlagKey.MeetTeamRocket         => MeetTeamRocket,
            StoryFlagKey.AfterWaterGym          => AfterWaterGym,
            StoryFlagKey.InCave                 => InCave,
            StoryFlagKey.OutCave                => OutCave,
            StoryFlagKey.AfterFireGym           => AfterFireGym,
            StoryFlagKey.Champion               => Champion,
            _                                   => false
        };
    }

    public void SetFlag(StoryFlagKey key, bool value, string optionalStarterPokemonId = null)
    {
        switch (key)
        {
            case StoryFlagKey.PrologueDone:           PrologueDone = value;           break;
            case StoryFlagKey.FirstMainQuestAccepted: FirstMainQuestAccepted = value; break;
            case StoryFlagKey.StarterChosen:
                StarterChosen = value;
                if (!string.IsNullOrWhiteSpace(optionalStarterPokemonId))
                    StarterPokemonId = optionalStarterPokemonId;
                break;
            case StoryFlagKey.MeetGreen:      MeetGreen = value;      break;
            case StoryFlagKey.MeetBlue:       MeetBlue = value;       break;
            case StoryFlagKey.AfterGrassGym:  AfterGrassGym = value;  break;
            case StoryFlagKey.MeetTeamRocket: MeetTeamRocket = value; break;
            case StoryFlagKey.AfterWaterGym:  AfterWaterGym = value;  break;
            case StoryFlagKey.InCave:         InCave = value;         break;
            case StoryFlagKey.OutCave:        OutCave = value;        break;
            case StoryFlagKey.AfterFireGym:   AfterFireGym = value;   break;
            case StoryFlagKey.Champion:       Champion = value;       break;
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
            runtimeMeetGreen = meetGreen;
            runtimeMeetBlue = meetBlue;
            runtimeAfterGrassGym = afterGrassGym;
            runtimeMeetTeamRocket = meetTeamRocket;
            runtimeAfterWaterGym = afterWaterGym;
            runtimeInCave = inCave;
            runtimeOutCave = outCave;
            runtimeAfterFireGym = afterFireGym;
            runtimeChampion = champion;
            runtimeStarterPokemonId = starterPokemonId;
            runtimeMainStorySequenceIndex = Mathf.Max(0, mainStorySequenceIndex);
            runtimeMainStoryStepIndex = Mathf.Max(0, mainStoryStepIndex);
        }
        else
        {
            prologueDone = runtimePrologueDone;
            firstMainQuestAccepted = runtimeFirstMainQuestAccepted;
            starterChosen = runtimeStarterChosen;
            meetGreen = runtimeMeetGreen;
            meetBlue = runtimeMeetBlue;
            afterGrassGym = runtimeAfterGrassGym;
            meetTeamRocket = runtimeMeetTeamRocket;
            afterWaterGym = runtimeAfterWaterGym;
            inCave = runtimeInCave;
            outCave = runtimeOutCave;
            afterFireGym = runtimeAfterFireGym;
            champion = runtimeChampion;
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
