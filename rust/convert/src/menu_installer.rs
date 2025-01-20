use anyhow::Result;

#[cfg(target_os = "windows")]
pub fn install() -> Result<()> {
    use winreg::enums::*;
    use winreg::RegKey;

    // Apuntamos a HKEY_CLASSES_ROOT
    let hkcr = RegKey::predef(HKEY_CLASSES_ROOT);

    // 1) Creamos la clave principal "*\shell\ImageConverter"
    let (convert_key, _) = hkcr.create_subkey(r"*\shell\ImageConverter")?;
    // Texto que aparecerá en el menú principal
    convert_key.set_value("", &"Convert to")?;
    convert_key.set_value("MUIVerb", &"Convert to")?;
    // Icono opcional (shell32.dll,277 es un ícono genérico)
    convert_key.set_value("Icon", &"shell32.dll,277")?;

    // 2) Lista de formatos que manejará tu conversión
    let formats = [
        "bmp", "eps", "exr", "gif", "ico", 
        "jpg", "png", "svg", "tga", "tiff", 
        "wbmp", "webp"
    ];

    // 3) Construimos el valor "SubCommands", que es un string con cada subcomando separado por ";"
    let subcommands = formats
        .iter()
        .map(|f| format!("ImageConverter.{}", f))
        .collect::<Vec<_>>()
        .join(";");

    // 4) Asignamos ese string para que Windows sepa que hay subopciones (SubCommands)
    convert_key.set_value("SubCommands", &subcommands)?;

    // 5) Creamos las claves para cada subcomando en:
    // "HKEY_CLASSES_ROOT\Software\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\..."
    let (command_store_key, _) = hkcr.create_subkey(
        r"Software\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell"
    )?;

    for format in &formats {
        // Nombre de cada subcomando, p.ej. "ImageConverter.bmp"
        let subcommand_name = format!("ImageConverter.{}", format);
        let (subcommand_key, _) = command_store_key.create_subkey(&subcommand_name)?;

        // Texto que se mostrará en el menú (ej. "Convert to BMP")
        subcommand_key.set_value("", &format!("Convert to {}", format.to_uppercase()))?;

        // Creamos la subclave "command" y su valor
        let (command_key, _) = subcommand_key.create_subkey("command")?;

        // Tu ejecutable actual y el comando:
        // => "C:\ruta\a\image_converter.exe convert <formato> "%1""
        let exe_path = std::env::current_exe()?;
        command_key.set_value(
            "",
            &format!("\"{}\" convert {} \"%1\"", exe_path.display(), format)
        )?;
    }

    println!("Context menu instalado correctamente (con submenús).");
    Ok(())
}

#[cfg(target_os = "windows")]
pub fn uninstall() -> Result<()> {
    use winreg::enums::*;
    use winreg::RegKey;

    let hkcr = RegKey::predef(HKEY_CLASSES_ROOT);

    // Eliminar el menú principal
    let path_main = r"*\shell\ImageConverter";
    if hkcr.delete_subkey_all(path_main).is_ok() {
        println!("Menú principal eliminado correctamente.");
    }

    // Eliminar los comandos almacenados (CommandStore)
    let path_commands = r"Software\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\ImageConverter";
    if hkcr.delete_subkey_all(path_commands).is_ok() {
        println!("Comandos eliminados correctamente.");
    }

    Ok(())
}

