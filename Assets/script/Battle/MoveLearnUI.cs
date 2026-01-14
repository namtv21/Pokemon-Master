using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class MoveLearnUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private List<MoveSelectionUI> moveSlots; // 5 slot: 4 hiện tại + 1 mới
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private int currentIndex = 0;
    private Action<int> onMoveSelected;

    public void Show(Pokemon pokemon, MoveBase newMoveBase, Action<int> onSelected)
    {
        gameObject.SetActive(true);
        titleText.text = "Which move?";

        onMoveSelected = onSelected;
        int moveIndex = 0;
        for (int i = 0; i < 5; i++)
        {
            if (i == 2) // slot giữa là chiêu mới
            {
                moveSlots[i].SetMoveData(newMoveBase != null ? new Move(newMoveBase) : null);
            }
            else
            {
                moveSlots[i].SetMoveData(pokemon.Moves[moveIndex]);
                moveIndex++;
            }
        }
        currentIndex = 0;
        HighlightCurrent();
    }

    private void HighlightCurrent()
    {
        for (int i = 0; i < moveSlots.Count; i++)
        {

            moveSlots[i].SetHighlight(i == currentIndex);
        }
    }

    public void HandleUpdate()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + moveSlots.Count) % moveSlots.Count;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % moveSlots.Count;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            onMoveSelected?.Invoke(currentIndex);
            gameObject.SetActive(false);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            // Hủy học chiêu
            onMoveSelected?.Invoke(-1);
            gameObject.SetActive(false);
        }
    }
}
