import json
import os

import bpy
from mathutils import Vector


def world_bounds(objects):
    points = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        return None
    minimum = Vector((
        min(point.x for point in points),
        min(point.y for point in points),
        min(point.z for point in points),
    ))
    maximum = Vector((
        max(point.x for point in points),
        max(point.y for point in points),
        max(point.z for point in points),
    ))
    return {
        "minimum": list(minimum),
        "maximum": list(maximum),
        "size": list(maximum - minimum),
        "center": list((minimum + maximum) * 0.5),
    }


exportable = [
    obj for obj in bpy.context.scene.objects
    if obj.type in {"MESH", "EMPTY", "ARMATURE"}
]

report = {
    "scene": bpy.context.scene.name,
    "objects": [],
    "materials": [],
    "images": [],
    "bounds": world_bounds(exportable),
}

for material in bpy.data.materials:
    images = []
    if material.use_nodes and material.node_tree:
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image is not None:
                images.append(node.image.name)
    report["materials"].append({
        "name": material.name,
        "images": images,
        "diffuse_color": list(material.diffuse_color),
        "metallic": material.metallic,
        "roughness": material.roughness,
    })
report["materials"].sort(key=lambda item: item["name"])

for obj in exportable:
    report["objects"].append({
        "name": obj.name,
        "type": obj.type,
        "location": list(obj.location),
        "rotation_euler_degrees": [
            value * 57.29577951308232 for value in obj.rotation_euler
        ],
        "scale": list(obj.scale),
        "dimensions": list(obj.dimensions),
        "parent": obj.parent.name if obj.parent else None,
        "materials": [
            slot.material.name if slot.material else None
            for slot in obj.material_slots
        ],
    })

for image in bpy.data.images:
    report["images"].append({
        "name": image.name,
        "size": list(image.size),
        "packed": image.packed_file is not None,
        "filepath": image.filepath,
    })

print("ALGOLAB_MONITOR_REPORT_BEGIN")
print(json.dumps(report, indent=2, ensure_ascii=False))
print("ALGOLAB_MONITOR_REPORT_END")

texture_output_dir = os.environ.get("ALGOLAB_MONITOR_TEXTURE_OUTPUT_DIR")
if texture_output_dir:
    os.makedirs(texture_output_dir, exist_ok=True)
    for image in bpy.data.images:
        if image.packed_file is None or image.size[0] <= 0 or image.size[1] <= 0:
            continue
        original_path = image.filepath_raw
        original_format = image.file_format
        safe_name = "".join(
            character if character.isalnum() or character in "-_" else "_"
            for character in image.name
        )
        image.filepath_raw = os.path.join(texture_output_dir, safe_name + ".png")
        image.file_format = "PNG"
        image.save()
        image.filepath_raw = original_path
        image.file_format = original_format
        print("ALGOLAB_MONITOR_TEXTURE=" + safe_name + ".png")

output_path = os.environ.get("ALGOLAB_MONITOR_FBX_OUTPUT")
if output_path:
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for obj in exportable:
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.hide_render = False
        obj.select_set(True)

    if exportable:
        bpy.context.view_layer.objects.active = exportable[0]

    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"MESH", "EMPTY", "ARMATURE"},
        use_mesh_modifiers=True,
        use_triangles=True,
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        path_mode="COPY",
        embed_textures=True,
    )
    print("ALGOLAB_MONITOR_EXPORTED=" + output_path)
