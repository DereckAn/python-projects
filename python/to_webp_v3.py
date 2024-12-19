#pip install pillow rawpy numpy
import os
from PIL import Image
import rawpy
import numpy as np

def convert_to_webp(source_folder, destination_folder):
    if not os.path.exists(destination_folder):
        os.makedirs(destination_folder)

    for filename in os.listdir(source_folder):
        try:
            image_path = os.path.join(source_folder, filename)
            base_name = os.path.splitext(filename)[0]
            webp_path = os.path.join(destination_folder, base_name + '.webp')
            
            # Handle RAW files
            if filename.upper().endswith('.RAW'):
                with rawpy.imread(image_path) as raw:
                    # Convert RAW to RGB
                    rgb = raw.postprocess()
                    # Convert numpy array to PIL Image
                    img = Image.fromarray(rgb)
            else:
                # Handle other image formats
                img = Image.open(image_path)
            
            # Save as WebP
            img.save(webp_path, 'webp')
            print(f'Image saved as {webp_path}')
            
        except Exception as e:
            print(f'Error converting {filename}: {e}')

source_folder = '/Users/dereckangeles/Downloads/raw'
destination_folder = './webp_images'

convert_to_webp(source_folder, destination_folder)