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
convert_to_webp('./sl.jpg')