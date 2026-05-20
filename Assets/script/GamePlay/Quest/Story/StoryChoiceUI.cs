using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class StoryChoiceUI : MonoBehaviour
{
    public static StoryChoiceUI Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI[] optionTexts;

    private readonly List<(string label, Action action)> actions = new();
    private int currentSelection;
    private int optionCount;
    private float inputLockedUntil;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowChoices(string prompt, IReadOnlyList<MainStoryChoiceOption> options, Action<int> onSelected, Action onCancel = null)
    {
        if (options == null || options.Count == 0)
            return;

        // Set Dialog state to block player input
        if (GameController.Instance != null)
            GameController.Instance.SetState(GameState.Dialog);

        actions.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            if (option == null)
                continue;

            int selectedIndex = i;
            actions.Add((string.IsNullOrWhiteSpace(option.OptionLabel) ? $"Option {i + 1}" : option.OptionLabel, () => onSelected?.Invoke(selectedIndex)));
        }

        if (actions.Count == 0)
            return;

        EnsureOptionTextsResolved(actions.Count);

        if (optionTexts == null || optionTexts.Length == 0)
        {
            Debug.LogWarning("[StoryChoiceUI] No option text slots assigned/found on panel.");
            return;
        }

        optionCount = Mathf.Min(actions.Count, optionTexts.Length);
        if (actions.Count > optionTexts.Length)
        {
            Debug.LogWarning($"[StoryChoiceUI] Options ({actions.Count}) exceed UI slots ({optionTexts.Length}). Only first {optionCount} options will be shown.");
        }

        if (promptText != null)
            promptText.text = string.IsNullOrWhiteSpace(prompt) ? "Choose an option:" : prompt;

        for (int i = 0; i < optionTexts.Length; i++)
        {
            bool active = i < optionCount;
            optionTexts[i].gameObject.SetActive(active);

            if (active)
                optionTexts[i].text = actions[i].label;
        }

        if (panel.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        currentSelection = 0;
        inputLockedUntil = Time.unscaledTime + 0.12f;
        UpdateSelection();
        _onCancel = onCancel;
    }

    public void HideChoices()
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

        actions.Clear();
        _onCancel = null;

        // Restore Overworld state when choices are hidden
        if (GameController.Instance != null && GameController.Instance.State == GameState.Dialog)
            GameController.Instance.SetState(GameState.Overworld);
    }

    private Action _onCancel;

    public bool CanRenderChoices()
    {
        if (panel == null)
            return false;

        EnsureOptionTextsResolved(0);

        if (optionTexts != null && optionTexts.Length > 0)
            return true;

        var detected = panel.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(t => t != null && t != promptText)
            .ToArray();
        return detected != null && detected.Length > 0;
    }

    private void Update()
    {
        if (panel == null || !panel.activeInHierarchy)
            return;

        // Only process input if dialog state is set and panel is actually showing
        if (GameController.Instance != null && GameController.Instance.State != GameState.Dialog)
            return;

        if (Time.unscaledTime < inputLockedUntil)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSelection = (currentSelection - 1 + optionCount) % optionCount;
            UpdateSelection();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSelection = (currentSelection + 1) % optionCount;
            UpdateSelection();
        }

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteSelection();
        }

    }

    private void UpdateSelection()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            bool active = i < optionCount;
            optionTexts[i].gameObject.SetActive(active);

            if (active)
                optionTexts[i].color = i == currentSelection ? Color.yellow : Color.white;
        }
    }

    private void EnsureOptionTextsResolved(int requiredCount)
    {
        if (panel == null)
            return;

        // Collect existing option text elements (excluding the prompt)
        var detected = panel.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(t => t != null && t != promptText)
            .OrderBy(t => t.transform.GetSiblingIndex())
            .ToList();

        if (detected.Count == 0)
        {
            optionTexts = Array.Empty<TextMeshProUGUI>();
            return;
        }

        // If we already have enough slots and optionTexts is set, use it
        if (optionTexts != null && optionTexts.Length >= requiredCount && optionTexts.Length > 0)
            return;

        // Start from detected as base list
        var list = new List<TextMeshProUGUI>(detected);

        // If caller expects more slots than we have, clone the first detected entry to create more
        if (requiredCount > list.Count)
        {
            var template = list[0];
            var parent = template.transform.parent;
            for (int i = list.Count; i < requiredCount; i++)
            {
                var go = GameObject.Instantiate(template.gameObject, parent);
                go.name = template.gameObject.name + " (clone)" + (i + 1);
                var tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.gameObject.SetActive(false);
                    list.Add(tmp);
                }
                else
                {
                    GameObject.Destroy(go);
                    break;
                }
            }
        }

        optionTexts = list.ToArray();
    }

    private void ExecuteSelection()
    {
        if (currentSelection < 0 || currentSelection >= actions.Count)
            return;

        var action = actions[currentSelection].action;
        HideChoices();
        action?.Invoke();
    }
}