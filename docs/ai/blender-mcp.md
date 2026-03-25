# BlenderMCP Integration Guide

This doc teaches AI agents how to use the BlenderMCP server to create and export assets for this Godot project.

## Prerequisites

- Blender must be running with the BlenderMCP addon active (default port 9876)
- The MCP server named `blender` must be configured in Claude Code settings
- All tools below are prefixed `mcp__blender__` in the tool list

## Available Tools

### Scene Inspection

| Tool | Purpose |
|------|---------|
| `get_scene_summary()` | Object list, collections, render settings, frame range |
| `get_selected_objects()` | Transforms and materials of selected objects |
| `get_object_info(name)` | Deep info on one object (mesh stats, modifiers, constraints) |

### Execution

| Tool | Purpose |
|------|---------|
| `run_blender_python(code, timeout_seconds=30)` | Run arbitrary Python in Blender via `bpy` |
| `run_workflow(name, params)` | Run a named workflow with param overrides |
| `list_workflows()` | See available workflows and their parameters |

### File And Render

| Tool | Purpose |
|------|---------|
| `open_file(path)` | Open a `.blend` file |
| `save_file(path=None)` | Save current or to new path |
| `render_preview(output_path=None)` | Quick half-res preview render |
| `undo_last(n=1)` | Undo N operations |

## Mandatory Work Loop

Never script blind. Always follow this cycle:

1. `get_scene_summary()` — orient to current scene state
2. Make changes via `run_blender_python()` or `run_workflow()`
3. `render_preview()` — visually verify the result
4. Repeat until correct

Always call `get_scene_summary()` first so you know actual object names, types, and counts before writing `bpy` code.

## Godot Export Rules

This project uses `.glb` as the interchange format. See `docs/assets-pipeline.md` for full ownership and folder rules.

### glTF Export (Primary)

```python
import bpy
bpy.ops.export_scene.gltf(
    filepath="/path/to/output.glb",
    export_format="GLB",
    use_selection=True,
    export_apply=True,            # apply modifiers
    export_materials="EXPORT",
    export_cameras=False,
    export_lights=False,
)
```

Key settings:
- `export_format="GLB"` — single binary file, Godot imports natively
- `use_selection=True` — export only selected objects to avoid scene debris
- `export_apply=True` — bake modifiers so Godot gets final geometry
- `export_cameras=False`, `export_lights=False` — Godot owns these

### Export Path Conventions

Follow the asset pipeline folder layout:

| Asset type | Source path | Export path |
|------------|-------------|-------------|
| Environment mesh | `assets/source/blender/<name>_src.blend` | `assets/models/environment/<name>.glb` |
| Character mesh | `assets/source/blender/<name>_src.blend` | `assets/models/characters/<name>.glb` |
| Prop mesh | `assets/source/blender/<name>_src.blend` | `assets/models/props/<name>.glb` |
| Baked texture | (generated in Blender) | `assets/textures/<category>/<name>.png` |

### Before Export Checklist

Run this cleanup before every export:

```python
import bpy

# Apply all transforms
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Remove unused data blocks
bpy.ops.outliner.orphans_purge(do_recursive=True)
```

## Texture Baking Workflow

Blender can generate procedural textures and bake them to PNG for Godot. This is useful for tileable environment textures, material maps, or stylized surfaces.

### Basic Procedural Texture Bake

```python
import bpy

# Create a plane to bake onto
bpy.ops.mesh.primitive_plane_add(size=2)
plane = bpy.context.active_object
plane.name = "BakePlane"

# Create a new image for baking
img = bpy.data.images.new("baked_texture", width=512, height=512)

# Set up material with procedural nodes
mat = bpy.data.materials.new(name="ProceduralMat")
mat.use_nodes = True
nodes = mat.node_tree.nodes
links = mat.node_tree.links

# Clear defaults
for node in nodes:
    nodes.remove(node)

# Add nodes: texture coordinate → noise/voronoi/etc → principled BSDF → output
tex_coord = nodes.new('ShaderNodeTexCoord')
# ... add procedural texture nodes here ...
bsdf = nodes.new('ShaderNodeBsdfPrincipled')
output = nodes.new('ShaderNodeOutputMaterial')

# Add image texture node for bake target
bake_node = nodes.new('ShaderNodeTexImage')
bake_node.image = img
bake_node.select = True
nodes.active = bake_node

# Assign material
plane.data.materials.append(mat)

# Set render engine to Cycles for baking
bpy.context.scene.render.engine = 'CYCLES'
bpy.context.scene.cycles.bake_type = 'DIFFUSE'
bpy.context.scene.render.bake.use_pass_direct = False
bpy.context.scene.render.bake.use_pass_indirect = False
bpy.context.scene.render.bake.use_pass_color = True

# Bake
bpy.ops.object.bake(type='DIFFUSE')

# Save
img.filepath_raw = "/absolute/path/to/assets/textures/world/texture_name.png"
img.file_format = 'PNG'
img.save()
```

