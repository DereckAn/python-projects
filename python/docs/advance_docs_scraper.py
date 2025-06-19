#!/usr/bin/env python3
"""
Advanced Documentation Scraper
===============================

Un scraper robusto para documentación web que convierte páginas a Markdown/MDX
con soporte para JavaScript (Playwright), rastreo inteligente de enlaces,
y post-procesamiento de contenido.

Características:
- Rastreo completo de enlaces hijos con persistencia
- Soporte opcional para Playwright (sitios con JavaScript)
- Corrección automática de formato Markdown
- Conversión a .mdx
- Análisis y reporte de progreso
- Configuración flexible

Autor: Dereck Angeles
Versión: 2.0
"""

import os
import re
import json
import pickle
import time
import asyncio
import argparse
import logging
from pathlib import Path
from typing import Set, Dict, List, Optional, Union
from urllib.parse import urlparse, urljoin, quote
from dataclasses import dataclass, asdict
from datetime import datetime

# Importaciones estándar
import requests
from bs4 import BeautifulSoup
import html2text
from markdownify import markdownify as md
import concurrent.futures
from tqdm import tqdm

# Importación opcional de Playwright
try:
    from playwright.async_api import async_playwright
    PLAYWRIGHT_AVAILABLE = True
except ImportError:
    PLAYWRIGHT_AVAILABLE = False
    print("⚠️  Playwright no disponible. Instala con: pip install playwright")

# Configurar logging mejorado
def setup_logging(log_file: str = "advanced_scraper.log", level: str = "INFO"):
    """Configura el sistema de logging"""
    log_level = getattr(logging, level.upper())

    # Crear formatters
    detailed_formatter = logging.Formatter(
        '%(asctime)s | %(levelname)-8s | %(funcName)s:%(lineno)d | %(message)s'
    )
    simple_formatter = logging.Formatter(
        '%(asctime)s | %(levelname)-8s | %(message)s'
    )

    # Configurar logger principal
    logger = logging.getLogger(__name__)
    logger.setLevel(log_level)

    # Handler para archivo (detallado)
    file_handler = logging.FileHandler(log_file, encoding='utf-8')
    file_handler.setFormatter(detailed_formatter)

    # Handler para consola (simple)
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(simple_formatter)

    logger.addHandler(file_handler)
    logger.addHandler(console_handler)

    return logger

logger = setup_logging()

@dataclass
class ScrapingStats:
    """Estadísticas del proceso de scraping"""
    total_discovered: int = 0
    total_processed: int = 0
    total_successful: int = 0
    total_failed: int = 0
    total_skipped: int = 0
    start_time: Optional[datetime] = None
    end_time: Optional[datetime] = None

    def to_dict(self) -> Dict:
        return asdict(self)

    @property
    def duration(self) -> Optional[float]:
        if self.start_time and self.end_time:
            return (self.end_time - self.start_time).total_seconds()
        return None

