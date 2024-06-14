from PIL import Image

def convert_to_ico(input_image_path, output_image_path, sizes=[(128,128)]):
    image = Image.open(input_image_path)
    image.save(output_image_path, format='ICO', sizes=sizes)

# Uso de la función
convert_to_ico('./brum.png', 'output.ico')
