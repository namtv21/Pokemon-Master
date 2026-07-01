using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Phần Main Story của PokemonDexMenuUI (partial class):
// dựng runtime UI và text tóm tắt/chi tiết các bước cốt truyện.
public partial class PokemonDexMenuUI
{
    private bool IsStoryStepUnlocked(MainStoryStepEntry entry, int currentSequenceIndex, int currentStepIndex)
    {
        if (entry == null || entry.Step == null)
            return false;

        if (entry.SequenceIndex < currentSequenceIndex)
            return true;

        if (entry.SequenceIndex > currentSequenceIndex)
            return false;

        return entry.StepIndex <= currentStepIndex;
    }

    private string BuildStoryStepRowText(int stepNumber, MainStoryStepEntry entry, bool unlocked)
    {
        if (entry == null || entry.Step == null)
            return $"Step {stepNumber:00}  ???";

        if (!unlocked)
            return $"Step {stepNumber:00}  ???";

        return $"Step {stepNumber:00}  {BuildStepSummary(entry.Step)}";
    }

    private string BuildStoryStepDetailText(MainStoryStepEntry entry, bool unlocked)
    {
        if (entry == null || entry.Step == null)
            return string.Empty;

        if (!unlocked)
            return "Requirement: ???";

        return string.IsNullOrWhiteSpace(entry.Step.Description)
            ? "- No description."
            : entry.Step.Description.Trim();
    }

    private string BuildStepSummary(MainStoryStep step)
    {
        if (step == null)
            return "???";

        return FormatOrUnknown(step.StepId);
    }

    private string BuildStepActionText(MainStoryStep step)
    {
        if (step == null || step.Actions == null || step.Actions.Count == 0)
            return "- None";

        var sb = new StringBuilder();
        int shown = 0;

        for (int i = 0; i < step.Actions.Count; i++)
        {
            var action = step.Actions[i];
            if (action == null)
                continue;

            string line = BuildActionText(action);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            sb.AppendLine($"- {line}");
            shown++;

            if (shown >= 4)
                break;
        }

        if (shown == 0)
            return "- None";

        return sb.ToString().TrimEnd();
    }

    private string BuildActionText(MainStoryAction action)
    {
        if (action == null)
            return string.Empty;

        switch (action.Type)
        {
            case MainStoryActionType.ShowDialog:
                return BuildFirstDialogLine(action.SpeakerName, action.DialogText);
            case MainStoryActionType.ShowChoice:
                return string.IsNullOrWhiteSpace(action.ChoicePrompt) ? "Choice" : action.ChoicePrompt;
            case MainStoryActionType.AcceptQuest:
                return "Accept quest";
            case MainStoryActionType.SubmitEvent:
                return string.IsNullOrWhiteSpace(action.TargetId) ? "Submit event" : $"Event: {action.TargetId}";
            case MainStoryActionType.Wait:
                return action.FreezePlayerInput
                    ? $"Wait/lock {action.WaitSeconds:0.##}s"
                    : $"Wait {action.WaitSeconds:0.##}s";
            case MainStoryActionType.PlayAnimationTrigger:
                return string.IsNullOrWhiteSpace(action.AnimationTrigger) ? "Play animation" : $"Anim: {action.AnimationTrigger}";
            case MainStoryActionType.SetStoryFlag:
                return $"Flag: {action.StoryFlag}";
            case MainStoryActionType.GivePokemon:
                return string.IsNullOrWhiteSpace(action.PokemonResourceId) ? "Give PokĂ©mon" : $"Give {action.PokemonResourceId}";
            case MainStoryActionType.StartBattle:
                string battleSummary = action.BattleType == MainStoryBattleType.Wild
                    ? $"Battle: {action.WildPokemonResourceId}"
                    : $"Battle: {action.TrainerNpcId}";
                return action.ContinueOnlyIfWon ? $"{battleSummary} (win required)" : battleSummary;
            case MainStoryActionType.GiveItem:
                return action.Item == null ? "Give item" : $"Give item: {action.Item.itemName}";
            case MainStoryActionType.TakeItem:
                return action.Item == null ? "Take item" : $"Take item: {action.Item.itemName}";
            default:
                return action.Type.ToString();
        }
    }

    private string BuildFirstDialogLine(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.IsNullOrWhiteSpace(speaker) ? "Dialog" : speaker;

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string first = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (string.IsNullOrWhiteSpace(first))
            return string.IsNullOrWhiteSpace(speaker) ? "Dialog" : speaker;

        return string.IsNullOrWhiteSpace(speaker) ? first.Trim() : $"{speaker}: {first.Trim()}";
    }

