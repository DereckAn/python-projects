use anyhow::Result;

#[cfg(target_os = "windows")]
pub fn install() -> Result<()> {
    use winreg::enums::*;
    use winreg::RegKey;
    
    let hkcu = RegKey::predef(HKEY_CLASSES_ROOT);
    
    // Create main menu
    let (convert_key, _) = hkcu.create_subkey(r"*\shell\Convert")?;
    convert_key.set_value("", &"Convert to")?;
    convert_key.set_value("SubCommands", &"")?;
    
    // Create submenus for each format
    let (commands_key, _) = hkcu.create_subkey(r"*\shell\Convert\shell")?;
    
    let formats = ["webp", "png", "jpg", "raw"];
    
    for format in formats {
        let (format_key, _) = commands_key.create_subkey(format)?;
        format_key.set_value("", &format!("Convert to {}", format.to_uppercase()))?;
        
        let (command_key, _) = format_key.create_subkey("command")?;
        let exe_path = std::env::current_exe()?;
        command_key.set_value(
            "", 
            &format!("\"{}\" {} \"%V\"", exe_path.display(), format)
        )?;
    }
    
    Ok(())
}

#[cfg(target_os = "windows")]
pub fn uninstall() -> Result<()> {
    use winreg::enums::*;
    use winreg::RegKey;

    let hkcu = RegKey::predef(HKEY_CLASSES_ROOT);
    
    // El subkey que se creó en install:
    let path_main = r"*\shell\Convert";
    if let Ok(_) = hkcu.delete_subkey_all(path_main) {
        println!("Registro '{}' eliminado correctamente.", path_main);
    } else {
        println!("No se pudo eliminar '{}'. Tal vez ya estaba eliminado.", path_main);
    }

    // Si hace falta borrar más cosas, agrégalo aquí.
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