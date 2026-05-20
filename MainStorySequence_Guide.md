# Main Story Sequence Guide

This project uses a data-driven main story flow.

## Decision

- Prologue stays hardcoded, because it is a special opening sequence.
- Main story after prologue uses ScriptableObject data plus one generic director.
- Map or location triggers only emit an ID and do not contain story logic.
- The quest manager keeps quest state and rewards, but does not own the cutscene script.

## Runtime pieces

- `MainStorySequence` stores the ordered story steps.
- `MainStoryDirector` plays the current step in order.
- `MainStoryTrigger` is a small scene trigger that activates the current step.
- `QuestManager` still handles accept, progress, ready-to-turn-in, and rewards.

## How to author a step

1. Create a `Quest/Main Story Sequence` asset.
2. Add steps in the order you want the player to experience them.
3. For each step, set the scene name, trigger ID, and actions.
4. Use actions to combine dialog, quest accept, event submission, and waiting.

## Typical linear flow

Example:

1. Player enters Town1 after prologue.
2. A scene-start step plays an intro dialog.
3. The step auto-accepts the main quest.
4. A map trigger fires when the player reaches Oak.
5. The trigger submits a location event and plays the next scripted action block.

## Recommended setup

- Keep one persistent `MainStoryDirector` in `SystemRoot` or the first loaded scene.
- Put `MainStoryTrigger` on map spots, doors, or event points.
- Use `triggerId` values that match the current step's trigger ID.
- Keep side quests on `NPC` and `QuestAutoTrigger`; do not mix them with the main story chain.

## Notes

- If an old `Town1MainStoryDirector` is still present in a scene, disable it before enabling the new generic director.
- The story sequence is linear by design. If you need branching later, add a separate branch field in the sequence asset rather than hardcoding the logic in a scene script.