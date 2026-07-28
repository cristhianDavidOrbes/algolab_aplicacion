import os

import bpy


def select_meshes():
    scene = bpy.context.scene
    view_layer = scene.view_layers[0]
    selected = []

    for obj in view_layer.objects:
        obj.select_set(False, view_layer=view_layer)

    for obj in view_layer.objects:
        if obj.type != "MESH":
            continue
        obj.hide_set(False, view_layer=view_layer)
        obj.select_set(True, view_layer=view_layer)
        selected.append(obj)

    if not selected:
        raise RuntimeError("El archivo no contiene mallas para exportar.")

    view_layer.objects.active = selected[0]
    return scene, view_layer, selected


output_path = os.environ.get("ALGOLAB_TEMPERATURE_FBX_OUTPUT")
if not output_path:
    raise RuntimeError("Falta la variable ALGOLAB_TEMPERATURE_FBX_OUTPUT.")

os.makedirs(os.path.dirname(output_path), exist_ok=True)
scene, view_layer, selected = select_meshes()

with bpy.context.temp_override(
    scene=scene,
    view_layer=view_layer,
    active_object=selected[0],
    object=selected[0],
    selected_objects=selected,
    selected_editable_objects=selected,
):
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        use_triangles=True,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        path_mode="COPY",
        embed_textures=True,
    )

print(f"ALGOLAB_TEMPERATURE_EXPORTED={output_path}")
