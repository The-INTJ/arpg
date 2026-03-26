# ARPG - Godot 4.6 .NET/C#

## What This Project Is

A real-time, exploration-first action RPG / roguelite set on a shattered planet of fractured floating islands. Built with Godot 4.6 and C# (.NET 8.0). See `GAME_IDEA.md` for the full vision and `docs/ai/direction.md` for the authoritative direction document.

The player traverses a fractured vertical world, discovering routes, navigating dangerous enemies, collecting modifiers that physically change how they interact with space, and restoring the planet piece by piece through a hub-based meta-progression loop.

**The prototype was built around turn-based combat. That direction is now deprecated.** All new work should assume real-time combat and exploration-first design. See `docs/ai/current-state-assessment.md` for what existing code is reusable vs dead ends.

## Development Philosophy

**Build foundationally and intentionally.** Every system should be simple today but designed so it can grow without being ripped out and rewritten. This means:

- Write clean, well-structured code that does what's needed *now* without being so rigid it blocks what's needed *next*
- When a system will clearly need to support multiplayer, multiple archetypes, or modifier stacking later — design the data structures with that in mind, even if the first implementation is single-player/single-archetype
- Prefer Godot-native patterns over custom engines. Use nodes, scenes, signals, and groups as Godot intends
- Avoid premature abstraction, but don't avoid *appropriate* abstraction. If three scripts are doing the same thing, that's a sign to extract — not a sign to keep copying
- Keep scripts focused. If one grows past ~150 lines, consider whether it's doing too many jobs

## Direction Litmus Test

For every proposed system, ask: **Does this make the player more excited to move through the world, discover routes, and feel their build physically changing how they interact with space?**

If not, it is probably secondary, mis-scoped, or the wrong direction.

## Design Pillars (Priority Order)

1. **Traversal & spatial curiosity** — Movement should feel good. The world presents visible possibilities.
2. **Real-time combat that supports movement** — Combat punctuates exploration, not replaces it. No mode swap. No movement lock.
3. **Build experimentation through modifiers** — Any modifier can affect any ability. Players feel changes physically.
4. **World restoration & hub expansion** — Returning fragments heals the planet. Hub grows with NPCs and services.
5. **Optional narrative depth** — Story enriches without slowing the core loop.

## Key Architectural Rules

### Combat Is Real-Time

- No turn-based sequencing. No movement locking during combat. No mode swap between exploration and combat.
- Hitbox/hurtbox model with animation-driven timing (wind-up, active frames, recovery).
- Combat happens in the same traversal space as exploration.
- Enemies use AI state machines (idle → patrol → alert → chase → attack → stagger → leash).

### Exploration Is Primary Progression

- Energy nodes and world pickups are the main progression fuel, not enemy drops.
- Enemies are sparse, dangerous, and consequential — not constant filler.
- The player should often choose whether to engage or avoid enemies.
- Reward exploration with energy, secrets, NPC discoveries, and fragments.

### Hub and Excursion Are Separate

- Excursions: movement, exploration, combat, fragment pursuit.
- Hub: story, NPC interaction, upgrades, world restoration, slower pacing.
- Persistent state (hub level, NPCs, fragments) survives death. Transient state (HP, energy, loadout) resets.

### Continuous Simulation, Not Mode Switching

- The old architecture had hard exploration → combat mode switches. Remove this pattern.
- The player controller should always have movement. Combat layers on top of exploration.
- AggroSystem should trigger enemy pursuit, not a game-state mode change.

## Multiplayer Awareness

Multiplayer is a core consideration from day one. This doesn't mean every feature ships multiplayer immediately, but:

- Data structures should assume multiple players exist
- Combat flow should not hardcode "the player" as a singleton concept
- Nothing should be designed in a way that makes multiplayer painful to retrofit

## Technical Rules

- **Engine**: Godot 4.6, C# (.NET 8.0)
- **All C# classes that extend Godot types (Node, Resource, etc.) must be `partial`** (Godot 4.x requirement). Pure data classes don't require `partial`.
- **Project must be runnable after every change** — don't leave it broken between commits
- **Primitive meshes + procedural sprites** — characters use `SpriteFactory` pixel art; environment uses primitive meshes
- **Hardcode gameplay values** in scripts for now — data-driven content comes later when we have enough systems to justify it