class LinkTracker:
    """Clase para manejar el seguimiento persistente de enlaces"""

    def __init__(self, base_url: str, cache_file: str = "link_cache.pkl"):
        self.base_url = base_url
        self.domain = urlparse(base_url).netloc
        self.cache_file = cache_file

        # Conjuntos de URLs
        self.discovered_urls: Set[str] = set()
        self.visited_urls: Set[str] = set()
        self.failed_urls: Set[str] = set()
        self.skipped_urls: Set[str] = set()

        # Metadata de URLs
        self.url_metadata: Dict[str, Dict] = {}

        self.load_cache()

    def load_cache(self):
        """Carga el cache de enlaces desde archivo"""
        if os.path.exists(self.cache_file):
            try:
                with open(self.cache_file, 'rb') as f:
                    data = pickle.load(f)
                    self.discovered_urls = data.get('discovered', set())
                    self.visited_urls = data.get('visited', set())
                    self.failed_urls = data.get('failed', set())
                    self.skipped_urls = data.get('skipped', set())
                    self.url_metadata = data.get('metadata', {})
                logger.info(f"Cache cargado: {len(self.discovered_urls)} URLs descubiertas")
            except Exception as e:
                logger.warning(f"Error cargando cache: {e}")

    def save_cache(self):
        """Guarda el cache de enlaces a archivo"""
        try:
            data = {
                'discovered': self.discovered_urls,
                'visited': self.visited_urls,
                'failed': self.failed_urls,
                'skipped': self.skipped_urls,
                'metadata': self.url_metadata,
                'last_update': datetime.now().isoformat()
            }
            with open(self.cache_file, 'wb') as f:
                pickle.dump(data, f)
        except Exception as e:
            logger.error(f"Error guardando cache: {e}")

    def is_valid_url(self, url: str) -> bool:
        """Verifica si una URL es válida para scraping"""
        try:
            parsed = urlparse(url)

            # Verificar dominio
            if parsed.netloc != self.domain:
                return False

            # Extensiones no deseadas
            unwanted_extensions = {
                '.pdf', '.zip', '.rar', '.7z', '.tar', '.gz',
                '.png', '.jpg', '.jpeg', '.gif', '.svg', '.webp', '.ico',
                '.mp4', '.mov', '.avi', '.mkv', '.mp3', '.wav',
                '.css', '.js', '.json', '.xml', '.rss',
                '.exe', '.msi', '.dmg', '.deb', '.rpm'
            }

            if any(url.lower().endswith(ext) for ext in unwanted_extensions):
                return False

            # Evitar parámetros problemáticos
            if '?' in url and any(param in url.lower() for param in ['download', 'export', 'print']):
                return False

            return True

        except Exception:
            return False

    def add_discovered_urls(self, urls: Set[str], source_url: str = None):
        """Añade URLs descubiertas al tracker"""
        new_urls = set()
        for url in urls:
            if self.is_valid_url(url) and url not in self.discovered_urls:
                self.discovered_urls.add(url)
                new_urls.add(url)

                # Añadir metadata
                self.url_metadata[url] = {
                    'discovered_at': datetime.now().isoformat(),
                    'source_url': source_url,
                    'depth': self._calculate_depth(url)
                }

        if new_urls:
            logger.info(f"Descubiertas {len(new_urls)} nuevas URLs")
            self.save_cache()

        return new_urls

    def _calculate_depth(self, url: str) -> int:
        """Calcula la profundidad de una URL relativa a la base"""
        base_path = urlparse(self.base_url).path.strip('/')
        url_path = urlparse(url).path.strip('/')

        if not base_path:
            return len(url_path.split('/')) if url_path else 0

        return len(url_path.replace(base_path, '').strip('/').split('/')) if url_path else 0

    def get_next_urls(self, limit: int = None) -> List[str]:
        """Obtiene las próximas URLs a procesar"""
        pending = self.discovered_urls - self.visited_urls - self.failed_urls - self.skipped_urls

        # Ordenar por profundidad (menos profundo primero)
        sorted_urls = sorted(pending, key=lambda url: self.url_metadata.get(url, {}).get('depth', 999))

        return sorted_urls[:limit] if limit else sorted_urls

    def mark_visited(self, url: str):
        """Marca una URL como visitada"""
        self.visited_urls.add(url)
        if url in self.url_metadata:
            self.url_metadata[url]['processed_at'] = datetime.now().isoformat()

    def mark_failed(self, url: str, error: str = None):
        """Marca una URL como fallida"""
        self.failed_urls.add(url)
        if url in self.url_metadata:
            self.url_metadata[url]['error'] = error
            self.url_metadata[url]['failed_at'] = datetime.now().isoformat()

    def mark_skipped(self, url: str, reason: str = None):
        """Marca una URL como omitida"""
        self.skipped_urls.add(url)
        if url in self.url_metadata:
            self.url_metadata[url]['skip_reason'] = reason
            self.url_metadata[url]['skipped_at'] = datetime.now().isoformat()

    def get_stats(self) -> Dict:
        """Obtiene estadísticas del tracker"""
        return {
            'discovered': len(self.discovered_urls),
            'visited': len(self.visited_urls),
            'failed': len(self.failed_urls),
            'skipped': len(self.skipped_urls),
            'pending': len(self.get_next_urls())
        }

