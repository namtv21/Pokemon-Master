using System.Collections.Generic;
using UnityEngine;

public class BattleTutorial : MonoBehaviour
{
    [SerializeField] private NotificationManager notification;

    // lưu các state đã hiện tutorial
    private HashSet<BattleState> shownStates = new HashSet<BattleState>();

    private BattleSystem battleSystem;

    private void Start()
    {
        battleSystem = FindObjectOfType<BattleSystem>();
        if (battleSystem != null)
        {
            battleSystem.OnStateChanged += HandleBattleStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (battleSystem != null)
        {
            battleSystem.OnStateChanged -= HandleBattleStateChanged;
        }
    }

    private void HandleBattleStateChanged(BattleState newState)
    {
        // chỉ hiện lần đầu tiên
        if (shownStates.Contains(newState)) return;

        switch (newState)
        {
            case BattleState.Start:
                notification.ShowNotification("Trận đấu bắt đầu! Nếu chiến thắng bạn sẽ nhận được kinh nhiệm và tiền (nếu đấu với Trainer) \nHãy chú ý đến hệ của Pokémon và chiêu thức để có lợi thế trong trận đấu \nchú ý chiêu thức hệ normal không có tác dụng với hệ Ghost và hệ Electric không có tác dụng với hệ Ground\nđây là trận đấu tập, hãy làm theo hướng dẫn");
                break;

            case BattleState.PlayerActionSelection:
                notification.ShowNotification("Chọn hành động: Fight (chọn chiêu thức để chiến đấu), Pokémon (thay đổi pokemon ra trận), Item (dùng item) hoặc Run (thoát chiến đấu).\nHãy chọn Fight để tấn công đối thủ");
                break;

            case BattleState.PlayerMoveSelection:
                notification.ShowNotification("Chọn chiêu thức để tấn công đối thủ, mỗi pokemon có nhiều nhất 4 chiêu \ncác kỹ năng có thể gây sát thương, gây hiệu ứng xấu hoặc tăng, giảm chỉ số \nmỗi chiêu có chỉ số, hệ và PP (số lần sử dụng) nhất định\n hãy sử dụng quickattack 2 lần rồi chuyển sang item");
                break;

            case BattleState.PlayerItemSelection:
                notification.ShowNotification("Chọn Item để sử dụng trong trận đấu \ncác item có thể hồi máu, hồi sinh, xóa hiệu ứng xấu hoặc bắt Pokémon\n Việc bắt pokemon chi có thể dùng với wild pokemon, càng ít máu thì bắt càng dễ\n hãy thử bắt pokemon này khi nó còn ít máu");
                break;

            case BattleState.PlayerPokemonSelection:
                notification.ShowNotification("Chọn Pokémon khác để ra trận \nHãy cân nhắc lv, hệ và trạng thái hiện tại của Pokémon\n pokemon tiếp theo");
                break;

            case BattleState.NewMoveSelection:
                notification.ShowNotification("Pokémon của bạn muốn học chiêu mới! \nChọn 1 trong 4 chiêu bên phải để thay thế hoặc chọn chính chiêu mới bên trái để từ chối");
                break;

            case BattleState.WaitForNextTrainerPokemon:
                notification.ShowNotification("Hãy chọn Pokemon có hệ khắc chế đối thủ: Grass > Ground");
                break;

            case BattleState.EnemyMove:
                notification.ShowNotification("Lượt đối thủ đang thực hiện chiêu thức.");
                break;

            case BattleState.Busy:
                notification.ShowNotification("sát thương được tính dựa trên chỉ số tấn công và phòng thủ của Pokémon, hệ của chiêu thức và hệ pokemon\ncó độ chính xác nhất định, có thể trượt và có 5% chí mạng ");
                break;

        }

        // đánh dấu đã hiện tutorial cho state này
        shownStates.Add(newState);
    }
}