## Godot C# Patterns

- Scripts attach to nodes in scenes (one script per node that needs behavior)
- `[Export]` for values tunable in the editor
- Movement in `_PhysicsProcess(double delta)` using `MoveAndSlide()`
- Scene changes: `GetTree().ChangeSceneToFile("res://scenes/SceneName.tscn")`
- Signal connections: prefer editor or `Connect()` in `_Ready()`
- Node references: `GetNode<Type>("path")` or `[Export] private NodePath`
- Dynamic node creation: use `new Enemy()` directly so the C# type is correct, not `SetScript()`

## Shared Systems

- **Palette** (`scripts/core/Palette.cs`): All colors and UI button styling. Vibrant earth-tone palette. Every material/color in the game should reference Palette — don't hardcode color values elsewhere.
- **GameKeys** (`scripts/core/GameKeys.cs`): Key binding display names. Actions defined in `project.godot` InputMap; `GameKeys.DisplayName(action)` reads the actual bound key at runtime.
- **GameState** (`scripts/core/GameState.cs`): Static state passed between scenes. Will evolve into RunState with persistent/transient separation.
- **SpriteFactory** (`scripts/core/SpriteFactory.cs`): Generates procedural pixel-art textures for characters at runtime.
- **PlayerStats** (`scripts/player/PlayerStats.cs`): Base stats + modifier stack. Effective stats computed on the fly. Archetype sets base values; modifiers layer with `+N → +N% → ×M → −N%` ordering.
- **ModifierGenerator** (`scripts/modifiers/ModifierGenerator.cs`): Random modifier creation for loot drops.

## Deprecated Systems (Turn-Based)

These systems exist in the codebase but reflect the old turn-based direction. They should not be extended. See `docs/ai/current-state-assessment.md` for what's reusable from each.

- **CombatManager** (`scripts/combat/CombatManager.cs`): Turn-based combat orchestration. Camera shake and damage numbers are reusable; turn alternation is not.
- **TurnManager** (`scripts/core/TurnManager.cs`): Turn-based state machine. The phase-tracking concept may be lightly reusable; the turn-specific states are dead ends.

## File Layout

```
scenes/                    — .tscn scene files (MainMenu, ArchetypeSelect, Game, VictoryScreen, DamageNumber, etc.)
scripts/
  core/                    — Shared utilities, singletons, global state (Palette, GameKeys, GameState, SpriteFactory, AudioManager, TurnManager)
  player/                  — Player character, stats, inventory, weapons, archetypes
  combat/                  — Combat system, enemies, abilities, damage numbers
  modifiers/               — Player modifier/stat math (Modifier, ModifierEffect, ModifierGenerator, etc.)
  monster_effects/         — Enemy effect system (definitions, generator, instances, tags, roll contexts)
  world/                   — Game scene controller, map generation, pickups, camera, room profiles
  ui/                      — All screen-level UI scripts (menus, overlays, history)
  dev/                     — Developer tools, god mode
plans/                     — development roadmap docs
docs/ai/                   — architecture and design documentation
```

Scene files reference scripts via `res://scripts/<subfolder>/ScriptName.cs`.

## Build & Run

- Build: `dotnet build` from project root
- Run: open in Godot Editor → F5
- Main scene: `res://scenes/MainMenu.tscn`

## Input Map (project.godot)

| Action | Key |
|--------|-----|
| `move_forward` | W |
| `move_back` | S |
| `move_left` | A |
| `move_right` | D |
| `attack` | E |
| `ability` | Q |

Add new actions to `project.godot` and reference them via `GameKeys` constants.

## Common Gotchas

- After creating new `.cs` files, rebuild the solution before attaching to nodes
- Godot uses its own `Vector3`/`Transform3D` types, not `System.Numerics`
- `_Ready()` ≈ Unity's `Start()`, `_Process()` ≈ `Update()`
- Node names in scene tree are PascalCase by convention
- `CharacterBody3D.MoveAndSlide()` uses the `Velocity` property — set it before calling
- Signals in C#: `[Signal] public delegate void MySignalEventHandler();`
