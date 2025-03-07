import os
import threading
import sys
import subprocess
from pydub import AudioSegment
import yt_dlp
import flet as ft
import ffmpeg

# Instalar pydub si no está instalado
try:
    from pydub import AudioSegment
except ImportError:
    print("Instalando pydub...")
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pydub"])

# Instalar yt-dlp si no está instalado
try:
    import yt_dlp
except ImportError:
    print("Instalando yt-dlp...")
    subprocess.check_call([sys.executable, "-m", "pip", "install", "yt-dlp"])

# Instalar ffmpeg-python si no está instalado
try:
    import ffmpeg
except ImportError:
    print("Instalando ffmpeg-python...")
    subprocess.check_call([sys.executable, "-m", "pip", "install", "ffmpeg-python"])

class YouTubeDownloader:
    def __init__(self):
        self.playlist_url = ""
        self.download_path = os.path.join(os.getcwd(), "music_downloads")
        self.current_progress = 0
        self.total_songs = 0
        self.current_song = 0
        self.current_song_title = ""
        self.downloading = False
        self.cancel_requested = False
        
        # Configurar opciones de descarga
        self.ydl_opts = {
            'format': 'bestaudio',  # Solo descargar el mejor formato de audio
            'quiet': True,
            'no_warnings': True,
            'outtmpl': os.path.join(self.download_path, '%(title)s.%(ext)s'),
            'ignoreerrors': True,
            'noplaylist': False,
        }

    def progress_hook(self, d):
        if d['status'] == 'downloading':
            if 'total_bytes' in d and d['total_bytes'] > 0:
                # Calcular progreso de canción basado en bytes descargados
                percent = (d['downloaded_bytes'] / d['total_bytes']) * 100
                self.song_progress_callback(percent)
            elif 'downloaded_bytes' in d:
                # Si no hay total_bytes, mostrar un progreso indeterminado
                self.song_progress_callback(50)  # progreso indeterminado
                
        elif d['status'] == 'finished':
            self.song_progress_callback(100)  # canción completa

    def download_playlist(self, playlist_url, progress_callback=None, song_info_callback=None):
        self.playlist_url = playlist_url
        self.cancel_requested = False
        self.current_progress = 0
        self.song_progress_callback = lambda percent: progress_callback(self.current_progress, percent) if progress_callback else None
        
        try:
            os.makedirs(self.download_path, exist_ok=True)
            
            # Configurar opciones para yt-dlp
            ydl_opts = self.ydl_opts.copy()
            ydl_opts['progress_hooks'] = [self.progress_hook]
            
            # Primero obtenemos información de la playlist
            with yt_dlp.YoutubeDL({'quiet': True, 'no_warnings': True, 'flat_playlist': True}) as ydl:
                info = ydl.extract_info(self.playlist_url, download=False)
                if 'entries' in info:
                    self.total_songs = len(info['entries'])
                else:
                    self.total_songs = 1  # No es una playlist, solo un video
            
            # Ahora descargamos los videos
            with yt_dlp.YoutubeDL(ydl_opts) as ydl:
                info = ydl.extract_info(self.playlist_url, download=False)
                entries = info['entries'] if 'entries' in info else [info]
                
                for index, entry in enumerate(entries):
                    if self.cancel_requested:
                        break
                    
                    self.current_song = index + 1
                    self.current_song_title = entry.get('title', f"Video {index+1}")
                    
                    # Actualizar info de la canción
                    if song_info_callback:
                        song_info_callback(self.current_song, self.total_songs, self.current_song_title)
                    
                    # Descargar individualmente para mejor control
                    if not self.cancel_requested:
                        try:
                            # Descargar el archivo
                            ydl.download([entry['webpage_url']])
                            
                            # Encontrar el archivo descargado
                            downloaded_file = None
                            for file in os.listdir(self.download_path):
                                if entry['title'] in file:
                                    downloaded_file = os.path.join(self.download_path, file)
                                    break
                            
                            if downloaded_file:
                                # Convertir a MP3 usando ffmpeg-python
                                mp3_path = os.path.splitext(downloaded_file)[0] + '.mp3'
                                try:
                                    # Cargar el archivo de audio
                                    audio = ffmpeg.input(downloaded_file)
                                    # Exportar como MP3
                                    audio.output(mp3_path, format="mp3", bitrate="192k").run()
                                    # Eliminar el archivo original
                                    os.remove(downloaded_file)
                                except Exception as e:
                                    print(f"Error convirtiendo a MP3 {self.current_song_title}: {str(e)}")
                            
                        except Exception as e:
                            print(f"Error descargando {self.current_song_title}: {str(e)}")
                    
                    # Actualizar progreso total
                    self.current_progress = (index + 1) / self.total_songs * 100
                    if progress_callback:
                        progress_callback(self.current_progress, 100)
            
            return True
        except Exception as e:
            print(f"Error general en la descarga: {str(e)}")
            return False

