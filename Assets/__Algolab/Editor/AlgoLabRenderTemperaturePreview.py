import os
import math

import bpy
from mathutils import Vector


def scene_bounds():
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        raise RuntimeError("No hay mallas para encuadrar.")
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return (minimum + maximum) * 0.5, maximum - minimum


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


output_dir = os.environ.get("ALGOLAB_TEMPERATURE_PREVIEW_DIR")
if not output_dir:
    raise RuntimeError("Falta ALGOLAB_TEMPERATURE_PREVIEW_DIR.")
os.makedirs(output_dir, exist_ok=True)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.world.color = (0.035, 0.045, 0.06)

center, size = scene_bounds()
maximum = max(size)

camera_data = bpy.data.cameras.new("PreviewCamera")
camera = bpy.data.objects.new("PreviewCamera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.lens = 55

light_data = bpy.data.lights.new("PreviewKey", "AREA")
light_data.energy = 900
light_data.shape = "DISK"
light_data.size = maximum * 2
light = bpy.data.objects.new("PreviewKey", light_data)
scene.collection.objects.link(light)
light.location = center + Vector((-maximum, -maximum, maximum * 1.6))
look_at(light, center)

views = {
    "front_y": Vector((0, -1, 0)),
    "front_z": Vector((0, 0, -1)),
    "side_x": Vector((-1, 0, 0)),
}
for name, direction in views.items():
    camera.location = center + direction * maximum * 2.25
    look_at(camera, center)
    scene.render.filepath = os.path.join(output_dir, f"temperature_{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"ALGOLAB_TEMPERATURE_PREVIEW={scene.render.filepath}")
