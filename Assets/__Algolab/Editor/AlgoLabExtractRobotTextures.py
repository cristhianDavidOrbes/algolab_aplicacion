import os

import bpy


TEXTURE_NAMES = {
    "RobotTexture": "RobotTexture.png",
    "CabezaImagen": "CabezaImagen.png",
    "TextureBrazoL": "TextureBrazoL.png",
    "BrazoR": "BrazoR.png",
    "PiernaIzquierdaImagen": "PiernaImagen.png",
}

output_dir = os.environ.get("ALGOLAB_ROBOT_TEXTURE_OUTPUT_DIR")
if not output_dir:
    raise RuntimeError("Falta ALGOLAB_ROBOT_TEXTURE_OUTPUT_DIR.")
os.makedirs(output_dir, exist_ok=True)

for image_name, filename in TEXTURE_NAMES.items():
    image = bpy.data.images.get(image_name)
    if image is None:
        raise RuntimeError(f"No se encontro la imagen empacada '{image_name}'.")

    destination = os.path.join(output_dir, filename)
    image.filepath_raw = destination
    image.file_format = "PNG"
    image.save()
    print(f"ALGOLAB_ROBOT_TEXTURE={destination}")
