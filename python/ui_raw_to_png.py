import os
import sys
from PIL import Image
import rawpy
from PyQt5.QtWidgets import (QApplication, QMainWindow, QPushButton, QFileDialog, 
                            QProgressBar, QLabel, QVBoxLayout, QHBoxLayout, QWidget,
                            QComboBox, QFrame, QSizePolicy)
from PyQt5.QtCore import Qt, QThread, pyqtSignal
from PyQt5.QtGui import QFont, QIcon, QColor

class ConversionThread(QThread):
    progress_update = pyqtSignal(int, str)
    conversion_complete = pyqtSignal()
    
    def __init__(self, input_folder, output_folder, output_format):
        super().__init__()
        self.input_folder = input_folder
        self.output_folder = output_folder
        self.output_format = output_format
        self.is_cancelled = False
        
    def run(self):
        # Asegurarse de que la carpeta de salida exista
        if not os.path.exists(self.output_folder):
            os.makedirs(self.output_folder)

        # Extensiones RAW comunes
        raw_extensions = ['.arw', '.cr2', '.nef', '.orf', '.raf', '.rw2']
        
        # Obtener lista de archivos RAW
        raw_files = []
        for filename in os.listdir(self.input_folder):
            name, extension = os.path.splitext(filename)
            if extension.lower() in raw_extensions:
                raw_files.append(filename)
        
        total_files = len(raw_files)
        
        # Procesar cada archivo
        for i, filename in enumerate(raw_files):
            # Verificar si se ha cancelado la conversión
            if self.is_cancelled:
                break
                
            name, extension = os.path.splitext(filename)
            input_path = os.path.join(self.input_folder, filename)
            output_path = os.path.join(self.output_folder, f"{name}.{self.output_format}")

            # Abrir y procesar la imagen RAW
            with rawpy.imread(input_path) as raw:
                rgb = raw.postprocess()

            # Convertir a imagen PIL y guardar
            image = Image.fromarray(rgb)
            image.save(output_path)
            
            # Actualizar progreso
            progress = int((i + 1) / total_files * 100)
            status_msg = f"Convertido: {i+1}/{total_files} - {filename}"
            self.progress_update.emit(progress, status_msg)
        
        self.conversion_complete.emit()
    
    def cancel(self):
        self.is_cancelled = True
        

