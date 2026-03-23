# Godot Concepts & Debugging Guide

## The Scene Tree — How Godot Organizes Everything

Everything in Godot is a **Node**. Nodes live in a tree. When you press F5, Godot loads the main scene and builds a tree from it.

You can see this tree **live at runtime**:
1. Run the game (F5)
2. In the editor, click the **"Remote"** tab at the top of the Scene panel (left side)
3. You'll see every node that exists right now, including dynamically-spawned ones

This is the single most useful debugging view. If enemies aren't showing up, check Remote to see if they exist in the tree at all.

## Node Types That Matter For This Project

| Type | What It Is | When To Use |
|------|-----------|-------------|
| `Node3D` | Base 3D node, just a transform (position/rotation/scale) | Containers, groups, empty parents |
| `MeshInstance3D` | Renders a 3D shape | Anything visible (floor, walls, characters) |
| `StaticBody3D` | Physics body that doesn't move | Walls, floor, enemies (they don't walk) |
| `CharacterBody3D` | Physics body you move with code | Player (has `MoveAndSlide()`) |
| `Area3D` | Detects overlap, no physics collision | Exit door trigger, proximity detection |
| `CollisionShape3D` | Defines the shape for physics | MUST be a child of a body/area to work |
| `Camera3D` | The viewpoint | One must be active or you see nothing |
| `CanvasLayer` | 2D layer on top of 3D | HUD elements live here |

## How Visibility Works

A node is visible if:
1. It has a mesh (or is a Control node for 2D)
2. Its `Visible` property is `true`
3. It's in the scene tree (has been `AddChild()`'d)
4. It's within camera view

**Common invisibility causes:**
- Position is (0,0,0) and the camera isn't looking there
- The node exists but has no MeshInstance3D child
- The mesh is tiny (default BoxMesh is 1x1x1 meter — visible but small)
- Object is below the floor (Y position too low)

## How Physics/Collision Works

A `StaticBody3D` or `CharacterBody3D` needs a `CollisionShape3D` child with a `shape` assigned. Without it:
- The body exists but has no physical presence
- Other bodies pass right through it
- Area3D won't detect it

**Debug collision shapes visually:**
Menu → Debug → Visible Collision Shapes (toggle this ON)

This draws wireframes around all collision shapes. Incredibly useful.

## Where To Look When Things Go Wrong

### "I can't see X"
1. **Remote tab** → Is the node in the tree?
2. Click the node in Remote → **Inspector panel** (right side) → check `Position` values
3. Does it have a MeshInstance3D child? Is Visible = true?
4. Is it behind the camera? Check camera position/rotation

### "Collision doesn't work"
1. Debug → Visible Collision Shapes → are shapes where you expect?
2. Does the body have a CollisionShape3D child?
3. Does that CollisionShape3D have a `shape` property set?
4. For Area3D: are the collision layers/masks compatible?

### "Script doesn't run"
1. Is the script attached to the node? (Inspector → Script property)
2. Did you rebuild? (Build → Build Solution, or `dotnet build`)
3. Check the **Output** panel at the bottom for errors (red text)
4. Check the **Debugger** panel → Errors tab

### "Signal not working"
1. Is the signal connected? Check Node → Signals tab in the editor
2. For code connections: is `_Ready()` actually running? Add `GD.Print("ready")` to verify
3. For Area3D.BodyEntered: the entering node must be a physics body (CharacterBody3D, etc.), not a plain Node3D

## The Output Panel

Bottom of the editor. Three critical things here:
- **Output**: `GD.Print()` statements go here. Use liberally for debugging
- **Debugger → Errors**: Runtime exceptions, null references, missing nodes
- **Debugger → Stack Frames**: When an error occurs, click to see where

### Useful Debug Prints

```csharp
// In _Ready(), verify the node loaded
GD.Print($"GameManager ready, player at {_player.GlobalPosition}");

// Check if enemies exist
GD.Print($"Enemies in group: {GetTree().GetNodesInGroup("enemies").Count}");

// In _Process(), check distances (remove after debugging — runs every frame)
GD.Print($"Nearest enemy dist: {dist}");
```

## Inspector Panel Tricks

Click any node in the Scene or Remote panel → Inspector shows all its properties.

Key things to check:
- **Transform → Position**: where is this node in 3D space?
- **Visible**: is it rendering?
- **Script**: is a script attached?
- **Groups**: is the node in the right group? (Node → Groups tab)

## Camera Debugging

If you see a gray/black screen:
1. Is there a Camera3D in the scene?
2. Is its `Current` property `true`? (Only one camera can be current)
3. In our project: camera is a child of the Player. If Player is at (0,1,0) and camera offset is (0,8,8) looking down at 45°, the camera is at (0,9,8) looking toward the player.
4. Try clicking the camera in Remote and checking its `Global Transform`

## The Godot Coordinate System

- **X** = left/right (positive = right)
- **Y** = up/down (positive = up)
- **Z** = forward/back (positive = toward camera in default view, i.e., "toward you")
- The floor is at Y=0. Objects sitting on the floor have their center at Y = half their height

In our project:
- Player starts at (0, 1, 5) — center of the map, slightly south
- Exit door is at (0, 0, -9) — far north edge
- Enemies spawn at Y=0.5 — their center is at floor level + half height

## Dynamic Node Creation (What Broke The Enemies)

In Godot C#, when you create nodes in code:

```csharp
// WRONG — creates a StaticBody3D, SetScript doesn't change the C# type
var node = new StaticBody3D();
node.SetScript(GD.Load<CSharpScript>("res://scripts/Enemy.cs"));
// node is Enemy → FALSE! Still a StaticBody3D in C#

// RIGHT — use the actual C# class constructor
var enemy = new Enemy();
// enemy is Enemy → TRUE!
```

`SetScript()` changes the *Godot* script but doesn't change the *C#* type. So `node is Enemy` returns false, and all the code that does `if (node is Enemy enemy)` silently skips it. The enemies exist in the tree but the game can't interact with them.

## Quick Reference: Running & Building

| Action | How |
|--------|-----|
| Run game | F5 (or ▶ button) |
| Stop game | F8 (or ■ button) |
| Build C# | Build → Build Solution (or `dotnet build` in terminal) |
| See runtime tree | Run game → Remote tab |
| See collision shapes | Debug → Visible Collision Shapes |
| See prints/errors | Output panel (bottom) |
