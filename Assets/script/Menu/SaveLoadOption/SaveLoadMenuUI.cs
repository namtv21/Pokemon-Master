using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;
public class SaveLoadMenuUI : MonoBehaviour
{
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private SaveLoadSlot[] slots; // 4 slot
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private int currentIndex = 0;
    private bool isSaveMenu = true; // true = Save, false = Load

    private bool isInGame = false;

    public void Open(bool saveMenu, bool inGame = false)
    {
        isSaveMenu = saveMenu;
        isInGame = inGame;   // truyền vào true nếu mở từ pause menu trong game
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

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + slots.Length) % slots.Length;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % slots.Length;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            string slotName = "SaveFile" + (currentIndex + 1);
            if (isSaveMenu)
                saveLoadSystem.Save(slotName);
            else
            { 
                if (isInGame) 
                    saveLoadSystem.Load(slotName); // load ngay trong map 
                else 
                    saveLoadSystem.LoadFromMenu(slotName); // load từ MainMenu 
            }
            Close();
            onClose?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Close();
            onClose?.Invoke();
        }
    }

    private void HighlightCurrent()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var text = slots[i].GetComponentInChildren<TMP_Text>();
            text.color = (i == currentIndex) ? highlightColor : normalColor;
        }
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

            slots[i].SetData(slotName, data, path);
        }
    }
}
