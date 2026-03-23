# ARPG - Godot 4.6 .NET/C# Prototype

## What This Project Is
A minimal MVP game loop prototype. See MVP_SPEC.md for full specification.

## Key Rules
- This is Godot 4.6 with C# (.NET 8.0)
- Use Godot's Node/Scene system as intended — nodes own their behavior, scenes own their composition
- Do NOT create abstractions, interfaces, service layers, or patterns meant for scale
- Do NOT add features beyond what MVP_SPEC.md requests
- Hardcode all values (no config files, no data-driven content)
- Use primitive meshes only (BoxMesh, CapsuleMesh, PlaneMesh, SphereMesh)
- Keep every script under ~150 lines — if it's getting long, you're overbuilding
- Project must be runnable after every change

## Godot C# Patterns to Follow
- Scripts attach to nodes in scenes (one script per node that needs behavior)
- Use `[Export]` for values you want tunable in the editor
- Movement goes in `_PhysicsProcess(double delta)` using `MoveAndSlide()`
- Scene changes: `GetTree().ChangeSceneToFile("res://scenes/SceneName.tscn")`
- Signal connections: prefer connecting in the editor or `Connect()` in `_Ready()`
- Node references: `GetNode<Type>("path")` or `[Export] private NodePath`
- Use `partial class` keyword on all C# classes (Godot 4.x C# requirement)

## File Layout
- Scenes go in `scenes/` (MainMenu.tscn, Game.tscn, VictoryScreen.tscn)
- Scripts go in `scripts/`
- Scene files reference scripts via `res://scripts/ScriptName.cs`

## Build & Run
- Build: `dotnet build` from project root
- Run: open in Godot Editor → F5 (or set main scene to MainMenu.tscn)
- Main scene should be `res://scenes/MainMenu.tscn`

## Common Gotchas
- All C# classes must be `partial` (e.g., `public partial class Enemy : Node3D`)
- After creating new .cs files, rebuild the solution before attaching to nodes
- Godot uses its own Vector3/Transform3D types, not System.Numerics
- `_Ready()` is like Start(), `_Process()` is like Update() (Unity equivalents)
- Node names in scene tree are PascalCase by convention
- `CharacterBody3D.MoveAndSlide()` uses the `Velocity` property — set it before calling
- Export variables: `[Export] public float Speed = 5.0f;`
- Signals in C#: use `[Signal] public delegate void MySignalEventHandler();`
