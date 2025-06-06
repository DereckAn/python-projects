import sys
import subprocess
import importlib

# Verificar e instalar paquetes necesarios
# def check_and_install_packages():
#     required_packages = ['pywhatkit', 'schedule']
#     for package in required_packages:
#         try:
#             importlib.import_module(package)
#             print(f"✓ {package} ya está instalado")
#         except ImportError:
#             print(f"✗ {package} no está instalado. Instalando...")
#             subprocess.check_call([sys.executable, "-m", "pip", "install", package])
#             print(f"✓ {package} ha sido instalado correctamente")

# # Ejecutar la verificación de paquetes
# check_and_install_packages()

# Ahora importamos los paquetes necesarios
import pywhatkit as kit
import time
import schedule
import datetime

# Configuración
PHONE_NUMBER = "+5215545641980"  # Reemplazar con el número de teléfono destino (incluir código de país)
MESSAGE = "Este es un mensaje automatizado para que Julieta recuerde que me tiene un amigo con hambre. Y para ahcerle saber que ando practicando ejerciciso de hackerrank  "
WAIT_TIME = 10  # Segundos de espera para que WhatsApp Web se abra
CLOSE_TIME = 3  # Segundos antes de cerrar la pestaña después de enviar el mensaje

def send_whatsapp_message():
    """Función para enviar un mensaje de WhatsApp"""
    print(f"Enviando mensaje a {PHONE_NUMBER} ahora mismo...")
    
    try:
        # Enviar el mensaje de WhatsApp inmediatamente
        kit.sendwhatmsg_instantly(PHONE_NUMBER, f" Mensaje enviado a las {datetime.datetime.now().strftime('%H:%M:%S')} : {MESSAGE}", wait_time=WAIT_TIME, close_time=CLOSE_TIME)
        print(f"Mensaje enviado con éxito a las {datetime.datetime.now().strftime('%H:%M:%S')}")
    except Exception as e:
        print(f"Error al enviar el mensaje: {e}")

def schedule_messages():
    """Configurar el horario para enviar mensajes cada 4 horas"""
    # Programar el envío de mensajes cada 4 horas
    schedule.every(4).hours.do(send_whatsapp_message)
    
    print("Programador iniciado. Los mensajes se enviarán cada 4 horas.")
    print("Presiona Ctrl+C para detener el programa.")
    
    # Enviar un mensaje inmediatamente al iniciar
    send_whatsapp_message()
    
    # Mantener el programa en ejecución
    while True:
        schedule.run_pending()
        time.sleep(1)

if __name__ == "__main__":
    schedule_messages()
