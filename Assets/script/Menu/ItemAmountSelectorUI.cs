using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemAmountSelectorUI : MonoBehaviour
{
    public static ItemAmountSelectorUI Instance { get; private set; }

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

        var canvas = FindObjectOfType<Canvas>(true);
        GameObject root = new GameObject("ItemAmountSelectorUI");
        if (canvas != null)
            root.transform.SetParent(canvas.transform, false);

        var selector = root.AddComponent<ItemAmountSelectorUI>();
        Instance = selector;
        return selector;
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI[] optionTexts;

    private readonly List<(int amount, string label)> options = new();
    private Action<int> onSelected;
    private Action onCancel;
    private int currentSelection;
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

    public void Show(int maxAmount, Action<int> onSelectedCallback, Action onCancelCallback = null)
    {
        BuildIfNeeded();

        if (panel == null || optionTexts == null || optionTexts.Length == 0)
            return;

        maxAmount = Mathf.Max(1, maxAmount);
        options.Clear();

        var presets = new[] { 10, 50, 100, 500, 1000, maxAmount };
        foreach (var preset in presets)
        {
            int amount = Mathf.Clamp(preset, 1, maxAmount);
            string label = amount >= maxAmount ? $"All ({maxAmount})" : amount.ToString();
            if (!options.Any(o => o.amount == amount))
                options.Add((amount, label));
        }

        onSelected = onSelectedCallback;
        onCancel = onCancelCallback;
        currentSelection = 0;
        inputLockedUntil = Time.unscaledTime + 0.12f;

        if (promptText != null)
            promptText.text = $"Choose EXP amount (max {maxAmount})";

        for (int i = 0; i < optionTexts.Length; i++)
        {
            bool active = i < options.Count;
            optionTexts[i].gameObject.SetActive(active);
            if (active)
                optionTexts[i].text = options[i].label;
        }

        if (panel.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        UpdateSelection();

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

        options.Clear();
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

        if (options.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSelection = (currentSelection - 1 + options.Count) % options.Count;
            UpdateSelection();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSelection = (currentSelection + 1) % options.Count;
            UpdateSelection();
        }
        else if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteSelection();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            var cancel = onCancel;
            Hide();
            cancel?.Invoke();
        }
    }

    private void ExecuteSelection()
    {
        if (currentSelection < 0 || currentSelection >= options.Count)
            return;

        int amount = options[currentSelection].amount;
        var callback = onSelected;
        Hide();
        callback?.Invoke(amount);
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            bool active = i < options.Count;
            optionTexts[i].gameObject.SetActive(active);
            if (active)
                optionTexts[i].color = i == currentSelection ? Color.yellow : Color.white;
        }
    }

    private void BuildIfNeeded()
    {
        if (built)
            return;

        built = true;

        if (panel == null)
        {
            var root = transform.Find("Panel");
            if (root != null)
                panel = root.gameObject;
        }

        if (panel != null)
        {
            promptText ??= panel.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t != null && t.gameObject.name == "PromptText");

            optionTexts ??= panel.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(t => t != null && t != promptText)
                .OrderBy(t => t.transform.GetSiblingIndex())
                .ToArray();

            return;
        }

        var panelRoot = new GameObject("Panel");
        panelRoot.transform.SetParent(transform, false);
        panel = panelRoot;

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 360f);

        var image = panelRoot.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.85f);

        var canvasGroup = panelRoot.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        promptText = CreateText(panelRoot.transform, "PromptText", new Vector2(0f, 130f), 32, TextAlignmentOptions.Center);
        promptText.text = "Choose EXP amount";

        optionTexts = new TextMeshProUGUI[6];
        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i] = CreateText(panelRoot.transform, $"Option{i + 1}", new Vector2(0f, 70f - i * 40f), 28, TextAlignmentOptions.Center);
            optionTexts[i].gameObject.SetActive(false);
        }
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchoredPos, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(460f, 36f);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
