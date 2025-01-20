# Covert Images in Context Menu

## Instalación

### Windows
1. Talvez tengas que hacerle build al proyecto con `cargo build --release`
2. Correr un powershell como administrador y ejecutar `./target/release/image_converter.exe --install` para instalar el menú contextual.
3. - para desinstalar el menú contextual, ejecutar `./target/release/image_converter.exe --uninstall`

### MacOS
1. Correr el comando `cargo build --release` en la carpeta `convert`
2. Ejecutar el comando `cp target/release/image_converter /usr/local/bin` para copiar el binario a `/usr/local/bin`
3. Ejecutar el comando `./image_converter --install` para instalar el menú contextual
4. Ejecutar el comando `./image_converter --uninstall` para desinstalar el menú contextual

### Linux
1. Correr el comando `cargo build --release` en la carpeta `convert`
2. Ejecutar el comando `cp target/release/image_converter /usr/local/bin` para copiar el binario a `/usr/local/bin`
3. Ejecutar el comando `./image_converter --install` para instalar el menú contextual
4. Ejecutar el comando `./image_converter --uninstall` para desinstalar el menú contextual

## Uso

- Abre el menú contextual de tu archivo de imagen
- Selecciona "Convert to" y elige el formato que deseas
- El archivo se convertirá automáticamente
