using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private int stepIndex = 0;

    void Update()
    {
        switch (stepIndex)
        {
            case 0:
                {
                    NotificationManager.Instance.ShowNotification("Chào mừng bạn đến với game!");
                    stepIndex++;
                }
                break;

            case 1:
                // Ví dụ: khi mở menu
                if (Input.GetKeyDown(KeyCode.M))
                {
                    NotificationManager.Instance.ShowNotification("Đây là menu chính, bạn có thể Save/Load ở đây.");
                    stepIndex++;
                }
                break;
                

            // thêm các bước khác
        }
    }
}