#[cfg(target_os = "macos")]
pub fn install() -> Result<()> {
    use std::fs;
    use std::path::PathBuf;
    
    let home = dirs::home_dir().unwrap();
    let services_dir = home.join("Library/Services");
    
    // Crear Quick Actions usando el nuevo formato
    let formats = ["webp", "png", "jpg", "raw"];
    
    for format in formats {
        let workflow_dir = services_dir.join(format!("ConvertTo{}.workflow", format.to_uppercase()));
        fs::create_dir_all(&workflow_dir)?;
        
        // Crear document.wflow (nuevo formato XML)
        let workflow = format!(
            r#"<?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>AMApplicationBuild</key>
                <string>521.1</string>
                <key>AMApplicationVersion</key>
                <string>2.10</string>
                <key>AMDocumentVersion</key>
                <string>2</string>
                <key>actions</key>
                <array>
                    <dict>
                        <key>action</key>
                        <dict>
                            <key>script</key>
                            <string>#!/bin/bash
                            "{}" {} "$@"</string>
                        </dict>
                    </dict>
                </array>
            </dict>
            </plist>"#,
            std::env::current_exe()?.display(),
            format
        );
        
        fs::write(workflow_dir.join("document.wflow"), workflow)?;
        
        // Crear Info.plist moderno
        let info_plist = format!(
            r#"<?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>NSServices</key>
                <array>
                    <dict>
                        <key>NSMenuItem</key>
                        <dict>
                            <key>default</key>
                            <string>Convert to {}</string>
                        </dict>
                        <key>NSMessage</key>
                        <string>runWorkflowAsService</string>
                        <key>NSRequiredFileTypes</key>
                        <array>
                            <string>public.image</string>
                        </array>
                        <key>NSSendFileTypes</key>
                        <array>
                            <string>public.image</string>
                        </array>
                    </dict>
                </array>
            </dict>
            </plist>"#,
            format.to_uppercase()
        );
        
        fs::write(workflow_dir.join("Info.plist"), info_plist)?;
    }
    
    // Registrar los servicios usando el nuevo API
    let status = std::process::Command::new("launchctl")
        .args(&["load", "-w", "/System/Library/LaunchAgents/com.apple.ServicesUIAgent.plist"])
        .status()?;
        
    if !status.success() {
        return Err(anyhow::anyhow!("Failed to register services"));
    }
    
    Ok(())
}

#[cfg(target_os = "macos")]
pub fn uninstall() -> Result<()> {
    use std::fs;
    use std::path::PathBuf;

    let home = dirs::home_dir().unwrap();
    let services_dir = home.join("Library/Services");
    
    let formats = ["webp", "png", "jpg", "raw"];
    for format in formats {
        let workflow_dir = services_dir.join(format!("ConvertTo{}.workflow", format.to_uppercase()));
        if workflow_dir.exists() {
            fs::remove_dir_all(&workflow_dir)?;
            println!("Eliminado Quick Action: {:?}", workflow_dir);
        }
    }

    // Comando para desregistrar servicios:
    let status = std::process::Command::new("launchctl")
        .args(&["load", "-w", "/System/Library/LaunchAgents/com.apple.ServicesUIAgent.plist"])
        .status()?;
    
    if !status.success() {
        println!("Advertencia: no se pudo refrescar los servicios en macOS. Hazlo manualmente si es necesario.");
    }

    Ok(())
}

#[cfg(target_os = "linux")]
pub fn install() -> Result<()> {
    use std::fs;
    use std::path::PathBuf;
    
    let home = dirs::home_dir().unwrap();
    let scripts_dir = home.join(".local/share/nautilus/scripts");
    fs::create_dir_all(&scripts_dir)?;
    
    // Crear scripts para cada formato
    let formats = ["webp", "png", "jpg", "raw"];
    
    for format in formats {
        let script = format!(
            r#"#!/bin/bash
            {} {} "$@""#,
            std::env::current_exe()?.display(),
            format
        );
        
        let script_path = scripts_dir.join(format!("Convert to {}", format.to_uppercase()));
        fs::write(&script_path, script)?;
        fs::set_permissions(&script_path, std::os::unix::fs::PermissionsExt::from_mode(0o755))?;
    }
    
    Ok(())
}

#[cfg(target_os = "linux")]
pub fn uninstall() -> Result<()> {
    use std::fs;
    use std::path::PathBuf;

    let home = dirs::home_dir().unwrap();
    let scripts_dir = home.join(".local/share/nautilus/scripts");

    // Borramos los scripts creados:
    let formats = ["webp", "png", "jpg", "raw"];
    for format in formats {
        let script_name = format!("Convert to {}", format.to_uppercase());
        let script_path = scripts_dir.join(&script_name);
        if script_path.exists() {
            fs::remove_file(&script_path)?;
            println!("Eliminado script: {:?}", script_path);
        }
    }

    Ok(())
}