### Texture Guidelines For This Project

- Target 256x256 or 512x512 for environment tiles — the project uses a stylized look, not photorealism
- Use the project Palette colors when possible (see `scripts/core/Palette.cs` for hex values)
- Prefer simple, readable textures that support form over detail
- Save to `assets/textures/<category>/` with boring obvious names

## Asset Integration After Export

After exporting from Blender, the asset must be integrated into Godot following the ownership model:

1. **Source file** stays in `assets/source/blender/`
2. **Exported `.glb`** goes in `assets/models/<category>/`
3. **Baked textures** go in `assets/textures/<category>/`
4. **Wrapper `.tscn`** must be created in `scenes/` — game code references the wrapper, not the raw asset
5. **Collision** is added in the wrapper scene, not baked into the mesh
6. **Materials** should be overridden in Godot to match the project palette where needed

### Physics Owns Visuals — Hard Rule

When creating wrapper scenes for Blender assets:

```
Correct:
  StaticBody3D (or CharacterBody3D)
  ├── CollisionShape3D
  └── VisualRoot (Node3D)
      └── ImportedMeshRoot

Wrong:
  ImportedMeshRoot
  └── CollisionShape3D
```

The physics body is always the root. The imported mesh lives under a visual child. See `docs/assets-pipeline.md` for the full rule.

## Common Pitfalls

- **Forgetting `get_scene_summary()` first** — you will reference wrong object names
- **Not applying transforms** — Godot import will have wrong scale/rotation
- **Exporting cameras and lights** — these conflict with Godot's scene setup
- **Hardcoding colors in Blender materials** — use Palette-driven materials in Godot instead
- **Skipping the wrapper scene** — game code must never reference raw `.glb` files directly
- **Putting collision under the mesh** — violates the physics-owns-visuals rule
- **Large texture sizes** — this is a stylized game, 512x512 is usually plenty

## When To Use Blender vs Other Tools

| Task | Tool |
|------|------|
| Low-poly environment meshes | Blender |
| Props and landmarks | Blender |
| 3D character models | Blender |
| Hand-painted textures | Aseprite / Krita |
| Procedural tileable textures | Blender (bake) or Material Maker |
| UI art | Inkscape / Figma |
| Quick blockout geometry | Godot primitives |
| Collision shapes | Godot editor (in wrapper scene) |

## Workflow Reference Snippets

### Create And Export A Simple Prop

```python
import bpy

# Clear scene
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# Create prop geometry
bpy.ops.mesh.primitive_cylinder_add(radius=0.3, depth=1.0, location=(0, 0, 0.5))
prop = bpy.context.active_object
prop.name = "Barrel"

# Apply transforms
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Export
bpy.ops.export_scene.gltf(
    filepath="E:/Coding/Godot/arpg/assets/models/props/barrel.glb",
    export_format="GLB",
    use_selection=True,
    export_apply=True,
    export_materials="EXPORT",
    export_cameras=False,
    export_lights=False,
)
```

### Batch Material Setup With Palette Colors

```python
import bpy

# Project palette colors (from Palette.cs)
# Update these if the palette changes
PALETTE = {
    "stone": (0.376, 0.365, 0.345, 1.0),   # approximate — check Palette.cs
    "wood": (0.427, 0.294, 0.176, 1.0),
    "metal": (0.529, 0.529, 0.529, 1.0),
}

def create_palette_material(name, color_key):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and color_key in PALETTE:
        bsdf.inputs["Base Color"].default_value = PALETTE[color_key]
    return mat
```

Note: Palette colors in Blender are approximations. Final material authority lives in Godot via `Palette.cs`. Prefer overriding materials in Godot rather than relying on Blender colors for final look.
