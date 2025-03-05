import flet as ft
import os
from mods_selenium import ModInfo
import pandas as pd
from datetime import datetime

class ModScraperUI:
    def __init__(self):
        self.selected_file = None
        self.scraper = ModInfo()
        
    def main(self, page: ft.Page):
        page.title = "Mod Info Scraper"
        page.window_width = 800
        page.window_height = 800
        page.padding = 20
        page.theme_mode = ft.ThemeMode.LIGHT
        
        # Título
        title = ft.Text("Mod Info Scraper", size=32, weight=ft.FontWeight.BOLD)
        
        # Información sobre el formato requerido
        csv_info = ft.Container(
            content=ft.Column([
                ft.Text("Formato requerido del archivo CSV:", size=16, weight=ft.FontWeight.BOLD),
                ft.Text("• El archivo debe ser un CSV con una columna llamada 'Download Link'"),
                ft.Text("• Cada fila debe contener un enlace a CurseForge o Modrinth"),
                ft.Text("• Los enlaces deben comenzar con 'https://www.curseforge.com' o 'https://modrinth.com'")
            ]),
            padding=10,
            border=ft.border.all(1, "#9E9E9E"),
            border_radius=10,
            margin=ft.margin.only(bottom=20)
        )
        
        # Selector de archivo
        file_path = ft.Text("Ningún archivo seleccionado", color="#616161")
        
        async def pick_file_result(e: ft.FilePickerResultEvent):
            if e.files:
                file_path.value = e.files[0].path
                self.selected_file = e.files[0].path
                await file_path.update()
                start_button.disabled = False
                await start_button.update()
        
        file_picker = ft.FilePicker(
            on_result=pick_file_result
        )
        page.overlay.append(file_picker)
        
        pick_file_button = ft.ElevatedButton(
            "Seleccionar archivo CSV",
            icon=ft.icons.UPLOAD,
            on_click=lambda _: file_picker.pick_files(
                allow_multiple=False,
                allowed_extensions=["csv"]
            )
        )
        
        # Estadísticas
        stats = ft.Column([
            ft.Text("Estadísticas:", size=16, weight=ft.FontWeight.BOLD),
            ft.Text("Total de mods: 0"),
            ft.Text("Mods procesados: 0"),
            ft.Text("Mods exitosos: 0"),
            ft.Text("Mods con error: 0")
        ])
        
        # Barras de progreso
        progress_total = ft.ProgressBar(width=400, value=0)
        progress_current = ft.ProgressBar(width=400, value=0)
        progress_info = ft.Text("Esperando inicio...", color="#616161")
        
        # Log de actividad
        activity_log = ft.ListView(
            height=200,
            expand=1
        )
        
        # Contenedor para el log con borde
        log_container = ft.Container(
            content=activity_log,
            border=ft.border.all(1, "#9E9E9E"),
            padding=10,
            height=200
        )
        
        def add_log(message: str):
            activity_log.controls.append(ft.Text(message))
            page.update()
        
        async def start_processing(e):
            if not self.selected_file:
                add_log("⚠️ Por favor selecciona un archivo CSV primero")
                return
            
            # Deshabilitar botón de inicio
            start_button.disabled = True
            await start_button.update()
            
            try:
                # Leer CSV y verificar formato
                df = pd.read_csv(self.selected_file)
                if 'Download Link' not in df.columns:
                    add_log("❌ Error: El archivo CSV debe tener una columna llamada 'Download Link'")
                    return
                
                total_mods = len(df)
                processed = 0
                successful = 0
                failed = 0
                
                # Actualizar estadísticas iniciales
                stats.controls[1].value = f"Total de mods: {total_mods}"
                await stats.update()
                
                # Procesar cada mod
                driver = self.scraper.setup_driver()
                try:
                    for index, row in df.iterrows():
                        url = row['Download Link']
                        
                        # Actualizar progreso
                        progress_total.value = processed / total_mods
                        progress_info.value = f"Procesando mod {processed + 1} de {total_mods}"
                        await progress_total.update()
                        await progress_info.update()
                        
                        # Procesar URL
                        add_log(f"\nProcesando: {url}")
                        try:
                            data = self.scraper.check_url(url, driver)
                            if data:
                                successful += 1
                                add_log(f"✅ Éxito: {data['Mod Name']}")
                            else:
                                failed += 1
                                add_log(f"❌ Error al procesar {url}")
                        except Exception as e:
                            failed += 1
                            add_log(f"❌ Error: {str(e)}")
                        
                        processed += 1
                        
                        # Actualizar estadísticas
                        stats.controls[2].value = f"Mods procesados: {processed}"
                        stats.controls[3].value = f"Mods exitosos: {successful}"
                        stats.controls[4].value = f"Mods con error: {failed}"
                        await stats.update()
                        
                finally:
                    driver.quit()
                
                # Guardar resultados
                timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
                if self.scraper.results:
                    success_file = f"mod_information_{timestamp}.csv"
                    pd.DataFrame(self.scraper.results).to_csv(success_file, index=False)
                    add_log(f"\n✅ Resultados exitosos guardados en: {success_file}")
                
                if self.scraper.results_erros:
                    error_file = f"mod_errors_{timestamp}.csv"
                    pd.DataFrame({'URL': self.scraper.results_erros}).to_csv(error_file, index=False)
                    add_log(f"❌ URLs con errores guardadas en: {error_file}")
                
                add_log("\n✨ Proceso completado!")
                
            except Exception as e:
                add_log(f"❌ Error general: {str(e)}")
            
            # Reactivar botón de inicio
            start_button.disabled = False
            await start_button.update()
        
        # Botón de inicio
        start_button = ft.ElevatedButton(
            "Iniciar procesamiento",
            icon=ft.icons.PLAY_ARROW,
            on_click=start_processing,
            disabled=True
        )
        
        # Layout principal
        page.add(
            title,
            csv_info,
            ft.Row([pick_file_button]),
            file_path,
            ft.Divider(),
            stats,
            ft.Column([
                ft.Text("Progreso total:", size=14),
                progress_total,
                progress_info
            ]),
            ft.Divider(),
            ft.Text("Log de actividad:", size=14),
            log_container,
            ft.Divider(),
            start_button
        )

if __name__ == "__main__":
    app = ModScraperUI()
    ft.app(target=app.main)