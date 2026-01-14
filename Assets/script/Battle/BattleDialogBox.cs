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
            currentMoveIndex = Mathf.Min(currentMoveIndex + 1, moveSlots.Length - 1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentMoveIndex = Mathf.Max(currentMoveIndex - 1, 0);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMoveIndex = Mathf.Min(currentMoveIndex + 2, moveSlots.Length - 1);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMoveIndex = Mathf.Max(currentMoveIndex - 2, 0);

        UpdateMoveHighlight();
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