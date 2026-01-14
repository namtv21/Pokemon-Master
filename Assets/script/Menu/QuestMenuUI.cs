using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text questText;

    private List<string> activeQuests = new List<string>();

    private void Awake()
    {
        questPanel.SetActive(false);
    }
    public void HandleUpdate()
    {
        if (!questPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.X))
        {
            HideQuests();
            MenuController.Instance.SetState(MenuState.Main);
        }
    }
    public void ShowQuests()
    {
        questPanel.SetActive(true);

        questText.text = "";
        foreach (var q in activeQuests)
        {
            questText.text += "- " + q + "\n";
        }
    }

    public void ShowQuests(List<string> quests)
    {
        activeQuests = quests;
        questPanel.SetActive(true);

        questText.text = "";
        foreach (var q in activeQuests)
        {
            questText.text += "- " + q + "\n";
        }
    }

    public void HideQuests()
    {
        questPanel.SetActive(false);
    }

    public void AddQuest(string quest)
    {
        activeQuests.Add(quest);
        ShowQuests(activeQuests);
    }

    public void CompleteQuest(string quest)
    {
        if (activeQuests.Contains(quest))
        {
            activeQuests.Remove(quest);
            ShowQuests(activeQuests);
        }
    }
}
