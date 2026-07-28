import os

import bpy


ARMATURE_NAME = "Armature"


def select_robot():
    armature = bpy.data.objects.get(ARMATURE_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError(f"No se encontró la armadura '{ARMATURE_NAME}'.")

    scene = bpy.context.scene
    view_layer = scene.view_layers[0]
    for obj in view_layer.objects:
        obj.select_set(False, view_layer=view_layer)

    selected = [armature]
    for child in armature.children_recursive:
        if child.type == "MESH":
            selected.append(child)

    for obj in selected:
        obj.hide_set(False, view_layer=view_layer)
        obj.select_set(True, view_layer=view_layer)

    view_layer.objects.active = armature
    return scene, view_layer, armature, selected


output_path = os.environ.get("ALGOLAB_ROBOT_FBX_OUTPUT")
if not output_path:
    raise RuntimeError("Falta la variable ALGOLAB_ROBOT_FBX_OUTPUT.")

os.makedirs(os.path.dirname(output_path), exist_ok=True)
scene, view_layer, armature, selected = select_robot()

with bpy.context.temp_override(
    scene=scene,
    view_layer=view_layer,
    active_object=armature,
    object=armature,
    selected_objects=selected,
    selected_editable_objects=selected,
):
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        use_triangles=True,
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        path_mode="COPY",
        embed_textures=True,
    )

print(f"ALGOLAB_ROBOT_EXPORTED={output_path}")
