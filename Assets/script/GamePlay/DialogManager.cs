using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [SerializeField] private GameObject dialogBox;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private int lettersPerSecond = 30;
    [SerializeField] private float inputDebounce = 0.08f;

    public static DialogManager Instance { get; private set; }

    public event Action OnDialogStarted;
    public event Action OnDialogFinished;

    public bool IsShowing { get; private set; }
    public bool IsInputBlocked => IsShowing;

    private readonly List<string> sentences = new();
    private int currentLineIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;
    private Action oneShotOnFinished;

    private float inputLockedUntil;
    private bool hasCachedState;
    private GameState cachedStateBeforeDialog;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialogBox != null)
            dialogBox.SetActive(false);

        if (dialogText != null)
            dialogText.text = string.Empty;

        if (speakerNameText != null)
            speakerNameText.gameObject.SetActive(false);

        if (portraitImage != null)
            portraitImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Chỉ tự xử lý input khi không có GameController điều phối,
        // hoặc khi GameController không ở Dialog state (fallback mode).
        if (IsShowing && (GameController.Instance == null || GameController.Instance.State != GameState.Dialog))
            HandleUpdate();
    }

    public bool IsDebouncingInput => Time.unscaledTime < inputLockedUntil;

    // ===== Public API =====

    public void ShowDialog(Dialog dialog, GameState? restoreState = null)
    {
        BeginDialog(
            dialog != null ? dialog.Sentences : null,
            speakerName: null,
            portrait: null,
            onFinished: null,
            restoreState: restoreState
        );
    }

    public void ShowDialog(string line, GameState? restoreState = null)
    {
        BeginDialog(
            new List<string> { line },
            speakerName: null,
            portrait: null,
            onFinished: null,
            restoreState: restoreState
        );
    }

    public void ShowDialog(string speakerName, Sprite portrait, Dialog dialog, GameState? restoreState = null)
    {
        BeginDialog(
            dialog != null ? dialog.Sentences : null,
            speakerName,
            portrait,
            onFinished: null,
            restoreState: restoreState
        );
    }

    public void ShowDialog(string speakerName, Sprite portrait, string line, GameState? restoreState = null)
    {
        BeginDialog(
            new List<string> { line },
            speakerName,
            portrait,
            onFinished: null,
            restoreState: restoreState
        );
    }

    // giữ tương thích code cũ nếu có callback
    public void ShowDialog(string line, Action onFinished, GameState? restoreState = null)
    {
        BeginDialog(
            new List<string> { line },
            speakerName: null,
            portrait: null,
            onFinished: onFinished,
            restoreState: restoreState
        );
    }

    public void HandleUpdate()
    {
        if (!IsShowing) return;
        
        // Chặn input ngay sau khi mở dialog, tránh ăn phím mở dialog
        // Nhưng cho phép sau ~0.08s
        if (Time.unscaledTime < inputLockedUntil)
            return;

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                CompleteCurrentLineInstantly();
                return;
            }

            currentLineIndex++;
            if (currentLineIndex < sentences.Count)
            {
                StartTypingCurrentLine();
                inputLockedUntil = Time.unscaledTime + inputDebounce; // debounce giữa lines
            }
            else
            {
                CloseDialog();
            }
        }
    }

    public void ClearOnDialogFinished()
    {
        OnDialogFinished = null;
    }

    // ===== Internal =====

    private void BeginDialog(List<string> lines, string speakerName, Sprite portrait, Action onFinished, GameState? restoreState)
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("Dialog is empty.");
            return;
        }

        sentences.Clear();
        foreach (var s in lines)
        {
            if (!string.IsNullOrWhiteSpace(s))
                sentences.Add(s);
        }

        if (sentences.Count == 0)
        {
            Debug.LogWarning("Dialog has no valid lines.");
            return;
        }

        ApplySpeaker(speakerName, portrait);

        oneShotOnFinished = onFinished;
        currentLineIndex = 0;
        IsShowing = true;

        if (dialogBox != null) dialogBox.SetActive(true);

        var gc = GameController.Instance;
        if (gc != null)
        {
            if (!hasCachedState)
            {
                cachedStateBeforeDialog = restoreState ?? gc.State;
                hasCachedState = true;
            }

            gc.SetState(GameState.Dialog); // khóa gameplay input
        }

        inputLockedUntil = Time.unscaledTime + inputDebounce; // tránh ăn luôn phím mở dialog
        OnDialogStarted?.Invoke();

        StartTypingCurrentLine();
    }

    private void StartTypingCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(sentences[currentLineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        if (dialogText != null) dialogText.text = string.Empty;

        float delay = lettersPerSecond > 0 ? 1f / lettersPerSecond : 0f;

        foreach (char c in line)
        {
            if (dialogText != null) dialogText.text += c;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            else yield return null;
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void CompleteCurrentLineInstantly()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogText != null && currentLineIndex >= 0 && currentLineIndex < sentences.Count)
            dialogText.text = sentences[currentLineIndex];

        isTyping = false;
    }

    private void CloseDialog()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        IsShowing = false;

        if (dialogBox != null) dialogBox.SetActive(false);

        var cb = oneShotOnFinished;
        oneShotOnFinished = null;
        cb?.Invoke();

        OnDialogFinished?.Invoke();

        // trả state về trước dialog để bấm Z tương tác lại bình thường
        var gc = GameController.Instance;
        if (gc != null)
        {
            if (gc.State == GameState.Dialog)
            {
                if (hasCachedState)
                    gc.SetState(cachedStateBeforeDialog);
                else
                    gc.SetState(GameState.Overworld);
            }
        }

        hasCachedState = false;
        inputLockedUntil = Time.unscaledTime + inputDebounce; // chống double-press
    }

    private void ApplySpeaker(string speakerName, Sprite portrait)
    {
        portrait = DialogSpeakerPortraitResolver.Resolve(speakerName, portrait);

        if (speakerNameText != null)
        {
            bool hasName = !string.IsNullOrWhiteSpace(speakerName);
            speakerNameText.gameObject.SetActive(hasName);
            speakerNameText.text = hasName ? speakerName : string.Empty;
        }

        if (portraitImage != null)
        {
            bool hasPortrait = portrait != null;
            portraitImage.gameObject.SetActive(hasPortrait);
            portraitImage.sprite = hasPortrait ? portrait : null;
        }
    }
}