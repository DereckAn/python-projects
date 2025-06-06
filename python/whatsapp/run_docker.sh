#!/bin/bash

echo "Ejecutando el contenedor de WhatsApp Scheduler..."
echo "NOTA: La primera vez que ejecutes el contenedor, necesitarás escanear el código QR de WhatsApp Web."

# Verificar si el contenedor ya existe
if docker ps -a --format '{{.Names}}' | grep -q "^whatsapp-scheduler-container$"; then
    echo "El contenedor ya existe. Reiniciándolo..."
    docker start -a whatsapp-scheduler-container
else
    echo "Creando y ejecutando un nuevo contenedor..."
    docker run -it --name whatsapp-scheduler-container whatsapp-scheduler
fi