def main(page: ft.Page):
    page.title = "YouTube Music Downloader"
    page.theme_mode = ft.ThemeMode.DARK
    page.window_width = 650
    page.window_height = 500
    page.horizontal_alignment = ft.CrossAxisAlignment.CENTER
    page.vertical_alignment = ft.MainAxisAlignment.CENTER

    downloader = YouTubeDownloader()
    download_thread = None
    
    # Variables para estado global
    download_status = {"success": False, "message": None}

    def on_download_progress(total_progress, song_progress):
        total_progress_bar.value = total_progress / 100
        song_progress_bar.value = song_progress / 100
        progress_text.value = f"{total_progress:.1f}% completado"
        page.update()

    def on_song_info(current_song, total_songs, song_title):
        song_info_text.value = f"Descargando: {current_song} de {total_songs}"
        song_title_text.value = f"🎵 {song_title}"
        song_progress_bar.value = 0
        page.update()

    def start_download(e):
        nonlocal download_thread
        if not url_input.value:
            status_text.value = "Por favor, introduce una URL de playlist válida"
            page.update()
            return
            
        downloader.downloading = True
        start_btn.disabled = True
        cancel_btn.disabled = False
        status_text.value = "Iniciando descarga..."
        page.update()
        
        download_thread = threading.Thread(
            target=execute_download_and_update_ui
        )
        download_thread.daemon = True
        download_thread.start()
    
    def execute_download_and_update_ui():
        nonlocal download_status
        try:
            success = downloader.download_playlist(
                playlist_url=url_input.value,
                progress_callback=on_download_progress,
                song_info_callback=on_song_info
            )
            
            # Guardamos el resultado
            if success and not downloader.cancel_requested:
                download_status = {"success": True, "message": None}
            elif downloader.cancel_requested:
                download_status = {"success": False, "message": "Descarga cancelada"}
            else:
                download_status = {"success": False, "message": "Error en la descarga"}
            
            # Actualizar UI desde el hilo
            page.controls.append(ft.Text("", visible=False))  # truco para forzar update
            page.update()
            
            # Llamamos a la función de actualización final
            update_completion_status()
            
        except Exception as e:
            print(f"Error en el hilo de descarga: {str(e)}")
            download_status = {"success": False, "message": f"Error: {str(e)}"}
            page.update()
            update_completion_status()
    
    def update_completion_status():
        start_btn.disabled = False
        cancel_btn.disabled = True
        downloader.downloading = False
        
        if download_status["success"]:
            status_text.value = "¡Descarga completada con éxito!"
            song_info_text.value = f"Total: {downloader.total_songs} canciones descargadas"
            song_title_text.value = "Completado"
        else:
            status_text.value = download_status["message"] or "Error en la descarga"
        
        page.update()

    def cancel_download(e):
        downloader.cancel_requested = True
        status_text.value = "Cancelando descarga..."
        page.update()

    # Componentes de la UI
    header = ft.Text(
        "YouTube Music Downloader", 
        size=28, 
        weight=ft.FontWeight.BOLD,
        color="#4285F4"
    )
    
    url_input = ft.TextField(
        label="URL de la playlist de YouTube",
        width=500,
        hint_text="Pega aquí el enlace de tu playlist",
        border_radius=10,
        border=ft.InputBorder.UNDERLINE,
        autofocus=True,
        prefix_icon=ft.Icons.PLAYLIST_PLAY
    )

    # Botones
    start_btn = ft.ElevatedButton(
        text="Comenzar Descarga",
        on_click=start_download,
        icon=ft.Icons.DOWNLOAD,
        width=180,
        bgcolor="#4285F4",
        color="white"
    )

    cancel_btn = ft.ElevatedButton(
        text="Cancelar",
        on_click=cancel_download,
        icon=ft.Icons.CANCEL,
        width=180,
        disabled=True,
        bgcolor="#DB4437",
        color="white"
    )

    # Indicadores de progreso
    song_info_text = ft.Text("Esperando playlist...", size=16, weight=ft.FontWeight.BOLD)
    song_title_text = ft.Text("", size=14, italic=True)
    
    song_progress_label = ft.Text("Progreso canción actual:", size=14)
    song_progress_bar = ft.ProgressBar(width=500, color="#0F9D58")
    
    total_progress_label = ft.Text("Progreso total:", size=14)
    total_progress_bar = ft.ProgressBar(width=500, color="#4285F4")
    
    progress_text = ft.Text("0% completado", size=14)
    status_text = ft.Text("Esperando para descargar...", size=16, color="#4FC3F7")
    
    # Info sobre ubicación de descarga
    download_location_text = ft.Text(
        f"Las canciones se guardarán en: {downloader.download_path}", 
        size=12, 
        italic=True,
        color="#9E9E9E"
    )

    # Contenedor para todos los elementos
    container = ft.Container(
        content=ft.Column(
            [
                header,
                ft.Divider(height=5, color="transparent"),
                url_input,
                ft.Divider(height=10, color="transparent"),
                ft.Row([start_btn, cancel_btn], alignment=ft.MainAxisAlignment.CENTER),
                ft.Divider(height=20, color="transparent"),
                song_info_text,
                song_title_text,
                ft.Divider(height=5, color="transparent"),
                song_progress_label,
                song_progress_bar,
                ft.Divider(height=10, color="transparent"),
                total_progress_label,
                total_progress_bar,
                progress_text,
                ft.Divider(height=10, color="transparent"),
                status_text,
                ft.Divider(height=20, color="transparent"),
                download_location_text
            ],
            alignment=ft.MainAxisAlignment.CENTER,
            horizontal_alignment=ft.CrossAxisAlignment.CENTER
        ),
        padding=20,
        border_radius=10,
        bgcolor="#263238",
        border=ft.border.all(1, "#546E7A"),
        width=600,
    )

    page.add(container)

ft.app(target=main)