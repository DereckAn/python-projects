use anyhow::Result;
use std::path::PathBuf;

mod convert;
mod menu_installer;

fn main() -> Result<()> {
    let args: Vec<String> = std::env::args().collect();

    if args.len() > 1 {
        match args[1].as_str() {
            "--install" => {
                menu_installer::install()?;
                println!("Context menu instalado correctamente!");
                return Ok(());
            }
            "--uninstall" => {
                menu_installer::uninstall()?;
                println!("Context menu desinstalado correctamente!");
                return Ok(());
            }
            format => {
                // Conversión de imágenes
                let files: Vec<PathBuf> = args[2..]
                    .iter()
                    .map(PathBuf::from)
                    .collect();
                convert::convert_images(files, format)?;
            }
        }
    } else {
        println!("Uso:");
        println!("  image_converter --install    -> Instala el menú contextual");
        println!("  image_converter --uninstall  -> Desinstala el menú contextual");
        println!("  image_converter <formato> <archivos...> -> Convierte imágenes");
        println!("Ejemplo: image_converter webp archivo1.png archivo2.jpg");
    }
    Ok(())
}