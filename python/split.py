# multypoint = "(-94.45312410593034 38.318673559287284),(-90.58593660593034 38.04232235983689), (-86.71874910593034 38.18062898005877)"

import re

# Tu string original
s = "(-94.45312410593034 38.318673559287284),(-90.58593660593034 38.04232235983689), (-86.71874910593034 38.18062898005877)"

# Ejecuta el código 15 veces
for _ in range(15):
    # Extrae los números del string
    numbers = re.findall(r"[-+]?\d*\.\d+|\d+", s)

    # Suma 4 a cada número y reconstruye el string
    new_s = s
    for number in numbers:
        old_number_str = number
        new_number_str = str(float(number) + 4)
        new_s = new_s.replace(old_number_str, new_number_str)

    # Actualiza s para la próxima iteración
    s = new_s
    print(s)
