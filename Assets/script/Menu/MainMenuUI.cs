using UnityEngine;
using UnityEngine.UI;
using System;

public enum MainMenuOption { Party, Item, Storage, Save, Load, Option, Quest }

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;       // Panel chính
    [SerializeField] private GameObject[] optionPanels;  // Các panel con, mỗi panel chứa Text hoặc Image

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    private int currentIndex = 0;
    private Action<MainMenuOption> onSelected;
    private Action onClose;

    public void Open(Action<MainMenuOption> onSelectedCallback, Action onCloseCallback)
    {
        menuPanel.SetActive(true);
        currentIndex = 0;
        HighlightCurrent();

        onSelected = onSelectedCallback;
        onClose = onCloseCallback;
    }

    public void Close()
    {
        menuPanel.SetActive(false);
        onSelected = null;
        onClose = null;
    }

    public void HandleUpdate()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // Nếu đang ở đầu thì quay xuống cuối
            if (currentIndex == 0)
                currentIndex = optionPanels.Length - 1;
            else
                currentIndex--;

            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // Nếu đang ở cuối thì quay lên đầu
            if (currentIndex == optionPanels.Length - 1)
                currentIndex = 0;
            else
                currentIndex++;

            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            onSelected?.Invoke((MainMenuOption)currentIndex);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Close();
            onClose?.Invoke();
        }
    }

    private void HighlightCurrent()
    {
        for (int i = 0; i < optionPanels.Length; i++)
        {
            var panel = optionPanels[i];
            if (panel == null) continue;

            // Lấy Text hoặc Image trong panel để đổi màu
            var text = panel.GetComponentInChildren<Text>();
            if (text != null)
                text.color = (i == currentIndex) ? highlightColor : normalColor;

            var img = panel.GetComponent<Image>();
            if (img != null)
                img.color = (i == currentIndex) ? highlightColor : normalColor;
        }
    }
}