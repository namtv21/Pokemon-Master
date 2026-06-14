# CLAUDE.md — Pokemon Master (Unity 2022 LTS)

## Project overview

A top-down 2D Pokemon RPG built in Unity 2022 LTS (C#). Inspired by classic Game Boy Pokemon games. Single developer project by Nam (tvnamtv21@gmail.com). Target platform: Windows PC (future: Android/iOS).

## Architecture

### Bootstrap & singletons

- `BootstrapLoader` — `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` auto-instantiates `Resources/SystemRoot.prefab` (or `SystemsRoot.prefab`) as a `DontDestroyOnLoad` root so systems survive scene transitions. Does NOT run in `MainMenuScreen` scene.
- All major singletons (`GameController`, `MusicManager`, `DialogManager`, `PlayerParty`, `MainStoryDirector`, `PokemonDB`, etc.) follow the same pattern: `public static T Instance { get; private set; }` with duplicate-destroy in `Awake`.

### GameController (partial class)

Split across two files:
- [GameController.cs](Assets/script/GamePlay/GameController.cs) — fields, properties, battle plumbing, camera/AudioListener isolation
- [GameController.StateFlow.cs](Assets/script/GamePlay/GameController.StateFlow.cs) — `Update` dispatch, `SetState`, dialog callbacks, scene events

**GameState FSM:**
```
Overworld ──(battle trigger)──► Battle
Overworld ──(X key)──────────► Menu
Overworld ──(NPC talk)───────► Dialog → NPCInteraction
Overworld ──(cutscene start)─► Cutscene
Dialog/Menu ──(done)─────────► Overworld
```

Full enum: `Overworld, Battle, Dialog, Noti, Menu, NPCInteraction, Shop, Storage, Cutscene, HealingCenter, Quest`

### Battle scene — additive loading

`BattleScene` is loaded **additively** on top of the current overworld scene (not replacing it). Consequences:
- `SetBattleCameraIsolation` must disable ALL overworld `Camera` components AND `AudioListener` components, then restore them after battle. Failing to disable AudioListeners causes Unity's "multiple AudioListeners" warning and silent audio in battle.
- `cachedCameraEnabledStates` and `cachedListenerEnabledStates` (both `Dictionary<T, bool>`) store enabled states for restoration.
- `BattleScene` is unloaded via `SceneManager.UnloadSceneAsync` after battle ends.

### NPC system

[NPC.cs](Assets/script/Player/NPC.cs) — NPCs can optionally: heal, shop, give quests, start trainer battles (once or repeatable), fade away after dialog, trigger story flags after badge.

**NPC state persistence key format:**
```
"{sceneName}|{npcId}|{posX:F3}|{posY:F3}|{posZ:F3}"
```
Stored in a static dictionary so defeated/interacted state survives scene unload.

**NPC movement in cutscenes:** `MoveTo` and `MoveTransformTo` have an inner pause guard. The guard must allow movement during `GameState.Cutscene` — if it only checks `!= Overworld` it will deadlock story cutscene steps. Correct condition:
```csharp
while (State != GameState.Overworld && State != GameState.Cutscene)
    yield return null;
```

### MainStoryDirector

[MainStoryDirector.cs](Assets/script/GamePlay/Story/MainStoryDirector.cs) — plays `MainStorySequence` ScriptableObjects step-by-step. Supports: Dialog, MoveNpc, MoveNpcToTransform, SetFlag, WaitForFlag, Choice, FadeOut/In, etc.

- `IsPlayingStep` blocks `HandleOverworldUpdate` while a cutscene step is running.
- `persistAcrossScenes = true` → `DontDestroyOnLoad`.
- On duplicate Awake: existing instance inherits sequence from incoming if it has none, then destroys the incoming duplicate.

### Dialog system

- `DialogManager` — singleton, fires `OnDialogStarted` / `OnDialogFinished` events consumed by `GameController`.
- `DialogSpeakerPortraitResolver` — resolves portrait: `explicitPortrait → Player → NPC (by Id/npcName) → Pokemon FrontSprite`. Falls through to `null` gracefully for unknown speakers.
- `PokemonDB.GetPokemonByName` returns `null` for unknown names — **this is valid** (used as a probe). Do NOT add `LogWarning` there.

### Audio

- `MusicManager` — `DontDestroyOnLoad` singleton. Restores `PlayerPrefs` volume (`"MusicVolumeLevel"`, int 0–9, default 2) in `Awake`. `PlayMusic(clip, isMapMusic)` skips restart if same clip.
- `AudioSettings` (Menu) — slider UI that writes to `PlayerPrefs` and calls `MusicManager.SetVolume`. Has null guard for `MusicManager.Instance`.
- Volume formula: `normalizedVolume = level / 9f`.

### Save / load

[SaveLoadSystem.cs](Assets/script/Menu/SaveLoadOption/SaveLoadSystem.cs) — JSON-based local save. `ApplyLoadedData()` called in `GameController.Start()`. Up to 3 save slots.

### Data (ScriptableObjects)

| Type | Location |
|------|----------|
| `PokemonBase` | `Resources/PokemonData/` |
| `MoveBase` | `Resources/MoveData/` |
| `ItemBase` | `Resources/Item/` |
| `MainStorySequence` | `Resources/` or assigned in Inspector |

`PokemonDB` and `MoveDB` use `Resources.LoadAll<T>()` at runtime.

## Scenes

| Scene | Purpose |
|-------|---------|
| `MainMenuScreen` | Title screen, no SystemRoot |
| `Prologue` / `Intro` | Opening cutscenes |
| `BattleScene` | Loaded additively during any battle |
| `Town01`, `GrassTown`, `WaterTown`, `FireTown` | Towns |
| `Road01`–`Road04`, `Cave`, `Mountain` | Overworld routes |
| `GrassGym`, `WaterGym`, `FireGym` | Gyms |
| `poke`, `poke_mart`, `oke_lab` | Interior buildings |
| `ChampionMeet`, `CM_studio` | Late-game story scenes |

## Conventions

- **No `Debug.LogWarning` in general-purpose lookup methods** — warnings belong in callers that expect a result.
- **Partial classes for large MonoBehaviours** — `GameController` is split by concern (`.cs` core + `.StateFlow.cs` + `.Evolution.cs`).
- **NPC portrait** — assign `Portrait` sprite in Inspector; if left null, portrait resolution silently returns null (no warning, no error).
- **Input** — keyboard only: Arrow keys navigate, Z = confirm, X = cancel/back, Enter = confirm dialogs.
- **No DOTween** — animations use Unity's built-in Animator and coroutine lerps.
- **Comments in Vietnamese** are normal in this codebase — do not change to English.

## Known design decisions

- BattleScene additive loading was chosen so overworld state (NPC positions, player position) is preserved during battle without manual serialization.
- NPC defeat state is tracked in a static dictionary (not saved to file) — resets on application quit but persists across scene transitions within a play session. Gym leaders use `canBattleOnce` + `StoryFlag` to persist defeat across saves.
- `BootstrapLoader` uses `Resources.Load` so SystemRoot works when entering any scene directly from the Unity Editor (play-in-editor from non-MainMenu scenes).
