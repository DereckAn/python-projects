import os
from dotenv import load_dotenv
from pathlib import Path
import requests
from square.client import Client
from square.configuration import Configuration
from PIL import Image
import io
import re

# Get the current directory of the script
script_dir = Path(__file__).resolve().parent

# Get the parent directory (where .env is located)
parent_dir = script_dir.parent

# Load environment variables from .env file
load_dotenv(parent_dir / '.env')

# Access environment variables
SQUARE_ACCESS_TOKEN = os.getenv('SQUARE_ACCESS_TOKEN')


client = Client(
    access_token='',
    environment= 'production'
)

# Create directory for images if it doesn't exist
images_dir = parent_dir / 'product_images'
if not images_dir.exists():
    images_dir.mkdir(parents=True)

try:
    # First fetch - get all food and beverage items
    items_result = client.catalog.search_catalog_items(
        body={
            "product_types": [
                "FOOD_AND_BEV"
            ]
        }
    )

    # Second fetch - get all images
    images_result = client.catalog.search_catalog_objects(
        body={
            "object_types": [
                "IMAGE"
            ],
            "include_deleted_objects": False,
            "include_related_objects": False,
            "include_category_path_to_root": False
        }
    )

    if items_result.is_success() and images_result.is_success():
        # Create a dictionary of image_id to image_url
        image_map = {
            img['id']: img['image_data']['url']
            for img in images_result.body['objects']
        }

        # Process each item
        for item in items_result.body['items']:
            # Get the item name and image IDs
            item_name = item['item_data']['name']
            image_ids = item['item_data'].get('image_ids', [])

            # Process each image ID for this item
            for img_id in image_ids:
                if img_id in image_map:
                    # Get the image URL
                    image_url = image_map[img_id]

                    try:
                        # Download the image
                        response = requests.get(image_url)
                        response.raise_for_status()  # Raise an exception for bad status codes

                        # Open the image with Pillow
                        img = Image.open(io.BytesIO(response.content))

                        # Create safe filename
                        safe_name = re.sub(r'[<>:"/\\|?*]', '', item_name)
                        safe_name = re.sub(r'\s+', '_', safe_name)  
                        webp_filename = images_dir / f'{safe_name}.webp'

                        # Convert and save as WebP
                        img.save(str(webp_filename), 'WEBP', quality=85)
                        print(f"Successfully downloaded and converted image for {item_name}")

                    except requests.RequestException as e:
                        print(f"Failed to download image for {item_name}: {e}")
                    except Exception as e:
                        print(f"Error processing image for {item_name}: {e}")
                else:
                    print(f"Image ID {img_id} not found for {item_name}")

    else:
        if items_result.is_error():
            print("Error fetching items:", items_result.errors)
        if images_result.is_error():
            print("Error fetching images:", images_result.errors)

except Exception as e:
    print(f"An error occurred: {e}")

# Print created files
print("\nCreated files:")
for file in images_dir.glob('*.webp'):
    print(file)