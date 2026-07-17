using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private SaveLoadMenuUI saveLoadMenuUI;
    [SerializeField] private string newGameScene = "Town01";
    [SerializeField] private Color yellow = Color.yellow;
    [SerializeField] private Color white = Color.white;
    [SerializeField] private TMP_Text[] optionTexts;

    private int currentIndex;
    private readonly string[] options = { "New Game", "Load Game", "Exit" };

    private void Start()
    {
        saveLoadMenuUI?.Close();
        HighlightCurrent();
    }

    private void Update()
    {
        HandleUpdate();
    }

    private void HandleUpdate()
    {
        if (saveLoadMenuUI == null)
            return;

        if (saveLoadMenuUI.gameObject.activeSelf)
        {
            saveLoadMenuUI.HandleUpdate(
                onCancel: ResetSelection,
                onSaveCompleted: ResetSelection,
                onLoadCompleted: ResetSelection);
            return;
        }

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
                case 0:
                    if (BootstrapLoader.EnsureSystemRoot() == null)
                    {
                        Debug.LogError("[MainMenu] Cannot start a new game because SystemRoot is missing.");
                        return;
                    }
                    SceneManager.LoadScene(newGameScene);
                    break;
                case 1:
                    saveLoadMenuUI.Open(false);
                    break;
                case 2:
                    Application.Quit();
                    break;
            }
        }
    }

    private void ResetSelection()
    {
        currentIndex = 0;
        HighlightCurrent();
    }

    private void HighlightCurrent()
    {
        if (optionTexts == null)
            return;

        for (int i = 0; i < options.Length && i < optionTexts.Length; i++)
        {
            if (optionTexts[i] != null)
                optionTexts[i].color = i == currentIndex ? yellow : white;
        }
    }
}
