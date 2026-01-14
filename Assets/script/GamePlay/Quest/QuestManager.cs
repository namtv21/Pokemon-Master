using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    [SerializeField] private Quest tutorialQuest;

    private List<Quest> activeQuests = new List<Quest>();
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

    }
    private void Start()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Tutorial")
        {
            AddQuest(tutorialQuest);
        }
    }

    public void AddQuest(Quest quest)
    {
        if (!activeQuests.Contains(quest))
        {
            activeQuests.Add(quest);
            // Có thể hiện thông báo
            DialogManager.Instance.ShowDialog($"Quest added: {quest.Title}");

        }
    }

    public List<Quest> GetActiveQuests()
    {
        return activeQuests;
    }

    public void CompleteQuest(Quest quest)
    {
        if (activeQuests.Contains(quest))
        {
            activeQuests.Remove(quest);
            // Có thể hiện thông báo
            DialogManager.Instance.ShowDialog($"Quest completed: {quest}");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nếu rời khỏi Tutorial scene thì đánh dấu quest Tutorial là Done
        if (scene.name != "Tutorial")
        {
            foreach (var quest in activeQuests)
            {
                if (quest.Title == "Tutorial" && quest.Status == QuestStatus.Ongoing)
                {
                    quest.MarkCompleted();
                }
            }
        }
    }
        
}