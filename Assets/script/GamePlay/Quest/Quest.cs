using UnityEngine;

public enum QuestStatus { Ongoing, Completed }

[CreateAssetMenu(menuName = "Quest")]
public class Quest : ScriptableObject
{
    [SerializeField] private string title;
    [TextArea] [SerializeField] private string description;

    public string Title => title;
    public string Description => description;

    public QuestStatus Status { get; private set; } = QuestStatus.Ongoing;

    public void MarkCompleted()
    {
        Status = QuestStatus.Completed;
    }

    public string GetDisplayTitle()
    {
        return Status == QuestStatus.Completed ? $"{Title} (Done)" : Title;
    }
}