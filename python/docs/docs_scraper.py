#!/usr/bin/env python3
import os
import re
import requests
from bs4 import BeautifulSoup
import urllib.parse
from urllib.parse import urlparse, urljoin
import time
import argparse
import logging
from markdownify import markdownify as md
import concurrent.futures
import tqdm
import html2text

# Configurar logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("docs_scraper.log"),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

class DocsScraperToMarkdown:
    """
    Clase para hacer scraping de páginas de documentación y convertirlas a markdown
    """
    
    def __init__(self, base_url, output_dir="./docs_output", max_pages=None, delay=1.0, concurrency=5):
        """
        Inicializa el scraper con la URL base y configuración.
        
        Args:
            base_url: URL de inicio para el scraping
            output_dir: Directorio donde se guardarán los archivos markdown
            max_pages: Número máximo de páginas a procesar (None para ilimitado)
            delay: Tiempo de espera entre solicitudes para no sobrecargar el servidor
            concurrency: Número de páginas a procesar en paralelo
        """
        self.base_url = base_url
        self.domain = urlparse(base_url).netloc
        self.output_dir = output_dir
        self.max_pages = max_pages
        self.delay = delay
        self.concurrency = concurrency
        self.visited_urls = set()
        self.to_visit = set([base_url])
        self.html_converter = html2text.HTML2Text()
        self.html_converter.ignore_links = False
        self.html_converter.ignore_images = False
        self.html_converter.body_width = 0  # No wrapping
        self.html_converter.protect_links = True
        self.html_converter.unicode_snob = True
        
        # Asegurarse de que el directorio de salida exista
        os.makedirs(output_dir, exist_ok=True)
    
    def is_valid_url(self, url):
        """Verifica si una URL es válida para el scraping"""
        try:
            # Verificar que sea del mismo dominio
            parsed_url = urlparse(url)
            
            # Verificar que no sea un archivo binario o no deseado
            unwanted_extensions = ['.pdf', '.zip', '.png', '.jpg', '.jpeg', '.gif', '.svg', 
                                  '.mp4', '.mov', '.avi', '.mp3', '.ico', '.css', '.js']
            
            if any(url.lower().endswith(ext) for ext in unwanted_extensions):
                return False
            
            # Verificar que sea del mismo dominio
            if parsed_url.netloc != self.domain:
                return False
                
            # Evitar anchors dentro de la misma página
            if '#' in url and url.split('#')[0] in self.visited_urls:
                return False
                
            return True
        except Exception as e:
            logger.error(f"Error validando URL {url}: {e}")
            return False
    
    def extract_links(self, soup, current_url):
        """Extrae todos los enlaces relevantes de la página"""
        links = set()
        for a_tag in soup.find_all('a', href=True):
            href = a_tag['href']
            absolute_url = urljoin(current_url, href)
            
            # Eliminar el fragmento de la URL (parte después del #)
            absolute_url = absolute_url.split('#')[0]
            
            if self.is_valid_url(absolute_url) and absolute_url not in self.visited_urls:
                links.add(absolute_url)
        
        return links
    
    def clean_content(self, soup):
        """Limpia el HTML para quedarse solo con el contenido relevante"""
        # Eliminar elementos no deseados
        for element in soup.select('nav, footer, header, script, style, iframe, .ad, .ads, .advertisement, .cookie-notice'):
            element.extract()
        
        # Buscar el contenido principal - esto puede variar según el sitio
        main_content = soup.select_one('main, article, .content, .documentation, .docs, #content, #main')
        
        if main_content:
            return main_content
        
        return soup
    
    def html_to_markdown(self, html_content):
        """Convierte el contenido HTML a markdown"""
        return self.html_converter.handle(str(html_content))
    
    def save_markdown(self, url, content):
        """Guarda el contenido en markdown en un archivo"""
        parsed_url = urlparse(url)
        path = parsed_url.path.strip('/')
        
        if not path:
            # Si es la página principal
            filename = "index.md"
        else:
            # Crear una estructura de directorios similar a la URL
            filename = f"{path}.md"
            if path.endswith('/'):
                filename = f"{path}index.md"
        
        file_path = os.path.join(self.output_dir, filename)
        os.makedirs(os.path.dirname(file_path), exist_ok=True)
        
        # Añadir metadatos al markdown
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(f"---\n")
            f.write(f"url: {url}\n")
            f.write(f"source: {self.domain}\n")
            f.write(f"scrape_date: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"---\n\n")
            f.write(content)
        
        logger.info(f"Guardado: {file_path}")
    
    def process_page(self, url):
        """Procesa una página: descarga, extrae contenido y guarda en markdown"""
        if url in self.visited_urls:
            return set()
        
        self.visited_urls.add(url)
        
        try:
            logger.info(f"Procesando: {url}")
            response = requests.get(url, timeout=10)
            if response.status_code != 200:
                logger.warning(f"No se pudo acceder a {url}, código: {response.status_code}")
                return set()
            
            # Detectar el encoding correcto
            if 'charset' in response.headers.get('content-type', '').lower():
                response.encoding = response.apparent_encoding
            
            soup = BeautifulSoup(response.text, 'html.parser')
            
            # Extraer el título
            title = soup.title.string if soup.title else url
            
            # Limpiar y obtener el contenido relevante
            cleaned_content = self.clean_content(soup)
            markdown_content = self.html_to_markdown(cleaned_content)
            
            # Asegurar que el título esté al principio
            markdown_content = f"# {title}\n\n{markdown_content}"
            
            # Guardar el contenido
            self.save_markdown(url, markdown_content)
            
            # Encontrar nuevos enlaces
            new_links = self.extract_links(soup, url)
            
            time.sleep(self.delay)  # Ser amable con el servidor
            return new_links
            
        except Exception as e:
            logger.error(f"Error procesando {url}: {e}")
            return set()
    
    def run(self):
        """Ejecuta el scraping completo"""
        logger.info(f"Iniciando scraping desde {self.base_url}")
        logger.info(f"Los archivos se guardarán en {os.path.abspath(self.output_dir)}")
        
        processed_count = 0
        
        # Usar ThreadPoolExecutor para procesar múltiples páginas en paralelo
        with concurrent.futures.ThreadPoolExecutor(max_workers=self.concurrency) as executor:
            while self.to_visit and (self.max_pages is None or processed_count < self.max_pages):
                # Tomar un lote de URLs para procesar en paralelo
                batch_size = min(self.concurrency, len(self.to_visit))
                if self.max_pages:
                    batch_size = min(batch_size, self.max_pages - processed_count)
                
                current_batch = []
                for _ in range(batch_size):
                    if not self.to_visit:
                        break
                    url = self.to_visit.pop()
                    current_batch.append(url)
                
                if not current_batch:
                    break
                
                # Procesar el lote en paralelo
                future_to_url = {executor.submit(self.process_page, url): url for url in current_batch}
                
                for future in concurrent.futures.as_completed(future_to_url):
                    url = future_to_url[future]
                    try:
                        new_links = future.result()
                        # Añadir enlaces nuevos a la lista por visitar
                        for link in new_links:
                            if link not in self.visited_urls and link not in self.to_visit:
                                self.to_visit.add(link)
                    except Exception as e:
                        logger.error(f"Error en el procesamiento futuro de {url}: {e}")
                
                processed_count += len(current_batch)
                logger.info(f"Progreso: {processed_count} páginas procesadas, {len(self.to_visit)} en cola")
        
        logger.info(f"Scraping completado. Páginas procesadas: {processed_count}")


def get_user_input():
    """Obtiene la configuración del usuario de forma interactiva"""
    print("\n===== Scraper de Documentación a Markdown =====\n")
    
    # Solicitar URL base
    while True:
        url = input("Introduce la URL de la documentación a procesar (ej: https://tailwindcss.com/docs): ")
        if url.startswith(('http://', 'https://')):
            break
        else:
            print("Por favor, introduce una URL válida que comience con http:// o https://")
    
    # Solicitar opciones adicionales
    print("\n--- Opciones adicionales (presiona Enter para usar los valores por defecto) ---")
    
    # Directorio de salida
    output_dir = input("Directorio de salida [./docs_output]: ").strip()
    if not output_dir:
        output_dir = "./docs_output"
    
    # Límite de páginas
    while True:
        max_pages_input = input("Número máximo de páginas a procesar (Enter para ilimitado): ").strip()
        if not max_pages_input:
            max_pages = None
            break
        try:
            max_pages = int(max_pages_input)
            if max_pages <= 0:
                print("Por favor, introduce un número positivo")
                continue
            break
        except ValueError:
            print("Por favor, introduce un número válido")
    
    # Delay entre peticiones
    while True:
        delay_input = input("Tiempo de espera entre peticiones en segundos [1.0]: ").strip()
        if not delay_input:
            delay = 1.0
            break
        try:
            delay = float(delay_input)
            if delay < 0:
                print("Por favor, introduce un número positivo")
                continue
            break
        except ValueError:
            print("Por favor, introduce un número válido")
    
    # Concurrencia
    while True:
        concurrency_input = input("Número de páginas a procesar en paralelo [5]: ").strip()
        if not concurrency_input:
            concurrency = 5
            break
        try:
            concurrency = int(concurrency_input)
            if concurrency <= 0:
                print("Por favor, introduce un número positivo")
                continue
            break
        except ValueError:
            print("Por favor, introduce un número válido")
    
    # Mostrar resumen de configuración
    print("\n=== Resumen de configuración ===")
    print(f"URL base: {url}")
    print(f"Directorio de salida: {output_dir}")
    print(f"Máximo de páginas: {'Ilimitado' if max_pages is None else max_pages}")
    print(f"Delay entre peticiones: {delay} segundos")
    print(f"Concurrencia: {concurrency} páginas en paralelo")
    
    # Confirmar inicio
    while True:
        confirm = input("\n¿Iniciar el scraping con esta configuración? (s/n): ").lower()
        if confirm in ['s', 'si', 'sí', 'y', 'yes']:
            return {
                'url': url,
                'output_dir': output_dir,
                'max_pages': max_pages,
                'delay': delay,
                'concurrency': concurrency
            }
        elif confirm in ['n', 'no']:
            print("\nVuelve a introducir la configuración.")
            return get_user_input()
        else:
            print("Por favor, responde 's' para sí o 'n' para no.")


def main():
    """Función principal interactiva"""
    try:
        # Verificar si hay argumentos en línea de comandos
        parser = argparse.ArgumentParser(description='Hacer scraping de documentación web y convertirla a markdown')
        parser.add_argument('url', nargs='?', help='URL base para iniciar el scraping')
        parser.add_argument('-o', '--output', help='Directorio de salida (default: ./docs_output)')
        parser.add_argument('-m', '--max-pages', type=int, help='Máximo número de páginas a procesar')
        parser.add_argument('-d', '--delay', type=float, help='Delay entre peticiones en segundos (default: 1.0)')
        parser.add_argument('-c', '--concurrency', type=int, help='Número de páginas a procesar en paralelo (default: 5)')
        parser.add_argument('-i', '--interactive', action='store_true', help='Modo interactivo')
        
        args = parser.parse_args()
        
        # Si no hay URL o se especifica modo interactivo, usar la interfaz interactiva
        if not args.url or args.interactive:
            config = get_user_input()
            url = config['url']
            output_dir = config['output_dir']
            max_pages = config['max_pages']
            delay = config['delay']
            concurrency = config['concurrency']
        else:
            # Usar argumentos de línea de comandos
            url = args.url
            output_dir = args.output if args.output else "./docs_output"
            max_pages = args.max_pages
            delay = args.delay if args.delay is not None else 1.0
            concurrency = args.concurrency if args.concurrency is not None else 5
        
        # Iniciar el scraper con la configuración obtenida
        scraper = DocsScraperToMarkdown(
            url,
            output_dir=output_dir,
            max_pages=max_pages,
            delay=delay,
            concurrency=concurrency
        )
        scraper.run()
        
        print("\n¡Scraping completado con éxito!")
        print(f"Los archivos se han guardado en: {os.path.abspath(output_dir)}")
        
    except KeyboardInterrupt:
        print("\nScraping interrumpido por el usuario")
        return 1
    except Exception as e:
        logger.error(f"Error: {e}")
        print(f"\nError durante el scraping: {e}")
        return 1
    
    return 0


if __name__ == "__main__":
    exit(main())
