import tkinter as tk
import customtkinter

# & System settings
customtkinter.set_appearance_mode('System')
customtkinter.set_default_color_theme('blue')

# & App frame
app = customtkinter.CTk()
app.geometry('1080x480')
app.title('Make your MarkDown file')

# Define los campos de entrada
campos = [
    {"label": "Title", "font": ('Arial', 20), "height": 40},
    {"label": "Function", "font": ('Arial', 15), "height": 40},
    {"label": "Description", "font": ('Arial', 15), "height": 100},
    {"label": "Returno", "font": ('Arial', 15), "height": 40},
]

# Crea los widgets para cada campo# Crea los widgets para cada campo
# Crea los widgets para cada campo
for campo in campos:
    label = customtkinter.CTkLabel(app, text=campo["label"], font=campo["font"])
    label.pack(pady=10)
    if campo["height"] > 40:
        input_widget = customtkinter.CTkTextbox(app, width=400, height=campo["height"])
    else:
        text_var = tk.StringVar()  # Textvariable for input
        input_widget = customtkinter.CTkEntry(app, width=400, height=campo["height"], textvariable=text_var)
    input_widget.pack(pady=10)

# Para obtener el texto del widget CTkTextbox
if isinstance(input_widget, customtkinter.CTkTextbox):
    texto = input_widget.get("1.0", "end-1c")
# Para obtener el texto del widget CTkEntry
elif isinstance(input_widget, customtkinter.CTkEntry):
    texto = input_widget.get()




# title = "GetUTMZoneBounds"
# fucntion = "GetUTMZoneBounds(utm)"
# description = "Converts UTM grid zone to geographic bounds."
# retorno = "Int32"

# # Define tus parámetros aquí. Cada diccionario representa una fila.
# parametros = [
#     {"Parameter": "param1", "Required": "Yes", "Type(s)": "int", "Description": "Este es el param1", "`null` Behavior": "N/A"},
#     {"Parameter": "param2", "Required": "No", "Type(s)": "string", "Description": "Este es el param2", "`null` Behavior": "N/A"},
#     # Puedes agregar más parámetros aquí...
# ]

# # Genera las filas de la tabla
# rows = "\n".join(
#     "| `{Parameter}` | {Required} | {Type(s)} | {Description} | {`null` Behavior} |".format(**param)
#     for param in parametros
# )

# info = f"""# {title}

# ## Description
# `{fucntion}`

# {description}

# ### Return Type
# `{retorno}` (see [Type Conversions](/docs/QueryExpression-Type))

# ## Parameters
# | Parameter | Required | Type(s) | Description | `null` Behavior |
# | :-------- | :------- | :------ | :---------- | :-------------- |
# {rows}

# ## Usage
# `{title}` may be used in the query SELECT and WHERE clauses for analyzing data and applying conditional logic.




# """

# # Abre el archivo en modo de escritura. Si el archivo no existe, se creará.
# with open(f'{title}.md', 'w') as f:
#     # Escribe la información en el archivo
#     f.write(info)


# & Run app
app.mainloop()