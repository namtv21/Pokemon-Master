using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

[System.Serializable]
public class DialogManager : MonoBehaviour
{
    [SerializeField] private GameObject dialogBox;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] int lettersPerSecond = 30;

    public event Action OnDialogStarted;
    public event Action OnDialogFinished;

    public static DialogManager Instance { get; private set; }

    private List<string> sentences;
    private int currentLineIndex;
    private bool isTyping;
    public bool isShowing { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowDialog(Dialog dialog)
    {
        sentences = dialog.Sentences;
        StartDialog();
    }

    public void ShowDialog(string line)
    {
        sentences = new List<string> { line };
        StartDialog();
    }
    public void ShowDialog(string text, System.Action onFinished = null)
    {
        StartCoroutine(ShowDialogRoutine(text, onFinished));
    }

    private IEnumerator ShowDialogRoutine(string text, System.Action onFinished)
    {
        dialogText.text = text;
        dialogBox.SetActive(true);
        isShowing = true;

        // chờ người chơi nhấn phím để đóng dialog
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return));

        dialogBox.SetActive(false);
        isShowing = false;

        // gọi callback nếu có
        onFinished?.Invoke();
    }

    public IEnumerator ShowDialogCoroutine(string text)
    {
        dialogText.text = text;
        dialogBox.SetActive(true);
        isShowing = true;
        yield return null;
        // chờ người chơi nhấn phím để đóng
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return));

        dialogBox.SetActive(false);
        isShowing = false;
    }

    private void StartDialog()
    {
        currentLineIndex = 0;
        dialogBox.SetActive(true);
        OnDialogStarted?.Invoke();

        StopAllCoroutines();
        StartCoroutine(TypeLine(sentences[currentLineIndex]));
        GameController.Instance.SetState(GameState.Dialog);
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (var letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        isTyping = false;
    }

    public void HandleUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogText.text = sentences[currentLineIndex];
                isTyping = false;
            }
            else
            {
                currentLineIndex++;
                if (currentLineIndex < sentences.Count)
                {
                    StopAllCoroutines();
                    StartCoroutine(TypeLine(sentences[currentLineIndex]));
                }
                else
                {
                    CloseDialog();
                }
            }
        }
    }

    private void CloseDialog()
    {
        dialogBox.SetActive(false);
        var finished = OnDialogFinished;
        OnDialogFinished = null; // reset event để tránh gọi lặp
        finished?.Invoke();
    }

    public void ClearOnDialogFinished()
    {
        OnDialogFinished = null;
    }

}