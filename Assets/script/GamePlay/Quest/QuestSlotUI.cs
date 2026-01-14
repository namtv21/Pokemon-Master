using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Color highlightColor = Color.yellow;

    public void SetData(string title)
    {
        titleText.text = title;
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        titleText.color = active ? highlightColor : Color.white;
    }
}