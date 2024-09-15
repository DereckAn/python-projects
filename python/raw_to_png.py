import os
from PIL import Image
import rawpy

def convert_raw_to_jpg_png(input_folder, output_folder, output_format='jpg'):
    # Asegurarse de que la carpeta de salida exista
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    # Extensiones RAW comunes
    raw_extensions = ['.arw', '.cr2', '.nef', '.orf', '.raf', '.rw2']

    # Recorrer todos los archivos en la carpeta de entrada
    for filename in os.listdir(input_folder):
        name, extension = os.path.splitext(filename)
        if extension.lower() in raw_extensions:
            input_path = os.path.join(input_folder, filename)
            output_path = os.path.join(output_folder, f"{name}.{output_format}")

            # Abrir y procesar la imagen RAW
            with rawpy.imread(input_path) as raw:
                rgb = raw.postprocess()

            # Convertir a imagen PIL y guardar
            image = Image.fromarray(rgb)
            image.save(output_path)
            print(f"Convertido: {filename} -> {os.path.basename(output_path)}")

# Uso del script
input_folder = '/Users/dereckangeles/Downloads/fotos_poster'
output_folder = '/Users/dereckangeles/Downloads/fotos_poster2'
output_format = 'jpg'  # o 'png'

convert_raw_to_jpg_png(input_folder, output_folder, output_format)