class RawConverterUI(QMainWindow):
    def __init__(self):
        super().__init__()
        self.init_ui()
        
    def init_ui(self):
        # Configuración de la ventana principal
        self.setWindowTitle("Convertidor de Imágenes RAW")
        self.setGeometry(100, 100, 600, 400)
        self.setStyleSheet("background-color: #2c3e50;")  # Fondo más oscuro
        
        # Widget central y layout principal
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        main_layout = QVBoxLayout(central_widget)
        main_layout.setContentsMargins(20, 20, 20, 20)
        main_layout.setSpacing(15)
        
        # Título
        title_label = QLabel("Convertidor de Imágenes RAW")
        title_label.setFont(QFont("Arial", 18, QFont.Bold))
        title_label.setAlignment(Qt.AlignCenter)
        title_label.setStyleSheet("color: #ecf0f1; margin-bottom: 10px;")  # Texto claro
        main_layout.addWidget(title_label)
        
        # Separador
        separator = QFrame()
        separator.setFrameShape(QFrame.HLine)
        separator.setFrameShadow(QFrame.Sunken)
        separator.setStyleSheet("background-color: #7f8c8d;")
        main_layout.addWidget(separator)
        
        # Sección de carpeta de entrada
        input_section = QHBoxLayout()
        input_label = QLabel("Carpeta de entrada:")
        input_label.setFont(QFont("Arial", 11))
        input_label.setStyleSheet("color: #ecf0f1;")  # Texto claro
        input_section.addWidget(input_label)
        
        self.input_path_label = QLabel("No seleccionada")
        self.input_path_label.setFont(QFont("Arial", 10))
        self.input_path_label.setStyleSheet("background-color: #34495e; color: #ecf0f1; padding: 8px; border-radius: 4px; border: 1px solid #7f8c8d;")  # Fondo más oscuro, texto claro
        self.input_path_label.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Preferred)
        input_section.addWidget(self.input_path_label)
        
        self.browse_input_btn = QPushButton("Explorar")
        self.browse_input_btn.setFont(QFont("Arial", 10))
        self.browse_input_btn.setStyleSheet("""
            QPushButton {
                background-color: #3498db;
                color: white;
                border-radius: 4px;
                padding: 8px 15px;
                border: none;
            }
            QPushButton:hover {
                background-color: #2980b9;
            }
        """)
        self.browse_input_btn.clicked.connect(self.browse_input_folder)
        input_section.addWidget(self.browse_input_btn)
        main_layout.addLayout(input_section)
        
        # Sección de carpeta de salida
        output_section = QHBoxLayout()
        output_label = QLabel("Carpeta de salida:")
        output_label.setFont(QFont("Arial", 11))
        output_label.setStyleSheet("color: #ecf0f1;")  # Texto claro
        output_section.addWidget(output_label)
        
        self.output_path_label = QLabel("No seleccionada")
        self.output_path_label.setFont(QFont("Arial", 10))
        self.output_path_label.setStyleSheet("background-color: #34495e; color: #ecf0f1; padding: 8px; border-radius: 4px; border: 1px solid #7f8c8d;")  # Fondo más oscuro, texto claro
        self.output_path_label.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Preferred)
        output_section.addWidget(self.output_path_label)
        
        self.browse_output_btn = QPushButton("Explorar")
        self.browse_output_btn.setFont(QFont("Arial", 10))
        self.browse_output_btn.setStyleSheet("""
            QPushButton {
                background-color: #3498db;
                color: white;
                border-radius: 4px;
                padding: 8px 15px;
                border: none;
            }
            QPushButton:hover {
                background-color: #2980b9;
            }
        """)
        self.browse_output_btn.clicked.connect(self.browse_output_folder)
        output_section.addWidget(self.browse_output_btn)
        main_layout.addLayout(output_section)
        
        # Sección de formato de salida
        format_section = QHBoxLayout()
        format_label = QLabel("Formato de salida:")
        format_label.setFont(QFont("Arial", 11))
        format_label.setStyleSheet("color: #ecf0f1;")  # Texto claro
        format_section.addWidget(format_label)
        
        self.format_combo = QComboBox()
        self.format_combo.addItems(["jpg", "png"])
        self.format_combo.setFont(QFont("Arial", 10))
        self.format_combo.setStyleSheet("""
            QComboBox {
                background-color: #34495e;
                color: #ecf0f1;
                padding: 8px;
                border-radius: 4px;
                border: 1px solid #7f8c8d;
            }
            QComboBox::drop-down {
                border: none;
            }
            QComboBox QAbstractItemView {
                background-color: #34495e;
                color: #ecf0f1;
                selection-background-color: #3498db;
            }
        """)
        format_section.addWidget(self.format_combo)
        format_section.addStretch()
        main_layout.addLayout(format_section)
        
        # Barra de progreso
        progress_layout = QVBoxLayout()
        self.progress_bar = QProgressBar()
        self.progress_bar.setStyleSheet("""
            QProgressBar {
                border: 1px solid #7f8c8d;
                border-radius: 4px;
                text-align: center;
                height: 25px;
                background-color: #34495e;
                color: #ecf0f1;
            }
            QProgressBar::chunk {
                background-color: #2ecc71;
                border-radius: 3px;
            }
        """)
        progress_layout.addWidget(self.progress_bar)
        
        self.status_label = QLabel("Listo para convertir")
        self.status_label.setFont(QFont("Arial", 10))
        self.status_label.setAlignment(Qt.AlignCenter)
        self.status_label.setStyleSheet("color: #ecf0f1;")  # Texto claro
        progress_layout.addWidget(self.status_label)
        
        main_layout.addLayout(progress_layout)
        
        # Botones de acción
        buttons_layout = QHBoxLayout()
        
        self.convert_btn = QPushButton("Iniciar Conversión")
        self.convert_btn.setFont(QFont("Arial", 12, QFont.Bold))
        self.convert_btn.setStyleSheet("""
            QPushButton {
                background-color: #2ecc71;
                color: white;
                border-radius: 4px;
                padding: 12px;
                border: none;
            }
            QPushButton:hover {
                background-color: #27ae60;
            }
            QPushButton:disabled {
                background-color: #7f8c8d;
            }
        """)
        self.convert_btn.clicked.connect(self.start_conversion)
        buttons_layout.addWidget(self.convert_btn)
        
        # Nuevo botón de cancelar
        self.cancel_btn = QPushButton("Cancelar")
        self.cancel_btn.setFont(QFont("Arial", 12, QFont.Bold))
        self.cancel_btn.setStyleSheet("""
            QPushButton {
                background-color: #e74c3c;
                color: white;
                border-radius: 4px;
                padding: 12px;
                border: none;
            }
            QPushButton:hover {
                background-color: #c0392b;
            }
            QPushButton:disabled {
                background-color: #7f8c8d;
            }
        """)
        self.cancel_btn.clicked.connect(self.cancel_conversion)
        self.cancel_btn.setEnabled(False)  # Inicialmente deshabilitado
        buttons_layout.addWidget(self.cancel_btn)
        
        main_layout.addLayout(buttons_layout)
        
        # Inicializar variables
        self.input_folder = ""
        self.output_folder = ""
        self.conversion_thread = None
        
        # Mostrar ventana
        self.show()
    
    def browse_input_folder(self):
        folder = QFileDialog.getExistingDirectory(self, "Seleccionar Carpeta de Entrada")
        if folder:
            self.input_folder = folder
            # Mostrar solo el nombre de la carpeta para una interfaz más limpia
            folder_name = os.path.basename(folder)
            self.input_path_label.setText(f".../{folder_name}")
            self.input_path_label.setToolTip(folder)  # Mostrar ruta completa al pasar el mouse
            self.update_convert_button()
    
    def browse_output_folder(self):
        folder = QFileDialog.getExistingDirectory(self, "Seleccionar Carpeta de Salida")
        if folder:
            self.output_folder = folder
            # Mostrar solo el nombre de la carpeta para una interfaz más limpia
            folder_name = os.path.basename(folder)
            self.output_path_label.setText(f".../{folder_name}")
            self.output_path_label.setToolTip(folder)  # Mostrar ruta completa al pasar el mouse
            self.update_convert_button()
    
    def update_convert_button(self):
        # Habilitar el botón solo si se han seleccionado ambas carpetas
        self.convert_btn.setEnabled(bool(self.input_folder and self.output_folder))
    
    def update_progress(self, progress, status_msg):
        # Actualizar la barra de progreso y el mensaje de estado
        self.progress_bar.setValue(progress)
        self.status_label.setText(status_msg)
    
    def start_conversion(self):
        if not self.input_folder or not self.output_folder:
            return
        
        # Deshabilitar controles durante la conversión
        self.convert_btn.setEnabled(False)
        self.browse_input_btn.setEnabled(False)
        self.browse_output_btn.setEnabled(False)
        self.format_combo.setEnabled(False)
        self.cancel_btn.setEnabled(True)  # Habilitar botón de cancelar
        
        # Reiniciar barra de progreso
        self.progress_bar.setValue(0)
        self.status_label.setText("Iniciando conversión...")
        
        # Iniciar hilo de conversión
        output_format = self.format_combo.currentText()
        self.conversion_thread = ConversionThread(self.input_folder, self.output_folder, output_format)
        self.conversion_thread.progress_update.connect(self.update_progress)
        self.conversion_thread.conversion_complete.connect(self.conversion_finished)
        self.conversion_thread.start()
    
    def cancel_conversion(self):
        if self.conversion_thread and self.conversion_thread.isRunning():
            self.conversion_thread.cancel()
            self.status_label.setText("Cancelando conversión...")
            self.cancel_btn.setEnabled(False)
    
    def conversion_finished(self):
        # Habilitar controles nuevamente
        self.convert_btn.setEnabled(True)
        self.browse_input_btn.setEnabled(True)
        self.browse_output_btn.setEnabled(True)
        self.format_combo.setEnabled(True)
        self.cancel_btn.setEnabled(False)  # Deshabilitar botón de cancelar
        
        # Actualizar estado
        if self.conversion_thread and self.conversion_thread.is_cancelled:
            self.status_label.setText("Conversión cancelada")
        else:
            self.status_label.setText("¡Conversión completada!")


def main():
    app = QApplication(sys.argv)
    window = RawConverterUI()
    sys.exit(app.exec_())


if __name__ == "__main__":
    main()