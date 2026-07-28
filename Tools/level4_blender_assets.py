import bpy
import math
import os
import sys


PROJECT = r"C:\UnityProjects\algolab"
DOWNLOADS = r"C:\Users\crist\Downloads"
OUTPUT = os.path.join(
    PROJECT, "Assets", "__Algolab", "Prefabs", "Objects", "level4", "Models"
)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def material(name, color, metallic=0.0, roughness=0.45, emission=None):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission is not None:
            bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 1.6
    return mat


def add_cube(name, location, scale, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0:
        modifier = obj.modifiers.new("BordesSuaves", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.materials.append(mat)
    return obj


def add_cylinder(name, location, radius, depth, mat, vertices=48):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    return obj


def export_fbx(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"MESH", "EMPTY"},
        path_mode="COPY",
        embed_textures=True,
        add_leaf_bones=False,
        bake_anim=False,
    )


def convert_glb(source_name, output_name):
    reset_scene()
    source = os.path.join(DOWNLOADS, source_name)
    if not os.path.isfile(source):
        raise FileNotFoundError(source)
    bpy.ops.import_scene.gltf(filepath=source)
    root = bpy.data.objects.new(output_name.replace(".fbx", ""), None)
    bpy.context.collection.objects.link(root)
    imported_roots = [o for o in bpy.context.scene.objects if o.parent is None and o != root]
    for obj in imported_roots:
        obj.parent = root
    export_fbx(os.path.join(OUTPUT, output_name))


def save_imported_image(image_name, relative_output_path):
    image = bpy.data.images.get(image_name)
    if image is None:
        raise RuntimeError(f"No se encontro la textura importada: {image_name}")

    output_path = os.path.join(OUTPUT, relative_output_path)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    image.filepath_raw = output_path
    image.file_format = "PNG"
    image.save()


def create_vinyl():
    reset_scene()
    black = material("ViniloNegro", (0.008, 0.012, 0.018), metallic=0.15, roughness=0.20)
    cyan = material("EtiquetaCian", (0.02, 0.72, 0.82), metallic=0.0, roughness=0.35)
    white = material("EtiquetaBlanca", (0.86, 0.93, 0.96), roughness=0.40)
    groove = material("Surcos", (0.035, 0.045, 0.06), metallic=0.25, roughness=0.26)

    root = bpy.data.objects.new("Vinilo_AbstractSong", None)
    bpy.context.collection.objects.link(root)
    disc = add_cylinder("DiscoVinilo", (0, 0, 0), 0.52, 0.045, black, 64)
    disc.parent = root
    label = add_cylinder("EtiquetaCentral", (0, 0, 0.025), 0.16, 0.012, cyan, 48)
    label.parent = root
    center = add_cylinder("CentroBlanco", (0, 0, 0.034), 0.035, 0.014, white, 32)
    center.parent = root

    for index, radius in enumerate((0.25, 0.33, 0.41, 0.47)):
        bpy.ops.mesh.primitive_torus_add(
            major_radius=radius,
            minor_radius=0.004,
            major_segments=64,
            minor_segments=6,
            location=(0, 0, 0.026),
        )
        ring = bpy.context.object
        ring.name = f"Surco_{index + 1}"
        ring.data.materials.append(groove)
        ring.parent = root

    export_fbx(os.path.join(OUTPUT, "Vinyl_CC0_Optimized.fbx"))


def create_board():
    reset_scene()
    pcb = material("PCB_Cian", (0.015, 0.24, 0.30), metallic=0.25, roughness=0.34)
    edge = material("PCB_Borde", (0.02, 0.65, 0.66), metallic=0.35, roughness=0.25)
    chip = material("ChipOscuro", (0.012, 0.018, 0.025), metallic=0.15, roughness=0.31)
    gold = material("Conectores", (0.95, 0.55, 0.04), metallic=0.72, roughness=0.20)
    blue = material("ComponenteAzul", (0.04, 0.22, 0.72), metallic=0.05, roughness=0.28)
    violet = material("ComponenteVioleta", (0.46, 0.08, 0.78), metallic=0.10, roughness=0.28)
    trace = material("PistasLuminosas", (0.02, 0.86, 0.76), metallic=0.25, roughness=0.18,
                     emission=(0.01, 0.46, 0.40))

    root = bpy.data.objects.new("PlacaInternaTelefono", None)
    bpy.context.collection.objects.link(root)
    base = add_cube("PlacaBase", (0, 0, 0), (0.32, 0.018, 0.58), pcb, 0.045)
    base.parent = root
    rim = add_cube("MarcoSuperior", (0, 0.026, 0), (0.33, 0.009, 0.59), edge, 0.048)
    rim.parent = root
    inner = add_cube("Interior", (0, 0.041, 0), (0.30, 0.008, 0.55), pcb, 0.040)
    inner.parent = root

    parts = [
        ("Procesador", (-0.04, 0.067, 0.10), (0.13, 0.025, 0.13), chip),
        ("Memoria", (-0.17, 0.064, -0.18), (0.055, 0.020, 0.13), violet),
        ("ControladorAudio", (0.15, 0.064, -0.22), (0.075, 0.020, 0.09), blue),
        ("ModuloRed", (0.14, 0.064, 0.30), (0.08, 0.020, 0.105), chip),
        ("Almacenamiento", (-0.16, 0.064, 0.35), (0.06, 0.020, 0.09), blue),
    ]
    for name, loc, scale, mat in parts:
        obj = add_cube(name, loc, scale, mat, 0.012)
        obj.parent = root

    for row in range(5):
        z = -0.42 + row * 0.18
        for side in (-1, 1):
            connector = add_cube(
                f"Conector_{row}_{side}",
                (side * 0.275, 0.064, z),
                (0.025, 0.018, 0.045),
                gold,
                0.006,
            )
            connector.parent = root

    trace_specs = [
        ((-0.15, 0.052, -0.02), (0.010, 0.006, 0.22)),
        ((0.15, 0.052, 0.02), (0.010, 0.006, 0.20)),
        ((0.00, 0.052, -0.34), (0.22, 0.006, 0.010)),
        ((0.00, 0.052, 0.46), (0.24, 0.006, 0.010)),
        ((-0.06, 0.052, 0.28), (0.12, 0.006, 0.010)),
    ]
    for index, (loc, scale) in enumerate(trace_specs):
        line = add_cube(f"Pista_{index + 1}", loc, scale, trace, 0.004)
        line.parent = root

    export_fbx(os.path.join(OUTPUT, "Phone_Internal_Board_CC0_Optimized.fbx"))


def main():
    os.makedirs(OUTPUT, exist_ok=True)
    convert_glb("Phone by Quaternius - k2kgBepoMU.glb", "Phone_Quaternius_CC0.fbx")
    convert_glb("Building by Kay Lousberg - EL3ePInr1N.glb", "MusicStore_KayLousberg_CC0.fbx")
    save_imported_image(
        "citybits_texture.png",
        os.path.join("Textures", "MusicStore_citybits.png"),
    )
    create_vinyl()
    create_board()
    print("ALGOLAB_LEVEL4_MODELS_OK", OUTPUT)


if __name__ == "__main__":
    main()
