#pip install pillow


import os
from PIL import Image

def convert_to_webp(source_folder, destination_folder):
    # Crea la carpeta de destino si no existe
    if not os.path.exists(destination_folder):
        os.makedirs(destination_folder)

    # Recorre todos los archivos en la carpeta de origen
    for filename in os.listdir(source_folder):
        if filename.endswith('.png') or filename.endswith('.jpg') or filename.endswith('.jpeg') or filename.endswith('.webp') or filename.endswith('.RAW'):
            try:
                # Ruta completa del archivo de origen
                image_path = os.path.join(source_folder, filename)
                # Abre la imagen
                img = Image.open(image_path)
                # Nombre del archivo sin extensión
                base_name = os.path.splitext(filename)[0]
                # Ruta completa del archivo de destino
                webp_path = os.path.join(destination_folder, base_name + '.webp')
                # Guarda la imagen en formato webp
                img.save(webp_path, 'webp')
                print(f'Image saved as {webp_path}')
            except Exception as e:
                print(f'Error converting {filename}: {e}')

# Carpeta de origen con las imágenes PNG
# source_folder = '/Users/dereckangeles/Downloads/convert'
source_folder = '/Users/dereckangeles/Downloads/raw'
# Carpeta de destino para las imágenes WEBP
destination_folder = './webp_images'

# Convierte las imágenes
convert_to_webp(source_folder, destination_folder)