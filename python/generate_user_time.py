import json
from datetime import datetime, timedelta

def generate_weekly_dates():
  today = datetime.now()
  weekly_hours = {}
  
  # Generamos las fechas de la última semana
  for i in range(6, -1, -1):
      date = today - timedelta(days=i)
      date_str = date.strftime('%Y-%m-%d')
      weekly_hours[date_str] = 0  # Inicializamos en 0
  
  return weekly_hours

# Datos predefinidos para cada empleado
employees = [
  {
      "id": "1",
      "name": "Ana García",
      "imageUrl": "https://images.unsplash.com/photo-1494790108377-be9c29b29330",
      "isWorking": True,
      "weeklyHours": {
          (datetime.now() - timedelta(days=6)).strftime('%Y-%m-%d'): 0,  # Lunes
          (datetime.now() - timedelta(days=5)).strftime('%Y-%m-%d'): 3,  # Martes
          (datetime.now() - timedelta(days=4)).strftime('%Y-%m-%d'): 1,  # Miércoles
          (datetime.now() - timedelta(days=3)).strftime('%Y-%m-%d'): 1,  # Jueves
          (datetime.now() - timedelta(days=2)).strftime('%Y-%m-%d'): 1,  # Viernes
          (datetime.now() - timedelta(days=1)).strftime('%Y-%m-%d'): 2,  # Sábado
          datetime.now().strftime('%Y-%m-%d'): 5,  # Domingo
      }
  },
  {
      "id": "2",
      "name": "Carlos Ruiz",
      "imageUrl": "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d",
      "isWorking": False,
      "weeklyHours": {
          (datetime.now() - timedelta(days=6)).strftime('%Y-%m-%d'): 4,
          (datetime.now() - timedelta(days=5)).strftime('%Y-%m-%d'): 6,
          (datetime.now() - timedelta(days=4)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=3)).strftime('%Y-%m-%d'): 2,
          (datetime.now() - timedelta(days=2)).strftime('%Y-%m-%d'): 5,
          (datetime.now() - timedelta(days=1)).strftime('%Y-%m-%d'): 0,
          datetime.now().strftime('%Y-%m-%d'): 0,
      }
  },
  {
      "id": "3",
      "name": "María López",
      "imageUrl": "https://images.unsplash.com/photo-1438761681033-6461ffad8d80",
      "isWorking": True,
      "weeklyHours": {
          (datetime.now() - timedelta(days=6)).strftime('%Y-%m-%d'): 2,
          (datetime.now() - timedelta(days=5)).strftime('%Y-%m-%d'): 2,
          (datetime.now() - timedelta(days=4)).strftime('%Y-%m-%d'): 4,
          (datetime.now() - timedelta(days=3)).strftime('%Y-%m-%d'): 4,
          (datetime.now() - timedelta(days=2)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=1)).strftime('%Y-%m-%d'): 1,
          datetime.now().strftime('%Y-%m-%d'): 2,
      }
  },
  {
      "id": "4",
      "name": "Pedro Sánchez",
      "imageUrl": "https://images.unsplash.com/photo-1500648767791-00dcc994a43e",
      "isWorking": True,
      "weeklyHours": {
          (datetime.now() - timedelta(days=6)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=5)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=4)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=3)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=2)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=1)).strftime('%Y-%m-%d'): 0,
          datetime.now().strftime('%Y-%m-%d'): 0,
      }
  },
  {
      "id": "5",
      "name": "Laura Martínez",
      "imageUrl": "https://images.unsplash.com/photo-1534528741775-53994a69daeb",
      "isWorking": False,
      "weeklyHours": {
          (datetime.now() - timedelta(days=6)).strftime('%Y-%m-%d'): 5,
          (datetime.now() - timedelta(days=5)).strftime('%Y-%m-%d'): 4,
          (datetime.now() - timedelta(days=4)).strftime('%Y-%m-%d'): 4,
          (datetime.now() - timedelta(days=3)).strftime('%Y-%m-%d'): 4,
          (datetime.now() - timedelta(days=2)).strftime('%Y-%m-%d'): 3,
          (datetime.now() - timedelta(days=1)).strftime('%Y-%m-%d'): 0,
          datetime.now().strftime('%Y-%m-%d'): 0,
      }
  }
]

# Guardamos el JSON en un archivo
with open('employees.json', 'w', encoding='utf-8') as f:
  json.dump({"employees": employees}, f, ensure_ascii=False, indent=2)

print("Archivo employees.json creado exitosamente")

# Created/Modified files during execution:
print("employees.json")