# PokeMaster V1 Release Checklist

## Kiểm tra tự động

- [ ] Unity batch mode import và compile không có lỗi.
- [ ] Không có missing script hoặc missing asset trong các scene thuộc Build Settings.
- [ ] `git diff --check` không phát hiện lỗi ở source code và tài liệu.
- [ ] Cloudflare Worker vượt qua kiểm tra cú pháp.

## Smoke test bắt buộc

- [ ] New Game từ Main Menu, hoàn thành đoạn mở đầu và chuyển scene.
- [ ] Save thủ công, tắt game, mở lại và Load từ Main Menu.
- [ ] Sau cold load, party, storage, item, badge, quest, story flag và vị trí NPC được giữ đúng.
- [ ] Bắt đầu và kết thúc wild battle, trainer battle, overworld battle và fishing battle.
- [ ] Battle sau cold load chỉ hiển thị BattleScene, không chồng camera overworld.
- [ ] Pokemon lên nhiều level có thể học chiêu và tiến hóa theo đúng thứ tự.
- [ ] Status Potion hoạt động trong và ngoài battle; poison/confusion không gây faint.
- [ ] Pokemon House hiển thị đúng ở 16:9 và các độ phân giải cửa sổ nhỏ hơn.
- [ ] Pokedex, inventory, storage và menu điều hướng đúng bằng bàn phím.
- [ ] Companion chat vẫn có phản hồi offline khi backend không khả dụng.

## Đóng gói

- [ ] Build Windows x64 từ scene `MainMenuScreen`.
- [ ] Chạy smoke test trên bản build sạch, không dùng dữ liệu trong Unity Editor.
- [ ] Kiểm tra Player log không có exception lặp lại.
- [ ] Cập nhật `CHANGELOG.md` nếu có sửa lỗi sau RC.
- [ ] Chỉ tạo tag `v1.0.0` sau khi toàn bộ smoke test bắt buộc đạt.

## Phạm vi V1

V1 ưu tiên một vòng chơi ổn định, save/load qua nhiều phiên, battle và story flow nhất quán. Tính năng mới không thuộc phạm vi nếu làm tăng rủi ro cho dữ liệu save hoặc thay đổi cấu trúc scene sát thời điểm phát hành.