class MarkdownProcessor:
    """Clase para procesar y corregir contenido Markdown"""

    def __init__(self):
        self.html_converter = html2text.HTML2Text()
        self.html_converter.ignore_links = False
        self.html_converter.ignore_images = False
        self.html_converter.body_width = 0
        self.html_converter.protect_links = True
        self.html_converter.unicode_snob = True
        self.html_converter.bypass_tables = False

    def html_to_markdown(self, html_content: str) -> str:
        """Convierte HTML a Markdown usando html2text"""
        try:
            return self.html_converter.handle(html_content)
        except Exception as e:
            logger.warning(f"Error convirtiendo HTML a Markdown: {e}")
            # Fallback usando markdownify
            return md(html_content, strip=['script', 'style'])

    def clean_markdown(self, content: str) -> str:
        """Limpia y corrige el contenido Markdown"""
        if not content:
            return ""

        # Correcciones comunes
        fixes = [
            # Eliminar líneas vacías excesivas
            (r'\n{4,}', '\n\n\n'),
            # Corregir espacios en enlaces
            (r'\[\s*([^\]]+)\s*\]\s*\(\s*([^)]+)\s*\)', r'[\1](\2)'),
            # Corregir listas mal formateadas
            (r'\n\s*[-*+]\s*\n', '\n\n* '),
            # Corregir código inline mal formado
            (r'`\s+([^`]+)\s+`', r'`\1`'),
            # Eliminar espacios al final de líneas
            (r' +\n', '\n'),
            # Corregir encabezados mal formados
            (r'\n(#{1,6})\s*\n', r'\n\1 '),
            # Corregir tablas rotas
            (r'\|\s*\|\s*\|', '| |'),
        ]

        for pattern, replacement in fixes:
            content = re.sub(pattern, replacement, content, flags=re.MULTILINE)

        return content.strip()

    def extract_metadata(self, soup: BeautifulSoup, url: str) -> Dict:
        """Extrae metadata de la página"""
        metadata = {
            'url': url,
            'title': '',
            'description': '',
            'keywords': [],
            'author': '',
            'scraped_at': datetime.now().isoformat()
        }

        # Título
        if soup.title:
            metadata['title'] = soup.title.get_text().strip()

        # Meta tags
        for meta in soup.find_all('meta'):
            name = meta.get('name', '').lower()
            content = meta.get('content', '')

            if name in ['description', 'og:description']:
                metadata['description'] = content
            elif name in ['keywords']:
                metadata['keywords'] = [k.strip() for k in content.split(',')]
            elif name in ['author']:
                metadata['author'] = content

        return metadata

    def add_frontmatter(self, content: str, metadata: Dict) -> str:
        """Añade frontmatter YAML al contenido"""
        frontmatter_lines = ['---']

        for key, value in metadata.items():
            if value:  # Solo añadir si tiene valor
                if isinstance(value, list):
                    if value:  # Solo si la lista no está vacía
                        frontmatter_lines.append(f'{key}:')
                        for item in value:
                            frontmatter_lines.append(f'  - {item}')
                else:
                    # Escapar comillas en el valor
                    safe_value = str(value).replace('"', '\\"')
                    frontmatter_lines.append(f'{key}: "{safe_value}"')

        frontmatter_lines.append('---')
        frontmatter_lines.append('')

        return '\n'.join(frontmatter_lines) + content

