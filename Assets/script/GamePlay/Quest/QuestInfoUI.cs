using TMPro;
using UnityEngine;

public class QuestInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void ShowInfo(Quest quest)
    {
        gameObject.SetActive(true);
        titleText.text = quest.Title;
        descriptionText.text = quest.Description;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        titleText.text = "";
        descriptionText.text = "";
    }
}