    private void EnsureStoryRuntimeUI()
    {
        if (storySummaryLines != null && storySummaryLines.Count(t => t != null) >= StoryRowsPerPage && storySummaryDetailText != null)
            return;

        GameObject panel = null;
        if (tabPanels != null && tabPanels.Length > (int)DexTab.MainStory)
            panel = tabPanels[(int)DexTab.MainStory];
        else if (rootPanel != null)
            panel = rootPanel;
        else
            panel = gameObject;

        if (panel == null)
            return;

        var listRoot = panel.transform.Find("MainStoryAutoList") as RectTransform;
        if (listRoot == null)
        {
            var listGo = new GameObject("MainStoryAutoList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listRoot = listGo.GetComponent<RectTransform>();
            listRoot.SetParent(panel.transform, false);
            listRoot.anchorMin = new Vector2(0.02f, 0.08f);
            listRoot.anchorMax = new Vector2(0.48f, 0.92f);
            listRoot.offsetMin = Vector2.zero;
            listRoot.offsetMax = Vector2.zero;

            var layout = listGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
        }

        var infoPanel = panel.transform.Find("MainStoryAutoInfoPanel") as RectTransform;
        if (infoPanel == null)
        {
            var infoGo = new GameObject("MainStoryAutoInfoPanel", typeof(RectTransform), typeof(Image));
            infoPanel = infoGo.GetComponent<RectTransform>();
            infoPanel.SetParent(panel.transform, false);
            infoPanel.anchorMin = new Vector2(0.52f, 0.08f);
            infoPanel.anchorMax = new Vector2(0.98f, 0.92f);
            infoPanel.offsetMin = Vector2.zero;
            infoPanel.offsetMax = Vector2.zero;

            var bg = infoGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.2f);
        }

        if (storySummaryDetailText == null)
        {
            var detailTf = infoPanel.Find("StoryInfoText") as RectTransform;
            if (detailTf == null)
            {
                var detailGo = new GameObject("StoryInfoText", typeof(RectTransform));
                detailTf = detailGo.GetComponent<RectTransform>();
                detailTf.SetParent(infoPanel, false);
                detailTf.anchorMin = Vector2.zero;
                detailTf.anchorMax = Vector2.one;
                detailTf.offsetMin = new Vector2(10f, 10f);
                detailTf.offsetMax = new Vector2(-10f, -10f);

                var tmp = detailGo.AddComponent<TextMeshProUGUI>();
                tmp.enableWordWrapping = true;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.fontSize = 22f;
                tmp.color = normalColor;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
                storySummaryDetailText = tmp;
            }
            else
            {
                storySummaryDetailText = detailTf.GetComponent<TextMeshProUGUI>();
                if (storySummaryDetailText == null)
                    storySummaryDetailText = detailTf.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        autoStoryTexts.Clear();
        int rowCount = Mathf.Max(1, StoryRowsPerPage);
        for (int i = 0; i < rowCount; i++)
        {
            var rowName = $"Row_{i + 1:00}";
            var rowTf = listRoot.Find(rowName) as RectTransform;
            TextMeshProUGUI rowText;

            if (rowTf == null)
            {
                var rowGo = new GameObject(rowName, typeof(RectTransform));
                rowTf = rowGo.GetComponent<RectTransform>();
                rowTf.SetParent(listRoot, false);
                rowTf.sizeDelta = new Vector2(0f, 28f);

                rowText = rowGo.AddComponent<TextMeshProUGUI>();
                rowText.enableWordWrapping = false;
                rowText.overflowMode = TextOverflowModes.Ellipsis;
                rowText.alignment = TextAlignmentOptions.MidlineLeft;
                rowText.fontSize = 22f;
                rowText.color = normalColor;
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
                if (TMP_Settings.defaultFontAsset != null)
                    rowText.font = TMP_Settings.defaultFontAsset;
            }
            else
            {
                rowText = rowTf.GetComponent<TextMeshProUGUI>();
                if (rowText == null)
                    rowText = rowTf.gameObject.AddComponent<TextMeshProUGUI>();
                rowText.margin = new Vector4(28f, 0f, 0f, 0f);
            }

            autoStoryTexts.Add(rowText);
        }
    }

    private void RefreshStorySummary()
    {
        var lines = GetStoryTexts();
        var storyFlags = StoryFlags.Instance;
        int currentSequenceIndex = storyFlags != null ? storyFlags.MainStorySequenceIndex : 0;
        int currentStepIndex = storyFlags != null ? storyFlags.MainStoryStepIndex : 0;

        if (mainStorySteps.Count == 0)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].text = string.Empty;
                lines[i].color = normalColor;
            }

            if (storySummaryDetailText != null)
                storySummaryDetailText.text = "No main story data found.";
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            var text = lines[i];
            if (i >= mainStorySteps.Count)
            {
                text.text = string.Empty;
                text.color = normalColor;
                continue;
            }

            var entry = mainStorySteps[i];
            bool unlocked = IsStoryStepUnlocked(entry, currentSequenceIndex, currentStepIndex);

            text.text = BuildStoryStepRowText(i + 1, entry, unlocked);
            text.color = BuildRowColor(i == storyIndex, unlocked);
            EnsureDexIndicator(text, unlocked);
        }

        if (storySummaryDetailText == null) return;

        var selected = mainStorySteps[Mathf.Clamp(storyIndex, 0, mainStorySteps.Count - 1)];
        bool selectedUnlocked = IsStoryStepUnlocked(selected, currentSequenceIndex, currentStepIndex);
        storySummaryDetailText.text = BuildStoryStepDetailText(selected, selectedUnlocked);
    }

    private List<TMP_Text> GetStoryTexts()
    {
        if (storySummaryLines != null && storySummaryLines.Count(t => t != null) >= StoryRowsPerPage)
            return storySummaryLines.Where(t => t != null).ToList();

        if (autoStoryTexts.Count == 0)
        {
            EnsureStoryRuntimeUI();
            ResolveAutoTextCollections();
        }

        return autoStoryTexts;
    }
}