class AdvancedDocsScraper:
    """Scraper avanzado de documentación con múltiples métodos de extracción"""

    def __init__(
        self,
        base_url: str,
        output_dir: str = "./docs_output",
        max_pages: Optional[int] = None,
        delay: float = 1.0,
        concurrency: int = 5,
        use_playwright: bool = False,
        convert_to_mdx: bool = False,
        custom_selectors: Optional[Dict[str, str]] = None
    ):
        self.base_url = base_url
        self.domain = urlparse(base_url).netloc
        self.output_dir = Path(output_dir)
        self.max_pages = max_pages
        self.delay = delay
        self.concurrency = concurrency
        self.use_playwright = use_playwright and PLAYWRIGHT_AVAILABLE
        self.convert_to_mdx = convert_to_mdx
        self.custom_selectors = custom_selectors or {}

        # Componentes
        self.link_tracker = LinkTracker(base_url)
        self.markdown_processor = MarkdownProcessor()
        self.stats = ScrapingStats()

        # Crear directorio de salida
        self.output_dir.mkdir(parents=True, exist_ok=True)

        # Session para requests
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        })

        logger.info(f"Scraper inicializado - Motor: {'Playwright' if self.use_playwright else 'Requests'}")

    def extract_content_selectors(self, soup: BeautifulSoup) -> BeautifulSoup:
        """Extrae contenido usando selectores personalizados o genéricos"""

        # Selectores personalizados del usuario
        if self.custom_selectors:
            for name, selector in self.custom_selectors.items():
                elements = soup.select(selector)
                if elements:
                    logger.debug(f"Usando selector personalizado '{name}': {selector}")
                    return elements[0]

        # Selectores genéricos comunes para documentación
        common_selectors = [
            'main',
            'article',
            '.content',
            '.documentation',
            '.docs',
            '.doc-content',
            '.main-content',
            '.primary-content',
            '#content',
            '#main',
            '.markdown-body',
            '[role="main"]',
            '.prose'
        ]

        for selector in common_selectors:
            elements = soup.select(selector)
            if elements:
                logger.debug(f"Usando selector genérico: {selector}")
                return elements[0]

        # Si no encuentra selectores específicos, limpiar el body
        body = soup.find('body')
        if body:
            # Eliminar elementos no deseados
            unwanted_selectors = [
                'nav', 'header', 'footer', 'aside', 'script', 'style', 'noscript',
                '.navigation', '.nav', '.sidebar', '.menu', '.header', '.footer',
                '.ad', '.ads', '.advertisement', '.cookie-notice', '.banner',
                '.social', '.share', '.related', '.comments'
            ]

            for selector in unwanted_selectors:
                for element in body.select(selector):
                    element.decompose()

            return body

        return soup

    def extract_links_from_soup(self, soup: BeautifulSoup, current_url: str) -> Set[str]:
        """Extrae enlaces de la página de forma inteligente"""
        links = set()

        # Selectores para enlaces de documentación
        link_selectors = [
            'nav a[href]',  # Enlaces de navegación
            '.toc a[href]', # Tabla de contenidos
            '.sidebar a[href]',  # Sidebar
            '.menu a[href]',     # Menú
            'main a[href]',      # Enlaces en contenido principal
            '.docs a[href]',     # Enlaces en docs
            '.documentation a[href]',  # Enlaces en documentación
        ]

        # Primero, intentar selectores específicos
        for selector in link_selectors:
            for link in soup.select(selector):
                href = link.get('href')
                if href:
                    absolute_url = urljoin(current_url, href)
                    # Limpiar URL (remover anchors)
                    clean_url = absolute_url.split('#')[0]
                    if self.link_tracker.is_valid_url(clean_url):
                        links.add(clean_url)

        # Si no encontramos suficientes enlaces, buscar todos los enlaces
        if len(links) < 5:  # Umbral mínimo
            for link in soup.find_all('a', href=True):
                href = link.get('href')
                if href:
                    absolute_url = urljoin(current_url, href)
                    clean_url = absolute_url.split('#')[0]
                    if self.link_tracker.is_valid_url(clean_url):
                        links.add(clean_url)

        return links

    async def scrape_with_playwright(self, url: str) -> tuple[str, Set[str]]:
        """Scraping usando Playwright para sitios con JavaScript"""
        try:
            async with async_playwright() as p:
                browser = await p.chromium.launch(headless=True)

                try:
                    page = await browser.new_page()
                    await page.goto(url, wait_until='domcontentloaded', timeout=30000)

                    # Esperar un poco para que se cargue el JavaScript
                    await page.wait_for_timeout(2000)

                    # Obtener el HTML renderizado
                    html_content = await page.content()

                    # Procesar con BeautifulSoup
                    soup = BeautifulSoup(html_content, 'html.parser')

                    # Extraer contenido y enlaces
                    content_element = self.extract_content_selectors(soup)
                    links = self.extract_links_from_soup(soup, url)

                    # Extraer metadata
                    metadata = self.markdown_processor.extract_metadata(soup, url)

                    # Convertir a markdown
                    markdown_content = self.markdown_processor.html_to_markdown(str(content_element))
                    cleaned_markdown = self.markdown_processor.clean_markdown(markdown_content)

                    # Añadir frontmatter
                    final_content = self.markdown_processor.add_frontmatter(cleaned_markdown, metadata)

                    return final_content, links

                finally:
                    await browser.close()

        except Exception as e:
            logger.error(f"Error con Playwright en {url}: {e}")
            raise

    def scrape_with_requests(self, url: str) -> tuple[str, Set[str]]:
        """Scraping usando requests para sitios estáticos"""
        try:
            response = self.session.get(url, timeout=30)
            response.raise_for_status()

            # Detectar encoding
            if response.encoding is None or response.encoding == 'ISO-8859-1':
                response.encoding = response.apparent_encoding or 'utf-8'

            soup = BeautifulSoup(response.text, 'html.parser')

            # Extraer contenido y enlaces
            content_element = self.extract_content_selectors(soup)
            links = self.extract_links_from_soup(soup, url)

            # Extraer metadata
            metadata = self.markdown_processor.extract_metadata(soup, url)

            # Convertir a markdown
            markdown_content = self.markdown_processor.html_to_markdown(str(content_element))
            cleaned_markdown = self.markdown_processor.clean_markdown(markdown_content)

            # Añadir frontmatter
            final_content = self.markdown_processor.add_frontmatter(cleaned_markdown, metadata)

            return final_content, links

        except Exception as e:
            logger.error(f"Error con requests en {url}: {e}")
            raise

    def get_file_path(self, url: str) -> Path:
        """Genera la ruta del archivo para una URL"""
        parsed = urlparse(url)
        path = parsed.path.strip('/')

        if not path:
            filename = "index"
        else:
            # Reemplazar caracteres problemáticos
            filename = re.sub(r'[<>:"/\\|?*]', '_', path)
            filename = filename.replace('/', '_')

        # Extensión
        extension = '.mdx' if self.convert_to_mdx else '.md'

        return self.output_dir / f"{filename}{extension}"

    def save_content(self, url: str, content: str):
        """Guarda el contenido en archivo"""
        file_path = self.get_file_path(url)

        try:
            # Crear directorios si no existen
            file_path.parent.mkdir(parents=True, exist_ok=True)

            # Guardar contenido
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)

            logger.info(f"Guardado: {file_path}")

        except Exception as e:
            logger.error(f"Error guardando {url} en {file_path}: {e}")
            raise

    async def process_page(self, url: str) -> bool:
        """Procesa una página individual"""
        if url in self.link_tracker.visited_urls:
            self.link_tracker.mark_skipped(url, "Ya visitada")
            return False

        # Verificar si el archivo ya existe
        file_path = self.get_file_path(url)
        if file_path.exists():
            self.link_tracker.mark_skipped(url, "Archivo ya existe")
            logger.info(f"Omitiendo {url} - archivo ya existe")
            return False

        try:
            logger.info(f"Procesando: {url}")

            # Scraping según el método configurado
            if self.use_playwright:
                content, links = await self.scrape_with_playwright(url)
            else:
                content, links = self.scrape_with_requests(url)

            # Guardar contenido
            self.save_content(url, content)

            # Actualizar tracker
            self.link_tracker.mark_visited(url)
            self.link_tracker.add_discovered_urls(links, url)

            # Delay entre peticiones
            if self.delay > 0:
                await asyncio.sleep(self.delay)

            self.stats.total_successful += 1
            return True

        except Exception as e:
            logger.error(f"Error procesando {url}: {e}")
            self.link_tracker.mark_failed(url, str(e))
            self.stats.total_failed += 1
            return False

    async def run_async(self):
        """Ejecuta el scraping de forma asíncrona"""
        self.stats.start_time = datetime.now()
        logger.info(f"Iniciando scraping desde: {self.base_url}")

        # Añadir URL base si no está en el tracker
        if self.base_url not in self.link_tracker.discovered_urls:
            self.link_tracker.add_discovered_urls({self.base_url})

        # Semáforo para controlar concurrencia
        semaphore = asyncio.Semaphore(self.concurrency)

        async def process_with_semaphore(url):
            async with semaphore:
                return await self.process_page(url)

        # Loop principal de scraping
        while True:
            # Obtener próximas URLs a procesar
            next_urls = self.link_tracker.get_next_urls(self.concurrency * 2)

            if not next_urls:
                break

            if self.max_pages and self.stats.total_processed >= self.max_pages:
                logger.info(f"Límite de páginas alcanzado: {self.max_pages}")
                break

            # Limitar por max_pages si está configurado
            if self.max_pages:
                remaining = self.max_pages - self.stats.total_processed
                next_urls = next_urls[:remaining]

            # Procesar URLs en lotes
            logger.info(f"Procesando lote de {len(next_urls)} URLs...")

            tasks = [process_with_semaphore(url) for url in next_urls]
            results = await asyncio.gather(*tasks, return_exceptions=True)

            # Actualizar estadísticas
            self.stats.total_processed += len(next_urls)

            # Mostrar progreso
            tracker_stats = self.link_tracker.get_stats()
            logger.info(f"Progreso: {tracker_stats}")

        self.stats.end_time = datetime.now()
        self.link_tracker.save_cache()

        # Reporte final
        self.print_final_report()

    def run(self):
        """Ejecuta el scraping (wrapper síncrono)"""
        if self.use_playwright:
            asyncio.run(self.run_async())
        else:
            # Para requests, podemos usar threading
            asyncio.run(self.run_async())

    def print_final_report(self):
        """Imprime reporte final del scraping"""
        stats = self.link_tracker.get_stats()

        print("\n" + "="*60)
        print("             REPORTE FINAL DE SCRAPING")
        print("="*60)
        print(f"🌐 Sitio web:       {self.domain}")
        print(f"📁 Directorio:      {self.output_dir}")
        print(f"⚙️  Motor:           {'Playwright' if self.use_playwright else 'Requests'}")
        print(f"📄 Formato:         {'.mdx' if self.convert_to_mdx else '.md'}")
        print("-"*60)
        print(f"🔍 URLs descubiertas:  {stats['discovered']}")
        print(f"✅ URLs procesadas:    {stats['visited']}")
        print(f"❌ URLs fallidas:      {stats['failed']}")
        print(f"⏭️  URLs omitidas:      {stats['skipped']}")
        print(f"⏳ URLs pendientes:    {stats['pending']}")
        print("-"*60)

        if self.stats.duration:
            print(f"⏱️  Duración:          {self.stats.duration:.2f} segundos")
            if stats['visited'] > 0:
                avg_time = self.stats.duration / stats['visited']
                print(f"📊 Promedio por página: {avg_time:.2f} segundos")

        print("="*60)

        # Sugerencias
        if stats['failed'] > 0:
            print("💡 Hay URLs fallidas. Revisa el log para más detalles.")

        if stats['pending'] > 0:
            print(f"💡 Quedan {stats['pending']} URLs pendientes. Ejecuta nuevamente para continuar.")

    def convert_md_to_mdx(self):
        """Convierte archivos .md existentes a .mdx"""
        md_files = list(self.output_dir.rglob("*.md"))

        if not md_files:
            logger.info("No se encontraron archivos .md para convertir")
            return

        logger.info(f"Convirtiendo {len(md_files)} archivos .md a .mdx...")

        for md_file in tqdm(md_files, desc="Convirtiendo"):
            mdx_file = md_file.with_suffix('.mdx')

            try:
                # Leer contenido
                content = md_file.read_text(encoding='utf-8')

                # Aplicar transformaciones específicas para MDX si es necesario
                # Por ejemplo, escape de caracteres especiales, imports, etc.

                # Guardar como .mdx
                mdx_file.write_text(content, encoding='utf-8')

                # Eliminar .md original
                md_file.unlink()

            except Exception as e:
                logger.error(f"Error convirtiendo {md_file}: {e}")

        logger.info("Conversión completada")

