from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException
import pandas as pd
import time

class ModInfo:
    def __init__(self):
        self.chrome_options = Options()
        # self.chrome_options.add_argument('--headless')
        # self.chrome_options.add_argument('--no-sandbox')
        # self.chrome_options.add_argument('--disable-dev-shm-usage')
        # self.chrome_options.add_argument('--disable-gpu')
        # self.chrome_options.add_argument('--window-size=1920,1080')
        # self.chrome_options.add_argument('--ignore-certificate-errors')
        # self.chrome_options.page_load_strategy = 'eager'
        self.results = []
        self.results_erros = []

    def setup_driver(self):
        self.chrome_options.add_argument('--disable-blink-features=AutomationControlled')
        self.chrome_options.add_argument('--disable-extensions')
        self.chrome_options.add_argument('--disable-notifications')
        self.chrome_options.add_argument('--start-maximized')
        self.chrome_options.add_argument('--disable-web-security')
        self.chrome_options.add_argument('--disable-features=IsolateOrigins,site-per-process')
        self.chrome_options.add_experimental_option('excludeSwitches', ['enable-automation'])
        self.chrome_options.add_experimental_option('useAutomationExtension', False)
        driver = webdriver.Chrome(options=self.chrome_options)
        driver.set_page_load_timeout(30)

        # Establecer un script para esperar a que la página esté completamente cargada
        driver.execute_script("window.onload = function() { document.readyState === 'complete'; }")

        return driver

    def process_urls_from_csv(self, csv_file):
        df = pd.read_csv(csv_file)
        driver = self.setup_driver()

        try:
            total_urls = len(df)
            successful = 0
            failed = 0

            print(f"\nIniciando procesamiento de {total_urls} URLs...")

            for index, row in df.iterrows():
                url = row['Download Link']
                print(f"\n[{index + 1}/{total_urls}] Procesando: {url}")

                try:
                    data = self.check_url(url, driver)
                    if data:
                        self.results.append(data)
                        successful += 1
                    else:
                        failed += 1
                except Exception as e:
                    print(f"Error procesando {url}: {str(e)}")
                    failed += 1

                # Mostrar progreso
                print(f"\nProgreso: {index + 1}/{total_urls}")
                print(f"Exitosos: {successful}")
                print(f"Fallidos: {failed}")

                time.sleep(2)  # Pausa entre requests

            # Resumen final
            print("\n=== RESUMEN FINAL ===")
            print(f"Total URLs procesadas: {total_urls}")
            print(f"URLs exitosas: {successful}")
            print(f"URLs fallidas: {failed}")
            print("===================")

        finally:
            driver.quit()

    def check_url(self, url, driver):
        if url.startswith('https://www.curseforge.com'):
            return self.get_mod_info_from_curseforge(url, driver)
        elif url.startswith('https://modrinth.com'):
            return self.get_mod_info_from_modrinth(url, driver)
        else:
            print(f"URL no válida: {url}")
            return None

    def get_mod_info_from_curseforge(self, url, driver):
        driver.get(url)
        wait = WebDriverWait(driver, 15)  # Aumentamos el tiempo de espera a 15 segundos

        try:
            print(f"\nIntentando recolectar datos de: {url}")

            # Get mod name
            print("Buscando nombre del mod...")
            mod_name = wait.until(
                EC.presence_of_element_located((By.TAG_NAME, "h1"))
            ).text
            print(f"✓ Nombre del mod encontrado: {mod_name}")

            # Get versions
            print("Buscando versiones...")
            versions_section = wait.until(
                EC.presence_of_element_located((By.ID, "versions-summary"))
            )
            version_items = versions_section.find_elements(By.ID, "version-item")[:5]
            versions = [item.find_element(By.TAG_NAME, "a").text for item in version_items]
            versions_str = ", ".join(versions)
            print(f"✓ Versiones encontradas: {versions_str}")

            # Get loaders
            print("Buscando loaders...")
            loaders_section = wait.until(
                EC.presence_of_element_located((By.ID, "game-version-types-summary"))
            )
            loader_items = loaders_section.find_elements(By.ID, "game-version-type-item")
            loaders = [item.find_element(By.TAG_NAME, "a").text for item in loader_items]
            loaders_str = ", ".join(loaders)
            print(f"✓ Loaders encontrados: {loaders_str}")

            # Get categories
            print("Buscando categorías...")
            categories_section = wait.until(
                EC.presence_of_element_located((By.ID, "project-categories"))
            )
            category_items = categories_section.find_elements(By.TAG_NAME, "li")
            categories = [category.find_element(By.TAG_NAME, "a").text for category in category_items]
            categories_str = ", ".join(categories)
            print(f"✓ Categorías encontradas: {categories_str}")

            # Get class tag
            print("Buscando class tag...")
            class_tag = wait.until(
                EC.presence_of_element_located((By.CLASS_NAME, "class-tag"))
            ).find_element(By.TAG_NAME, "a").text
            print(f"✓ Class tag encontrado: {class_tag}")

            # Crear diccionario con los datos
            mod_data = {
                'Mod Name': mod_name,
                'Versions': versions_str,
                'Loaders': loaders_str,
                'Categories': categories_str,
                'Type': class_tag,
                'Source': 'CurseForge',
                'URL': url
            }

            # Imprimir resumen de datos recolectados
            print("\n=== DATOS RECOLECTADOS ===")
            for key, value in mod_data.items():
                print(f"{key}: {value}")
            print("========================")

            return mod_data

        except TimeoutException as e:
            print(f"\n❌ Timeout esperando elementos en {url}")
            print(f"Último elemento buscado: {e.msg}")
            self.results_erros.append(url)
            return None
        except Exception as e:
            print(f"\n❌ Error procesando {url}")
            print(f"Error específico: {str(e)}")
            self.results_erros.append(url)
            return None
    
    def get_mod_info_from_modrinth(self, url, driver):
        driver.get(url)
        wait = WebDriverWait(driver, 15)

        try:
            print(f"\nIntentando recolectar datos de: {url}")

            # Get mod name - usando un selector más específico
            print("Buscando nombre del mod...")
            try:
                mod_name = wait.until(
                    EC.presence_of_element_located((By.CSS_SELECTOR, "h1.text-2xl.font-extrabold"))
                ).text
                print(f"✓ Nombre del mod encontrado: {mod_name}")
            except:
                print("⚠ Intentando método alternativo para el nombre...")
                mod_name = wait.until(
                    EC.presence_of_element_located((By.CSS_SELECTOR, ".normal-page__header h1"))
                ).text

            # Get versions - buscando en la sección de Compatibility
            print("Buscando versiones...")
            try:
                compatibility_section = wait.until(
                    EC.presence_of_element_located((By.XPATH, "//h3[contains(text(), 'Minecraft: Java Edition')]/following-sibling::div[contains(@class, 'tag-list')]"))
                )
                version_items = compatibility_section.find_elements(By.CLASS_NAME, "tag-list__item")
                versions = [item.text for item in version_items if item.text]
                versions_str = ", ".join(versions)
                print(f"✓ Versiones encontradas: {versions_str}")
            except:
                versions_str = "No especificado"
                print("⚠ No se encontraron versiones")

            # Get loaders (platforms)
            print("Buscando loaders...")
            try:
                platforms_section = wait.until(
                    EC.presence_of_element_located((By.XPATH, "//h3[contains(text(), 'Platforms')]/following-sibling::div[contains(@class, 'tag-list')]"))
                )
                platform_items = platforms_section.find_elements(By.CLASS_NAME, "tag-list__item")
                platforms = []
                for item in platform_items:
                    # Obtener el texto sin el contenido SVG
                    platform_text = item.get_attribute('textContent').strip()
                    if platform_text:
                        platforms.append(platform_text)
                platforms_str = ", ".join(platforms)
                print(f"✓ Loaders encontrados: {platforms_str}")
            except:
                platforms_str = "No especificado"
                print("⚠ No se encontraron loaders")

            # Get environment type
            print("Buscando tipo de entorno...")
            try:
                env_section = wait.until(
                    EC.presence_of_element_located((By.XPATH, "//h3[contains(text(), 'Supported environments')]/following-sibling::div[contains(@class, 'tag-list')]"))
                )
                env_items = env_section.find_elements(By.CLASS_NAME, "tag-list__item")
                env_type = env_items[0].get_attribute('textContent').strip() if env_items else "Not specified"
                print(f"✓ Tipo de entorno encontrado: {env_type}")
            except:
                env_type = "Not specified"
                print("⚠ No se encontró tipo de entorno")

            # Get Categories
            print("Buscando categorías...")
            try:
                categories = []
                category_items = driver.find_elements(By.CSS_SELECTOR, "div.flex.flex-wrap.gap-2 .tag-list__item")
                for item in category_items:
                    category_text = item.text.strip()
                    if category_text and not any(char.isdigit() for char in category_text):
                        categories.append(category_text)
                categories_str = ", ".join(categories)
                print(f"✓ Categorías encontradas: {categories_str}")
            except:
                categories_str = "No especificado"
                print("⚠ No se encontraron categorías")

            # Crear diccionario con los datos
            mod_data = {
                'Mod Name': mod_name,
                'Versions': versions_str,
                'Loaders': platforms_str,
                'Categories': categories_str,
                'Type': env_type,
                'Source': 'Modrinth',
                'URL': url
            }

            # Imprimir resumen de datos recolectados
            print("\n=== DATOS RECOLECTADOS ===")
            for key, value in mod_data.items():
                print(f"{key}: {value}")
            print("========================")

            return mod_data

        except TimeoutException as e:
            print(f"\n❌ Timeout esperando elementos en {url}")
            print(f"Último elemento buscado: {e.msg}")
            self.results_erros.append(url)
            return None
        except Exception as e:
            print(f"\n❌ Error procesando {url}")
            print(f"Error específico: {str(e)}")
            self.results_erros.append(url)
            return None
    
    def save_results(self, filename):
        if self.results:
            df = pd.DataFrame(self.results)
            df.to_csv(filename, index=False)
            print(f"Resultados guardados en {filename}")
        else:
            print("No hay resultados para guardar")

def main():
    input_csv = 'Mods.csv'  # Asegúrate de que este archivo existe
    scraper = ModInfo()
    scraper.process_urls_from_csv(input_csv)
    scraper.save_results("mod_information.csv")
    print(f"URLs con errores: {scraper.results_erros}")
    scraper.results_erros.save_results("mod_information_erros.csv")

if __name__ == "__main__":
    main()