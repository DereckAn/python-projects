import matplotlib.pyplot as plt
import math

# Tu LineString
linestring = "LINESTRING(-113.994140625 42.00032514831619,-111.09375000000001 41.96765920367815,-111.04980468750001 41.07935114946898,-109.07226562500001 41.04621681452065,-109.0283203125 37.020098201368114,-113.994140625 37.020098201368114,-113.994140625 41.934976500546554)"

# Extraer las coordenadas de la LineString
coords = linestring.replace("LINESTRING(", "").replace(")", "").split(",")

print(f"Coordenadas: {coords}")
print(f"Coordenadas: {type(coords)}")

# Separar las coordenadas en listas de latitudes y longitudes
lats = [float(coord.split()[1]) for coord in coords]
lons = [float(coord.split()[0]) for coord in coords]

print(f"Latitudes: {lats}")
print(f"Longitudes: {lons}")

# Definir la función de rotación
def rotate(origin, point, angle):
    """
    Rotate a point counterclockwise by a given angle around a given origin.

    The angle should be given in radians.
    """
    ox, oy = origin
    px, py = point

    qx = ox + math.cos(angle) * (px - ox) - math.sin(angle) * (py - oy)
    qy = oy + math.sin(angle) * (px - ox) + math.cos(angle) * (py - oy)

    return qx, qy

# Definir el punto de origen para la rotación
# origin = (sum(lons) / len(lons), sum(lats) / len(lats))
origin = ((max(lons) + min(lons)) / 2, (max(lats) + min(lats)) / 2)
print(f"Origen: {origin}")
print(f"Origen: {type(origin)}")

# Definir el ángulo de rotación
angle = math.radians(0)  # 90 grados

# Rotar las coordenadas
rotated_coords = [rotate(origin, point, angle) for point in zip(lons, lats)]
print(f"Coordenadas rotadas: {rotated_coords}")

# Separar las coordenadas rotadas en listas de latitudes y longitudes
rotated_lats = [coord[1] for coord in rotated_coords]
rotated_lons = [coord[0] for coord in rotated_coords]

# Crear un gráfico
plt.figure()

# Dibujar los puntos
plt.scatter(rotated_lons, rotated_lats, color='green')

# Unir los puntos con una línea roja
plt.plot(rotated_lons, rotated_lats, color='red')

# Dibujar el punto de origen
plt.scatter(*origin, color='blue')

# Agregar las coordenadas de cada punto
for i, coord in enumerate(rotated_coords):
    plt.text(coord[0], coord[1], f"{coord[0]:.2f}, {coord[1]:.2f}", fontsize=8)

# Mostrar el gráfico
plt.show()