def get_interactive_config():
    """Obtiene configuración de forma interactiva"""
    print("\n" + "="*60)
    print("        SCRAPER AVANZADO DE DOCUMENTACIÓN")
    print("="*60)

    # URL base
    while True:
        url = input("\n🌐 URL de la documentación: ").strip()
        if url.startswith(('http://', 'https://')):
            break
        print("❌ Por favor, introduce una URL válida (http:// o https://)")

    # Directorio de salida
    output_dir = input(f"📁 Directorio de salida [./docs_output]: ").strip()
    if not output_dir:
        output_dir = "./docs_output"

    # Límite de páginas
    while True:
        max_pages_input = input("📄 Número máximo de páginas [ilimitado]: ").strip()
        if not max_pages_input:
            max_pages = None
            break
        try:
            max_pages = int(max_pages_input)
            if max_pages <= 0:
                print("❌ Introduce un número positivo")
                continue
            break
        except ValueError:
            print("❌ Introduce un número válido")

    # Delay entre peticiones
    while True:
        delay_input = input("⏱️  Delay entre peticiones (segundos) [1.0]: ").strip()
        if not delay_input:
            delay = 1.0
            break
        try:
            delay = float(delay_input)
            if delay < 0:
                print("❌ Introduce un número positivo")
                continue
            break
        except ValueError:
            print("❌ Introduce un número válido")

    # Concurrencia
    while True:
        concurrency_input = input("🔄 Páginas en paralelo [5]: ").strip()
        if not concurrency_input:
            concurrency = 5
            break
        try:
            concurrency = int(concurrency_input)
            if concurrency <= 0:
                print("❌ Introduce un número positivo")
                continue
            break
        except ValueError:
            print("❌ Introduce un número válido")

    # Usar Playwright
    use_playwright = False
    if PLAYWRIGHT_AVAILABLE:
        while True:
            playwright_input = input("🎭 ¿Usar Playwright para JavaScript? [s/N]: ").strip().lower()
            if playwright_input in ['s', 'si', 'sí', 'y', 'yes']:
                use_playwright = True
                break
            elif playwright_input in ['n', 'no', '']:
                use_playwright = False
                break
            else:
                print("❌ Responde 's' para sí o 'n' para no")
    else:
        print("⚠️  Playwright no disponible - usando requests")

    # Convertir a MDX
    while True:
        mdx_input = input("📝 ¿Convertir a formato .mdx? [s/N]: ").strip().lower()
        if mdx_input in ['s', 'si', 'sí', 'y', 'yes']:
            convert_to_mdx = True
            break
        elif mdx_input in ['n', 'no', '']:
            convert_to_mdx = False
            break
        else:
            print("❌ Responde 's' para sí o 'n' para no")

    # Selectores personalizados
    print("\n🎯 Selectores CSS personalizados (opcional)")
    print("   Ejemplo: main=main.content, sidebar=.sidebar")
    custom_selectors = {}
    selectors_input = input("   Selectores [Enter para omitir]: ").strip()

    if selectors_input:
        try:
            for selector_pair in selectors_input.split(','):
                if '=' in selector_pair:
                    name, selector = selector_pair.split('=', 1)
                    custom_selectors[name.strip()] = selector.strip()
        except Exception as e:
            print(f"⚠️  Error procesando selectores: {e}")

    # Mostrar resumen
    print("\n" + "="*60)
    print("                RESUMEN DE CONFIGURACIÓN")
    print("="*60)
    print(f"🌐 URL:                {url}")
    print(f"📁 Directorio:         {output_dir}")
    print(f"📄 Límite de páginas:  {'Ilimitado' if max_pages is None else max_pages}")
    print(f"⏱️  Delay:              {delay}s")
    print(f"🔄 Concurrencia:       {concurrency}")
    print(f"🎭 Playwright:         {'Sí' if use_playwright else 'No'}")
    print(f"📝 Formato MDX:        {'Sí' if convert_to_mdx else 'No'}")
    if custom_selectors:
        print(f"🎯 Selectores:         {custom_selectors}")
    print("="*60)

    # Confirmar
    while True:
        confirm = input("\n✅ ¿Iniciar scraping? [S/n]: ").strip().lower()
        if confirm in ['s', 'si', 'sí', 'y', 'yes', '']:
            return {
                'base_url': url,
                'output_dir': output_dir,
                'max_pages': max_pages,
                'delay': delay,
                'concurrency': concurrency,
                'use_playwright': use_playwright,
                'convert_to_mdx': convert_to_mdx,
                'custom_selectors': custom_selectors
            }
        elif confirm in ['n', 'no']:
            print("❌ Scraping cancelado")
            return None
        else:
            print("❌ Responde 's' para sí o 'n' para no")

