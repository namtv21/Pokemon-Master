using UnityEngine;
using TMPro;

public class NotiManager : MonoBehaviour
{
    public static NotiManager Instance;

    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private TMP_Text continueText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        notificationPanel.SetActive(false);
    }

    public void ShowNotification(string message)
    {
        notificationText.text = message;
        notificationPanel.SetActive(true);
        continueText.text = ">> Nhấn C để tắt thông báo này";
        continueText.gameObject.SetActive(true);
        // Tạm dừng trò chơi
        Time.timeScale = 0f;
    }

    public void HideNotification()
    {
        notificationPanel.SetActive(false);
        // Tiếp tục trò chơi
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Nếu panel đang mở, lắng nghe input
        if (notificationPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                HideNotification();
            }
        }
    }
}