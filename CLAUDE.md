# ARPG - Godot 4.6 .NET/C# Prototype

## What This Project Is
A turn-based ARPG prototype built with Godot 4.6 and C# (.NET 8.0). MVP game loop is complete — now in active feature development.

## Key Rules
- This is Godot 4.6 with C# (.NET 8.0)
- Use Godot's Node/Scene system as intended — nodes own their behavior, scenes own their composition
- Keep scripts focused and under ~150 lines — split if growing beyond that
- Project must be runnable after every change
- Use primitive meshes only (BoxMesh, CapsuleMesh, PlaneMesh, SphereMesh)
- Hardcode gameplay values directly in scripts (no config files or data-driven content yet)

## Godot C# Patterns to Follow
- Scripts attach to nodes in scenes (one script per node that needs behavior)
- All C# classes must be `partial` (e.g., `public partial class Enemy : Node3D`)
- Use `[Export]` for values you want tunable in the editor
- Movement goes in `_PhysicsProcess(double delta)` using `MoveAndSlide()`
- Scene changes: `GetTree().ChangeSceneToFile("res://scenes/SceneName.tscn")`
- Signal connections: prefer connecting in the editor or `Connect()` in `_Ready()`
- Node references: `GetNode<Type>("path")` or `[Export] private NodePath`

## Shared Systems
- **Palette** (`scripts/Palette.cs`): All colors live here. Vibrant earth-tone palette. Also has `StyleButton()` for consistent UI button styling. Every material/color in the game should reference Palette.
- **GameKeys** (`scripts/GameKeys.cs`): Key binding display names. Actions are defined in project.godot InputMap; `GameKeys.DisplayName(action)` reads the actual bound key at runtime. Change a key in one place (project.godot) and the UI updates everywhere.

## File Layout
- Scenes: `scenes/` (MainMenu.tscn, Game.tscn, VictoryScreen.tscn)
- Scripts: `scripts/`
- Scene files reference scripts via `res://scripts/ScriptName.cs`

## Build & Run
- Build: `dotnet build` from project root
- Run: open in Godot Editor → F5 (or set main scene to MainMenu.tscn)
- Main scene: `res://scenes/MainMenu.tscn`

## Input Map (project.godot)
- `move_forward` → W
- `move_back` → S
- `move_left` → A
- `move_right` → D
- `attack` → E

## Common Gotchas
- After creating new .cs files, rebuild the solution before attaching to nodes
- Godot uses its own Vector3/Transform3D types, not System.Numerics
- `_Ready()` is like Start(), `_Process()` is like Update() (Unity equivalents)
- Node names in scene tree are PascalCase by convention
- `CharacterBody3D.MoveAndSlide()` uses the `Velocity` property — set it before calling
- Export variables: `[Export] public float Speed = 5.0f;`
- Signals in C#: use `[Signal] public delegate void MySignalEventHandler();`
- Dynamic node creation: use `new Enemy()` directly so the C# type is correct, not `SetScript()`
