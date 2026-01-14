using System.Collections.Generic;
using UnityEngine;

public class MenuTutorial : MonoBehaviour
{
    [SerializeField] private NotiManager notification;

    private HashSet<MenuState> shownStates = new HashSet<MenuState>();
    private MenuController menuController;

    private void Start()
    {
        menuController = FindObjectOfType<MenuController>();
        if (menuController != null)
            menuController.OnStateChanged += HandleMenuStateChanged;
    }

    private void OnDestroy()
    {
        if (menuController != null)
            menuController.OnStateChanged -= HandleMenuStateChanged;
    }

    private void HandleMenuStateChanged(MenuState newState)
    {
        if (shownStates.Contains(newState)) return;

        switch (newState)
        {
            case MenuState.Party:
                notification.ShowNotification("Đây là Party Menu, bạn có thể xem thông tin và sắp xếp Pokémon.");
                break;

            case MenuState.Item:
                notification.ShowNotification("Đây là Item Menu, quản lý các vật phẩm bạn sở hữu.");
                break;

            case MenuState.Storage:
                notification.ShowNotification("Đây là Storage Menu, bạn có thể xem Pokémon trong kho và lấy Pokemon ra từ kho.");
                break;

            case MenuState.Save:
                notification.ShowNotification("Bạn có thể lưu tiến trình trò chơi tại đây.");
                break;

            case MenuState.Load:
                notification.ShowNotification("Bạn có thể tải lại tiến trình đã lưu.");
                break;

            case MenuState.Option:
                notification.ShowNotification("Đây là Option Menu, nơi chỉnh cài đặt trò chơi.");
                break;

            case MenuState.Quest:
                notification.ShowNotification("Đây là Quest Menu, theo dõi nhiệm vụ hiện tại.");
                break;
        }

        shownStates.Add(newState);
    }
}
