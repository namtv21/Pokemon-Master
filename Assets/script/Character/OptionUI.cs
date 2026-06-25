using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OptionUI : MonoBehaviour
{
    public static OptionUI Instance { get; private set; }

    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private TextMeshProUGUI[] optionTexts;

    private int currentSelection;
    private int optionCount;
    private NPC currentNPC;
    private float inputLockedUntil;
    private List<(string label, System.Action action)> availableOptions;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        optionsPanel?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowOptions(NPC npc)
    {
        if (npc == null)
            return;

        currentNPC = npc;
        availableOptions = new List<(string, System.Action)>();

        if (npc.HasBattle())
        {
            availableOptions.Add(("Team", OnSelectTeam));
            availableOptions.Add(("Fight", OnSelectFight));
        }

        if (npc.HasQuest())
            availableOptions.Add(("Quest", OnSelectQuest));

        if (npc.HasHealer())
            availableOptions.Add(("Heal", OnSelectHeal));

        if (npc.HasShop())
            availableOptions.Add(("Shop", OnSelectShop));

        if (npc.HasStorage())
            availableOptions.Add(("Storage", OnSelectStorage));

        if (availableOptions.Count == 0)
        {
            HideOptions();
            GameController.Instance?.SetState(GameState.Overworld);
            return;
        }

        availableOptions.Add(("Leave", OnSelectLeave));
        optionCount = availableOptions.Count;

        if (optionTexts == null || optionTexts.Length == 0)
            optionTexts = optionsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < optionTexts.Length; i++)
        {
            bool active = i < optionCount;
            optionTexts[i].gameObject.SetActive(active);

            if (active && i < availableOptions.Count)
                optionTexts[i].text = availableOptions[i].label;
        }

        optionsPanel.SetActive(true);
        currentSelection = 0;
        inputLockedUntil = Time.unscaledTime + 0.12f;
        UpdateSelection();
        GameController.Instance?.SetState(GameState.NPCInteraction);
    }

    public void HideOptions()
    {
        optionsPanel.SetActive(false);
        availableOptions?.Clear();
    }

    public void HandleUpdate()
    {
        if (optionsPanel == null || !optionsPanel.activeInHierarchy)
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
            ExecuteSelection();

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
        {
            HideOptions();
            GameController.Instance?.SetState(GameState.Overworld);
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

    private void ExecuteSelection()
    {
        if (currentSelection >= 0 && currentSelection < availableOptions.Count)
            availableOptions[currentSelection].action?.Invoke();
    }

    private void OnSelectTeam()
    {
        HideOptions();
        if (currentNPC == null)
            return;

        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.OnDialogFinished += EndNpcInteraction;
            DialogManager.Instance.ShowDialog(currentNPC.ShowPokemon());
        }
        else
        {
            EndNpcInteraction();
        }
    }

    private void OnSelectFight()
    {
        HideOptions();
        if (currentNPC.HasBattle())
            currentNPC.StartBattle();
        else
            Debug.LogWarning($"{currentNPC.npcName} has no battle function.");
    }

    private void OnSelectQuest()
    {
        HideOptions();
        if (currentNPC.HasQuest())
            currentNPC.GiveQuest();
        else
            Debug.LogWarning($"{currentNPC.npcName} has no quest.");
    }

    private void OnSelectHeal()
    {
        HideOptions();
        if (currentNPC.HasHealer())
            currentNPC.HealPlayerPokemon();
        else
            Debug.LogWarning($"{currentNPC.npcName} has no heal function.");
    }

    private void OnSelectShop()
    {
        HideOptions();
        if (currentNPC.HasShop())
            currentNPC.OpenShop();
        else
            Debug.LogWarning($"{currentNPC.npcName} has no shop.");
    }

    private void OnSelectStorage()
    {
        HideOptions();

        if (currentNPC != null && currentNPC.HasStorage())
            currentNPC.OpenStorageSendMenu();
        else
            Debug.LogWarning("This NPC has no storage function.");
    }

    private void OnSelectLeave()
    {
        HideOptions();
        currentNPC = null;
        GameController.Instance?.SetState(GameState.Overworld);
    }

    private void EndNpcInteraction()
    {
        if (DialogManager.Instance != null)
            DialogManager.Instance.OnDialogFinished -= EndNpcInteraction;

        currentNPC = null;
        HideOptions();
        GameController.Instance?.SetState(GameState.Overworld);
    }
}