def analyze_scraped_content(output_dir: str):
    """Analiza el contenido ya scrapeado"""
    output_path = Path(output_dir)

    if not output_path.exists():
        print(f"❌ Directorio {output_dir} no existe")
        return

    # Contar archivos
    md_files = list(output_path.rglob("*.md"))
    mdx_files = list(output_path.rglob("*.mdx"))

    print("\n" + "="*50)
    print("         ANÁLISIS DE CONTENIDO SCRAPEADO")
    print("="*50)
    print(f"📁 Directorio:      {output_path.absolute()}")
    print(f"📄 Archivos .md:    {len(md_files)}")
    print(f"📝 Archivos .mdx:   {len(mdx_files)}")
    print(f"📊 Total archivos:  {len(md_files) + len(mdx_files)}")

    # Analizar tamaños
    if md_files or mdx_files:
        all_files = md_files + mdx_files
        total_size = sum(f.stat().st_size for f in all_files)
        avg_size = total_size / len(all_files) if all_files else 0

        print(f"💾 Tamaño total:    {total_size / 1024 / 1024:.2f} MB")
        print(f"📏 Tamaño promedio: {avg_size / 1024:.2f} KB")

        # Archivos más grandes
        largest_files = sorted(all_files, key=lambda f: f.stat().st_size, reverse=True)[:5]
        print(f"\n📈 Archivos más grandes:")
        for i, file in enumerate(largest_files, 1):
            size_kb = file.stat().st_size / 1024
            print(f"   {i}. {file.name} ({size_kb:.1f} KB)")

    print("="*50)

