using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private SaveLoadMenuUI saveLoadMenuUI;
    [SerializeField] private string newGameScene = "Tutorial";
    [SerializeField] private Color yellow = Color.yellow;
    [SerializeField] private Color white = Color.white;

    private int currentIndex = 0;
    private string[] options = { "New Game", "Load Game", "Exit" };
    [SerializeField] private TMP_Text[] optionTexts;

    void Update()
    {
        HandleUpdate();
    }

    private void HandleUpdate()
    {
        // Nếu đang mở Load menu thì chỉ cho SaveLoadMenuUI xử lý input
        if (saveLoadMenuUI.gameObject.activeSelf)
        {
            saveLoadMenuUI.HandleUpdate(() => {
                // Khi SaveLoadMenuUI đóng thì quay lại MainMenu
                currentIndex = 0;
                HighlightCurrent();
            });
            return; // 👉 dừng, không xử lý MainMenu
        }

        // Nếu SaveLoadMenuUI chưa mở thì xử lý MainMenu bình thường
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + options.Length) % options.Length;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % options.Length;
            HighlightCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            switch (currentIndex)
            {
                case 0: SceneManager.LoadScene(newGameScene); break;
                case 1: saveLoadMenuUI.Open(false, false); break;
                case 2: Application.Quit(); break;
            }
        }
    }
    private void Start()
    {
        HighlightCurrent();
    }
    private void HighlightCurrent()
    {
        for (int i = 0; i < options.Length; i++)
        {
            optionTexts[i].color = (i == currentIndex) ? yellow : white;
        }
    }
}
