from bs4 import BeautifulSoup
from collections import Counter

def extract_instagram_usernames(file_path):
    # Leer el archivo HTML
    with open(file_path, 'r', encoding='utf-8') as file:
        html_content = file.read()
    
    # Crear el objeto BeautifulSoup
    soup = BeautifulSoup(html_content, 'html.parser')
    
    # Buscar todos los enlaces que contienen nombres de usuario
    username_links = soup.find_all('a', class_='x1i10hfl xjqpnuy xa49m3k xqeqjp1 x2hbi6w xdl72j9 x2lah0s xe8uvvx xdj266r x11i5rnm xat24cr x1mh8g0r x2lwn1j xeuugli x1hl2dhg xggy1nq x1ja2u2z x1t137rt x1q0g3np x1lku1pv x1a2a7pz x6s0dn4 xjyslct x1ejq31n xd10rxx x1sy0etr x17r0tee x9f619 x1ypdohk x1f6kntn xwhw2v2 xl56j7k x17ydfre x2b8uid xlyipyv x87ps6o x14atkfc xcdnw81 x1i0vuye xjbqb8w xm3z3ea x1x8b98j x131883w x16mih1h x972fbf xcfux6l x1qhh985 xm0m39n xt0psk2 xt7dq6l xexx8yu x4uap5 x18d9i69 xkhd6sd x1n2onr6 x1n5bzlp xqnirrm xj34u2y x568u83')
    
    # Extraer los nombres de usuario
    usernames = [link.text.strip() for link in username_links]
    
    # Contar las ocurrencias de cada nombre de usuario
    username_counts = Counter(usernames)
    
    return username_counts

# Ejemplo de uso
file_path = 'coments.html'  # Reemplaza esto con la ruta real de tu archivo HTML
username_counts = extract_instagram_usernames(file_path)

print("Nombres de usuario y sus ocurrencias (ordenados de mayor a menor):")
for username, count in sorted(username_counts.items(), key=lambda x: x[1], reverse=True):
    print(f"{username}: {count}")

print(f"\nTotal de usuarios únicos encontrados: {len(username_counts)}")
print(f"Total de ocurrencias de usuarios: {sum(username_counts.values())}")

# Opcionalmente, guardar los resultados en un archivo CSV
import csv

csv_file_path = 'resultados_usuarios_instagram.csv'
with open(csv_file_path, 'w', newline='', encoding='utf-8') as csvfile:
    csv_writer = csv.writer(csvfile)
    csv_writer.writerow(['Username', 'Occurrences'])  # Escribir encabezados
    for username, count in sorted(username_counts.items(), key=lambda x: x[1], reverse=True):
        csv_writer.writerow([username, count])

print(f"\nLos resultados han sido guardados en '{csv_file_path}'")