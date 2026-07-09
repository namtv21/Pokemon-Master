using System;
using UnityEngine;

public class NotiManager : MonoBehaviour
{
    public static NotiManager Instance { get; private set; }

    [SerializeField] private GameObject[] tutorialPages;
    private int currentPageIndex;
    private Action onClosed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.RightArrow))
            NextPage();
        else if (Input.GetKeyDown(KeyCode.X))
            Close();
    }

    public void OpenTutorial(Action onClose = null)
    {
        onClosed = onClose;
        currentPageIndex = 0;
        gameObject.SetActive(true);
        ShowPage(currentPageIndex);
    }

    private void NextPage()
    {
        currentPageIndex++;
        if (currentPageIndex >= tutorialPages.Length)
        {
            Close();
            return;
        }

        ShowPage(currentPageIndex);
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < tutorialPages.Length; i++)
            tutorialPages[i].SetActive(i == index);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        var cb = onClosed;
        onClosed = null;
        cb?.Invoke();
    }
}