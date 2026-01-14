using UnityEngine;
using TMPro;
public class OptionUI : MonoBehaviour
{

    public static OptionUI Instance { get; private set; } 
    private void Awake() {
        optionsPanel.SetActive(false);
        //Debug.Log("OptionUI Awake on " + gameObject.name);
        if (Instance == null) Instance = this; 
        else Destroy(gameObject); 
        //DontDestroyOnLoad(gameObject);
    }

    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private TMPro.TextMeshProUGUI[] optionTexts;
    private int currentSelection;
    private int optionCount;
    private NPC currentNPC;

    public void ShowOptions(NPC npc)
    {
        currentNPC = npc; 
        if (optionTexts == null || optionTexts.Length == 0) 
            optionTexts = optionsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

        // mặc định 3 option
        optionCount = 3;

        // nếu NPC đặc biệt thì thêm option
        if (npc.npcType == NPCType.Healer || npc.npcType == NPCType.Shopkeeper || npc.npcType == NPCType.StorageKeeper)
            optionCount = 4;

        optionsPanel.SetActive(true); 
        currentSelection = 0; 
        UpdateSelection(); 
        GameController.Instance.SetState(GameState.NPCInteraction);

        // cập nhật text hiển thị
        optionTexts[0].text = "Team";
        optionTexts[1].text = "Fight";
        optionTexts[2].text = "Leave";

        if (optionCount == 4)
        {
            switch (npc.npcType)
            {
                case NPCType.Healer:
                    optionTexts[3].text = "Heal Pokémon";
                    break;
                case NPCType.Shopkeeper:
                    optionTexts[3].text = "Shop";
                    break;
                case NPCType.StorageKeeper:
                    optionTexts[3].text = "Storage";
                    break;
            }
        }
    }

    public void HideOptions()
    {
        optionsPanel.SetActive(false);
    }

    public void HandleUpdate()
    {
        if (optionsPanel == null || !optionsPanel.activeInHierarchy) return;

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

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
        {
            HideOptions();
            GameController.Instance.SetState(GameState.Overworld);
        }
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            bool active = i < optionCount;
            optionTexts[i].gameObject.SetActive(active);
            if (active) optionTexts[i].color = i == currentSelection ? Color.yellow : Color.black;
        }
    }

    private void ExecuteSelection()
    {
        switch (currentSelection)
        {
            case 0: // Team → hiển thị dialog rồi quay lại Option
                HideOptions(); // ẩn menu trong lúc hiện dialog
                DialogManager.Instance.OnDialogFinished += ReturnToOptions;
                DialogManager.Instance.ShowDialog(currentNPC.ShowPokemon());
                break;

            case 1: // Fight → vào battle
                HideOptions();
                GameController.Instance.StartTrainerBattle(currentNPC);
                break;

            case 2: // Leave → dialog rồi về Overworld
                HideOptions();
                currentNPC = null;
                DialogManager.Instance.OnDialogFinished += () =>
                {
                    GameController.Instance.SetState(GameState.Overworld);
                };
                DialogManager.Instance.ShowDialog("See you next time!");
                break;

            case 3: // Special NPC action
                HideOptions();
                switch (currentNPC.npcType)
                {
                    case NPCType.Healer:
                        GameController.Instance.HealAllPlayerPokemon();
                        break;
                    case NPCType.Shopkeeper:
                        GameController.Instance.OpenShop();
                        break;
                    case NPCType.StorageKeeper:
                        GameController.Instance.OpenStorageParty(currentNPC);
                    break;
                }
                break;


        }
    }

    private void ReturnToOptions()
    {
        DialogManager.Instance.OnDialogFinished -= ReturnToOptions;
        ShowOptions(currentNPC); // quay lại Option sau khi xem Team
    }
}
