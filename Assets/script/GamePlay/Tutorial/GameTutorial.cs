using System.Collections.Generic;
using UnityEngine;

public class GameTutorial : MonoBehaviour
{
    [SerializeField] private NotiManager notification;

    private HashSet<GameState> shownStates = new HashSet<GameState>();
    private GameController gameController;

    private void Start()
    {
        gameController = FindObjectOfType<GameController>();
        if (gameController != null)
            gameController.OnStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (gameController != null)
            gameController.OnStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (shownStates.Contains(newState)) return;

        switch (newState)
        {
            case GameState.NPCInteraction:
                notification.ShowNotification("Có 4 loại NPC là trainer bình thường có các pokemon để đấu, chiến thắng sẽ được tiền và kinh nhiệm\nNPC healer để hồi phục tất cả trạng thái cho toàn team, NPC cửa hàng để mua bán vật phẩm và NPC storage để gửi pokemon\nHãy xem thông tin pokemon của NPC và bắt đầu chiến đấu");
                break;

            case GameState.Shop:
                notification.ShowNotification("Đây là cửa hàng, bạn có thể mua các vật phẩm cần thiết ở đây \ncác potion hồi máu, ball bắt pokemon, revive hồi sinh, status heal xóa hiệu ứng xấu ");
                break;
            
            case GameState.Storage:
                notification.ShowNotification("Đây là hệ thống cất giữ Pokémon, hãy gửi bulbasaur vào đây, sau này bạn có thể lấy nó ra bất cứ lúc nào trong menu storage");
                break;
        }

        shownStates.Add(newState);
    }
}
