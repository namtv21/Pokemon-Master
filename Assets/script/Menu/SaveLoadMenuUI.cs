using UnityEngine;
using System.IO;
using System.Linq;

public class SaveLoadMenuUI : MonoBehaviour
{
    private const string AutoSaveSlotName = "AutoSave";
    private const string RuntimeSaveLoadSystemName = "RuntimeSaveLoadSystem";

    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private SaveLoadSlot[] slots; // 5 slots: AutoSave + SaveFile1~4
    [SerializeField] private string[] slotNames = new[] { "AutoSave", "SaveFile1", "SaveFile2", "SaveFile3", "SaveFile4" };
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
        isSaveMode = inGame;
        currentStep = MenuStep.SelectSlot;
        gameObject.SetActive(true);
        currentIndex = slotNames != null && slotNames.Length > 1 ? 1 : 0;
        ResolveSaveLoadSystem();
        EnsureSlotsInitialized();
        RefreshSlots();
        HighlightCurrent();
    }


    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void HandleUpdate(System.Action onCancel, System.Action onSaveCompleted, System.Action onLoadCompleted)
    {
        if (!gameObject.activeSelf) return;

        if (currentStep == MenuStep.SelectSlot)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentIndex = (currentIndex - 1 + GetSlotCount()) % GetSlotCount();
                RefreshSlots();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentIndex = (currentIndex + 1) % GetSlotCount();
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
                onCancel?.Invoke();
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
            string slotName = GetSlotName(currentIndex);

            if (isSaveMode && string.Equals(slotName, AutoSaveSlotName))
            {
                ToastNotificationManager.Instance?.Show("Auto Save is reserved for story autosaves.", Color.yellow);
                return;
            }

            if (isSaveMode)
            {
                if (!ResolveSaveLoadSystem())
                {
                    ToastNotificationManager.Instance?.Show("Save system is unavailable.", Color.yellow);
                    return;
                }

                saveLoadSystem.Save(slotName);
                Close();
                onSaveCompleted?.Invoke();
            }
            else
            {
                if (!ResolveSaveLoadSystem())
                {
                    ToastNotificationManager.Instance?.Show("Load system is unavailable.", Color.yellow);
                    return;
                }

                if (isInGame)
                    saveLoadSystem.Load(slotName);
                else
                    saveLoadSystem.LoadFromMenu(slotName);
                Close();
                onLoadCompleted?.Invoke();
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            currentStep = MenuStep.SelectSlot;
            RefreshSlots();
        }
    }

    private void HighlightCurrent()
    {
        int count = GetSlotCount();
        for (int i = 0; i < count; i++)
            slots[i].SetHighlighted(i == currentIndex, highlightColor, normalColor);
    }

    private void RefreshSlots()
    {
        EnsureSlotsInitialized();
        int count = GetSlotCount();
        for (int i = 0; i < count; i++)
        {
            string slotName = GetSlotName(i);
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

    private int GetSlotCount()
    {
        return slots != null ? Mathf.Min(slots.Length, slotNames != null ? slotNames.Length : 0) : 0;
    }

    private void EnsureSlotsInitialized()
    {
        int expected = slotNames != null ? slotNames.Length : 0;
        if (expected <= 0)
            return;

        var discoveredSlots = GetComponentsInChildren<SaveLoadSlot>(true)
            .OrderBy(slot => slot.transform.GetSiblingIndex())
            .ToArray();

        if (discoveredSlots.Length >= expected)
        {
            slots = discoveredSlots.Take(expected).ToArray();
            return;
        }

        if (slots == null || slots.Length < expected)
            slots = discoveredSlots.Length > 0 ? discoveredSlots : slots;
    }

    private string GetSlotName(int index)
    {
        if (slotNames != null && index >= 0 && index < slotNames.Length)
            return slotNames[index];

        return "SaveFile" + (index + 1);
    }

    private bool ResolveSaveLoadSystem()
    {
        if (saveLoadSystem != null)
            return true;

        saveLoadSystem = FindObjectOfType<SaveLoadSystem>(true);
        if (saveLoadSystem != null)
            return true;

        var runtimeGo = GameObject.Find(RuntimeSaveLoadSystemName);
        if (runtimeGo == null)
            runtimeGo = new GameObject(RuntimeSaveLoadSystemName);

        saveLoadSystem = runtimeGo.GetComponent<SaveLoadSystem>();
        if (saveLoadSystem == null)
            saveLoadSystem = runtimeGo.AddComponent<SaveLoadSystem>();

        return saveLoadSystem != null;
    }
}
