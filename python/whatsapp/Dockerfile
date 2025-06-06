# Usar una imagen base de Python con soporte para navegador
FROM python:3.9-slim

# Instalar dependencias del sistema necesarias para el navegador
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    xvfb \
    libgbm1 \
    libasound2 \
    libatk1.0-0 \
    libc6 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libexpat1 \
    libfontconfig1 \
    libgcc1 \
    libgconf-2-4 \
    libgdk-pixbuf2.0-0 \
    libglib2.0-0 \
    libgtk-3-0 \
    libnspr4 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libstdc++6 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxss1 \
    libxtst6 \
    ca-certificates \
    fonts-liberation \
    libappindicator1 \
    libnss3 \
    lsb-release \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

# Instalar Google Chrome
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list' \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Crear un directorio de trabajo
WORKDIR /app

# Copiar los archivos de requisitos y el script
COPY python/whatsapp_scheduler.py /app/
COPY requirements.txt /app/

# Instalar dependencias de Python
RUN pip install --no-cache-dir -r requirements.txt

# Dar permisos de ejecución al script
RUN chmod +x /app/whatsapp_scheduler.py

# Configurar variables de entorno para Chrome
ENV DISPLAY=:99
ENV PYTHONUNBUFFERED=1

# Crear un script de inicio para configurar Xvfb y ejecutar el programa
RUN echo '#!/bin/bash\nXvfb :99 -screen 0 1280x1024x24 &\nsleep 1\npython /app/whatsapp_scheduler.py' > /app/start.sh \
    && chmod +x /app/start.sh

# Comando para ejecutar el script de inicio
CMD ["/app/start.sh"]
