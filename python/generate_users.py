import json
import random
from datetime import datetime, timedelta
import faker

fake = faker.Faker()

# Productos con categorías y rangos de precios más realistas
products = {
  "Electronics": [
      {"name": "iPhone 13", "price_range": (799, 1099)},
      {"name": "Samsung Galaxy S21", "price_range": (699, 999)},
      {"name": "MacBook Pro", "price_range": (1299, 2499)},
      {"name": "Dell XPS 13", "price_range": (999, 1799)},
      {"name": "iPad Pro", "price_range": (799, 1099)},
      {"name": "AirPods Pro", "price_range": (249, 279)},
      {"name": "Sony WH-1000XM4", "price_range": (299, 349)}
  ],
  "Gaming": [
      {"name": "PS5", "price_range": (499, 549)},
      {"name": "Xbox Series X", "price_range": (499, 549)},
      {"name": "Nintendo Switch", "price_range": (299, 349)},
      {"name": "Gaming Mouse", "price_range": (49, 149)},
      {"name": "Gaming Keyboard", "price_range": (79, 199)}
  ],
  "Accessories": [
      {"name": "Phone Case", "price_range": (19, 49)},
      {"name": "Screen Protector", "price_range": (9, 29)},
      {"name": "Laptop Bag", "price_range": (29, 79)},
      {"name": "USB-C Hub", "price_range": (29, 69)},
      {"name": "Power Bank", "price_range": (39, 79)}
  ]
}

def generate_purchase():
  # Fecha aleatoria en los últimos 2 años
  date = datetime.now() - timedelta(days=random.randint(0, 730))
  
  # Seleccionar categoría y productos aleatorios
  category = random.choice(list(products.keys()))
  num_products = random.randint(1, 4)
  selected_products = random.sample(products[category], min(num_products, len(products[category])))
  
  # Calcular monto total
  total_amount = sum(random.randint(product["price_range"][0], product["price_range"][1]) 
                    for product in selected_products)
  
  return {
      "date": date.strftime("%Y-%m-%d"),
      "products": [product["name"] for product in selected_products],
      "amount": total_amount,
      "status": random.choice(["Completed", "Pending", "Cancelled"])
  }

def generate_users(count=500):
  users = []
  for i in range(count):
      num_purchases = random.randint(1, 8)
      user = {
          "id": i + 1,
          "name": fake.name(),
          "email": fake.email(),
          "phone": fake.phone_number(),
          "status": random.choice(["Active", "Active", "Active", "Inactive"]),  # 75% activos
          "avatar": f"/avatars/user{i + 1}.jpg",
          "address": fake.address().replace('\n', ', '),
          "dateJoined": (datetime.now() - timedelta(days=random.randint(0, 1095))).strftime("%Y-%m-%d"),
          "purchases": [generate_purchase() for _ in range(num_purchases)]
      }
      users.append(user)
  return users

# Generar usuarios y guardar en JSON
users_data = generate_users(500)

with open('users_data.json', 'w', encoding='utf-8') as f:
  json.dump(users_data, f, indent=2, ensure_ascii=False)

# Imprimir estadísticas
print(f"Generated {len(users_data)} users")
print(f"Total purchases: {sum(len(user['purchases']) for user in users_data)}")
print("Created/Modified files during execution:")
print("users_data.json")