# Project Setup Instructions

Execute these steps to prepare the Godot 4.6 .NET/C# project for development. The Godot project already exists at this directory with `project.godot` configured.

## Prerequisites to Verify

### 1. .NET SDK
The project requires .NET SDK 8.0+ (Godot 4.6 .NET build requires this).

Run:
```bash
dotnet --version
```

If not installed or below 8.0, download from https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Godot .NET Build
Confirm the user has the **Godot .NET build** (not the standard build). The .NET build is a separate download — the filename includes `mono` or `.NET`. The standard Godot executable cannot run C# scripts.

Check: The Godot executable name should contain "mono" or ".NET" (e.g., `Godot_v4.6-stable_mono_win64.exe`).

### 3. C# Solution File
If no `.sln` or `.csproj` exists yet, the user needs to open the project in Godot Editor and build once (Build → Build Solution or just open a .cs file) to generate the solution. Alternatively, create it manually:

Check if `*.csproj` exists in the project root. If not, create `ARPG.csproj`:

```xml
<Project Sdk="Godot.NET.Sdk/4.6.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RootNamespace>ARPG</RootNamespace>
  </PropertyGroup>
</Project>
```

Then run `dotnet restore` to verify it resolves.

**Note**: The exact Godot.NET.Sdk version should match the installed Godot version. Check the Godot release notes if `4.6.0` doesn't resolve — it may be `4.6.0-beta.1` or similar during preview periods.

## Directory Structure to Create

Create these directories:

```
scenes/
scripts/
```

## Files to Create

### CLAUDE.md (Project Guidance)

Create `CLAUDE.md` in the project root with this content:

```markdown
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
```

### .gitignore Updates

Append to the existing `.gitignore` if these entries are missing:

```
# Godot
.godot/

# .NET
bin/
obj/
*.user
*.suo
```

### Input Map Setup Note

The project needs input actions configured. Add this note — the implementing model should add input actions to `project.godot` under `[input]`:

Required input actions:
- `move_forward` → W key
- `move_back` → S key
- `move_left` → A key
- `move_right` → D key
- `attack` → Space key (backup for UI button)

These go in `project.godot` under `[input]` section. Format:
```ini
[input]

move_forward={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":87,"key_label":0,"unicode":119,"location":0,"echo":false,"script":null)
]
}
move_back={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":83,"key_label":0,"unicode":115,"location":0,"echo":false,"script":null)
]
}
move_left={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":65,"key_label":0,"unicode":97,"location":0,"echo":false,"script":null)
]
}
move_right={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":68,"key_label":0,"unicode":100,"location":0,"echo":false,"script":null)
]
}
attack={
"deadzone": 0.2,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":0,"physical_keycode":32,"key_label":0,"unicode":32,"location":0,"echo":false,"script":null)
]
}
```

## Summary Checklist

After setup, the following should be true:
- [ ] `dotnet --version` returns 8.0+
- [ ] `ARPG.csproj` exists and `dotnet restore` succeeds
- [ ] `scenes/` and `scripts/` directories exist
- [ ] `CLAUDE.md` exists with project guidance
- [ ] `.gitignore` covers `.godot/`, `bin/`, `obj/`
- [ ] Input actions added to `project.godot`
- [ ] `dotnet build` succeeds with no errors
