#pip install pillow
from PIL import Image

# Esto codigo es para convertir una imagen de png o jpg a webp
def convert_to_webp(image_path):
    try:
        img = Image.open(image_path)
        webp_path = image_path.rsplit('.', 1)[0] + '.webp'
        img.save(webp_path, 'webp')
        print(f'Image saved as {webp_path}')
    except Exception as e:
        print(f'Error: {e}')

# Prueba la función con una imagen
# convert_to_webp('./twitter.png')

lista_de_imagenes = ['./comida (1).jpg', './comida (2).jpg', './comida (3).jpg', './comida (4).jpg', './comida (5).jpg', './comida (6).jpg', './comida (7).jpg', './comida (8).jpg', './comida (9).jpg', './comida (10).jpg', './comida (11).jpg', './comida (12).jpg']

for imagen in lista_de_imagenes:
    convert_to_webp(imagen)
