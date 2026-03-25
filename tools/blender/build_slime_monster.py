from __future__ import annotations

from pathlib import Path
import bmesh
import bpy
import math


REPO_ROOT = Path(__file__).resolve().parents[2]
SOURCE_BLEND_PATH = REPO_ROOT / "assets" / "source" / "blender" / "slime_monster_src.blend"
PREVIEW_PATH = REPO_ROOT / "assets" / "source" / "blender" / "slime_monster_preview.png"
EXPORT_GLB_PATH = REPO_ROOT / "assets" / "models" / "characters" / "slime_monster.glb"


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.outliner.orphans_purge(do_recursive=True)


def ensure_parent_dirs() -> None:
    SOURCE_BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
    EXPORT_GLB_PATH.parent.mkdir(parents=True, exist_ok=True)


def set_object_mode() -> None:
    if bpy.context.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")


def create_preview_floor() -> None:
    bpy.ops.mesh.primitive_plane_add(size=5.5, location=(0.0, 0.0, 0.0))
    floor = bpy.context.active_object
    floor.name = "PreviewFloor"
    floor_mat = bpy.data.materials.new("PreviewFloorMaterial")
    floor_mat.use_nodes = True
    bsdf = floor_mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.18, 0.14, 0.12, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.92
    floor.data.materials.append(floor_mat)


def add_meta_element(meta: bpy.types.MetaBall, co: tuple[float, float, float], radius: float) -> None:
    element = meta.elements.new(type="BALL")
    element.co = co
    element.radius = radius
    element.stiffness = 2.2


def create_slime_body() -> bpy.types.Object:
    bpy.ops.object.metaball_add(type="BALL", radius=0.68, location=(0.0, 0.0, 0.52))
    body = bpy.context.active_object
    body.name = "SlimeBody"
    meta = body.data
    meta.resolution = 0.12
    meta.render_resolution = 0.08
    body.scale = (1.18, 1.02, 0.76)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    add_meta_element(meta, (0.0, 0.02, 0.58), 0.15)
    add_meta_element(meta, (-0.26, -0.08, 0.18), 0.13)
    add_meta_element(meta, (0.24, -0.16, 0.12), 0.1)

    set_object_mode()
    bpy.ops.object.convert(target="MESH")
    body = bpy.context.active_object
    body.name = "SlimeBody"

    bm = bmesh.new()
    bm.from_mesh(body.data)
    for vert in bm.verts:
        if vert.co.z < 0.18:
            flatten_strength = (0.18 - vert.co.z) / 0.18
            vert.co.z = max(0.03, vert.co.z * 0.24)
            spread = 1.0 + flatten_strength * 0.18
            vert.co.x *= spread
            vert.co.y *= spread

        if vert.co.y > 0.0 and vert.co.z > 0.24:
            vert.co.z += 0.08 * min(vert.co.y / 0.75, 1.0)

        if abs(vert.co.x) < 0.16 and vert.co.y > 0.18 and vert.co.z > 0.58:
            vert.co.z += 0.05

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(body.data)
    bm.free()

    body.data.update()
    bpy.ops.object.shade_smooth()

    subsurf = body.modifiers.new("SlimeSmooth", type="SUBSURF")
    subsurf.levels = 1
    subsurf.render_levels = 1
    return body


def create_slime_core() -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, radius=0.2, location=(0.0, 0.02, 0.56))
    core = bpy.context.active_object
    core.name = "SlimeCore"
    core.scale = (0.95, 0.8, 0.8)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.shade_smooth()
    return core


def create_slime_eyes() -> bpy.types.Object:
    eye_positions = [(-0.14, 0.62, 0.53), (0.14, 0.62, 0.5)]
    eye_objects: list[bpy.types.Object] = []
    for index, position in enumerate(eye_positions):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.09, location=position)
        eye = bpy.context.active_object
        eye.name = f"SlimeEye_{index + 1}"
        eye.scale = (0.7, 0.34, 0.94)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        bpy.ops.object.shade_smooth()
        eye_objects.append(eye)

    bpy.ops.object.select_all(action="DESELECT")
    for eye in eye_objects:
        eye.select_set(True)
    bpy.context.view_layer.objects.active = eye_objects[0]
    bpy.ops.object.join()
    eyes = bpy.context.active_object
    eyes.name = "SlimeEyes"
    return eyes


def assign_placeholder_materials(body: bpy.types.Object, core: bpy.types.Object, eyes: bpy.types.Object) -> None:
    specs = (
        (body, "BodyMaterial", (0.28, 0.68, 0.32, 1.0), 0.28, 0.0),
        (core, "CoreMaterial", (0.82, 0.93, 0.46, 1.0), 0.18, 1.3),
        (eyes, "EyeMaterial", (0.10, 0.16, 0.08, 1.0), 0.42, 0.1),
    )
    for obj, material_name, color, roughness, emission_strength in specs:
        material = bpy.data.materials.new(material_name)
        material.use_nodes = True
        bsdf = material.node_tree.nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        if emission_strength > 0.0:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emission_strength
        obj.data.materials.clear()
        obj.data.materials.append(material)


def setup_preview(camera_target: bpy.types.Object) -> None:
    bpy.ops.object.light_add(type="SUN", location=(2.4, -1.8, 4.6))
    sun = bpy.context.active_object
    sun.data.energy = 2.5
    sun.rotation_euler = (math.radians(46.0), 0.0, math.radians(35.0))

    bpy.ops.object.light_add(type="AREA", location=(-1.8, -2.2, 1.8))
    rim = bpy.context.active_object
    rim.data.energy = 1800
    rim.data.shape = "RECTANGLE"
    rim.data.size = 3.2
    rim.data.size_y = 1.8
    rim.rotation_euler = (math.radians(78.0), 0.0, math.radians(-28.0))

    bpy.ops.object.camera_add(location=(2.8, -3.0, 1.9))
    camera = bpy.context.active_object
    camera.name = "PreviewCamera"
    constraint = camera.constraints.new(type="TRACK_TO")
    constraint.target = camera_target
    constraint.track_axis = "TRACK_NEGATIVE_Z"
    constraint.up_axis = "UP_Y"

    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "CYCLES"
    scene.render.filepath = str(PREVIEW_PATH)
    scene.render.image_settings.file_format = "PNG"
    scene.render.resolution_x = 1080
    scene.render.resolution_y = 1080
    scene.cycles.samples = 32


def export_glb(objects: list[bpy.types.Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_GLB_PATH),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_materials="EXPORT",
        export_cameras=False,
        export_lights=False,
    )


def main() -> None:
    ensure_parent_dirs()
    clear_scene()
    create_preview_floor()

    body = create_slime_body()
    core = create_slime_core()
    eyes = create_slime_eyes()
    assign_placeholder_materials(body, core, eyes)
    setup_preview(body)

    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_BLEND_PATH))
    bpy.ops.render.render(write_still=True)
    export_glb([body, core, eyes])
    print(f"Saved source blend to {SOURCE_BLEND_PATH}")
    print(f"Rendered preview to {PREVIEW_PATH}")
    print(f"Exported GLB to {EXPORT_GLB_PATH}")


if __name__ == "__main__":
    main()
