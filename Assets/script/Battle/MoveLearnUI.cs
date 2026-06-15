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
        titleText.text = "Delete which move?";

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
            MoveSelection(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            MoveSelection(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            MoveSelection(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            MoveSelection(Vector2.right);
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

    private void MoveSelection(Vector2 direction)
    {
        if (moveSlots == null || moveSlots.Count == 0)
            return;

        var currentRect = moveSlots[currentIndex] != null ? moveSlots[currentIndex].transform as RectTransform : null;
        if (currentRect == null)
            return;

        int bestIndex = -1;
        float bestScore = float.NegativeInfinity;

        Vector2 currentPos = currentRect.anchoredPosition;
        Vector2 dir = direction.normalized;

        for (int i = 0; i < moveSlots.Count; i++)
        {
            if (i == currentIndex || moveSlots[i] == null || !moveSlots[i].gameObject.activeSelf)
                continue;

            var candidateRect = moveSlots[i].transform as RectTransform;
            if (candidateRect == null)
                continue;

            Vector2 delta = candidateRect.anchoredPosition - currentPos;
            if (delta.sqrMagnitude < 0.001f)
                continue;

            float alignment = Vector2.Dot(delta.normalized, dir);
            if (alignment <= 0.1f)
                continue;

            float score = alignment * 1000f - delta.magnitude;
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            currentIndex = bestIndex;
            HighlightCurrent();
        }
    }
}
