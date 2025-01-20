use anyhow::Result;
use image::ImageFormat;
use std::path::PathBuf;
use rayon::prelude::*;

pub fn convert_images(files: Vec<PathBuf>, format: &str) -> Result<()> {
    let output_format = match format.to_lowercase().as_str() {
        "bmp" => ImageFormat::Bmp,
        "eps" => return Err(anyhow::anyhow!("EPS format requires additional dependencies")),
        "exr" => ImageFormat::OpenExr,
        "gif" => ImageFormat::Gif,
        "ico" => ImageFormat::Ico,
        "jpg" | "jpeg" => ImageFormat::Jpeg,
        "png" => ImageFormat::Png,
        "svg" => return Err(anyhow::anyhow!("SVG format requires additional dependencies")),
        "tga" => ImageFormat::Tga,
        "tiff" => ImageFormat::Tiff,
        "wbmp" => return Err(anyhow::anyhow!("WBMP format not supported")),
        "webp" => ImageFormat::WebP,
        _ => return Err(anyhow::anyhow!("Unsupported format: {}", format)),
    };
    
    files.par_iter().try_for_each(|file| {
        let img = image::open(file)?;
        let output_path = file.with_extension(format);
        
        // Configuraciones específicas por formato
        match format.to_lowercase().as_str() {
            "jpg" | "jpeg" => {
                img.save_with_format(&output_path, output_format)?;
            },
            "png" => {
                img.save_with_format(&output_path, output_format)?;
            },
            "webp" => {
                img.save_with_format(&output_path, output_format)?;
            },
            "gif" => {
                // Para GIF, podríamos necesitar configuración especial si es animado
                img.save_with_format(&output_path, output_format)?;
            },
            "ico" => {
                // Los ICO pueden necesitar redimensionamiento
                let resized = img.resize(32, 32, image::imageops::FilterType::Lanczos3);
                resized.save_with_format(&output_path, output_format)?;
            },
            _ => {
                img.save_with_format(&output_path, output_format)?;
            }
        }
        
        println!("Converted: {} -> {}", file.display(), output_path.display());
        Ok(())
    })
}