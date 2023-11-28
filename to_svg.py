import cairosvg

def convert_to_svg(input_file, output_file):
    try:
        cairosvg.svg2png(url=input_file, write_to=output_file)
        print(f"La imagen ha sido convertida exitosamente y guardada como {output_file}")
    except Exception as e:
        print(f"Ha ocurrido un error durante la conversión: {e}")

# Uso de la función
convert_to_svg("./car.png", "car.svg")
