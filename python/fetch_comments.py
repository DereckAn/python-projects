import instaloader
import json
from datetime import datetime

def handle_challenge(L):
    print("Se requiere verificación adicional.")
    choice = input("Ingresa 1 para recibir un código por SMS o 2 para recibir un código por correo electrónico: ")
    if choice == "1":
        L.challenge_code_handler = lambda: input("Ingresa el código recibido por SMS: ")
    elif choice == "2":
        L.challenge_code_handler = lambda: input("Ingresa el código recibido por correo electrónico: ")
    else:
        print("Opción no válida.")
        return False
    
    try:
        L.handle_challenge()
        return True
    except Exception as e:
        print(f"Error al manejar el desafío: {e}")
        return False

def get_post_comments(post_url, username, password):
    L = instaloader.Instaloader()

    try:
        L.login(username, password)
        print("Inicio de sesión exitoso")
    except instaloader.exceptions.TwoFactorAuthRequiredException:
        print("Se requiere autenticación de dos factores.")
        two_factor_code = input("Ingresa el código de autenticación de dos factores: ")
        try:
            L.two_factor_login(two_factor_code)
            print("Inicio de sesión con autenticación de dos factores exitoso")
        except Exception as e:
            print(f"Error en la autenticación de dos factores: {e}")
            return
    except instaloader.exceptions.ConnectionException as e:
        if "challenge_required" in str(e):
            if not handle_challenge(L):
                print("No se pudo completar el desafío de seguridad.")
                return
        else:
            print(f"Error de conexión: {e}")
            return

    try:
        shortcode = post_url.split("/")[-2]
        post = instaloader.Post.from_shortcode(L.context, shortcode)
        comments = post.get_comments()

        comments_data = {
            "post_url": post_url,
            "total_comments": post.comments,
            "comments": []
        }

        for comment in comments:
            comment_info = {
                "username": comment.owner.username,
                "text": comment.text,
                "date": comment.created_at_utc.isoformat()
            }
            comments_data["comments"].append(comment_info)

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        filename = f"instagram_comments_{timestamp}.json"

        with open(filename, 'w', encoding='utf-8') as f:
            json.dump(comments_data, f, ensure_ascii=False, indent=4)

        print(f"Información de comentarios guardada en {filename}")
        print(f"Número total de comentarios: {post.comments}")

    except instaloader.exceptions.InstaloaderException as e:
        print(f"Error: {e}")

# URL del post de Instagram
post_url = "https://www.instagram.com/p/C_7C0peRqAR/"

# Solicitar credenciales al usuario
username = input("Ingresa tu nombre de usuario de Instagram: ")
password = input("Ingresa tu contraseña de Instagram: ")

get_post_comments(post_url, username, password)