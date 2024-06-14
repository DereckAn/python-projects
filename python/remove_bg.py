import cv2
import numpy as np

# Leer la imagen
img = cv2.imread('./carro.png')

# Convertir la imagen a escala de grises
gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

# Aplicar desenfoque gaussiano
blur = cv2.GaussianBlur(gray, (5, 5), 0)

# Aplicar detección de bordes Canny
edges = cv2.Canny(blur, 50, 200)

# Encontrar contornos
contours, _ = cv2.findContours(edges.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

# Ordenar los contornos por área y mantener el más grande
contours = sorted(contours, key=cv2.contourArea, reverse=True)[:1]

# Crear una máscara de la misma forma que la imagen
mask = np.zeros_like(edges)

# Dibujar el contorno más grande en la máscara
cv2.drawContours(mask, contours, -1, (255), thickness=cv2.FILLED)

# Aplicar la máscara a la imagen
output = cv2.bitwise_and(img, img, mask=mask)

# Guardar la imagen de salida
cv2.imwrite('output.png', output)
