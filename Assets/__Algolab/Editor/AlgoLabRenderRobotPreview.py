import os

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
    return (minimum + maximum) * 0.5, maximum - minimum


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


output_dir = os.environ.get("ALGOLAB_ROBOT_PREVIEW_DIR")
if not output_dir:
    raise RuntimeError("Falta ALGOLAB_ROBOT_PREVIEW_DIR.")
os.makedirs(output_dir, exist_ok=True)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 720
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world.color = (0.025, 0.035, 0.055)

center, size = scene_bounds()
maximum = max(size)

camera_data = bpy.data.cameras.new("RobotPreviewCamera")
camera = bpy.data.objects.new("RobotPreviewCamera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.lens = 62

key_data = bpy.data.lights.new("RobotPreviewKey", "AREA")
key_data.energy = 1100
key_data.shape = "DISK"
key_data.size = maximum * 1.3
key = bpy.data.objects.new("RobotPreviewKey", key_data)
scene.collection.objects.link(key)
key.location = center + Vector((-maximum, -maximum, maximum * 1.6))
look_at(key, center)

fill_data = bpy.data.lights.new("RobotPreviewFill", "AREA")
fill_data.energy = 750
fill_data.size = maximum
fill = bpy.data.objects.new("RobotPreviewFill", fill_data)
scene.collection.objects.link(fill)
fill.location = center + Vector((maximum, -maximum * 0.5, maximum * 0.5))
look_at(fill, center)

views = {
    "minus_y": Vector((0, -1, 0)),
    "plus_y": Vector((0, 1, 0)),
    "minus_z": Vector((0, 0, -1)),
    "plus_z": Vector((0, 0, 1)),
    "minus_x": Vector((-1, 0, 0)),
}
for name, direction in views.items():
    camera.location = center + direction * maximum * 2.1
    look_at(camera, center)
    scene.render.filepath = os.path.join(output_dir, f"robot_{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"ALGOLAB_ROBOT_PREVIEW={scene.render.filepath}")
