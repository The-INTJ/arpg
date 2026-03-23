# Existing Architecture

## High-Level Shape

The project is a Godot-first vertical slice built around one primary runtime scene:

- Menu flow uses scene changes between `MainMenu`, `ArchetypeSelect`, `Game`, and `VictoryScreen`.
- The active gameplay loop lives almost entirely in `scenes/Game.tscn`.
- `scripts/GameManager.cs` is the current orchestration hub for exploration, combat entry, HUD updates, room progression, loot spawning, and overlay wiring.
- Cross-scene run state is stored in the static `scripts/GameState.cs`.
- Player build state lives in the plain C# model `scripts/PlayerStats.cs`.

## Repo Layout

- `scenes/`: top-level scenes and a few placeholder/minimal scenes for runtime-spawned types
- `scripts/`: both Godot node scripts and plain gameplay model classes
- `plans/`: forward-looking feature notes, not runtime truth

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
  - Contains world, player, exit door, enemies container, HUD, attack button, and pause screen node
  - Runtime setup creates `TurnManager`, `CombatManager`, `ModifyStatsSimple`, enemies, loot, and some HUD widgets
- `scenes/VictoryScreen.tscn`
  - Styled by `scripts/VictoryScreen.cs`
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
- CanvasLayer
  - HUD labels
  - AttackButton
  - PauseScreen
- TurnManager            (added in code)
- CombatManager          (added in code)
- ModifyStatsSimple      (added in code under CanvasLayer)
- AbilityButton          (added in code under CanvasLayer)
- EnemyHpDisplay         (added in code under CanvasLayer)
- RoomLabel              (added in code under CanvasLayer)
```

## Core Systems And Boundaries

### Scene Flow

- `MainMenu` and `ArchetypeSelect` are thin scene controllers.
- `ExitDoor` handles room-to-room and room-to-victory transitions.
- `GameState` is the run bridge between scenes.

### Exploration And Combat

- `GameManager` owns exploration state checks, aggro scanning, HUD refresh, input forwarding, kill tracking, and loot spawning.
- `TurnManager` stores a minimal state machine: exploring, player turn, enemy turn, busy, victory, defeat.
- `CombatManager` owns combat entry/exit camera motion, damage application, retaliation timing, and floating damage numbers.
- `Enemy` is currently a simple data-and-reaction node, not a real AI system.

### Player Build And Progression

- `PlayerController` owns movement and initializes persistent stats for the run.
- `PlayerStats` computes effective stats from base values, weapon slots, and modifier lists.
- `Weapon`, `Ability`, `Modifier`, and `ArchetypeData` are plain model helpers in `scripts/`.
- `ModifierGenerator` creates random loot modifiers.

### World And Encounter Generation

- `MapGenerator` places walls procedurally and returns enemy spawn positions.
- `GameManager` decides how many enemies exist for a room and whether one is a boss.

### UI

- Top-level menu scenes are mostly scene-authored with light styling in code.
- Gameplay UI is mixed:
  - some nodes are authored in `Game.tscn`
  - several widgets are created directly in `GameManager`
  - `PauseScreen` and `ModifyStatsSimple` build most of their own UI in code

## State Ownership

- `GameState`
  - selected archetype
  - current room number
  - persistent player stats across rooms
- `PlayerStats`
  - mutable run state for player build and HP
  - equipped weapon slot modifiers
  - backpack modifiers
- `TurnManager`
  - combat/exploration mode only
- `GameManager`
  - room-local state such as enemy counts, aggro timers, spawned runtime UI references

## Existing Architecture Pattern

This is currently a hybrid of:

- scene-driven shell
- code-built runtime composition
- shared static run state
- thin data models for build math

That architecture is viable for the current slice, but the single-scene gameplay hub is already carrying too many responsibilities.
