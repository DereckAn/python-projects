use anyhow::Result;
use image::ImageFormat;
use std::path::PathBuf;
use rayon::prelude::*;

pub fn convert_images(files: Vec<PathBuf>, format: &str) -> Result<()> {
    let output_format = match format.to_lowercase().as_str() {
        "webp" => ImageFormat::WebP,
        "png" => ImageFormat::Png,
        "jpg" | "jpeg" => ImageFormat::Jpeg,
        _ => return Err(anyhow::anyhow!("Unsupported format")),
    };
    
    files.par_iter().try_for_each(|file| {
        let img = image::open(file)?;
        let output_path = file.with_extension(format);
        img.save_with_format(&output_path, output_format)?;
        println!("Converted: {} -> {}", file.display(), output_path.display());
        Ok(())
    })
}