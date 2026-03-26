# Existing Patterns

> **Direction change:** The project is redirecting to real-time exploration-first design. Patterns below still apply unless noted. New patterns for the real-time direction are listed at the end.

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

### Physics Owns Visuals

- Gameplay-facing nodes should use a physics or interaction root that owns presentation children.
- Collision must not be parented under a sprite, imported model root, or other visual-first node.
- Visual flipping, mirroring, squash-and-stretch, and swap-outs should happen on a `VisualRoot`-style subtree without changing physics.
- If you find code or a scene where the visual owns the collision, call it out explicitly as a correctness risk.

### Domain Model Separation

The repo already has a useful split between:

- node behavior classes such as `GameManager`, `PlayerController`, and `Enemy`
- plain gameplay model classes such as `PlayerStats`, `Weapon`, `Ability`, `Modifier`, `PlayerInventory`, and `InventoryItem`

That split should be reinforced, not erased.

## Emerging Implementation Habits

### Programmatic Node Construction

Several systems are created directly in code rather than instanced from `.tscn` scenes:

- `TurnManager`
- `CombatManager`
- `Enemy`
- some gameplay HUD controls

This works, but future contributors need a clear rule for when code creation is preferred over packed scenes.

For UI specifically, the rule should be strict:

- default to editing or adding `.tscn` scenes for menus, HUD widgets, overlays, and reusable controls
- do not build programmatic UI just because it is faster for an agent
- only create UI in code when the task is inherently runtime-generated and scene authoring would be the worse Godot solution

### Thin Scene Controllers Outside Gameplay

Menu and victory scenes are straightforward:

- scene defines layout
- script styles nodes and wires button signals

`GameOverScreen` follows the same pattern. This is a healthy pattern and easy for agents to extend safely.

### Central Orchestrator In Gameplay **(needs splitting)**

`GameManager` currently acts as:

- scene bootstrapper
- encounter coordinator
- HUD controller
- aggro scanner
- combat entry point
- loot flow coordinator
- room progression tracker

This is the repo's clearest current pattern and also its biggest scaling risk. Under the new direction, this should split into smaller owned systems (see `architecture-plan.md`).

### Consumable MVP For A Permanent Item Goal

The current item system is intentionally not the final shape:

- today: simple run inventory, two starting slots, one pickup per room, consumable item kinds
- today: simple run inventory, two starting slots, one cave chest reward per room, consumable item kinds
- intended direction: permanent items with usable skills on cooldown

Future contributors should treat the current consumables as the first vertical-slice implementation, not as proof that all items should disappear on use.

## Current Inconsistencies

These are important for future AI contributors to notice:

- `Palette` is meant to centralize colors, but some colors are still hardcoded in scene files and scripts.
- `GameKeys` exists, but movement still uses raw input action strings in `PlayerController`.
- Some reusable gameplay elements are scene-backed, while other reusable world geometry is still built directly in code.
- The repo policy says all C# classes should be `partial`, but plain model classes currently are not.

## Preferred Direction As The Repo Grows

### Use Scenes For Authorable Things

Prefer `.tscn` scenes when a thing benefits from editor ownership, reusable structure, or artist/designer tweaking:

- enemy variants
- authored pickups
- reusable HUD widgets
- encounter rooms
- reusable world slices and set pieces under `scenes/world_slices/`
- nearly all UI

Concrete example:

- `RockWallSlice.tscn` is the canonical authored wall unit
- longer cave or room walls should be composed from repeated `RockWallSlice` instances instead of scene-local wall meshes

When those scenes are gameplay-facing, keep the visual subtree replaceable and the physics root authoritative.

### Keep Builders As The Decision Makers

For room generation, the current preferred split is:

- builders choose layouts, placement, and gameplay rules
- scene slices own geometry, collisions, local lights, and anchor markers

Do not move room progression or encounter rules into slice scenes just because the geometry is scene-authored.

### Use Plain Classes For Pure Data And Math

Prefer plain C# types for:

- stat math
- item definitions
- inventory slot state
- combat result payloads
- flow-independent game rules

### Use Managers Sparingly

If a script starts coordinating multiple unrelated systems, split by responsibility instead of adding another catch-all manager.

Good future seams include:

- room/run progression
- combat flow
- HUD presentation
- inventory/build management
- item skill and cooldown handling

## Multi-Agent Pattern Guidance

- New shared systems should expose narrow APIs and clear owners.
- Runtime-created nodes should be discoverable from one obvious composition root.
- Avoid hidden coupling through string node paths spread across many files.
- If you change a canonical shared utility, search its callers before editing behavior.

## New Direction Patterns

These patterns apply under the real-time exploration-first direction. See `direction.md`.

### Combat Is Continuous, Not Modal

- Do not create mode-swap logic between exploration and combat
- The player controller should always have movement enabled
- Enemy aggro triggers behavior changes in the enemy, not game-state changes
- Hitbox/hurtbox overlap detection drives damage, not turn resolution methods

### Exploration Drives Reward

- Place reward sources (energy nodes, chests, secrets) through exploration space
- Enemy drops should be supplemental, not dominant
- World pickups should be the primary progression fuel

### Hub and Excursion Are Distinct Scenes

- Hub is a separate scene with its own manager, NPC registry, and restoration state
- Excursion scenes own combat, traversal, and reward systems
- RunState bridges persistent and transient data between them

### Prefer AI State Machines For Enemies

- Enemies should have behavior states (idle, patrol, alert, chase, attack, stagger, leash)
- Enemies should navigate the world, not stand still waiting for combat mode
- Aggro and leash ranges shape encounter design

### Animation-Driven Combat Timing

- Attacks have wind-up frames (can be interrupted), active frames (hitbox on), and recovery frames (vulnerable)
- This applies to both player and enemies
- Do not use timer-based delays as a substitute for animation states
