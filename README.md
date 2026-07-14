# PokeMaster

PokeMaster là game nhập vai Pokemon 2D được phát triển bằng Unity và C#. Project tập trung vào gameplay khám phá thế giới, thu phục và phát triển Pokemon, chiến đấu theo lượt, nhiệm vụ và cốt truyện nhiều scene.

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
4. Mở scene khởi đầu trong Build Settings và chạy Play Mode.

## Lưu ý

- PokeMaster là project phi thương mại, chỉ phục vụ mục đích học tập và đồ án.
