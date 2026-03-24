# Existing Architecture

## High-Level Shape

The project is still a Godot-first vertical slice centered on one main gameplay scene:

- Menu flow uses scene changes between `MainMenu`, `ArchetypeSelect`, `Game`, `VictoryScreen`, and `GameOverScreen`.
- The active gameplay loop lives almost entirely in `scenes/Game.tscn`.
- `scripts/GameManager.cs` is the orchestration hub for exploration, combat entry, HUD updates, item hotkeys, pickup spawning, room progression, and overlay wiring.
- Cross-scene run state is stored in the static `scripts/GameState.cs`.
- Player build state lives in the plain C# model `scripts/PlayerStats.cs`, which now also owns the small run inventory via `PlayerInventory`.

## Repo Layout

- `scenes/`: top-level scenes and a few minimal scenes for shared UI/end states
- `scripts/`: both Godot node scripts and plain gameplay model classes
- `plans/`: forward-looking notes, not runtime truth

## Scene Map

- `scenes/MainMenu.tscn`
  - Styled by `scripts/MainMenu.cs`
  - Starts the flow by loading `scenes/ArchetypeSelect.tscn`
- `scenes/ArchetypeSelect.tscn`
  - Styled by `scripts/ArchetypeSelect.cs`
  - Writes archetype selection into `GameState`
  - Starts a fresh run and loads `scenes/Game.tscn`
- `scenes/Game.tscn`
  - Root script: `scripts/GameManager.cs`
  - Contains the world, player, exit door, HUD labels, attack button, pause screen, and modify-stats overlay node
  - Runtime setup creates `TurnManager`, `CombatManager`, ability/enemy/item HUD widgets, enemies, cave chests, loot pickups, and dropped item pickups
- `scenes/VictoryScreen.tscn`
  - Styled by `scripts/VictoryScreen.cs`
- `scenes/GameOverScreen.tscn`
  - Styled by `scripts/GameOverScreen.cs`
- `scenes/DamageNumber.tscn`
  - Instanced by `CombatManager`

## Game Scene Runtime Graph

Current gameplay ownership looks like this:

```text
GameManager
- World
  - Floor
  - MapGenerator
  - Player (PlayerController)
    - CameraRig
      - Camera3D
  - Enemies
  - ExitDoor
  - LootPickup / ItemPickup instances (spawned in code)
- CanvasLayer
  - HUD labels
  - AttackButton
  - PauseScreen
  - ModifyStatsSimple
  - AbilityButton      (added in code)
  - EnemyHpDisplay     (added in code)
  - RoomLabel          (added in code)
  - ItemBarCenter      (added in code)
- TurnManager          (added in code)
- CombatManager        (added in code)
```

## Core Systems And Boundaries

### Scene Flow

- `MainMenu` and `ArchetypeSelect` are thin scene controllers.
- `ExitDoor` handles room-to-room and room-to-victory transitions.
- `VictoryScreen` and `GameOverScreen` restart or exit runs.
- `GameState` is the run bridge between scenes.

### Exploration And Combat

- `GameManager` owns exploration checks, aggro scanning, HUD refresh, input forwarding, item hotkeys, kill tracking, and pickup spawning.
- `TurnManager` stores a minimal state machine: exploring, player turn, enemy turn, busy, victory, defeat.
- `CombatManager` owns combat entry/exit camera motion, damage application, retaliation timing, floating damage numbers, and simple item-action turn handoff.
- `Enemy` is still a simple data-and-reaction node, not a real AI system.

### Player Build, Loot, And Inventory

- `PlayerController` owns movement and initializes persistent stats for the run.
- `PlayerStats` computes effective stats from base values, weapon slots, and modifier lists.
- `PlayerInventory` is currently a fixed-slot run inventory owned by `PlayerStats`.
- `InventoryItem` is the current item model for the item bar.
- `Weapon`, `Ability`, `Modifier`, and `ArchetypeData` are plain model helpers in `scripts/`.
- `ModifierGenerator` creates random loot modifiers.

### World And Encounter Generation

- `MapGenerator` now uses a hybrid room-building model:
  - broad room layout and gameplay-facing placement rules stay in code
  - reusable authorable set pieces can be instanced as scene slices
  - current live slices include the cave pocket, the reusable single-ramp rise slice, and tree variants
- `MapGenerator` still returns the spawn/chest result that `GameManager` expects.
- `GameManager` uses the returned enemy spawn positions for encounters and the returned cave chest position for room rewards.
- tougher elite or boss enemies can also drop consumable `ItemPickup` rewards on death
- `GameManager` decides how many enemies exist for a room and whether one is a boss.
- `PlayerController` handles open-void recovery in code: leaving zone bounds and falling for about a second respawns the player at the last grounded point for `-5 HP`.

### UI

- Top-level menu and end-state scenes are mostly scene-authored with light styling in code.
- Gameplay UI is mixed:
  - some nodes are authored in `Game.tscn`
  - several widgets are created directly in `GameManager`
  - `PauseScreen` and `ModifyStatsSimple` still build most of their own UI in code
- preferred direction: future UI changes should move toward authored `.tscn` scenes unless runtime generation is clearly the better Godot pattern

## State Ownership

- `GameState`
  - selected archetype
  - current room number
  - persistent player stats across rooms
- `PlayerStats`
  - mutable run state for player build and HP
  - equipped weapon slot modifiers
  - backpack modifiers
  - item inventory state
- `TurnManager`
  - combat/exploration mode only
- `GameManager`
  - room-local state such as enemy counts, aggro timers, spawned runtime UI references, and run-end transition state

## Existing Architecture Pattern

This is still a hybrid of:

- scene-driven shell
- code-built runtime composition
- shared static run state
- thin data models for build math and item definitions

That architecture is still viable for the slice, but `GameManager`, `PlayerStats`, and `CombatManager` are the main places where cross-system pressure is accumulating.
