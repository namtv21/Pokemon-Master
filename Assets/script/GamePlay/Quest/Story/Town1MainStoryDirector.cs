using System;
using System.Collections;
using UnityEngine;

public class Town1MainStoryDirector : MonoBehaviour
{
    [Header("Run Conditions")]
    [SerializeField] private bool requirePrologueDone = false;
    [SerializeField] private bool runOnlyOnce = true;

    [Header("Quest")]
    [SerializeField] private Quest mainQuestToStart;
    [SerializeField] private bool fallbackToCurrentMainStoryQuest = true;
    [SerializeField] private bool acceptOnceOnly = true;

    [Header("Intro Dialog (optional)")]
    [SerializeField] private Dialog introDialog;
    [SerializeField] private string speakerName = "Narrator";
    [SerializeField] private Sprite speakerPortrait;

    [Header("Objective Hint Event")]
    [SerializeField] private bool submitStartEvent = true;
    [SerializeField] private QuestEventType startEventType = QuestEventType.Custom;
    [SerializeField] private string startEventTargetId = "town1_main_started";

    private bool running;

    private void Start()
    {
        StartCoroutine(RunFlow());
    }

    private IEnumerator RunFlow()
    {
        if (running)
            yield break;

        running = true;
        yield return null;

        var qm = QuestManager.Instance;
        if (qm == null)
            yield break;

        var targetQuest = ResolveQuest(qm);
        if (targetQuest == null)
            yield break;

        if (runOnlyOnce && (qm.HasAcceptedBefore(targetQuest) || qm.IsQuestCompleted(targetQuest)))
            yield break;

        if (introDialog != null && DialogManager.Instance != null)
            yield return ShowDialogAndWait(introDialog, speakerName, speakerPortrait);

        qm.AddQuest(targetQuest, acceptOnceOnly);

        if (submitStartEvent)
            qm.SubmitEvent(new QuestEvent(startEventType, ResolveStartTarget(), 1));
    }

    private Quest ResolveQuest(QuestManager qm)
    {
        if (mainQuestToStart != null)
            return mainQuestToStart;

        if (!fallbackToCurrentMainStoryQuest)
            return null;

        return qm.GetCurrentMainStoryQuest();
    }

    private string ResolveStartTarget()
    {
        if (!string.IsNullOrWhiteSpace(startEventTargetId))
            return startEventTargetId;

        return gameObject.name;
    }

    private IEnumerator ShowDialogAndWait(Dialog dialog, string speaker, Sprite portrait)
    {
        bool finished = false;

        void OnFinish()
        {
            finished = true;
        }

        DialogManager.Instance.OnDialogFinished += OnFinish;
        DialogManager.Instance.ShowDialog(speaker, portrait, dialog, GameState.Overworld);

        float timeout = Time.time + 30f;
        while (!finished && Time.time <= timeout)
            yield return null;

        DialogManager.Instance.OnDialogFinished -= OnFinish;
    }
}
