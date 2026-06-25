// ...existing code...
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestInfoUI : MonoBehaviour
{
    [Header("Quest Info")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text objectivesText;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Image accept;
    [SerializeField] private Image decline;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    public static QuestInfoUI Instance { get; private set; }

    private Quest currentQuest;
    private Action onAccept;
    private Action onDecline;
    private bool confirmMode;
    private int selectedIndex; // 0 = Accept, 1 = Decline

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!gameObject.activeSelf || !confirmMode) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedIndex = 0;
            UpdateConfirmVisual();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedIndex = 1;
            UpdateConfirmVisual();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            Confirm();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Decline();
        }
    }

    // Xem info quest (không xác nhận)
    public void ShowInfo(Quest quest)
    {
        currentQuest = quest;
        confirmMode = false;
        onAccept = null;
        onDecline = null;

        gameObject.SetActive(true);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        Render();
    }

    // Nhận quest (có xác nhận)
    public void ShowQuestConfirm(Quest quest, Action accept, Action decline)
    {
        currentQuest = quest;
        onAccept = accept;
        onDecline = decline;
        confirmMode = true;
        selectedIndex = 0;

        gameObject.SetActive(true);
        Render();

        if (confirmPanel != null) confirmPanel.SetActive(true);
        UpdateConfirmVisual();
    }

    public void RefreshCurrent()
    {
        if (currentQuest == null) return;
        Render();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentQuest = null;
        confirmMode = false;
        onAccept = null;
        onDecline = null;

        if (titleText != null) titleText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (objectivesText != null) objectivesText.text = "";
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void Render()
    {
        if (currentQuest == null)
        {
            if (titleText != null) titleText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            if (objectivesText != null) objectivesText.text = "";
            return;
        }

        if (titleText != null) titleText.text = currentQuest.GetDisplayTitle();
        if (descriptionText != null) descriptionText.text = currentQuest.Description;

        if (objectivesText != null)
        {
            var sb = new StringBuilder();
            sb.Append(BuildObjectivesText(currentQuest));
            string rewards = BuildRewardsText(currentQuest);
            if (!string.IsNullOrEmpty(rewards))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("--- Phần thưởng ---");
                sb.Append(rewards);
            }
            objectivesText.text = sb.ToString();
        }
    }

    private string BuildObjectivesText(Quest quest)
    {
        var objectives = quest.Objectives;
        if (objectives == null || objectives.Count == 0)
            return "No objectives.";

        var qm = QuestManager.Instance;
        var state = qm != null ? qm.GetState(quest) : null;
        bool allDone = qm != null && qm.IsQuestCompleted(quest);

        var sb = new StringBuilder();
        for (int i = 0; i < objectives.Count; i++)
        {
            var obj = objectives[i];
            if (obj == null)
            {
                sb.Append("[ ] Unknown objective");
                if (i < objectives.Count - 1) sb.AppendLine();
                continue;
            }

            int required = Mathf.Max(1, obj.RequiredCount);
            bool done = allDone || (state != null && state.IsObjectiveCompleted(i));
            int current = done ? required : (state != null ? Mathf.Min(required, state.GetObjectiveCurrent(i)) : 0);

            string line = obj.Text;
            if (required > 1)
                line += $" ({current}/{required})";

            sb.Append(done ? "[v] <s>" : "[ ] ");
            sb.Append(line);
            if (done) sb.Append("</s>");

            if (i < objectives.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    private string BuildRewardsText(Quest quest)
    {
        var sb = new StringBuilder();

        if (quest.RewardMoney > 0)
            sb.AppendLine($"• {quest.RewardMoney} đồng");

        if (quest.RewardItems != null)
        {
            foreach (var r in quest.RewardItems)
            {
                if (r == null || r.item == null) continue;
                string line = r.amount > 1 ? $"• {r.item.itemName} x{r.amount}" : $"• {r.item.itemName}";
                sb.AppendLine(line);
            }
        }

        if (quest.RewardPokemons != null)
        {
            foreach (var p in quest.RewardPokemons)
            {
                if (p == null || p.pokemonBase == null) continue;
                sb.AppendLine($"• {p.pokemonBase.Name} Lv.{p.level}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private void Confirm()
    {
        var cb = selectedIndex == 0 ? onAccept : onDecline;
        Hide();
        cb?.Invoke();
    }

    private void Decline()
    {
        var cb = onDecline;
        Hide();
        cb?.Invoke();
    }

    private void UpdateConfirmVisual()
    {
        if (accept != null)
            accept.color = selectedIndex == 0 ? selectedColor : normalColor;

        if (decline != null)
            decline.color = selectedIndex == 1 ? selectedColor : normalColor;
    }
}