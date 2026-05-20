# Phân Tích Thiết Kế Game

- Game là RPG phiêu lưu kiểu Pokemon, trọng tâm là khám phá overworld, đối thoại NPC, battle theo lượt và thu thập Pokemon.
- Vòng lặp chính: khám phá map -> gặp trigger/NPC -> nhận story/quest -> battle nếu cần -> nhận thưởng/mở khóa bước tiếp theo -> quay lại khám phá.

## Module Tổng Quan

- `Player`: điều khiển di chuyển trên map, animation, tương tác với NPC/trigger, mở menu và tham gia battle khi cần.
- `NPC`: đứng trong world, phát dialog, gắn quest hoặc story trigger, có thể đóng vai trainer để vào battle.
- `BattleSystem`: xử lý battle theo lượt, gồm chọn hành động, chọn move, item, đổi Pokemon, tính damage, faint, EXP và kết thúc trận.
- `Story/Quest`: điều khiển tiến trình cốt truyện theo step, dialog, choice, event submit, quest accept và chuyển bước.
- `Overworld/Trigger`: quản lý vùng kích hoạt trên map, scene gate, trigger story theo id và chặn/mở đường đi theo tiến trình.
- `Menu/UI`: chứa party, item, pokedex, save/load, choice UI, info UI và các menu phụ trợ cho người chơi.
- `Pokemon/Party/Storage`: quản lý dữ liệu Pokemon, party hiện tại, storage box, chỉ số, level, chiêu và trạng thái cá nhân.
- `Save/Load`: lưu và khôi phục trạng thái game, gồm scene, player, party, storage, money, quest, pokedex và story progress.
- `Pokedex`: ghi nhận Pokemon đã thấy/đã bắt và hiển thị dữ liệu theo dạng danh sách + chi tiết.
- `Audio/Visual`: quản lý nhạc nền, hiệu ứng hiển thị, dialog box, portrait, transition và cảm giác trình bày của game.

## Hướng Phát Triển

- Giai đoạn 1: chốt flow chính của game từ Prologue -> Town01 -> Oke_Lab -> chọn starter.
- Giai đoạn 2: hoàn thiện module overworld, NPC, trigger và scene gate để khóa/mở tiến trình đúng.
- Giai đoạn 3: hoàn thiện battle loop, party, item và reward.
- Giai đoạn 4: hoàn thiện save/load, pokedex và các menu phụ trợ.
- Giai đoạn 5: cân bằng UI/UX, polish animation, dialog và tuning trải nghiệm người chơi.

## Bản 5 Dòng

- Game của tôi là RPG phiêu lưu kiểu Pokemon, xoay quanh khám phá map, tương tác NPC và battle theo lượt.
- Hệ thống Player xử lý di chuyển, animation và tương tác với NPC, trigger hoặc menu.
- NPC dùng để phát dialog, giao nhiệm vụ, kích hoạt story và có thể dẫn vào battle.
- BattleSystem quản lý trận đấu theo lượt, gồm chọn hành động, đánh, đổi Pokemon, item và kết thúc trận.
- Menu, Save/Load và Pokedex hỗ trợ người chơi quản lý party, vật phẩm, dữ liệu đã gặp và tiến trình game.

## Thử Nghiệm Và Đánh Giá

- Kiểm tra từng module riêng lẻ: di chuyển Player, trigger NPC, battle, menu và save/load để bảo đảm hoạt động đúng.
- Đánh giá dựa trên tiêu chí: đúng flow game, không lỗi trigger, battle chạy ổn, lưu/đọc dữ liệu chính xác.
- Nếu phát hiện lỗi, ghi lại vị trí và nguyên nhân để điều chỉnh từng module thay vì sửa lan toàn hệ thống.
