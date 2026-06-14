using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class BattleDialogBox : MonoBehaviour
{
    [Header("Dialog Panel")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI dialogText;

    [Header("Item Menu Panel")]
    [SerializeField] private GameObject itemMenuPanel;

    [Header("Pokemon Menu Panel")]
    [SerializeField] private GameObject pokemonMenuPanel;

    [Header("Action Menu Panel")]
    [SerializeField] private GameObject actionMenuPanel;
    [SerializeField] private List<TextMeshProUGUI> actionTexts; // Fight, Pokémon, Item, Run
    private int currentActionIndex = 0;

    [Header("Move Menu Panel")]
    [SerializeField] private GameObject moveMenuPanel;
    [SerializeField] private MoveSelectionUI[] moveSlots;
    private int currentMoveIndex = 0;

    // ================== DIALOG ==================
    public void ShowDialog(string message)
    {
        dialogPanel.SetActive(true);
        actionMenuPanel.SetActive(false);
        moveMenuPanel.SetActive(false);
        itemMenuPanel.SetActive(false);
        pokemonMenuPanel.SetActive(false);
        dialogText.text = message;
    }

    public IEnumerator ShowDialogAndWait(string message)
    {
        dialogPanel.SetActive(true);
        dialogText.text = message;

        // Đợi 1 frame để tránh việc phím bấm chọn Pokemon bị tính luôn cho Dialog
        yield return null; 

        // Chờ người dùng bấm phím Z (hoặc phím Enter/Space tùy bạn)
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z));
        
        // Sau khi bấm xong thì ẩn Dialog đi
        dialogPanel.SetActive(false);
    }

    // ================== ACTION MENU ==================
    public void ShowActionMenu()
    {
        dialogPanel.SetActive(false);
        actionMenuPanel.SetActive(true);
        moveMenuPanel.SetActive(false);
        itemMenuPanel.SetActive(false);
        pokemonMenuPanel.SetActive(false);

        currentActionIndex = 0;
        UpdateActionHighlight();
    }

    public void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentActionIndex = (currentActionIndex % 2 == 0) ? currentActionIndex + 1 : currentActionIndex;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentActionIndex = (currentActionIndex % 2 == 1) ? currentActionIndex - 1 : currentActionIndex;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentActionIndex = (currentActionIndex < 2) ? currentActionIndex + 2 : currentActionIndex;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentActionIndex = (currentActionIndex >= 2) ? currentActionIndex - 2 : currentActionIndex;

        UpdateActionHighlight();
    }

    private void UpdateActionHighlight()
    {
        for (int i = 0; i < actionTexts.Count; i++)
        {
            actionTexts[i].color = (i == currentActionIndex) ? Color.yellow : Color.black;
        }
    }

    public int GetSelectedAction() => currentActionIndex;

    // ================== MOVE MENU ==================
    public void ShowMoveMenu(List<Move> moves)
    {
        actionMenuPanel.SetActive(false);
        moveMenuPanel.SetActive(true);

        for (int i = 0; i < moveSlots.Length; i++)
        {
            if (i < moves.Count)
            {
                moveSlots[i].gameObject.SetActive(true);
                moveSlots[i].SetMoveData(moves[i]);
            }
            else
            {
                moveSlots[i].gameObject.SetActive(false);
            }
        }

        currentMoveIndex = 0;
        UpdateMoveHighlight();
    }

    public void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            MoveSelection(Vector2.right);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            MoveSelection(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            MoveSelection(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            MoveSelection(Vector2.up);

        UpdateMoveHighlight();
    }

    private void MoveSelection(Vector2 direction)
    {
        if (moveSlots == null || moveSlots.Length == 0)
            return;

        var currentRect = moveSlots[currentMoveIndex] != null ? moveSlots[currentMoveIndex].transform as RectTransform : null;
        if (currentRect == null)
            return;

        int bestIndex = -1;
        float bestScore = float.NegativeInfinity;
        Vector2 currentPos = currentRect.anchoredPosition;
        Vector2 dir = direction.normalized;

        for (int i = 0; i < moveSlots.Length; i++)
        {
            if (i == currentMoveIndex || moveSlots[i] == null || !moveSlots[i].gameObject.activeSelf)
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
            currentMoveIndex = bestIndex;
    }

    private void UpdateMoveHighlight()
    {
        for (int i = 0; i < moveSlots.Length; i++)
        {
            moveSlots[i].SetHighlight(i == currentMoveIndex);
        }
    }

    public int GetSelectedMove() => currentMoveIndex;

    public void HideAll()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }


}