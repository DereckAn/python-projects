# WhatsApp Scheduler Dockerizado

Este proyecto contiene un script que envía mensajes de WhatsApp automáticamente cada 4 horas utilizando la biblioteca `pywhatkit`.

## Requisitos

- Docker instalado en tu sistema
- Conexión a Internet

## Instrucciones de uso

### 1. Construir la imagen Docker

```bash
./build_docker.sh
```

o manualmente:

```bash
docker build -t whatsapp-scheduler -f python/Dockerfile .
```

### 2. Ejecutar el contenedor

```bash
./run_docker.sh
```

o manualmente:

```bash
docker run -it --name whatsapp-scheduler-container whatsapp-scheduler
```

### 3. Primera ejecución

La primera vez que ejecutes el contenedor, necesitarás:

1. Escanear el código QR de WhatsApp Web que aparecerá en la ventana del navegador
2. Autenticarte en WhatsApp Web
3. El programa enviará mensajes automáticamente cada 4 horas

### Notas importantes

- El script está configurado para enviar mensajes al número +5215545641980
- El mensaje predeterminado es un recordatorio para Julieta
- Para cambiar el número de teléfono o el mensaje, modifica las variables `PHONE_NUMBER` y `MESSAGE` en el archivo `whatsapp_scheduler.py`

## Solución de problemas

Si encuentras problemas con la autenticación de WhatsApp Web:

1. Detén el contenedor: `docker stop whatsapp-scheduler-container`
2. Elimina el contenedor: `docker rm whatsapp-scheduler-container`
3. Vuelve a ejecutar el contenedor: `./run_docker.sh`
