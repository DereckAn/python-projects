#!/usr/bin/env python3
"""
Utilidades para análisis y corrección de archivos Markdown
"""

import os
import re
import argparse
from pathlib import Path
from typing import List, Dict, Tuple
import logging

# Configurar logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class MarkdownAnalyzer:
    """Analizador de archivos Markdown para detectar problemas comunes"""

    def __init__(self):
        self.issues = []

    def analyze_file(self, file_path: Path) -> Dict:
        """Analiza un archivo Markdown y retorna problemas encontrados"""
        try:
            content = file_path.read_text(encoding='utf-8')
            issues = []

            # Análisis de problemas comunes
            issues.extend(self._check_broken_links(content))
            issues.extend(self._check_malformed_headers(content))
            issues.extend(self._check_excessive_whitespace(content))
            issues.extend(self._check_broken_tables(content))
            issues.extend(self._check_unescaped_characters(content))

            return {
                'file': str(file_path),
                'issues': issues,
                'line_count': len(content.splitlines()),
                'size_kb': file_path.stat().st_size / 1024
            }

        except Exception as e:
            logger.error(f"Error analizando {file_path}: {e}")
            return {'file': str(file_path), 'issues': [f"Error: {e}"], 'line_count': 0, 'size_kb': 0}

    def _check_broken_links(self, content: str) -> List[str]:
        """Detecta enlaces rotos o mal formados"""
        issues = []

        # Enlaces con espacios
        if re.search(r'\[\s+[^\]]+\s+\]', content):
            issues.append("Enlaces con espacios extras")

        # Enlaces vacíos
        if re.search(r'\[\s*\]\s*\(\s*\)', content):
            issues.append("Enlaces vacíos")

        # Enlaces con URLs mal formadas
        if re.search(r'\]\s*\(\s*[^http][^)]*\s+[^)]*\)', content):
            issues.append("URLs con espacios")

        return issues

    def _check_malformed_headers(self, content: str) -> List[str]:
        """Detecta encabezados mal formados"""
        issues = []

        # Encabezados sin espacio
        if re.search(r'^#{1,6}[^\s#]', content, re.MULTILINE):
            issues.append("Encabezados sin espacio después de #")

        # Encabezados con múltiples espacios
        if re.search(r'^#{1,6}\s{2,}', content, re.MULTILINE):
            issues.append("Encabezados con múltiples espacios")

        return issues

    def _check_excessive_whitespace(self, content: str) -> List[str]:
        """Detecta espacios en blanco excesivos"""
        issues = []

        # Múltiples líneas vacías
        if re.search(r'\n\s*\n\s*\n\s*\n', content):
            issues.append("Múltiples líneas vacías consecutivas")

        # Espacios al final de líneas
        if re.search(r' +\n', content):
            issues.append("Espacios al final de líneas")

        # Tabs mezclados con espacios
        if '\t' in content and '    ' in content:
            issues.append("Mezcla de tabs y espacios")

        return issues

    def _check_broken_tables(self, content: str) -> List[str]:
        """Detecta tablas mal formadas"""
        issues = []

        # Tablas con pipes desalineados
        table_lines = [line for line in content.split('\n') if '|' in line]
        if table_lines:
            for line in table_lines:
                if line.count('|') == 1:  # Línea con un solo pipe
                    issues.append("Tabla con pipes desalineados")
                    break

        return issues

    def _check_unescaped_characters(self, content: str) -> List[str]:
        """Detecta caracteres que deberían estar escapados"""
        issues = []

        # Caracteres especiales sin escapar en contextos problemáticos
        if re.search(r'[<>&](?![a-zA-Z]+;)', content):
            issues.append("Caracteres HTML sin escapar")

        return issues

def batch_analyze(directory: str) -> Dict:
    """Analiza todos los archivos Markdown en un directorio"""
    dir_path = Path(directory)

    if not dir_path.exists():
        raise ValueError(f"Directorio no existe: {directory}")

    analyzer = MarkdownAnalyzer()

    # Encontrar archivos Markdown
    md_files = list(dir_path.rglob("*.md"))
    mdx_files = list(dir_path.rglob("*.mdx"))
    all_files = md_files + mdx_files

    if not all_files:
        return {'files': [], 'summary': {'total_files': 0, 'total_issues': 0}}

    # Analizar archivos
    results = []
    total_issues = 0

    for file_path in all_files:
        result = analyzer.analyze_file(file_path)
        results.append(result)
        total_issues += len(result['issues'])

    # Resumen
    summary = {
        'total_files': len(all_files),
        'total_issues': total_issues,
        'total_size_mb': sum(r['size_kb'] for r in results) / 1024,
        'avg_size_kb': sum(r['size_kb'] for r in results) / len(results),
        'files_with_issues': len([r for r in results if r['issues']])
    }

    return {
        'files': results,
        'summary': summary
    }

def print_analysis_report(analysis: Dict):
    """Imprime un reporte de análisis"""
    summary = analysis['summary']
    files = analysis['files']

    print("\n" + "="*60)
    print("           REPORTE DE ANÁLISIS MARKDOWN")
    print("="*60)
    print(f"📄 Total archivos:      {summary['total_files']}")
    print(f"❌ Total problemas:     {summary['total_issues']}")
    print(f"📁 Archivos con issues: {summary['files_with_issues']}")
    print(f"💾 Tamaño total:        {summary['total_size_mb']:.2f} MB")
    print(f"📏 Tamaño promedio:     {summary['avg_size_kb']:.2f} KB")
    print("="*60)

    if summary['total_issues'] > 0:
        print("\n🔍 ARCHIVOS CON PROBLEMAS:")
        print("-"*60)

        for file_data in files:
            if file_data['issues']:
                print(f"\n📄 {file_data['file']}")
                print(f"   Líneas: {file_data['line_count']}, Tamaño: {file_data['size_kb']:.1f} KB")
                for issue in file_data['issues']:
                    print(f"   ❌ {issue}")
    else:
        print("\n✅ ¡No se encontraron problemas!")

def main():
    parser = argparse.ArgumentParser(description="Analizador de archivos Markdown")
    parser.add_argument('directory', help='Directorio a analizar')
    parser.add_argument('--fix', action='store_true', help='Intentar corregir problemas automáticamente')

    args = parser.parse_args()

    try:
        # Analizar
        analysis = batch_analyze(args.directory)
        print_analysis_report(analysis)

        # Corregir si se solicita
        if args.fix and analysis['summary']['total_issues'] > 0:
            print(f"\n🔧 Para corregir los problemas, usa:")
            print(f"   python advanced_docs_scraper.py --fix-format {args.directory}")

        return 0

    except Exception as e:
        print(f"❌ Error: {e}")
        return 1

if __name__ == "__main__":
    exit(main())
