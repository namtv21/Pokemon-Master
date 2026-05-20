using UnityEngine;
using System.IO;

public class SaveLoadMenuUI : MonoBehaviour
{
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private SaveLoadSlot[] slots; // 4 slot
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private enum MenuStep
    {
        SelectSlot,
        SelectMode
    }

    private int currentIndex = 0;
    private bool isInGame = false;
    private bool isSaveMode = true;
    private MenuStep currentStep = MenuStep.SelectSlot;

    public void Open(bool inGame = false)
    {
        isInGame = inGame;
        isSaveMode = true;
        currentStep = MenuStep.SelectSlot;
        gameObject.SetActive(true);
        currentIndex = 0;
        RefreshSlots();
        HighlightCurrent();
    }


    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void HandleUpdate(System.Action onClose)
    {
        if (!gameObject.activeSelf) return;

        if (currentStep == MenuStep.SelectSlot)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentIndex = (currentIndex - 1 + slots.Length) % slots.Length;
                RefreshSlots();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentIndex = (currentIndex + 1) % slots.Length;
                RefreshSlots();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                currentStep = MenuStep.SelectMode;
                RefreshSlots();
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                Close();
                onClose?.Invoke();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            isSaveMode = !isSaveMode;
            RefreshSlots();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            string slotName = "SaveFile" + (currentIndex + 1);
            if (isSaveMode)
                saveLoadSystem.Save(slotName);
            else
            {
                if (isInGame)
                    saveLoadSystem.Load(slotName);
                else
                    saveLoadSystem.LoadFromMenu(slotName);
            }
            Close();
            onClose?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            currentStep = MenuStep.SelectSlot;
            RefreshSlots();
        }
    }

    private void HighlightCurrent()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetHighlighted(i == currentIndex, highlightColor, normalColor);
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            string slotName = "SaveFile" + (i + 1);
            SaveData data = null;

            string path = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), slotName + ".json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<SaveData>(json);
            }

            slots[i].SetData(slotName, data, path, i == currentIndex, currentStep == MenuStep.SelectMode, isSaveMode);
        }

        HighlightCurrent();
    }
}
