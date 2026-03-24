# ARPG - Godot 4.6 .NET/C#

## What This Project Is

A tactical roguelike RPG with Baldur's Gate–style turn-based combat and a player-driven modifier system. Built with Godot 4.6 and C# (.NET 8.0). See `GAME_IDEA.md` for the full vision.

The MVP game loop is complete (menu → explore → fight → win). We are now building real systems on top of that foundation.

## Development Philosophy

**Build foundationally and intentionally.** Every system should be simple today but designed so it can grow without being ripped out and rewritten. This means:

- Write clean, well-structured code that does what's needed *now* without being so rigid it blocks what's needed *next*
- When a system will clearly need to support multiplayer, multiple archetypes, or modifier stacking later — design the data structures with that in mind, even if the first implementation is single-player/single-archetype
- Prefer Godot-native patterns over custom engines. Use nodes, scenes, signals, and groups as Godot intends
- Avoid premature abstraction, but don't avoid *appropriate* abstraction. If three scripts are doing the same thing, that's a sign to extract — not a sign to keep copying
- Keep scripts focused. If one grows past ~150 lines, consider whether it's doing too many jobs

## Multiplayer Awareness

Multiplayer is a core consideration from day one. This doesn't mean every feature ships multiplayer immediately, but:

- Data structures should assume multiple players exist (e.g., turn order as a list, not a single player reference)
- Combat flow should not hardcode "the player" as a singleton concept
- Nothing should be designed in a way that makes multiplayer painful to retrofit

## Technical Rules

- **Engine**: Godot 4.6, C# (.NET 8.0)
- **All C# classes must be `partial`** (Godot 4.x requirement)
- **Project must be runnable after every change** — don't leave it broken between commits
- **Primitive meshes + procedural sprites** — characters use `SpriteFactory` pixel art; environment uses primitive meshes
- **Hardcode gameplay values** in scripts for now — data-driven content comes later when we have enough systems to justify it
- **Prefer scene-authored UI** — AI contributors should update or add `.tscn` files for UI by default, and only build UI in code when that is clearly the Godot best-practice solution for the task
- **Prefer scene-authored world slices for reusable environment pieces** — if a room feature is meant to be edited in the Godot editor and reused, author it as a `.tscn` under `scenes/world_slices/` and have builders instance it instead of rebuilding the same geometry in C#
- **Physics/collision roots must own visuals, never the reverse** — for actors and interactables, keep `CharacterBody3D` / `StaticBody3D` / `Area3D` as the gameplay root, keep collision shapes under that root, and put meshes/sprites under a separate visual child such as `VisualRoot`. If you see code or a scene where the visual owns the collision, call it out explicitly. Flipping or mirroring the visual by `-1` must not affect physics.

## Godot C# Patterns

- Scripts attach to nodes in scenes (one script per node that needs behavior)
- `[Export]` for values tunable in the editor
- Movement in `_PhysicsProcess(double delta)` using `MoveAndSlide()`
- Scene changes: `GetTree().ChangeSceneToFile("res://scenes/SceneName.tscn")`
- Signal connections: prefer editor or `Connect()` in `_Ready()`
- Node references: `GetNode<Type>("path")` or `[Export] private NodePath`
- Dynamic node creation: use `new Enemy()` directly so the C# type is correct, not `SetScript()`
- For characters, props, pickups, and other gameplay-facing assets, treat the physics body as the owner and the visual subtree as replaceable presentation. Do not parent collision under sprites or imported model roots
- For menus, HUD, overlays, and reusable controls, prefer authored scenes over programmatic control trees even if the scene edit is more tedious
- For reusable world set pieces, prefer scene slices with `SceneSliceAnchor` markers so builders can place scenes and read back gameplay anchors without hardcoding the full geometry in C#

## Shared Systems

- **Palette** (`scripts/Palette.cs`): All colors and UI button styling. Vibrant earth-tone palette. Every material/color in the game should reference Palette — don't hardcode color values elsewhere.
- **GameKeys** (`scripts/GameKeys.cs`): Key binding display names. Actions defined in `project.godot` InputMap; `GameKeys.DisplayName(action)` reads the actual bound key at runtime. Change a key binding in one place (`project.godot`) and the UI updates everywhere.
- **GameState** (`scripts/GameState.cs`): Static state passed between scenes (e.g., selected archetype).
- **SpriteFactory** (`scripts/SpriteFactory.cs`): Generates procedural pixel-art textures for characters at runtime.
- **PlayerStats** (`scripts/PlayerStats.cs`): Base stats + modifier stack. Effective stats computed on the fly. Archetype sets base values; modifiers layer with `+N → +N% → ×M → −N%` ordering.
- **CombatManager** (`scripts/CombatManager.cs`): Handles combat flow — zoom in/out, attack/ability/retaliate exchange, damage numbers, camera shake.
- **ModifierGenerator** (`scripts/ModifierGenerator.cs`): Random modifier creation for loot drops.

## File Layout

```
scenes/          — .tscn scene files (MainMenu, ArchetypeSelect, Game, VictoryScreen, DamageNumber, etc.)
scenes/world_slices/ — reusable environment set pieces instanced by room builders
scripts/         — .cs script files
plans/           — development roadmap docs
```

Scene files reference scripts via `res://scripts/ScriptName.cs`.

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
