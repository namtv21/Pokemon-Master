using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObjectiveRuntimeProgress
{
    public int current;
    public bool completed;
}

public class QuestRuntimeState
{
    public Quest Definition { get; }
    public QuestStatus Status { get; private set; } = QuestStatus.Ongoing;

    private readonly List<ObjectiveRuntimeProgress> objectiveProgress = new();

    public QuestRuntimeState(Quest definition)
    {
        Definition = definition;

        int count = definition != null && definition.Objectives != null
            ? definition.Objectives.Count
            : 0;

        for (int i = 0; i < count; i++)
            objectiveProgress.Add(new ObjectiveRuntimeProgress());
    }

    public int ObjectiveCount => objectiveProgress.Count;

    public int GetObjectiveCurrent(int index)
    {
        if (index < 0 || index >= objectiveProgress.Count) return 0;
        return objectiveProgress[index].current;
    }

    public bool IsObjectiveCompleted(int index)
    {
        if (index < 0 || index >= objectiveProgress.Count) return false;
        return objectiveProgress[index].completed;
    }

    public bool AddProgress(int index, int amount, int required)
    {
        if (index < 0 || index >= objectiveProgress.Count) return false;
        if (required <= 0) required = 1;
        if (amount <= 0) amount = 1;

        var p = objectiveProgress[index];
        if (p.completed) return false;

        int old = p.current;
        p.current = Mathf.Min(required, p.current + amount);
        p.completed = p.current >= required;

        bool changed = p.current != old || p.completed;
        if (changed) RefreshStatus();

        return changed;
    }

    public bool MarkObjectiveCompleted(int index, int required)
    {
        if (index < 0 || index >= objectiveProgress.Count) return false;

        var p = objectiveProgress[index];
        if (p.completed) return false;

        p.current = Mathf.Max(required, 1);
        p.completed = true;
        RefreshStatus();
        return true;
    }

    public void SetObjectiveProgress(int index, int current, bool completed)
    {
        if (index < 0 || index >= objectiveProgress.Count) return;

        var p = objectiveProgress[index];
        p.current = Mathf.Max(0, current);
        p.completed = completed;
        RefreshStatus();
    }

    public void MarkAllCompleted()
    {
        for (int i = 0; i < objectiveProgress.Count; i++)
        {
            objectiveProgress[i].completed = true;
            if (objectiveProgress[i].current <= 0)
                objectiveProgress[i].current = 1;
        }
        Status = QuestStatus.Completed;
    }

    private void RefreshStatus()
    {
        if (objectiveProgress.Count == 0)
        {
            Status = QuestStatus.Completed;
            return;
        }

        for (int i = 0; i < objectiveProgress.Count; i++)
        {
            if (!objectiveProgress[i].completed)
            {
                Status = QuestStatus.Ongoing;
                return;
            }
        }

        Status = QuestStatus.Completed;
    }
}