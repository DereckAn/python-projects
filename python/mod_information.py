import pandas as pd
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.chrome.options import Options
from selenium.common.exceptions import TimeoutException, WebDriverException
import time
import random

class ModScraper:
    def __init__(self):
        self.chrome_options = Options()
        self.chrome_options.add_argument('--headless')
        self.chrome_options.add_argument('--no-sandbox')
        self.chrome_options.add_argument('--disable-dev-shm-usage')
        self.chrome_options.add_argument('--disable-gpu')
        self.chrome_options.add_argument('--window-size=1920,1080')
        self.chrome_options.add_argument('--ignore-certificate-errors')
        self.chrome_options.page_load_strategy = 'eager'
        self.results = []
        self.max_retries = 3
        self.retry_delay = 5

    def setup_driver(self):
        driver = webdriver.Chrome(options=self.chrome_options)
        driver.set_page_load_timeout(30)
        return driver

    def scrape_with_retry(self, url, original_data, driver):
        for attempt in range(self.max_retries):
            try:
                # Reiniciar el driver si no es el primer intento
                if attempt > 0:
                    print(f"Reintento {attempt + 1} para {url}")
                    driver.quit()
                    driver = self.setup_driver()
                    time.sleep(self.retry_delay)

                return self.scrape_curseforge(url, original_data, driver)

            except Exception as e:
                print(f"Error en intento {attempt + 1} para {url}: {str(e)}")
                if attempt == self.max_retries - 1:
                    print(f"Fallaron todos los intentos para {url}")
                    return None
                time.sleep(self.retry_delay)

    def scrape_curseforge(self, url, original_data, driver):
        try:
            # Delay aleatorio entre requests
            time.sleep(random.uniform(3, 6))
            
            print(f"Cargando página: {url}")
            driver.get(url)
            
            # Esperar a que la página cargue
            WebDriverWait(driver, 20).until(
                EC.presence_of_element_located((By.TAG_NAME, "body"))
            )

            # Scroll suave para cargar contenido dinámico
            for i in range(3):
                driver.execute_script(f"window.scrollTo(0, {i * 500});")
                time.sleep(0.5)

            page_text = driver.page_source.lower()
            
            versions = []
            try:
                version_elements = WebDriverWait(driver, 10).until(
                    EC.presence_of_all_elements_located(
                        (By.CSS_SELECTOR, '.game-version-support, .version-label')
                    )
                )
                versions = [v.text.strip() for v in version_elements if v.text.strip()]
            except TimeoutException:
                print(f"No se encontraron versiones para {url}")

            return {
                'Property': original_data['Property'],
                'Name': original_data['Name'],
                'Original_Category': original_data['Category'],
                'Original_Version': original_data['Version'],
                'Download_Link': url,
                'Type': original_data['Type'],
                'Side': original_data['Side'],
                'Scraped_Versions': versions,
                'Is_Client': 'client' in page_text,
                'Is_Server': 'server' in page_text,
                'Has_Forge': 'forge' in page_text,
                'Has_Fabric': 'fabric' in page_text
            }

        except Exception as e:
            raise Exception(f"Error en scrape_curseforge: {str(e)}")

    def process_urls_from_csv(self, csv_file):
        df = pd.read_csv(csv_file)
        driver = self.setup_driver()
        
        try:
            total_urls = len(df)
            for index, row in df.iterrows():
                url = row['Download Link']
                print(f"\nProcesando {index + 1}/{total_urls}: {url}")
                
                # Guardar resultados parciales cada 10 URLs
                if index > 0 and index % 10 == 0:
                    self.save_results(f'mod_data_partial_{index}.csv')
                
                data = self.scrape_with_retry(url, row, driver)
                if data:
                    self.results.append(data)
                    print(f"Successfully scraped: {url}")
                
                # Pausa más larga cada 20 URLs
                if index > 0 and index % 20 == 0:
                    print("Pausa de descanso...")
                    time.sleep(30)
                
        finally:
            driver.quit()

    def save_results(self, filename='mod_data_enriched.csv'):
        if self.results:
            df = pd.DataFrame(self.results)
            df.to_csv(filename, index=False)
            print(f"Results saved to {filename}")
        else:
            print("No results to save")

def main():
    input_csv = 'Mods.csv'  # Reemplaza con el nombre de tu archivo CSV
    
    scraper = ModScraper()
    scraper.process_urls_from_csv(input_csv)
    scraper.save_results()

if __name__ == "__main__":
    main()

# Created/Modified files during execution:
# - mod_data_enriched.csv
# - mod_data_partial_*.csv