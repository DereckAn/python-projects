# ! pip install pytube, tk, custometkinter, packaging

import tkinter as tk
import customtkinter
from pytube import YouTube

# & System settings
customtkinter.set_appearance_mode('System')
customtkinter.set_default_color_theme('blue')

# & App frame
app = customtkinter.CTk()
app.geometry('720x480')
app.title('YouTube Downloader')

# & App widgets
title = customtkinter.CTkLabel(app, text='Insert Youtube Link ', font=('Arial', 20))
title.pack(pady=10)

# & Link input
url_tex = tk.StringVar() # Textvariable for input
link = customtkinter.CTkEntry(app, width=400, height=40, textvariable=url_tex)
link.pack(pady=10)

# Progress bar (extra - not necessary)
progress = customtkinter.CTkProgressBar(app,  height=10)
progress.set(0)
progress.pack(pady=10)

# Percentage (extra - not necessary)
percentage = customtkinter.CTkLabel(app, text='0%', font=('Arial', 20))
percentage.pack(pady=10)

# Progress bar function (extra - not necessary)
def progress_function(stream, chunk, bytes_remaining):
    size = stream.filesize
    size -= bytes_remaining
    progress.set((1-(bytes_remaining/size)) * 100)
    percentage.configure(text=f'{100*(1-(bytes_remaining/size)):.0f}%')
    app.update()

# & Download button
download = customtkinter.CTkButton(app, text='Download', width=400, height=40, command=lambda: download_video(url_tex.get())) # note: Get input from url_tex
download.pack(pady=10) # note: si no ponemos el .pack no se mostrara en la pantalla el boton

# & Function to download video
def download_video(url):
    try:
        yt = YouTube(url, on_progress_callback=progress_function)
        video = yt.streams.get_highest_resolution()
        video.download()
        title.configure(text=f'{yt.title}', text_color='green')
        finishLabel.configure(text='', text_color='white') # note: esto es para volver a restableces los colores del label. No se si sea tan necesario
        print('Download completed')
        finishLabel.configure(text=f'You have downloaded {yt.title}', text_color='green')
        
    except Exception as e:
        print('Download failed')
        print(e)
        finishLabel.configure(text='Download failed', text_color='red')

# Finish Download (extra - not necessary)
finishLabel = customtkinter.CTkLabel(app, text='', font=('Arial', 20))
finishLabel.pack(pady=10)

# & Run app
app.mainloop()