using UnityEngine;
using TMPro;

public class NotiManager : MonoBehaviour
{
    public static NotiManager Instance;
    private int currentPageIndex = 0;
    [SerializeField] private GameObject[] pages; // Mảng chứa các trang thông báo
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void Open()
    {
        currentPageIndex = 0;
        gameObject.SetActive(true);
        ShowPage(currentPageIndex);
    }

    void Update()
    {
        //if (GameController.Instance.State != GameState.Noti) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            NextPage();
        }
    }

    void ShowPage(int index)
    {
        // Ẩn tất cả trang
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }
    }

    void NextPage()
    {
        currentPageIndex++;
        if (currentPageIndex >= pages.Length)
        {
           gameObject.SetActive(false); // Ẩn thông báo khi hết trang
           //GameController.Instance.SetState(GameState.Overworld);
            return;
        }
        ShowPage(currentPageIndex);
    }

}