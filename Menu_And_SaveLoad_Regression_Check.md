# Menu + PokemonDex + SaveLoad Regression Check

Date: 2026-04-15

## Scope checked
- Main menu order/state changes
- PokemonDex integration
- Save/Load merge into one flow
- Save/load persistence for quest + pokedex
- Backward behavior with old save files

## Summary
- No compile errors found in changed feature files.
- Found and fixed 2 logic regressions that could break old behavior.

## Regressions found and fixed

### 1) Double `X` handling in menu controller
- File: `Assets/script/GamePlay/MenuController.cs`
- Issue:
  - `X` was handled inside specific states (Quest/Pokedex/SaveLoad) and also globally at end of `HandleUpdate()`.
  - Result: user could be kicked out of menu to overworld immediately instead of returning to Main menu as intended.
- Fix:
  - Removed global `X` fallback block.
  - Added explicit `Storage` back handling to return to Main menu.

### 2) Legacy save could wipe quest runtime state
- File: `Assets/script/Menu/SaveLoadOption/SaveLoadSystem.cs`
- Issue:
  - For old save files without `questSnapshot`, `ImportSaveSnapshot(null)` cleared all quest runtime sets.
- Fix:
  - Only call `ImportSaveSnapshot` when `data.questSnapshot != null`.
  - Legacy saves now keep runtime quest flow safer.

## Compatibility notes
- New save schema is backward-compatible for missing fields (`partyPokemons`, `storagePokemons`, `sceneName`, `pokedex` guarded in load path).
- Main Story Summary tab in PokemonDex is intentionally blank placeholder now (as requested).

## Manual test checklist
1. Open menu in map -> navigate order: Party, Item, Storage, Quest, PokemonDex, Save/Load, Option, Exit.
2. In Quest screen: press `X` -> must return to Main menu (not close to overworld).
3. In PokemonDex: press `X` -> must return to Main menu.
4. In Save/Load:
   - mode select screen: `X` closes save/load and returns Main menu.
   - slot select screen: `X` returns to mode select (not close whole menu).
5. In Storage: `X` returns to Main menu.
6. Save then Load in same scene: party/storage/money/position restored.
7. Save then Load across scene: scene switches and state applies.
8. Load old save file (without quest snapshot): no hard reset/clear of current quest state due to null snapshot import.

## Remaining non-blocking items
- Existing Unity analyzer warning `No method with RuntimeInitializeOnLoadMethod attribute` appears unrelated and pre-existing.
- Inspector wiring still required for new refs:
  - `MenuController.pokemonDexMenuUI`
  - optional labels in `SaveLoadMenuUI` (`saveModeText`, `loadModeText`, `headerText`)

## Latest follow-up changes
- Save/Load slot labels now include the active mode prefix (`SAVE` or `LOAD`) so the selected operation is visible in the slot text itself.
- Quest menu now hides main story quests, so only side quests remain there.
- PokemonDex main story tab now shows only quests that are currently active or already completed; not-started main story quests stay hidden.
- PokemonDex Pokemon DB tab still shows selected Pokemon detail when the highlight moves across the list.
- PokemonDex now auto-imports Pokemon entries from PokemonDB at runtime, so the DB list does not need manual data entry.
- PokemonDex now auto-imports visible main story quests from QuestManager, so the main story tab is also data-driven at runtime.
