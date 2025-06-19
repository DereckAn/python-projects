# Docs Scraper - Extractor de Documentación a Markdown

Un script de Python para extraer documentación de sitios web y convertirla automáticamente a formato Markdown. Este script está diseñado para alimentar sistemas RAG (Retrieval-Augmented Generation) con documentación técnica estructurada.

## Características

- Extracción completa de sitios de documentación a partir de una URL inicial
- Navegación automática y recursiva a través de enlaces relacionados
- Conversión de HTML a Markdown limpio y estructurado
- Conservación de la estructura de enlaces y referencias
- Procesamiento en paralelo para mayor velocidad
- Modo interactivo para una configuración sencilla
- Modo de línea de comandos para uso avanzado/automatizado
- Metadatos incrustados en cada archivo para facilitar el rastreo

## Requisitos

El script requiere Python 3.6 o superior y las siguientes bibliotecas:

- `requests`: Para realizar peticiones HTTP
- `beautifulsoup4`: Para parsear HTML
- `html2text`: Para convertir HTML a Markdown
- `markdownify`: Para mejorar la conversión a Markdown
- `tqdm`: Para barras de progreso (opcional)

## Instalación

1. Clona o descarga este repositorio
2. Instala las dependencias:

```bash
pip install -r requirements.txt
```

## Uso

El script puede usarse de dos maneras: modo interactivo o modo de línea de comandos.

### Modo Interactivo

Para usar el modo interactivo, simplemente ejecuta el script sin argumentos:

```bash
python docs_scraper.py
```

El script te guiará a través de un proceso paso a paso para configurar:

1. La URL del sitio de documentación a procesar
2. La carpeta de salida para los archivos Markdown
3. El número máximo de páginas a procesar (opcional)
4. El tiempo de espera entre peticiones (para no sobrecargar el servidor)
5. El número de páginas a procesar en paralelo

### Modo de Línea de Comandos

Para uso avanzado o automatizado, puedes usar argumentos de línea de comandos:

```bash
python docs_scraper.py URL [opciones]
```

#### Opciones disponibles:

| Opción                  | Descripción                                                      |
| ----------------------- | ---------------------------------------------------------------- |
| `URL`                   | La URL base para iniciar el scraping                             |
| `-o, --output DIR`      | Directorio de salida (por defecto: ./docs_output)                |
| `-m, --max-pages NUM`   | Número máximo de páginas a procesar                              |
| `-d, --delay SECONDS`   | Tiempo de espera entre peticiones en segundos (por defecto: 1.0) |
| `-c, --concurrency NUM` | Número de páginas a procesar en paralelo (por defecto: 5)        |
| `-i, --interactive`     | Forzar modo interactivo aunque se proporcionen argumentos        |

## Ejemplos de Uso

### Ejemplo básico:

```bash
python docs_scraper.py https://tailwindcss.com/docs
```

### Limitar a 50 páginas con un directorio personalizado:

```bash
python docs_scraper.py https://docs.fabricmc.net/develop/ -o ./fabric_docs -m 50
```

### Ser más gentil con el servidor (esperar 2 segundos entre peticiones):

```bash
python docs_scraper.py https://docs.python.org/es/3/ -d 2.0
```

### Aumentar la concurrencia para servidores más robustos:

```bash
python docs_scraper.py https://docs.djangoproject.com/es/4.2/ -c 10
```

## Estructura de Archivos Generados

El script genera una estructura de directorios similar a la del sitio web original. Por ejemplo, si scrapeas `https://docs.python.org/3/library/index.html`, obtendrás:

```
docs_output/
├── index.md             # Página principal
├── library/
│   ├── index.md         # Índice de la biblioteca
│   ├── functions.md     # Funciones integradas
│   └── ...
└── tutorial/
    ├── index.md         # Índice del tutorial
    └── ...
```

Cada archivo Markdown incluye metadatos en formato YAML al principio:

```markdown
---
url: https://docs.python.org/3/library/functions.html
source: docs.python.org
scrape_date: 2025-06-19 05:10:15
---

# Funciones Integradas — Python 3.x documentación

...contenido...
```

## Cómo Funciona

1. El script inicia con una URL base y la coloca en la cola "por visitar"
2. Para cada URL en la cola:
   - Descarga la página web
   - Extrae el contenido principal (eliminando menús, encabezados, etc.)
   - Convierte el contenido a Markdown
   - Guarda el contenido en un archivo Markdown
   - Extrae todos los enlaces en la página
   - Añade los enlaces válidos (mismo dominio, no visitados) a la cola
3. El proceso continúa hasta que no haya más enlaces por visitar o se alcance el límite configurado

## Personalización Avanzada

Si necesitas ajustar el comportamiento del script para sitios de documentación específicos, puedes editar estas secciones del código:

- `is_valid_url()`: Para modificar qué URLs se consideran válidas para el scraping
- `clean_content()`: Para ajustar cómo se extrae el contenido principal de una página
- `html_to_markdown()`: Para personalizar la conversión de HTML a Markdown

## Solución de Problemas Comunes

### El script no captura todo el contenido

Algunos sitios cargan contenido dinámicamente con JavaScript. Este script utiliza solicitudes HTTP simples que no ejecutan JavaScript. Para sitios que requieren JavaScript, necesitarías modificar el script para usar Selenium o Playwright.

### El script es demasiado lento

- Reduce el tiempo de espera (`--delay`) si el servidor objetivo puede manejar más peticiones
- Aumenta la concurrencia (`--concurrency`) para procesar más páginas en paralelo
- Limita el alcance con `--max-pages` si solo necesitas una parte de la documentación

### El contenido Markdown no tiene el formato adecuado

- Verifica la sección de contenido principal en `clean_content()` y ajústala para seleccionar correctamente el contenido relevante del sitio específico
- Ajusta la configuración del convertidor HTML a Markdown en la función `html_to_markdown()`

## Licencia

Este script es de código abierto y está disponible para uso personal y comercial.

## Contribuciones

Las contribuciones son bienvenidas. Si encuentras un error o tienes una mejora, por favor crea un pull request o abre un issue.

---

Desarrollado con ♥ para facilitar el acceso a documentación en formato procesable.
