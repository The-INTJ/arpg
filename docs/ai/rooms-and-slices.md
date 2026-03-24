# Rooms And Scene Slices

This doc explains the current foundation for scene-authored world building.

## What A Scene Slice Is

A scene slice is a reusable world `.tscn` that owns:

- geometry and collisions
- local lights and simple presentation
- anchor markers that the builder can read back

A slice does not own:

- room selection
- encounter rules
- dark-energy thresholds
- bridge progression
- run flow

Those decisions stay in code for now.

## Current Builder Contract

`MapGenerator` is still the room builder. It now supports a hybrid model:

- code chooses which layout to build
- code places reusable scene slices at layout-specific positions
- slices provide anchor data back to the builder
- `GeneratedMapResult` stays the output contract to `GameManager`

The first live slice is the cave pocket:

- scene: `res://scenes/world_slices/CavePocketSlice.tscn`
- placement: `MapGenerator.PlaceCavePocket(...)`
- reuse: the same slice is rotated `0` or `180` degrees around Y to support left/right variants

## Anchor Contract

Scene-authored world slices use `SceneSliceAnchor : Marker3D` plus `SceneSliceAnchorKind`.

Current kinds:

- `EnemySpawn`
- `CaveChest`
- `FallbackItem`

Use anchors for positions the builder or runtime systems need to query back out of the scene.

## Folder Convention

- reusable world slices live under `scenes/world_slices/`
- slice support scripts live with other world scripts under `scripts/world/`

If a thing is meant to be edited in the Godot editor and reused across rooms, prefer making it a world slice scene.

## What Should Become Slices Next

Strong next candidates:

- full ridge and mesa room layouts
- repeated platform-and-ramp set pieces
- tree and rock dressing clusters
- distant background chunk variants

Keep these in code for now:

- layout selection rules
- enemy composition
- room progression rules
- dark-energy and bridge gameplay