def fix_markdown_formatting(output_dir: str):
    """Corrige problemas de formato en archivos markdown existentes"""
    output_path = Path(output_dir)

    if not output_path.exists():
        print(f"❌ Directorio {output_dir} no existe")
        return

    processor = MarkdownProcessor()

    # Encontrar archivos markdown
    md_files = list(output_path.rglob("*.md"))
    mdx_files = list(output_path.rglob("*.mdx"))
    all_files = md_files + mdx_files

    if not all_files:
        print("❌ No se encontraron archivos markdown")
        return

    print(f"\n🔧 Corrigiendo formato en {len(all_files)} archivos...")

    fixed_count = 0

    for file_path in tqdm(all_files, desc="Corrigiendo"):
        try:
            # Leer contenido
            original_content = file_path.read_text(encoding='utf-8')

            # Separar frontmatter del contenido
            if original_content.startswith('---'):
                parts = original_content.split('---', 2)
                if len(parts) >= 3:
                    frontmatter = f"---{parts[1]}---"
                    content = parts[2]
                else:
                    frontmatter = ""
                    content = original_content
            else:
                frontmatter = ""
                content = original_content

            # Aplicar correcciones
            cleaned_content = processor.clean_markdown(content)

            # Reconstruir archivo
            final_content = frontmatter + "\n" + cleaned_content if frontmatter else cleaned_content

            # Guardar solo si hay cambios
            if final_content != original_content:
                file_path.write_text(final_content, encoding='utf-8')
                fixed_count += 1

        except Exception as e:
            logger.error(f"Error corrigiendo {file_path}: {e}")

    print(f"✅ {fixed_count} archivos corregidos")

