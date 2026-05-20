using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestMenuUI : MonoBehaviour
{
    [SerializeField] private Transform questListParent;
    [SerializeField] private QuestSlotUI questSlotPrefab;
    [SerializeField] private QuestInfoUI infoUI;

    private readonly List<QuestSlotUI> slotUIs = new();
    private int selectedIndex = 0;

    public void Open()
    {
        gameObject.SetActive(true);
        RefreshUI();

        if (slotUIs.Count > 0)
            ShowInfo(selectedIndex);
        else
            infoUI.Hide();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        infoUI.Hide();
    }

    private void RefreshUI()
    {
        foreach (Transform child in questListParent)
            Destroy(child.gameObject);
        slotUIs.Clear();

        var quests = QuestManager.Instance.GetActiveQuests()
            .Where(q => q != null && q.Category != QuestCategory.MainStory)
            .ToList();

        for (int i = 0; i < quests.Count; i++)
        {
            var slot = Instantiate(questSlotPrefab, questListParent);
            slot.SetData(quests[i].Title);
            slotUIs.Add(slot);
        }

        selectedIndex = 0;
    }

    public void HandleUpdate()
    {
        if (slotUIs.Count == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % slotUIs.Count;
            ShowInfo(selectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + slotUIs.Count) % slotUIs.Count;
            ShowInfo(selectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Close();
        }
    }

    private void ShowInfo(int index)
    {
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].SetHighlight(i == index);

        var quests = QuestManager.Instance.GetActiveQuests()
            .Where(q => q != null && q.Category != QuestCategory.MainStory)
            .ToList();

        if (index >= 0 && index < quests.Count)
            infoUI.ShowInfo(quests[index]);
    }
}