# Changelog

Các thay đổi đáng chú ý của PokeMaster được ghi lại trong file này.

## [1.0.0-rc.1] - 2026-07-17

### Added

- Chuẩn hóa dữ liệu save với `schemaVersion` và `gameVersion`.
- Lưu save vào `Application.persistentDataPath`, đồng thời hỗ trợ đọc save cũ cạnh bản build.
- Bộ chọn số lượng item dạng từng chữ số và friendship tự tăng theo thời gian chơi.
- Pokemon House, companion HUD và tương tác theo tính cách Pokemon.

### Fixed

- Ổn định bootstrap khi load trực tiếp từ Main Menu.
- Cô lập camera BattleScene khi load game từ một phiên mới.
- Không để poison và confusion làm Pokemon giảm xuống dưới 1 HP.
- Sửa luồng Status Potion và sprite battle của Nidoran F/Nidoran M.
- Dùng thời gian unscaled cho hiệu ứng chuyển cảnh battle.

### Changed

- Chuẩn hóa tên sản phẩm thành `PokeMaster`, version `1.0.0` và application identifier `com.namtv21.pokemaster`.
- Friendship không còn phụ thuộc vào thời gian ở trong Pokemon House.

## [Unreleased]

- Chưa có thay đổi.
