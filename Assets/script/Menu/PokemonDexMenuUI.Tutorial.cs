using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// Phần Tutorial của PokemonDexMenuUI (partial class):
// nội dung hướng dẫn dựng sẵn và điều hướng trang tutorial.
public partial class PokemonDexMenuUI
{
    private void AdvanceTutorialPage()
    {
        if (tutorialPages == null || tutorialPages.Length == 0)
            return;

        tutorialPageIndex = (tutorialPageIndex + 1) % tutorialPages.Length;
    }

    private List<TMP_Text> GetTutorialTexts()
    {
        if (tutorialTopicLines != null && tutorialTopicLines.Any(t => t != null))
            return tutorialTopicLines.Where(t => t != null).ToList();

        if (autoTutorialTexts.Count == 0)
            ResolveAutoTextCollections();

        return autoTutorialTexts;
    }

    // Số trang tutorial: ưu tiên các trang GameObject gán trong Inspector;
    // nếu không có thì dùng các trang text dựng sẵn trong code (BuiltInTutorialPages).
    private int TutorialPageCount =>
        (tutorialPages != null && tutorialPages.Length > 0)
            ? tutorialPages.Length
            : BuiltInTutorialPages.Length;

    // Nội dung hướng dẫn chơi dựng sẵn, hiển thị trong tab PokemonDEX -> Tutorial
    // khi chưa cấu hình trang GameObject nào. Dùng phím Lên/Xuống để chuyển trang.
    private static readonly string[] BuiltInTutorialPages =
    {
        "<b>HƯỚNG DẪN CHƠI</b>\n\nChào mừng đến với Pokemon Master! Bạn vào vai một nhà huấn luyện trẻ: thu thập và nuôi Pokemon, chinh phục ba Phòng Tập (Cỏ, Nước, Lửa), ngăn chặn Team Rocket và trở thành Nhà Vô Địch.\n\nDùng phím Lên/Xuống để chuyển trang hướng dẫn.",

        "<b>ĐIỀU KHIỂN</b>\n\n- Phím mũi tên: di chuyển nhân vật và điều hướng menu.\n- Z: xác nhận, nói chuyện với NPC, tiếp tục hội thoại.\n- X: hủy, quay lại, hoặc mở menu chính.\n- Shift: chạy nhanh.\n- C: gửi Pokemon vào Kho (trong màn Party).\n- Ctrl: tua nhanh hội thoại.\n- Enter / Esc: vào / thoát ô nhập chat.",

        "<b>KHÁM PHÁ</b>\n\nDi chuyển qua các thị trấn, đường mòn, hang động và phòng tập. Nói chuyện với NPC bằng phím Z để nhận thông tin, vật phẩm và nhiệm vụ.\n\nBước vào vùng cỏ cao có thể gặp Pokemon hoang dã. Một số cổng bị khóa cho tới khi bạn đạt mốc cốt truyện tương ứng.",

        "<b>CHIẾN ĐẤU THEO LƯỢT</b>\n\nTrong trận, chọn một trong bốn lựa chọn: Fight (đánh), Pokemon (đổi), Item (vật phẩm) hoặc Run (bỏ chạy - chỉ với Pokemon hoang dã).\n\nPokemon có Speed cao hơn thường ra đòn trước. Thắng trận sẽ nhận EXP để lên cấp, học chiêu mới và tiến hóa.",

        "<b>HỆ TƯƠNG KHẮC</b>\n\nMỗi chiêu và mỗi Pokemon thuộc một hoặc hai hệ. Đánh trúng hệ khắc thì sát thương nhân đôi (rất hiệu quả); đánh vào hệ kháng thì giảm một nửa; có trường hợp hoàn toàn miễn nhiễm.\n\nNếu đối thủ có hai hệ, hiệu quả là tích của cả hai. Chọn chiêu đúng hệ là chìa khóa thắng trận.",

        "<b>BẮT POKEMON</b>\n\nChỉ bắt được Pokemon hoang dã, không bắt được Pokemon của huấn luyện viên.\n\nPokemon còn càng ít máu thì tỉ lệ bắt càng cao - hãy đánh cho yếu trước khi ném bóng. Bóng tốt hơn (Great Ball, Ultra Ball) cho tỉ lệ cao hơn. Bóng rung đủ 3 lần là bắt thành công.",

        "<b>ĐỘI HÌNH & VẬT PHẨM</b>\n\nĐội hình tối đa 6 Pokemon; phần dư được gửi vào Kho (nhấn C trong màn Party). Mở Menu bằng phím X để vào Party, Item, Quest...\n\nDùng thuốc để hồi máu, hồi PP hoặc chữa trạng thái. Mua vật phẩm ở cửa hàng bằng tiền thắng trận.",

        "<b>LƯU GAME</b>\n\nMở Menu rồi chọn Save/Load. Game có nhiều slot lưu độc lập, hiển thị địa điểm, số Pokemon, tiền và thời gian lưu.\n\nGame cũng tự lưu (autosave) sau các mốc cốt truyện quan trọng. Bạn có thể lưu thủ công khi đang ở ngoài bản đồ.",

        "<b>BẠN ĐỒNG HÀNH AI & MẸO</b>\n\nVào Menu > Chat để trò chuyện với Pokemon đứng đầu đội. Khi có mạng (Online) bạn hỏi tự do; khi mất mạng vẫn có phản hồi cơ bản. Hãy hỏi nó hướng đi tiếp theo nếu bị lạc.\n\nMẹo: luôn mang thuốc hồi máu, đa dạng hệ Pokemon trong đội, và ghé Trung tâm Pokemon để hồi phục miễn phí.",
    };

    private void RefreshTutorialPage()
    {
        if (tutorialPages == null || tutorialPages.Length == 0)
        {
            // Chưa cấu hình trang GameObject: hiển thị nội dung tutorial dựng sẵn trong code.
            if (tutorialPageText != null && BuiltInTutorialPages.Length > 0)
            {
                tutorialPageIndex = ClampIndex(tutorialPageIndex, BuiltInTutorialPages.Length);
                tutorialPageText.text =
                    $"[{tutorialPageIndex + 1}/{BuiltInTutorialPages.Length}]  (Lên/Xuống để chuyển trang)\n\n"
                    + BuiltInTutorialPages[tutorialPageIndex];
            }
            return;
        }

        tutorialPageIndex = ClampIndex(tutorialPageIndex, tutorialPages.Length);

        for (int i = 0; i < tutorialPages.Length; i++)
            tutorialPages[i].SetActive(i == tutorialPageIndex);

        if (tutorialPageText != null)
            tutorialPageText.text = $"Page {tutorialPageIndex + 1}/{tutorialPages.Length} (Up/Down) - {tutorialPages[tutorialPageIndex].name}";
    }
}
