using System.Collections.Generic;
using UnityEngine;

public class QuestMenuUI : MonoBehaviour
{
    [SerializeField] private Transform questListParent;
    [SerializeField] private QuestSlotUI questSlotPrefab;
    [SerializeField] private QuestInfoUI infoUI;

    private List<QuestSlotUI> slotUIs = new List<QuestSlotUI>();
    private int selectedIndex = 0;

    public void Open()
    {
        gameObject.SetActive(true);
        RefreshUI();
        if (slotUIs.Count > 0)
            ShowInfo(selectedIndex);
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

        var quests = QuestManager.Instance.GetActiveQuests();
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
        if (slotUIs.Count == 0) return;

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
            GameController.Instance.SetState(GameState.Overworld);
        }
    }

    private void ShowInfo(int index)
    {
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].SetHighlight(i == index);

        var quests = QuestManager.Instance.GetActiveQuests();
        infoUI.ShowInfo(quests[index]);
    }
}