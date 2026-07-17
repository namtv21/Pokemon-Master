# PokeMaster

PokeMaster là game nhập vai Pokemon 2D được phát triển bằng Unity và C#. Project tập trung vào gameplay khám phá thế giới, thu phục và phát triển Pokemon, chiến đấu theo lượt, nhiệm vụ và cốt truyện nhiều scene.

Phiên bản hiện tại: **V1.0.0 Release Candidate**. Trạng thái kiểm thử trước khi phát hành được theo dõi tại [RELEASE_CHECKLIST.md](docs/RELEASE_CHECKLIST.md).

## Công nghệ

- Unity 2022.3.62f2 LTS
- C#
- Unity 2D, Tilemap, Animator và UI
- ScriptableObject và Resources cho dữ liệu game
- JSON cho hệ thống save/load
- Git và GitHub để quản lý phiên bản
- Cloudflare Workers/KV cho dữ liệu companion AI

## Hệ thống nổi bật

- Turn-based battle với type, chỉ số, hiệu ứng chiêu thức và trainer battle
- Party, inventory, Pokemon storage và Pokédex
- Tiến hóa, học chiêu, EXP, friendship và bond
- Main story, quest, trigger và story flag qua nhiều scene
- Save/load nhiều slot cho tiến độ người chơi và trạng thái thế giới
- Overworld encounter, fishing, trainer và Pokemon xuất hiện trực tiếp trên bản đồ
- Pokemon House với hành vi theo tính cách và tương tác companion
- Companion chat, phản ứng theo sự kiện và HUD trên overworld
- Bootstrap runtime để quản lý các system dùng chung giữa scene

## Cấu trúc chính

```text
Assets/
  Game/Resources/       Dữ liệu và prefab được nạp lúc runtime
  Scenes/               Các scene gameplay
  script/
    Battle/             Battle flow và UI chiến đấu
    Character/          Player, NPC và movement
    Menu/               Menu, Pokédex, Party và Pokemon House
    Pokemon/            Pokemon model, move, item và database
    Story/              Main story, sequence và trigger
    System/             Game state, save/load, inventory và quest
```

## Chạy project

1. Cài Unity Hub và Unity `2022.3.62f2`.
2. Thêm thư mục repository vào Unity Hub.
3. Mở project và chờ Unity import toàn bộ asset.
4. Mở scene `Assets/Scenes/MainMenuScreen.unity` và chạy Play Mode.

Save mới được lưu bằng `Application.persistentDataPath`. Trên Windows, thư mục mặc định là `%USERPROFILE%\AppData\LocalLow\NamTV21\PokeMaster`. Game vẫn có thể đọc các save cũ nằm cạnh bản build để hỗ trợ chuyển đổi.

## Companion AI (tùy chọn)

Gameplay chính có thể chạy mà không cần tự cấu hình Cloudflare. Tính năng chat online với Pokemon sử dụng Cloudflare Worker đã triển khai; khi dịch vụ không khả dụng, các phản hồi companion offline vẫn có thể được sử dụng.

Để tự triển khai backend companion AI, cần cấu hình Worker trong thư mục `CloudflareWorker`, KV binding `GAME_DATA`, hai secret `CLAUDE_API_KEY` và `GAME_TOKEN`, sau đó cập nhật Worker URL và game token trong `CompanionChatSystem` của Unity.

## Lưu ý

- PokeMaster là project phi thương mại, chỉ phục vụ mục đích học tập và đồ án.
- Pokemon và các tài sản liên quan thuộc quyền sở hữu của các chủ sở hữu tương ứng; repository không đại diện cho một sản phẩm Pokemon chính thức.
