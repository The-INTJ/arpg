# Asset Pipeline

This doc defines how 3D assets should be created, organized, imported, wrapped, and used in this repo.

The goal is not to build a heavyweight studio pipeline yet. The goal is to keep assets:

- sliceable
- replaceable
- readable in code and scenes
- safe for gameplay and physics
- consistent with the current Godot-first workflow

## Current Repo Posture

Right now the project favors:

- primitive-mesh environment geometry
- procedural or lightweight materials
- scene-authored reusable world slices under `scenes/world_slices/`
- procedural sprite-based character visuals

That means the default pipeline should still be:

1. block out in Godot
2. convert repeated pieces into slices
3. only move into Blender when silhouette or reuse justifies it

Do not jump straight from an idea to a large imported mesh library.

## Core Ownership Model

Every gameplay-facing asset should have a clear ownership chain:

1. source asset ownership
2. runtime asset ownership
3. wrapper scene ownership
4. builder/runtime placement ownership

### 1. Source Asset Ownership

The DCC source file is the editable master:

- `.blend` for 3D meshes
- layered texture files such as `.kra` or `.aseprite`
- `.svg` for UI ornaments

This is the file humans return to when changing the actual art.

### 2. Runtime Asset Ownership

The exported runtime asset is what Godot imports:

- `.glb` for 3D meshes
- `.png` for textures
- `.svg` where vector import makes sense

This file is a build artifact in spirit, but it is still committed because Godot consumes it directly.

### 3. Wrapper Scene Ownership

The wrapper `.tscn` is the gameplay-facing asset. It owns:

- collision
- scripts
- anchor markers
- material overrides
- local lights
- VFX children
- editor-tweakable structure

Game code and builders should prefer referencing the wrapper scene, not the raw imported mesh.

### 4. Builder And Runtime Placement Ownership

Builders and runtime systems own:

- where an asset is placed
- when it appears
- what gameplay logic uses it
- how often it spawns

Builders should not need to know about a mesh's internal child structure beyond the wrapper scene's public contract.

## Physics And Visual Ownership Rule

This is a hard rule for the repo:

- physics owns visuals
- visuals do not own physics

For actors and interactables, the collision body must be the gameplay root:

- `CharacterBody3D`
- `StaticBody3D`
- `Area3D`

Visual nodes must live under a separate visual child such as `VisualRoot`.

Correct shape:

```text
Player (CharacterBody3D)
- CollisionShape3D
- VisualRoot (Node3D)
  - HeroModelRoot
    - MeshInstance3D / Skeleton3D / Sprite3D
```

Unsafe shape:

```text
HeroModelRoot (Node3D or Sprite3D)
- MeshInstance3D
- CollisionShape3D
```

Why this matters:

- you must be able to mirror, rotate, scale, or tween the visual independently
- multiplying the visual by `-1` to flip facing must not affect physics
- collision and movement must not distort because of art-facing changes
- physics bodies should remain authoritative for gameplay

When reviewing or writing code, always call out any scene or script where the collision mesh or shape is parented under the visual/sprite/graphic side of the hierarchy.

If a future 3D player model needs left-right flipping or facing changes, do it on the visual subtree only. Do not flip the physics root.

## Tools

Use these tools by default:

- `Godot` for blockout, wrapper scenes, collisions, anchors, quick gameplay iteration
- `Blender` for low-poly environment assets, props, landmarks, and the first true 3D character experiment
- `Aseprite` or `Krita` for textures, decals, and sprite or UI-support art
- `Inkscape` or `Figma` for SVG UI art

Use these tools only if the need appears:

- `Material Maker` for reusable procedural textures
- `Blockbench` for especially simple stylized props if that workflow is faster than Blender

Preferred interchange format:

- use `glb`
- avoid `fbx` unless forced by a third-party asset source

## Folder Layout

Recommended structure:

```text
assets/
- source/
  - blender/
  - textures/
  - ui/
- models/
  - characters/
  - environment/
  - props/
- materials/
  - world/
  - characters/
  - props/
- textures/
  - world/
  - characters/
  - props/
  - fx/
  - ui/
- environments/

scenes/
- world_slices/
- props/
- actors/
```

Notes:

- `assets/source/` holds editable source files
- `assets/models/` holds exported Godot-imported meshes such as `.glb`
- `scenes/world_slices/` holds reusable environment set pieces and authored geometry contracts
- `scenes/actors/` is the natural home for future reusable player or enemy presentation scenes
- `scenes/props/` is for reusable interactables or standalone placed props that are not room slices

