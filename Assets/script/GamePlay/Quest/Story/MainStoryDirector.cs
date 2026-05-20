using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainStoryDirector : MonoBehaviour
{
    public static MainStoryDirector Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    [Header("Sequence")]
    [SerializeField] private MainStorySequence sequence;
    [SerializeField] private List<MainStorySequence> sequenceChain = new();

    [Header("Runtime")]
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool autoPlaySceneStartSteps = true;
    [SerializeField] private StoryChoiceUI choiceUI;
    [SerializeField] private bool allowAutoAdvanceOnSceneMismatch = false; // if true, director will auto-select a step that matches the active scene when possible

    private int currentSequenceIndex;
    private int currentStepIndex;
    private bool isPlayingStep;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.TryInheritRuntimeConfigFrom(this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void TryInheritRuntimeConfigFrom(MainStoryDirector incoming)
    {
        if (incoming == null)
            return;

        bool hasSequence = sequence != null || (sequenceChain != null && sequenceChain.Count > 0);
        bool incomingHasSequence = incoming.sequence != null || (incoming.sequenceChain != null && incoming.sequenceChain.Count > 0);

        if (!hasSequence && incomingHasSequence)
        {
            sequence = incoming.sequence;
            sequenceChain = incoming.sequenceChain != null
                ? new List<MainStorySequence>(incoming.sequenceChain)
                : new List<MainStorySequence>();
            currentSequenceIndex = 0;
            currentStepIndex = 0;
        }

        if (choiceUI == null && incoming.choiceUI != null)
            choiceUI = incoming.choiceUI;
    }

    public IReadOnlyList<MainStorySequence> GetStorySequences()
    {
        if (sequenceChain != null && sequenceChain.Count > 0)
            return sequenceChain;

        if (sequence != null)
            return new[] { sequence };

        return Array.Empty<MainStorySequence>();
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
        RestoreProgressFromStoryFlags();
        TryPlaySceneStartStep();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreProgressFromStoryFlags();
        var step = GetCurrentStep();;
        // If current step doesn't match the loaded scene, optionally try to advance to a step that does.
        AlignStepToActiveSceneIfNeeded();
        TryPlaySceneStartStep();
    }

    private void AlignStepToActiveSceneIfNeeded()
    {
        if (!allowAutoAdvanceOnSceneMismatch)
            return;

        var activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var step = GetCurrentStep();
        if (step != null && !string.IsNullOrWhiteSpace(step.SceneName) && !string.Equals(step.SceneName, activeSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            var seq = GetCurrentSequence();
            if (seq == null || seq.Steps == null)
                return;

            for (int i = 0; i < seq.Steps.Count; i++)
            {
                var s = seq.Steps[i];
                if (s == null || string.IsNullOrWhiteSpace(s.SceneName))
                    continue;

                if (string.Equals(s.SceneName, activeSceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    currentStepIndex = i;
                    SaveProgressToStoryFlags();
                    return;
                }
            }
        }
    }

    private void RestoreProgressFromStoryFlags()
    {
        var flags = StoryFlags.Instance;
        if (flags == null)
            return;

        var savedSequenceIndex = Mathf.Max(0, flags.MainStorySequenceIndex);
        var savedStepIndex = Mathf.Max(0, flags.MainStoryStepIndex);

        var shouldRestore = savedSequenceIndex > currentSequenceIndex ||
                            (savedSequenceIndex == currentSequenceIndex && savedStepIndex >= currentStepIndex);

        if (!shouldRestore)
            return;

        currentSequenceIndex = savedSequenceIndex;
        currentStepIndex = savedStepIndex;
        ClampProgressToValidRange();
    }

    private void SaveProgressToStoryFlags()
    {
        var flags = StoryFlags.GetOrCreate();
        flags.MainStorySequenceIndex = Mathf.Max(0, currentSequenceIndex);
        flags.MainStoryStepIndex = Mathf.Max(0, currentStepIndex);
    }

    private void ClampProgressToValidRange()
    {
        var activeSequence = GetCurrentSequence();
        if (activeSequence == null || activeSequence.Steps == null || activeSequence.Steps.Count == 0)
        {
            currentStepIndex = 0;
            return;
        }

        currentStepIndex = Mathf.Clamp(currentStepIndex, 0, activeSequence.Steps.Count - 1);
    }

    public bool TryTrigger(string triggerId)
    {
        if (isPlayingStep)
            return false;

        var step = GetCurrentStep();
        if (step == null)
            return false;

        if (!IsStepMatch(step, triggerId, sceneStart: false))
            return false;

        StartCoroutine(PlayCurrentStep(triggerId, sceneStart: false));
        return true;
    }

    public bool IsCurrentStep(string triggerId)
    {
        var step = GetCurrentStep();
        if (step == null)
            return false;

        return string.Equals(step.TriggerId, triggerId, System.StringComparison.OrdinalIgnoreCase);
    }

    private void TryPlaySceneStartStep()
    {
        if (!autoPlaySceneStartSteps || isPlayingStep)
            return;

        var step = GetCurrentStep();
        if (!IsStepMatch(step, triggerId: null, sceneStart: true))
            return;
        StartCoroutine(PlayCurrentStep(triggerId: null, sceneStart: true));
    }

    private MainStoryStep GetCurrentStep()
    {
        var activeSequence = GetCurrentSequence();
        if (activeSequence == null || activeSequence.Steps == null)
            return null;

        if (currentStepIndex < 0 || currentStepIndex >= activeSequence.Steps.Count)
            return null;

        return activeSequence.Steps[currentStepIndex];
    }

    private MainStorySequence GetCurrentSequence()
    {
        if (sequenceChain != null && sequenceChain.Count > 0)
        {
            if (currentSequenceIndex >= 0 && currentSequenceIndex < sequenceChain.Count)
                return sequenceChain[currentSequenceIndex];

            return null;
        }

        return sequence;
    }

    private bool MoveToNextStepOrSequence()
    {
        var activeSequence = GetCurrentSequence();
        if (activeSequence == null || activeSequence.Steps == null)
            return false;

        currentStepIndex++;
        if (currentStepIndex < activeSequence.Steps.Count)
            return true;

        if (sequenceChain != null && sequenceChain.Count > 0)
        {
            currentSequenceIndex++;
            currentStepIndex = 0;

            if (currentSequenceIndex < sequenceChain.Count)
            {
                SaveProgressToStoryFlags();
                return true;
            }
        }

        SaveProgressToStoryFlags();
        return false;
    }

    private bool IsStepMatch(MainStoryStep step, string triggerId, bool sceneStart)
    {
        if (step == null)
            return false;

        if (!string.IsNullOrWhiteSpace(step.SceneName))
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (!string.Equals(sceneName, step.SceneName, System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (sceneStart)
            return step.TriggerOnSceneLoad;

        if (string.IsNullOrWhiteSpace(step.TriggerId))
            return true;

        return string.Equals(step.TriggerId, triggerId, System.StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerator PlayCurrentStep(string triggerId, bool sceneStart)
    {
        if (isPlayingStep)
            yield break;

        var step = GetCurrentStep();
        if (!IsStepMatch(step, triggerId, sceneStart))
            yield break;

        isPlayingStep = true;

        if (step.Actions != null)
        {
            for (int i = 0; i < step.Actions.Count; i++)
            {
                var action = step.Actions[i];
                if (action == null)
                    continue;

                yield return ExecuteAction(action);
            }
        }

        if (step.OneShot)
            MoveToNextStepOrSequence();

        isPlayingStep = false;

        SaveProgressToStoryFlags();

        TryPlaySceneStartStep();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private IEnumerator ExecuteAction(MainStoryAction action)
    {
        if (action == null)
            yield break;

        bool hasChoiceOptions = action.ChoiceOptions != null && action.ChoiceOptions.Count > 0;

        switch (action.Type)
        {
            case MainStoryActionType.ShowDialog:
                if (!string.IsNullOrWhiteSpace(action.DialogText) && DialogManager.Instance != null)
                    yield return ShowDialogTextAndWait(action.DialogText, action.SpeakerName, action.Portrait);
                break;

            case MainStoryActionType.AcceptQuest:
                ExecuteAcceptQuest(action);
                break;

            case MainStoryActionType.SubmitEvent:
                if (action.SubmitQuestEvent)
                    ExecuteSubmitEvent(action);
                break;

            case MainStoryActionType.Wait:
                yield return new WaitForSeconds(action.WaitSeconds);
                break;

            case MainStoryActionType.MoveNpc:
                yield return ExecuteMoveNpc(action);
                break;

            case MainStoryActionType.PlayAnimationTrigger:
                ExecutePlayAnimation(action);
                break;

            case MainStoryActionType.SetStoryFlag:
                ExecuteSetStoryFlag(action.StoryFlag, action.StoryFlagValue, action.StarterPokemonId);
                break;

            case MainStoryActionType.GivePokemon:
                ExecuteGivePokemon(action.PokemonResourceId, action.PokemonLevel);
                break;

            case MainStoryActionType.ShowChoice:
                yield return ExecuteShowChoice(action);
                break;

            case MainStoryActionType.StartBattle:
                yield return ExecuteStartBattle(action);
                break;
        }
    }

    private void ExecuteAcceptQuest(MainStoryAction action)
    {
        var qm = QuestManager.Instance;
        if (qm == null)
            return;

        var quest = action.UseCurrentMainStoryQuest ? qm.GetCurrentMainStoryQuest() : action.QuestToAccept;
        if (quest == null)
            return;

        qm.AddQuest(quest, action.AcceptOnceOnly);
    }

    private void ExecuteSubmitEvent(MainStoryAction action)
    {
        var qm = QuestManager.Instance;
        if (qm == null)
            return;

        var targetId = string.IsNullOrWhiteSpace(action.TargetId)
            ? ResolveDefaultTargetId()
            : action.TargetId;

        qm.SubmitEvent(new QuestEvent(action.EventType, targetId, Mathf.Max(1, action.Amount)));
    }

    private string ResolveDefaultTargetId()
    {
        var step = GetCurrentStep();
        if (step != null && !string.IsNullOrWhiteSpace(step.StepId))
            return step.StepId;

        return SceneManager.GetActiveScene().name;
    }

    private IEnumerator ShowDialogTextAndWait(string dialogText, string speaker, Sprite portrait)
    {
        if (string.IsNullOrWhiteSpace(dialogText) || DialogManager.Instance == null)
            yield break;

        var lines = SplitDialogLines(dialogText);
        for (int i = 0; i < lines.Count; i++)
            yield return ShowSpeakerLineAndWait(lines[i], speaker, portrait);
    }

    private IEnumerator ShowSpeakerLineAndWait(string line, string speaker, Sprite portrait)
    {
        if (string.IsNullOrWhiteSpace(line) || DialogManager.Instance == null)
            yield break;

        bool finished = false;

        void OnFinish()
        {
            finished = true;
        }

        DialogManager.Instance.OnDialogFinished += OnFinish;
        DialogManager.Instance.ShowDialog(speaker, portrait, line, GameState.Overworld);

        float timeout = Time.time + 30f;
        while (!finished && Time.time <= timeout)
            yield return null;

        DialogManager.Instance.OnDialogFinished -= OnFinish;
    }

    private List<string> SplitDialogLines(string text)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return lines;

        var raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int i = 0; i < raw.Length; i++)
        {
            var line = raw[i].Trim();
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        return lines;
    }

    private IEnumerator ShowLineAndWait(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || DialogManager.Instance == null)
            yield break;

        bool finished = false;

        void OnFinish()
        {
            finished = true;
        }

        DialogManager.Instance.OnDialogFinished += OnFinish;
        DialogManager.Instance.ShowDialog(line, GameState.Overworld);

        float timeout = Time.time + 30f;
        while (!finished && Time.time <= timeout)
            yield return null;

        DialogManager.Instance.OnDialogFinished -= OnFinish;
    }

    private void ExecutePlayAnimation(MainStoryAction action)
    {
        if (action == null || string.IsNullOrWhiteSpace(action.AnimationTrigger))
            return;

        if (action.Animator == null)
            return;

        action.Animator.SetTrigger(action.AnimationTrigger);
    }

    private IEnumerator ExecuteMoveNpc(MainStoryAction action)
    {
        if (action == null)
            yield break;

        var npc = ResolveNpc(action.NpcId);
        if (npc == null)
            yield break;

        var targetPoint = ResolveStoryMovePoint(action.MoveTargetId);
        if (targetPoint == null)
            yield break;

        yield return npc.MoveTo(targetPoint.position, action.MoveSpeed, action.FaceTargetOnArrive);
    }

    private void ExecuteSetStoryFlag(StoryFlagKey key, bool value, string starterPokemonId)
    {
        var flags = StoryFlags.Instance;
        if (flags == null)
            return;

        flags.SetFlag(key, value, starterPokemonId);
    }

    private void ExecuteGivePokemon(string pokemonResourceId, int level)
    {
        TryGivePokemon(pokemonResourceId, level, out _);
    }

    private StoryChoiceUI ResolveChoiceUI()
    {
        if (choiceUI != null)
            return choiceUI;

        var sceneChoiceUI = UnityEngine.Object.FindObjectOfType<StoryChoiceUI>(true);
        if (sceneChoiceUI != null)
        {
            choiceUI = sceneChoiceUI;
            return choiceUI;
        }

        var systemRootPrefab = Resources.Load<GameObject>("SystemRoot");
        if (systemRootPrefab == null)
            return null;

        var prefabChoiceUI = systemRootPrefab.GetComponentInChildren<StoryChoiceUI>(true);
        if (prefabChoiceUI == null)
            return null;

        var runtimeRoot = UnityEngine.Object.Instantiate(systemRootPrefab);
        if (persistAcrossScenes)
            UnityEngine.Object.DontDestroyOnLoad(runtimeRoot);

        choiceUI = runtimeRoot.GetComponentInChildren<StoryChoiceUI>(true);
        return choiceUI;
    }

    private IEnumerator ExecuteShowChoice(MainStoryAction action)
    {
        if (action == null || action.ChoiceOptions == null || action.ChoiceOptions.Count == 0)
            yield break;

        // Resolve an active runtime StoryChoiceUI. If the inspector `choiceUI` references a prefab asset
        // (not part of the active scene), instantiate it so the UI can be shown at runtime.
        StoryChoiceUI runtimeChoiceUI = null;
        StoryChoiceUI activeChoiceUI = null;

        choiceUI = ResolveChoiceUI();

        if (choiceUI != null)
        {
            bool inScene = false;
            try
            {
                inScene = choiceUI.gameObject.scene.IsValid();
            }
            catch { inScene = false; }

            if (!inScene)
            {
                var go = GameObject.Instantiate(choiceUI.gameObject);
                go.name = choiceUI.gameObject.name + " (runtime)";
                if (persistAcrossScenes)
                    DontDestroyOnLoad(go);

                runtimeChoiceUI = go.GetComponent<StoryChoiceUI>();
                Debug.Log($"[MainStoryDirector.ExecuteShowChoice] runtimeChoiceUI={(runtimeChoiceUI==null?"null":"found")}");
                activeChoiceUI = runtimeChoiceUI;
            }
            else
            {
                activeChoiceUI = choiceUI;
            }
        }

        if (activeChoiceUI == null)
            activeChoiceUI = StoryChoiceUI.Instance;

        if (activeChoiceUI != null && activeChoiceUI.CanRenderChoices())
        {
            bool finished = false;
            int selectedIndex = -1;

            activeChoiceUI.ShowChoices(
                action.ChoicePrompt,
                action.ChoiceOptions,
                onSelected: index =>
                {
                    selectedIndex = index;
                    finished = true;
                },
                onCancel: () => finished = true
            );

            while (!finished)
                yield return null;

            if (selectedIndex < 0 || selectedIndex >= action.ChoiceOptions.Count)
                yield break;

            yield return ResolveChoiceOption(action, action.ChoiceOptions[selectedIndex]);
            yield break;
        }

        if (activeChoiceUI != null)
            Debug.LogWarning("[MainStoryDirector] StoryChoiceUI is assigned but not configured. Falling back to keyboard choice.");

        // fallback without panel UI
        yield return ShowLineAndWait(BuildChoicePrompt(action.ChoicePrompt, action.ChoiceOptions));

        int keyboardSelectedIndex = -1;
        while (keyboardSelectedIndex < 0)
        {
            for (int i = 0; i < action.ChoiceOptions.Count && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    keyboardSelectedIndex = i;
                    break;
                }
            }

            yield return null;
        }

        yield return ResolveChoiceOption(action, action.ChoiceOptions[keyboardSelectedIndex]);
    }

    private IEnumerator ResolveChoiceOption(MainStoryAction action, MainStoryChoiceOption option)
    {
        if (option == null)
            yield break;

        string pickedPokemonName = string.Empty;

        if (option.GivePokemon)
        {
            TryGivePokemon(option.PokemonResourceId, option.PokemonLevel, out pickedPokemonName);
            
            var saveLoadSys = FindObjectOfType<SaveLoadSystem>();
            if (saveLoadSys != null)
                saveLoadSys.Save("SaveFile1");
        }

        if (option.SetStoryFlag)
        {
            ExecuteSetStoryFlag(option.StoryFlag, option.StoryFlagValue, option.StarterPokemonId);
            
            var saveLoadSys = FindObjectOfType<SaveLoadSystem>();
            if (saveLoadSys != null)
                saveLoadSys.Save("SaveFile1");
        }

        if (option.SubmitQuestEvent)
        {
            Debug.Log($"[MainStoryDirector.ResolveChoiceOption] SubmitEvent: {option.EventType} to {option.TargetId}");
            ExecuteChoiceEvent(option);
        }

        if (!string.IsNullOrWhiteSpace(option.ResultLine))
        {
            Debug.Log($"[MainStoryDirector.ResolveChoiceOption] Result line suppressed: {option.ResultLine}");
        }
    }

    private IEnumerator ExecuteStartBattle(MainStoryAction action)
    {
        if (action == null || GameController.Instance == null)
            yield break;

        switch (action.BattleType)
        {
            case MainStoryBattleType.Wild:
            {
                var pokemonBase = Resources.Load<PokemonBase>($"PokemonData/{action.WildPokemonResourceId?.ToLowerInvariant()}");
                if (pokemonBase == null)
                    yield break;

                GameController.Instance.StartWildBattle(new Pokemon(pokemonBase, action.WildPokemonLevel));
                break;
            }

            case MainStoryBattleType.Trainer:
            {
                var trainer = ResolveTrainer(action.TrainerNpcId);
                if (trainer == null)
                    yield break;

                GameController.Instance.StartTrainerBattle(trainer);
                break;
            }
        }

        if (!action.WaitForBattleEnd)
            yield break;

        yield return new WaitUntil(() => GameController.Instance.State == GameState.Overworld);
    }

    private void ExecuteChoiceEvent(MainStoryChoiceOption option)
    {
        var qm = QuestManager.Instance;
        if (qm == null || option == null)
            return;

        var targetId = string.IsNullOrWhiteSpace(option.TargetId)
            ? ResolveDefaultTargetId()
            : option.TargetId;

        qm.SubmitEvent(new QuestEvent(option.EventType, targetId, Mathf.Max(1, option.Amount)));
    }

    private string BuildChoicePrompt(string prompt, System.Collections.Generic.IReadOnlyList<MainStoryChoiceOption> options)
    {
        var basePrompt = string.IsNullOrWhiteSpace(prompt) ? "Choose an option:" : prompt;

        for (int i = 0; i < options.Count && i < 9; i++)
        {
            var option = options[i];
            if (option == null)
                continue;

            var label = string.IsNullOrWhiteSpace(option.OptionLabel)
                ? $"Option {i + 1}"
                : option.OptionLabel;

            basePrompt += $"\n[{i + 1}] {label}";
        }

        return basePrompt;
    }

    private bool TryGivePokemon(string pokemonResourceId, int level, out string pokemonName)
    {
        pokemonName = string.Empty;

        if (string.IsNullOrWhiteSpace(pokemonResourceId))
            return false;

        var party = PlayerParty.Instance;
        if (party == null)
            return false;

        var pokemonBase = Resources.Load<PokemonBase>($"PokemonData/{pokemonResourceId.ToLowerInvariant()}");
        if (pokemonBase == null)
            return false;

        var pokemon = new Pokemon(pokemonBase, Mathf.Max(1, level));
        party.AddPokemon(pokemon);
        pokemonName = pokemonBase.Name;
        return true;
    }

    private NPC ResolveTrainer(string trainerNpcId)
    {
        if (string.IsNullOrWhiteSpace(trainerNpcId))
            return null;

        var npcs = UnityEngine.Object.FindObjectsOfType<NPC>(true);
        for (int i = 0; i < npcs.Length; i++)
        {
            var npc = npcs[i];
            if (npc == null)
                continue;

            if (string.Equals(npc.NPCId, trainerNpcId, System.StringComparison.OrdinalIgnoreCase))
                return npc;
        }

        return null;
    }

    private NPC ResolveNpc(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return null;

        var npcs = UnityEngine.Object.FindObjectsOfType<NPC>(true);
        for (int i = 0; i < npcs.Length; i++)
        {
            var npc = npcs[i];
            if (npc == null)
                continue;

            if (string.Equals(npc.NPCId, npcId, System.StringComparison.OrdinalIgnoreCase))
                return npc;
        }

        return null;
    }

    private Transform ResolveStoryMovePoint(string moveTargetId)
    {
        if (string.IsNullOrWhiteSpace(moveTargetId))
            return null;

        var points = UnityEngine.Object.FindObjectsOfType<SpawnPoint>(true);
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            if (point == null || string.IsNullOrWhiteSpace(point.Id))
                continue;

            if (string.Equals(point.Id, moveTargetId, System.StringComparison.OrdinalIgnoreCase))
                return point.transform;
        }

        var legacy = GameObject.Find("StoryMove_" + moveTargetId);
        return legacy != null ? legacy.transform : null;
    }

    private string FormatResultLine(string line, string pokemonName)
    {
        if (string.IsNullOrWhiteSpace(line))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(pokemonName))
            return line;

        return line.Replace("{pokemon}", pokemonName);
    }
}