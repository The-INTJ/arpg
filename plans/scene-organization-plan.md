# Scene Organization Plan

## Goal

Keep `scenes/` top-level reserved for full-screen or full-run assembly scenes, and move reusable world props, pickups, and UI parts into purpose-specific subfolders.

## Target Structure

```text
scenes/
- screens/
  - MainMenu.tscn
  - ArchetypeSelect.tscn
  - Game.tscn
  - VictoryScreen.tscn
  - GameOverScreen.tscn
- props/
  - CaveChest.tscn
  - BridgePoint.tscn
- pickups/
  - ItemPickup.tscn
  - LootPickup.tscn
- combat/
  - DamageNumber.tscn
- ui/
  - PauseScreen.tscn
  - ModifyStatsSimple.tscn
  - ItemSlot.tscn
  - RunHistorySnippet.tscn
  - components/
    - BackpackItemRow.tscn
    - RunHistoryCard.tscn
- actors/
- world_slices/
```

## Rules

- Top-level `scenes/` should only contain major screen or game-assembly scenes.
- Reusable world interactables belong in `scenes/props/`.
- Temporary or collectible world objects belong in `scenes/pickups/`.
- Small combat presentation scenes belong in `scenes/combat/`.
- Reusable UI scenes belong in `scenes/ui/`, with leaf controls in `scenes/ui/components/`.
- Imported meshes should still be wrapped by scene-owned gameplay roots before code references them.

## First Migration Pass

1. Move `CaveChest.tscn` to `scenes/props/CaveChest.tscn`.
2. Move `BridgePoint.tscn` to `scenes/props/BridgePoint.tscn`.
3. Move `ItemPickup.tscn` and `LootPickup.tscn` to `scenes/pickups/`.
4. Move `DamageNumber.tscn` to `scenes/combat/`.
5. Move `PauseScreen.tscn`, `ModifyStatsSimple.tscn`, `ItemSlot.tscn`, and `RunHistorySnippet.tscn` to `scenes/ui/`.
6. Move `ui_components/` to `ui/components/`.

## Refactor Sequence

1. Create the target folders first.
2. Move scenes in one category at a time.
3. Update [scenes.cs](E:/Coding/Godot/arpg/scripts/constants/scenes.cs) immediately after each category move.
4. Run `dotnet build` after each category move so broken scene paths do not pile up.
5. Open the project in Godot after each batch so `.tscn` ext-resource paths and imports settle cleanly.

## Notes

- `CavePocketSlice.tscn` and the rest of `world_slices/` are already in the right kind of folder.
- `actors/` is also already aligned with the current ownership model.
- If we want one more layer later, `props/` can split into `props/interactables/` and `props/landmarks/`, but that is not necessary for the first cleanup pass.