## Naming

Keep names boring and obvious.

Examples:

- source: `player_fighter_a_src.blend`
- export: `player_fighter_a.glb`
- wrapper: `PlayerFighterA.tscn`
- material: `fighter_cloth_a.tres`

Avoid names that only make sense to the person who authored them.

## Materials And Palette

The project already has a strong styling anchor in `Palette`.

Preferred material strategy:

- use shared Godot materials or material factories where possible
- keep colors palette-driven
- prefer simple, stylized surfaces over detailed realism
- use texture detail to support form, not replace silhouette

For imported meshes:

- do not rely on random embedded colors from Blender if the asset should follow the repo palette
- prefer assigning or overriding materials in Godot
- where practical, use grayscale or mask textures that can be tinted in-engine

## Slice And Prop Workflow

For new environment content:

1. block out the footprint in Godot using primitive meshes
2. if it becomes reusable, make it a scene under `scenes/world_slices/` or `scenes/props/`
3. add collisions in the wrapper scene
4. add `SceneSliceAnchor` markers when gameplay needs positions back out of the scene
5. if the primitive version is visually too weak, replace only the visual child with an imported mesh

This keeps the gameplay contract stable while the art changes.

Do not push gameplay rules into the slice scene just because the geometry moved there.

## Import Workflow For Blender Assets

1. Author the mesh in Blender.
2. Apply transforms before export.
3. Export to `glb`.
4. Import into Godot.
5. Create a wrapper scene that instances the imported mesh.
6. Add collision, anchors, scripts, and material overrides in the wrapper.
7. Reference the wrapper scene from code, not the raw `glb`.

Keep scales consistent with Godot world units. The repo already works in meter-like dimensions, so imported assets should match that expectation.

## Collision Workflow

Collision should usually be simple and authored separately from visual detail.

Preferred order:

1. box, capsule, sphere, or cylinder collision
2. a small set of simple composed shapes
3. mesh-derived collision only when truly necessary

Reasons:

- it is easier to debug
- it is safer for gameplay
- it avoids art changes silently changing physics

If the visual mesh changes, collision should usually remain stable unless gameplay footprint truly changed.

## Character Pipeline Direction

The repo currently uses procedural sprites for characters. That is still valid.

For the first experiment replacing the main player sprite with a true 3D asset, use this path:

1. keep `Player` as the `CharacterBody3D` root
2. keep `PlayerCollision` as a sibling of the visual subtree
3. add a `VisualRoot` child under the player body
4. put the imported Blender character scene under `VisualRoot`
5. do visual-facing changes by rotating or mirroring `VisualRoot` or a child under it
6. do not parent collision under the imported character model

Target scene shape:

```text
Player (CharacterBody3D)
- PlayerCollision
- CameraRig
- VisualRoot
  - HeroModelRoot
    - Skeleton3D
    - MeshInstance3D
    - WeaponVisualRoot
```

That setup lets us swap:

- sprite character -> 3D model
- one 3D model -> another 3D model
- one weapon visual -> another

without rewriting movement or physics.

## Replaceable Asset Strategy

To keep assets replaceable:

- keep gameplay roots stable
- keep wrapper scene names stable
- keep placement contracts stable
- swap visuals inside wrapper scenes rather than changing external callers

Examples:

- a tree slice can start as primitive Godot meshes and later swap to a Blender-authored tree mesh
- the player can start with a sprite child and later swap to a Blender character model under the same `VisualRoot`
- a cave slice can keep the same anchors while its meshes change completely

## What AI Can Help With

AI is useful for:

- concept sheets and moodboard language
- prop family planning
- drafting naming conventions
- generating wrapper scenes and support scripts
- setting up anchors and scene contracts
- reviewing assets for ownership mistakes
- writing import checklists and validation docs

AI should not be trusted alone for:

- final mesh quality
- topology decisions that matter for animation
- exact collision feel
- final style consistency without human review

## Review Checklist

When adding or reviewing an asset, check:

- Is there a clear source file?
- Is the runtime export in the right folder?
- Is there a wrapper scene?
- Does code reference the wrapper scene instead of the raw asset?
- Does the physics root own the visuals?
- Can the visual be mirrored or replaced without changing collision?
- Are anchors present where gameplay needs them?
- Are materials consistent with the project palette and world style?
- Is collision simple and intentional?
