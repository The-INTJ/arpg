# Existing Patterns

## Stable Patterns To Preserve

### Godot-First Composition

- Node scripts attach to scenes or runtime-created nodes.
- Scene changes use `GetTree().ChangeSceneToFile(...)`.
- Signals are used for local event handoff such as combat end, pause actions, and loot actions.
- Groups are used for loose world queries such as the `enemies` group.

### Hardcoded Gameplay Values For Now

- Archetype stats, room counts, enemy scaling, and modifier ranges are still hardcoded.
- That matches current project guidance. Do not over-data-drive small systems prematurely.

### Shared Utility Anchors

- `Palette` is intended to be the single styling/color source.
- `GameKeys` is intended to be the single input-display source.
- `GameState` is the single cross-scene run-state holder.

### Domain Model Separation

The repo already has a useful split between:

- node behavior classes such as `GameManager`, `PlayerController`, and `Enemy`
- plain gameplay model classes such as `PlayerStats`, `Weapon`, `Ability`, and `Modifier`

That split should be reinforced, not erased.

## Emerging Implementation Habits

### Programmatic Node Construction

Several systems are created directly in code rather than instanced from `.tscn` scenes:

- `TurnManager`
- `CombatManager`
- `Enemy`
- `LootPickup`
- `ModifyStatsSimple`
- some gameplay HUD controls

This works, but future contributors need a clear rule for when code creation is preferred over packed scenes.

### Thin Scene Controllers Outside Gameplay

Menu and victory scenes are straightforward:

- scene defines layout
- script styles nodes and wires button signals

This is a healthy pattern and easy for agents to extend safely.

### Central Orchestrator In Gameplay

`GameManager` currently acts as:

- scene bootstrapper
- encounter coordinator
- HUD controller
- aggro scanner
- combat entry point
- loot flow coordinator
- room progression tracker

This is the repo's clearest current pattern and also its biggest scaling risk.

## Current Inconsistencies

These are important for future AI contributors to notice:

- `Palette` is meant to centralize colors, but some colors are still hardcoded in scene files and scripts.
- `GameKeys` exists, but movement still uses raw input action strings in `PlayerController`.
- Some reusable gameplay elements have `.tscn` files, but the game instantiates the C# types directly instead of using those scenes.
- The repo policy says all C# classes should be `partial`, but plain model classes currently are not.

## Preferred Direction As The Repo Grows

### Use Scenes For Authorable Things

Prefer `.tscn` scenes when a thing benefits from editor ownership, reusable structure, or artist/designer tweaking:

- enemy variants
- authored pickups
- reusable HUD widgets
- encounter rooms

### Use Plain Classes For Pure Data And Math

Prefer plain C# types for:

- stat math
- item definitions
- combat result payloads
- flow-independent game rules

### Use Managers Sparingly

If a script starts coordinating multiple unrelated systems, split by responsibility instead of adding another catch-all manager.

Good future seams include:

- room/run progression
- combat flow
- HUD presentation
- inventory/build management

## Multi-Agent Pattern Guidance

- New shared systems should expose narrow APIs and clear owners.
- Runtime-created nodes should be discoverable from one obvious composition root.
- Avoid hidden coupling through string node paths spread across many files.
- If you change a canonical shared utility, search its callers before editing behavior.