def main():
    """Función principal con interfaz mejorada"""
    parser = argparse.ArgumentParser(
        description="Scraper avanzado de documentación web",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Ejemplos de uso:
  python advanced_docs_scraper.py                                    # Modo interactivo
  python advanced_docs_scraper.py https://docs.example.com           # Scraping básico
  python advanced_docs_scraper.py https://docs.example.com -p        # Con Playwright
  python advanced_docs_scraper.py --analyze ./docs_output            # Analizar contenido
  python advanced_docs_scraper.py --fix-format ./docs_output         # Corregir formato
  python advanced_docs_scraper.py --convert-mdx ./docs_output        # Convertir a MDX
        """
    )

    # Argumentos principales
    parser.add_argument('url', nargs='?', help='URL base para scraping')
    parser.add_argument('-o', '--output', default='./docs_output',
                       help='Directorio de salida (default: ./docs_output)')
    parser.add_argument('-m', '--max-pages', type=int,
                       help='Número máximo de páginas')
    parser.add_argument('-d', '--delay', type=float, default=1.0,
                       help='Delay entre peticiones (default: 1.0)')
    parser.add_argument('-c', '--concurrency', type=int, default=5,
                       help='Páginas en paralelo (default: 5)')

    # Opciones avanzadas
    parser.add_argument('-p', '--playwright', action='store_true',
                       help='Usar Playwright para JavaScript')
    parser.add_argument('--mdx', action='store_true',
                       help='Convertir a formato .mdx')
    parser.add_argument('-i', '--interactive', action='store_true',
                       help='Modo interactivo')
    parser.add_argument('-s', '--selectors',
                       help='Selectores CSS personalizados (formato: name=selector,name2=selector2)')

    # Utilidades
    parser.add_argument('--analyze', metavar='DIR',
                       help='Analizar contenido existente')
    parser.add_argument('--fix-format', metavar='DIR',
                       help='Corregir formato de archivos existentes')
    parser.add_argument('--convert-mdx', metavar='DIR',
                       help='Convertir archivos .md a .mdx')

    args = parser.parse_args()

    try:
        # Utilidades
        if args.analyze:
            analyze_scraped_content(args.analyze)
            return 0

        if args.fix_format:
            fix_markdown_formatting(args.fix_format)
            return 0

        if args.convert_mdx:
            scraper = AdvancedDocsScraper(
                base_url="http://example.com",  # Dummy URL
                output_dir=args.convert_mdx
            )
            scraper.convert_md_to_mdx()
            return 0

        # Configuración principal
        if args.interactive or not args.url:
            config = get_interactive_config()
            if not config:
                return 1
        else:
            # Parsear selectores personalizados
            custom_selectors = {}
            if args.selectors:
                try:
                    for pair in args.selectors.split(','):
                        if '=' in pair:
                            name, selector = pair.split('=', 1)
                            custom_selectors[name.strip()] = selector.strip()
                except Exception as e:
                    print(f"❌ Error parseando selectores: {e}")
                    return 1

            config = {
                'base_url': args.url,
                'output_dir': args.output,
                'max_pages': args.max_pages,
                'delay': args.delay,
                'concurrency': args.concurrency,
                'use_playwright': args.playwright,
                'convert_to_mdx': args.mdx,
                'custom_selectors': custom_selectors
            }

        # Verificar Playwright si se solicita
        if config['use_playwright'] and not PLAYWRIGHT_AVAILABLE:
            print("❌ Playwright no disponible. Instala con: pip install playwright")
            print("   Luego ejecuta: playwright install")
            return 1

        # Crear y ejecutar scraper
        scraper = AdvancedDocsScraper(**config)
        scraper.run()

        print("\n🎉 ¡Scraping completado exitosamente!")
        print(f"📁 Archivos guardados en: {os.path.abspath(config['output_dir'])}")

        return 0

    except KeyboardInterrupt:
        print("\n❌ Scraping interrumpido por el usuario")
        return 1
    except Exception as e:
        logger.error(f"Error fatal: {e}")
        print(f"\n❌ Error durante el scraping: {e}")
        return 1

if __name__ == "__main__":
    exit(main())
