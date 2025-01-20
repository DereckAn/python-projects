# Use official Python image
FROM python:3.9-slim

# Set working directory
WORKDIR /app

# Copy requirements file
COPY requirements.txt .

# Install dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy the rest of the application code
COPY . .

# Set environment variables
ENV SQUARE_ACCESS_TOKEN=<your_square_access_token>

# Run the script
CMD ["python", "download_square_images.py"]