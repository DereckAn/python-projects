#!/bin/bash

echo "Construyendo la imagen Docker para WhatsApp Scheduler..."
docker build -t whatsapp-scheduler -f python/Dockerfile .

if [ $? -eq 0 ]; then
    echo "✅ Imagen Docker construida exitosamente!"
    echo "Para ejecutar el contenedor, usa: ./run_docker.sh"
else
    echo "❌ Error al construir la imagen Docker."
    exit 1
fi
