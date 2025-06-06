#!/bin/bash

echo "Configurando permisos para los scripts..."

# Dar permisos de ejecución a los scripts
chmod +x build_docker.sh
chmod +x run_docker.sh
chmod +x python/whatsapp_scheduler.py

echo "✅ Permisos configurados correctamente."
echo "Ahora puedes ejecutar:"
echo "  ./build_docker.sh - Para construir la imagen Docker"
echo "  ./run_docker.sh - Para ejecutar el contenedor"
