titulo = "Mi Título"
fucntion = "Esta es la descripción"
description = "Esta es la descripción"
retorno = "Int32"

# Define tus parámetros aquí. Cada diccionario representa una fila.
parametros = [
    {"Parameter": "param1", "Required": "Yes", "Type(s)": "int", "Description": "Este es el param1", "`null` Behavior": "N/A"},
    {"Parameter": "param2", "Required": "No", "Type(s)": "string", "Description": "Este es el param2", "`null` Behavior": "N/A"},
    # Puedes agregar más parámetros aquí...
]

# Genera las filas de la tabla
filas = "\n".join(
    "| {Parameter} | {Required} | {Type(s)} | {Description} | {`null` Behavior} |".format(**param)
    for param in parametros
)

info = f"""# {titulo}

## Description
{fucntion}

{description}

### Return Type
`{retorno}` (see Type Conversions)

## Parameters
| Parameter | Required | Type(s) | Description | `null` Behavior |
| :-------- | :------- | :------ | :---------- | :-------------- |
{filas}

"""

# Abre el archivo en modo de escritura. Si el archivo no existe, se creará.
with open('archivo.md', 'w') as f:
    # Escribe la información en el archivo
    f.write(info)
