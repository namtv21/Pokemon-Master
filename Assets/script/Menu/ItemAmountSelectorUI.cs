using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemAmountSelectorUI : MonoBehaviour
{
    private const int DigitCount = 5;
    private const int MaxRepresentableAmount = 99999;
    private const string DefaultHint = "[Left/Right] Select digit   [Up/Down] Change   [Z] Confirm   [X] Cancel";

    public static ItemAmountSelectorUI Instance { get; private set; }
    public int CurrentAmount => ComposeAmount();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    public static ItemAmountSelectorUI GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        var existing = FindObjectOfType<ItemAmountSelectorUI>(true);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var root = new GameObject(
            "ItemAmountSelectorUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        if (MenuController.Instance != null)
            root.transform.SetParent(MenuController.Instance.transform, false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return root.AddComponent<ItemAmountSelectorUI>();
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI[] digitTexts;

    private readonly int[] digits = new int[DigitCount];
    private Action<int> onSelected;
    private Action onCancel;
    private int currentDigit;
    private int maxAmount;
    private float inputLockedUntil;
    private bool built;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildIfNeeded();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(int availableAmount, Action<int> onSelectedCallback, Action onCancelCallback = null)
    {
        BuildIfNeeded();
        if (panel == null || digitTexts == null || digitTexts.Length != DigitCount)
            return;

        maxAmount = Mathf.Clamp(availableAmount, 1, MaxRepresentableAmount);
        Array.Clear(digits, 0, digits.Length);
        currentDigit = DigitCount - 1;
        onSelected = onSelectedCallback;
        onCancel = onCancelCallback;
        inputLockedUntil = Time.unscaledTime + 0.12f;

        if (promptText != null)
        {
            promptText.text = availableAmount > MaxRepresentableAmount
                ? $"Choose EXP amount (available {availableAmount}, max per use {MaxRepresentableAmount})"
                : $"Choose EXP amount (available {availableAmount})";
        }

        SetHint(DefaultHint, Color.white);

        if (panel.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        RefreshDigits();
        UiFx.PopIn(panel);
    }

    public void Hide()
    {
        if (panel != null)
        {
            if (panel.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            panel.SetActive(false);
        }

        onSelected = null;
        onCancel = null;
    }

    private void Update()
    {
        if (panel == null || !panel.activeInHierarchy)
            return;

        if (GameController.Instance != null)
        {
            var state = GameController.Instance.State;
            if (state != GameState.Menu && state != GameState.Dialog)
                return;
        }

        if (Time.unscaledTime < inputLockedUntil)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentDigit = (currentDigit - 1 + DigitCount) % DigitCount;
            RefreshDigits();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentDigit = (currentDigit + 1) % DigitCount;
            RefreshDigits();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            digits[currentDigit] = (digits[currentDigit] + 1) % 10;
            RefreshDigits();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            digits[currentDigit] = (digits[currentDigit] + 9) % 10;
            RefreshDigits();
        }
        else if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteSelection();
        }
        else if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
        {
            var cancel = onCancel;
            Hide();
            cancel?.Invoke();
        }
    }

    private void ExecuteSelection()
    {
        int amount = ComposeAmount();
        if (amount <= 0)
        {
            SetHint("Amount must be at least 0 0 0 0 1.", Color.yellow);
            return;
        }

        if (amount > maxAmount)
        {
            SetHint($"Only {maxAmount} EXP is available.", new Color(1f, 0.45f, 0.35f));
            return;
        }

        var callback = onSelected;
        Hide();
        callback?.Invoke(amount);
    }

    private int ComposeAmount()
    {
        int amount = 0;
        for (int i = 0; i < DigitCount; i++)
            amount = amount * 10 + digits[i];
        return amount;
    }

    private void RefreshDigits()
    {
        int amount = ComposeAmount();
        bool exceedsAvailable = amount > maxAmount;

        for (int i = 0; i < DigitCount; i++)
        {
            var digitText = digitTexts[i];
            if (digitText == null)
                continue;

            digitText.text = digits[i].ToString();
            digitText.color = exceedsAvailable
                ? (i == currentDigit ? new Color(1f, 0.65f, 0.15f) : new Color(1f, 0.4f, 0.35f))
                : (i == currentDigit ? Color.yellow : Color.white);
            digitText.transform.localScale = i == currentDigit ? Vector3.one * 1.18f : Vector3.one;
        }

        if (exceedsAvailable)
            SetHint($"Selected {amount}; only {maxAmount} EXP is available.", new Color(1f, 0.45f, 0.35f));
        else
            SetHint(DefaultHint, Color.white);
    }

    private void SetHint(string message, Color color)
    {
        if (hintText == null)
            return;

        hintText.text = message;
        hintText.color = color;
    }

    private void BuildIfNeeded()
    {
        if (built)
            return;

        built = true;

        if (panel == null)
        {
            var existingPanel = transform.Find("Panel");
            if (existingPanel != null)
                panel = existingPanel.gameObject;
        }

        if (panel == null)
            CreatePanel();

        EnsureCanvasGroup();

        promptText ??= FindText("PromptText");
        if (promptText == null)
            promptText = CreateText(panel.transform, "PromptText", new Vector2(0f, 105f), new Vector2(680f, 48f), 30f);

        hintText ??= FindText("HintText");
        if (hintText == null)
            hintText = CreateText(panel.transform, "HintText", new Vector2(0f, -100f), new Vector2(700f, 44f), 21f);

        digitTexts = new TextMeshProUGUI[DigitCount];
        for (int i = 0; i < DigitCount; i++)
        {
            digitTexts[i] = FindText($"Digit{i}");
            if (digitTexts[i] == null)
            {
                float x = (i - (DigitCount - 1) * 0.5f) * 92f;
                digitTexts[i] = CreateText(panel.transform, $"Digit{i}", new Vector2(x, 5f), new Vector2(72f, 86f), 58f);
            }
        }
    }

    private void CreatePanel()
    {
        var panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelRoot.transform.SetParent(transform, false);
        panel = panelRoot;

        var rect = panelRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(780f, 320f);

        var image = panelRoot.GetComponent<Image>();
        image.color = new Color(0.035f, 0.055f, 0.08f, 0.96f);
    }

    private void EnsureCanvasGroup()
    {
        if (panel != null && panel.GetComponent<CanvasGroup>() == null)
            panel.AddComponent<CanvasGroup>();
    }

    private TextMeshProUGUI FindText(string objectName)
    {
        if (panel == null)
            return null;

        var child = panel.transform.Find(objectName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
