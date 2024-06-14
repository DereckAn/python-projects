import numpy as np
from PIL import Image
import potrace

# Cargar imagen y convertirla a blanco y negro
image = Image.open("input.png").convert("L")
bitmap = np.array(image) > 128

# Crear un objeto bitmap de potrace
potrace_bitmap = potrace.Bitmap(bitmap)

# Vectorizar la imagen
potrace_bitmap = potrace_bitmap.trace()

# Ahora, `potrace_bitmap` es una lista de curvas, y puedes procesarla como quieras.
# Por ejemplo, puedes imprimir las curvas en formato SVG.
