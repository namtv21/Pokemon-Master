# BattleScene Setup Guide

## Cấu Trúc Hierarchy Required

```
BattleScene
├── Battle (root GameObject)
│   ├── BattleSystem (script component)
│   ├── BattleItemHandler (script component)
│   ├── BattlePartyHandler (script component) 
│   ├── BattleTransition (script component)
│   └── Canvas (UI)
│       └── BattleUI (GameObject - parent của UI elements)
│           ├── BattleCamera (Camera)
│           ├── LearnMoveCanvas
│           ├── BattleCanvas (GameObject - parent của tất cả battle elements)
│           │   ├── DialogCanvas
│           │   │   └── DialogBox (với BattleDialogBox script)
│           │   ├── PlayerAnchor (Transform - vị trí player unit)
│           │   │   └── PlayerUnit (Sprite Renderer, BattleUnit script)
│           │   ├── EnemyAnchor (Transform - vị trí enemy unit)
│           │   │   └── EnemyUnit (Sprite Renderer, BattleUnit script)
│           │   ├── BattleItemMenu (với BattleItemMenu script)
│           │   ├── BattlePartyMenu (với BattlePartyMenu script)
│           │   └── MoveLearnUI (với MoveLearnUI script)
│           └── StatusPanel (nơi chứa HP bars)
```

## BattleSystem SerializeField Assignment

### Battle Units (Required - assign pre-made prefabs/GameObjects)
- **playerUnit**: Drag PlayerUnit GameObject từ hierarchy
- **enemyUnit**: Drag EnemyUnit GameObject từ hierarchy

### UI References (Required)
- **battleUI**: Drag BattleUI GameObject từ hierarchy
- **dialogBox**: Drag DialogBox GameObject (sẽ auto-find via GetComponentInChildren nếu không assign)
- **battleItemMenu**: Drag BattleItemMenu GameObject
- **battlePartyMenu**: Drag BattlePartyMenu GameObject
- **moveLearnUI**: Drag MoveLearnUI GameObject

### Audio (Optional)
- **trainerBattleClip**: Drag audio clip cho trainer battle
- **wildBattleClip**: Drag audio clip cho wild battle

## Auto-Find Components (Already Fixed)
✅ BattleItemHandler - auto-find via `GetComponentInChildren<BattleItemHandler>()`
✅ BattlePartyHandler - auto-find via `GetComponentInChildren<BattlePartyHandler>()`
✅ PlayerParty - sử dụng `PlayerParty.Instance`

## BattleUnit Setup

### PlayerUnit Prefab/GameObject
- **Sprite Renderer**: Assign player sprite
- **BattleUnit script**:
  - `isPlayerUnit`: true
  - `hud`: Assign HP bar HUD reference
  - Animator (optional)

### EnemyUnit Prefab/GameObject
- **Sprite Renderer**: Assign enemy sprite
- **BattleUnit script**:
  - `isPlayerUnit`: false
  - `hud`: Assign HP bar HUD reference
  - Animator (optional)

## BattleDialogBox Setup

### Required SerializeFields
- **dialogPanel**: Drag hoặc tìm panel chứa dialog text
- **dialogText**: TextMeshProUGUI component hiển thị text
- **actionMenuPanel**: Panel chứa Fight/Pokémon/Item/Run buttons
- **actionTexts**: 4 TextMeshProUGUI cho các action (Fight, Pokémon, Item, Run)
- **moveMenuPanel**: Panel chứa move selection
- **moveSlots**: Array của MoveSelectionUI[] (4 slots cho 4 moves)
- **itemMenuPanel**: Panel chứa item menu
- **pokemonMenuPanel**: Panel chứa pokemon menu

## BattleItemMenu Setup
- Requires references đến các UI elements:
  - Item list parent
  - Item slot prefab
  - Item name/description text
  - Quantity text

## BattlePartyMenu Setup
- Requires references đến:
  - PartySlotUI prefab
  - Slot parent container
  - PokemonInfoUI component

## Checklist Trước Khi Test

- [ ] BattleSystem có reference đến playerUnit
- [ ] BattleSystem có reference đến enemyUnit
- [ ] BattleSystem có reference đến battleUI
- [ ] BattleSystem có reference đến dialogBox
- [ ] BattleSystem có reference đến battleItemMenu
- [ ] BattleSystem có reference đến battlePartyMenu
- [ ] BattleDialogBox tất cả các panel/text được assign
- [ ] PlayerUnit và EnemyUnit có BattleUnit script
- [ ] PlayerUnit và EnemyUnit có reference đến HUD
- [ ] BattleItemHandler là child của Battle GameObject
- [ ] BattlePartyHandler là child của Battle GameObject
- [ ] MusicManager configured (cho music playback)

## Common Issues & Fixes

### "BattleSystem not found"
→ Đảm bảo BattleScene được tạo đúng, Battle GameObject có BattleSystem script

### "null reference exception in itemHandler/partyHandler"  
→ Đảm bảo GetComponentInChildren tìm được components
→ Check nếu BattleItemHandler/BattlePartyHandler có SetActive(false)

### Dialog không hiển thị
→ Đảm bảo dialogBox.SetActive(true) được gọi
→ Check nếu dialogPanel/dialogText được assign đúng

### Input không hoạt động
→ Đảm bảo BattleState được set đúng
→ Check nếu Canvas ở front (sorting order)

## Next Steps

1. ✅ Fix BattleSystem để auto-find components
2. 🔄 Setup hierarchy theo guide trên
3. 🔄 Assign SerializeFields trong Inspector
4. 🔄 Test StartWildBattle() từ PrologueDirector
5. 🔄 Verify dialog, menus hoạt động
6. 🔄 Test pokémon switching, item usage
7. 🔄 Verify battle end và scene unload đúng
