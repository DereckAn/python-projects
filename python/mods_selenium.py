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

            # Get mod name
            print("Buscando nombre del mod...")
            try:
                mod_name = wait.until(
                    EC.presence_of_element_located((By.TAG_NAME, "h1"))
                ).text.strip()
                print(f"✓ Nombre del mod encontrado: {mod_name}")
            except:
                mod_name = "No especificado"
                print("⚠ No se encontró el nombre del mod")

            # Get versions
            print("Buscando versiones...")
            try:
                versions_container = wait.until(
                    EC.presence_of_element_located((
                        By.XPATH,
                        '//*[@id="__nuxt"]/div[3]/main/div[5]/div[6]/div[2]/div[1]/section[1]/div'
                    ))
                )
                version_divs = versions_container.find_elements(By.XPATH, './/div[contains(@class, "rounded-full")]')
                versions = [div.text.strip() for div in version_divs if div.text.strip()]
                versions_str = ", ".join(versions)
                print(f"✓ Versiones encontradas: {versions_str}")
            except:
                versions_str = "No especificado"
                print("⚠ No se encontraron versiones")

            # Get loaders (platforms)
            print("Buscando loaders...")
            try:
                platforms_container = wait.until(
                    EC.presence_of_element_located((
                        By.XPATH,
                        '//*[@id="__nuxt"]/div[3]/main/div[5]/div[6]/div[2]/div[1]/section[2]/div'
                    ))
                )
                platform_divs = platforms_container.find_elements(By.XPATH, './/div[contains(@class, "rounded-full")]')
                platforms = []
                for div in platform_divs:
                    platform_text = div.get_attribute('textContent').strip()
                    # Limpiar texto de SVG y comentarios
                    platform_text = ''.join(c for c in platform_text if c.isalpha() or c.isspace()).strip()
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
                env_container = wait.until(
                    EC.presence_of_element_located((
                        By.XPATH,
                        '//*[@id="__nuxt"]/div[3]/main/div[5]/div[6]/div[2]/div[1]/section[3]/div'
                    ))
                )
                env_divs = env_container.find_elements(By.XPATH, './/div[contains(@class, "rounded-full")]')
                env_types = []
                for div in env_divs:
                    env_text = div.get_attribute('textContent').strip()
                    env_text = ''.join(c for c in env_text if c.isalpha() or c.isspace()).strip()
                    if env_text:
                        env_types.append(env_text)
                env_type = ", ".join(env_types)
                print(f"✓ Tipo de entorno encontrado: {env_type}")
            except:
                env_type = "Not specified"
                print("⚠ No se encontró tipo de entorno")

            # Get categories
            print("Buscando categorías...")
            try:
                categories_container = wait.until(
                    EC.presence_of_element_located((
                        By.XPATH,
                        '//*[@id="__nuxt"]/div[3]/main/div[5]/div[6]/div[1]/div/div[1]/div/div[2]/div[3]/div'
                    ))
                )
                category_divs = categories_container.find_elements(By.XPATH, './/div[contains(@class, "rounded-full")]')
                categories = []
                for div in category_divs:
                    category_text = div.get_attribute('textContent').strip()
                    category_text = category_text.replace('<!--[-->', '').replace('<!--]-->', '').strip()
                    if category_text:
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

            print("\n=== DATOS RECOLECTADOS ===")
            for key, value in mod_data.items():
                print(f"{key}: {value}")
            print("========================")

            return mod_data

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