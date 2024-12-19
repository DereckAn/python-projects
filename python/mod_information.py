import requests
from bs4 import BeautifulSoup
import pandas as pd
from urllib.parse import urlparse
import concurrent.futures

class ModScraper:
    def __init__(self):
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
        }
        self.results = []

    def get_site_type(self, url):
        domain = urlparse(url).netloc
        if 'curseforge.com' in domain:
            return 'curseforge'
        elif 'modrinth.com' in domain:
            return 'modrinth'
        return 'unknown'

    def scrape_curseforge(self, url):
        try:
            response = requests.get(url, headers=self.headers)
            soup = BeautifulSoup(response.text, 'html.parser')

            # Estos selectores necesitarán ser ajustados según la estructura actual de CurseForge
            versions = [v.text for v in soup.select('.game-version-support')]
            categories = [c.text for c in soup.select('.categories a')]

            # Detectar si es cliente/servidor y loader basado en descripciones o tags
            description = soup.select_one('.project-description').text.lower()
            is_client = 'client' in description
            is_server = 'server' in description
            has_forge = 'forge' in description
            has_fabric = 'fabric' in description

            return {
                'url': url,
                'versions': versions,
                'client': is_client,
                'server': is_server,
                'forge': has_forge,
                'fabric': has_fabric,
                'categories': categories
            }
        except Exception as e:
            print(f"Error scraping {url}: {str(e)}")
            return None

    def process_urls(self, urls, max_workers=5):
        with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
            future_to_url = {executor.submit(self.scrape_curseforge, url): url 
                           for url in urls if self.get_site_type(url) == 'curseforge'}

            for future in concurrent.futures.as_completed(future_to_url):
                url = future_to_url[future]
                try:
                    data = future.result()
                    if data:
                        self.results.append(data)
                except Exception as e:
                    print(f"Error processing {url}: {str(e)}")

    def save_results(self, filename='mod_data.csv'):
        df = pd.DataFrame(self.results)
        df.to_csv(filename, index=False)
        print(f"Results saved to {filename}")

# Uso del script
def main():
    # Asume que los URLs están en un archivo de texto, uno por línea
    with open('mod_urls.txt', 'r') as f:
        urls = [line.strip() for line in f]

    scraper = ModScraper()
    scraper.process_urls(urls)
    scraper.save_results()

if __name__ == "__main__":
    main()

# Created/Modified files during execution:
# - mod_data.csv