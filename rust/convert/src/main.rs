use anyhow::Result;
use std::path::PathBuf;

mod convert;
mod menu_installer;

fn main() -> Result<()> {
    let args: Vec<String> = std::env::args().collect();

    if args.len() < 2 {
        print_usage();
        return Ok(());
    }

    match args[1].as_str() {
        "--install" => {
            menu_installer::install()?;
            println!("Context menu instalado correctamente!");
        }
        "--uninstall" => {
            menu_installer::uninstall()?;
            println!("Context menu desinstalado correctamente!");
        }
        "convert" => {
            if args.len() < 4 {
                println!("Error: Faltan argumentos para la conversión");
                print_usage();
                return Ok(());
            }
            let format = &args[2];
            let files: Vec<PathBuf> = args[3..].iter().map(PathBuf::from).collect();
            convert::convert_images(files, format)?;
            println!("Conversión completada exitosamente!");
        }
        _ => {
            println!("Comando no reconocido: {}", args[1]);
            print_usage();
        }
    }
    Ok(())
}

fn print_usage() {
    println!("Uso:");
    println!("  image_converter --install    -> Instala el menú contextual");
    println!("  image_converter --uninstall  -> Desinstala el menú contextual");
    println!("  image_converter convert <formato> <archivos...> -> Convierte imágenes");
    println!("Ejemplo: image_converter convert webp imagen.png");
}