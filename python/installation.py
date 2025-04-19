#!/usr/bin/env python3

from rich.console import Console
from rich.table import Table
from rich.panel import Panel
from rich.prompt import Confirm, Prompt
from rich.progress import Progress
import subprocess
import os
import sys
import time
from typing import List, Dict

console = Console()

# Definición de categorías y aplicaciones
APPS_BY_CATEGORY = {
    "Gestores de Paquetes": [
        {"name": "pnpm", "command": "brew install pnpm", "cask": False},
        {"name": "Bun", "command": "brew install oven-sh/bun/bun", "cask": False},
        {"name": "Yarn", "command": "brew install yarn", "cask": False},
        {"name": "npm", "command": "brew install node", "cask": False},
    ],
    "Herramientas de Contenedores": [
        {"name": "Docker", "command": "brew install --cask docker", "cask": True},
        {"name": "kubectl", "command": "brew install kubectl", "cask": False},
        {"name": "Minikube", "command": "brew install minikube", "cask": False},
    ],
    "IDEs y Editores": [
        {"name": "Visual Studio Code", "command": "brew install --cask visual-studio-code", "cask": True},
        {"name": "Cursor", "command": "brew install --cask cursor", "cask": True},
        {"name": "IntelliJ IDEA", "command": "brew install --cask intellij-idea", "cask": True},
        {"name": "WebStorm", "command": "brew install --cask webstorm", "cask": True},
    ],
    "Navegadores": [
        {"name": "Google Chrome", "command": "brew install --cask google-chrome", "cask": True},
        {"name": "Brave", "command": "brew install --cask brave-browser", "cask": True},
        {"name": "Opera", "command": "brew install --cask opera", "cask": True},
        {"name": "Firefox", "command": "brew install --cask firefox", "cask": True},
    ],
    "Terminales": [
        {"name": "iTerm2", "command": "brew install --cask iterm2", "cask": True},
        {"name": "Warp", "command": "brew install --cask warp", "cask": True},
    ],
    "Lenguajes de Programación": [
        {"name": "Python", "command": "brew install python", "cask": False, "path": "/opt/homebrew/bin", "executable": "python3"},
        {"name": "Java", "command": "brew install java", "cask": False, "path": "/opt/homebrew/opt/openjdk/bin", "executable": "java"},
        {"name": "Rust", "command": "brew install rust", "cask": False, "path": "/opt/homebrew/bin", "executable": "rustc"},
        {"name": "C++ (clang)", "command": "brew install llvm", "cask": False, "path": "/opt/homebrew/opt/llvm/bin", "executable": "clang++"},
        {"name": "C# (Mono)", "command": "brew install mono", "cask": False, "path": "/opt/homebrew/bin", "executable": "mono"},
        {"name": "Go", "command": "brew install go", "cask": False, "path": "/opt/homebrew/bin", "executable": "go"},
        {"name": "Ruby", "command": "brew install ruby", "cask": False, "path": "/opt/homebrew/bin", "executable": "ruby"},
        {"name": "PHP", "command": "brew install php", "cask": False, "path": "/opt/homebrew/bin", "executable": "php"},
        {"name": "TypeScript", "command": "brew install typescript", "cask": False, "path": "/opt/homebrew/bin", "executable": "tsc"},
    ],
    "Bases de Datos": [
        {"name": "PostgreSQL", "command": "brew install postgresql", "cask": False},
        {"name": "MySQL", "command": "brew install mysql", "cask": False},
        {"name": "MongoDB", "command": "brew install mongodb-community", "cask": False},
    ],
    "Otros": [
        {"name": "Raycast", "command": "brew install raycast", "cask": False},
        {"name": "Telegram", "command": "brew install --cask telegram", "cask": True},
        {"name": "Slack", "command": "brew install --cask slack", "cask": True},
        {"name": "Tailscale", "command": "brew install tailscale", "cask": False},
        {"name": "fzf", "command": "brew install fzf", "cask": False},
        {"name": "GitHub CLI (gh)", "command": "brew install gh", "cask": False},
        {"name": "Obsidian", "command": "brew install --cask obsidian", "cask": True},
        {"name": "Notion", "command": "brew install --cask notion", "cask": True},
    ],
}

def run_command(command: str, description: str) -> bool:
    """Ejecuta un comando y muestra una barra de progreso."""
    with Progress() as progress:
        task = progress.add_task(f"[cyan]{description}...", total=100)
        process = subprocess.run(command, shell=True, capture_output=True, text=True)
        progress.update(task, advance=100)
        return process.returncode == 0

def check_brew() -> bool:
    """Verifica e instala Homebrew si no está presente."""
    console.print(Panel("Verificando Homebrew...", style="yellow"))
    if subprocess.run("which brew", shell=True, capture_output=True).returncode != 0:
        console.print("[yellow]Homebrew no encontrado. Instalando...[/yellow]")
        return run_command(
            '/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"',
            "Instalando Homebrew"
        )
    console.print("[green]Homebrew encontrado.[/green]")
    return True

def check_git() -> bool:
    """Verifica e instala Git si no está presente."""
    console.print(Panel("Verificando Git...", style="yellow"))
    if subprocess.run("which git", shell=True, capture_output=True).returncode != 0:
        console.print("[yellow]Git no encontrado. Instalando...[/yellow]")
        return run_command("brew install git", "Instalando Git")
    console.print("[green]Git encontrado.[/green]")
    return True

def install_oh_my_zsh() -> None:
    """Instala Oh My Zsh y Powerlevel10k con plugins."""
    console.print(Panel("Configurando ZSH y Oh My Zsh...", style="yellow"))
    if os.path.exists(os.path.expanduser("~/.oh-my-zsh")):
        console.print("[green]Oh My Zsh ya está instalado.[/green]")
    else:
        console.print("[yellow]Instalando Oh My Zsh...[/yellow]")
        if not run_command(
            'sh -c "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)" --unattended',
            "Instalando Oh My Zsh"
        ):
            console.print("[red]Error instalando Oh My Zsh.[/red]")
            sys.exit(1)

    # Instalar Powerlevel10k
    console.print("[yellow]Instalando Powerlevel10k...[/yellow]")
    run_command(
        "git clone --depth=1 https://github.com/romkatv/powerlevel10k.git $HOME/.oh-my-zsh/custom/themes/powerlevel10k",
        "Instalando Powerlevel10k"
    )
    run_command(
        "sed -i '' 's#robbyrussell#powerlevel10k/powerlevel10k#g' ~/.zshrc",
        "Configurando Powerlevel10k"
    )

    # Instalar plugins
    console.print("[yellow]Instalando plugins de ZSH...[/yellow]")
    plugins = [
        ("zsh-autosuggestions", "https://github.com/zsh-users/zsh-autosuggestions"),
        ("zsh-history-substring-search", "https://github.com/zsh-users/zsh-history-substring-search"),
        ("zsh-syntax-highlighting", "https://github.com/zsh-users/zsh-syntax-highlighting"),
    ]
    for plugin, url in plugins:
        run_command(
            f"git clone {url} $HOME/.oh-my-zsh/custom/plugins/{plugin}",
            f"Instalando {plugin}"
        )
    run_command(
        "sed -i '' 's/plugins=(git)/plugins=(git jump zsh-autosuggestions zsh-history-substring-search jsontools zsh-syntax-highlighting zsh-interactive-cd)/g' ~/.zshrc",
        "Configurando plugins"
    )

def add_to_path(app: Dict) -> None:
    """Añade la ruta de un lenguaje al PATH si no está presente."""
    if "path" not in app or "executable" not in app:
        return

    executable = app["executable"]
    path_to_add = app["path"]

    # Verificar si el ejecutable ya está en el PATH
    if subprocess.run(f"which {executable}", shell=True, capture_output=True).returncode == 0:
        console.print(f"[green]{executable} ya está en el PATH.[/green]")
        return

    # Añadir al PATH en ~/.zshrc
    zshrc_path = os.path.expanduser("~/.zshrc")
    export_line = f'export PATH="{path_to_add}:$PATH"'
    
    with open(zshrc_path, "r") as f:
        if export_line in f.read():
            console.print(f"[green]Ruta {path_to_add} ya está en ~/.zshrc.[/green]")
            return

    console.print(f"[yellow]Añadiendo {path_to_add} al PATH en ~/.zshrc...[/yellow]")
    with open(zshrc_path, "a") as f:
        f.write(f"\n# Añadido por setup_macos.py\n{export_line}\n")
    console.print(f"[green]Ruta para {executable} añadida al PATH.[/green]")

def select_apps() -> List[Dict]:
    """Muestra un menú interactivo por categorías para seleccionar aplicaciones."""
    console.print(Panel("Selecciona las aplicaciones a instalar por categoría:", style="cyan"))
    selected_apps = []

    for category, apps in APPS_BY_CATEGORY.items():
        console.print(Panel(f"Categoría: {category}", style="bold magenta"))

        # Mostrar tabla de aplicaciones en la categoría
        table = Table(title=f"Aplicaciones en {category}")
        table.add_column("Seleccionar", style="cyan")
        table.add_column("Nombre", style="green")
        table.add_column("Comando", style="magenta")
        for i, app in enumerate(apps, 1):
            table.add_row(f"[{i}]", app["name"], app["command"])
        console.print(table)

        # Selección interactiva
        try:
            choices = Prompt.ask(
                f"Ingresa los números de las aplicaciones a instalar (ej. 1,3,5) o 'all' para todas",
                default="",
            )
            if choices.lower() == "all":
                selected_apps.extend(apps)
            elif choices.strip():  # Procesar solo si hay entrada no vacía
                try:
                    indices = [int(i) - 1 for i in choices.split(",") if i.strip()]
                    for idx in indices:
                        if 0 <= idx < len(apps):
                            selected_apps.append(apps[idx])
                except ValueError:
                    console.print(f"[yellow]Entrada inválida para {category}. Saltando...[/yellow]")
        except EOFError:
            console.print(f"[yellow]Entrada interrumpida para {category}. Saltando...[/yellow]")

    return selected_apps

def install_apps(selected_apps: List[Dict]) -> None:
    """Instala las aplicaciones seleccionadas y muestra un resumen."""
    if not selected_apps:
        console.print("[yellow]No se seleccionaron aplicaciones.[/yellow]")
        return

    # Mostrar tabla de aplicaciones seleccionadas
    table = Table(title="Aplicaciones a instalar")
    table.add_column("Nombre", style="cyan")
    table.add_column("Comando", style="magenta")
    for app in selected_apps:
        table.add_row(app["name"], app["command"])
    console.print(table)

    # Instalar aplicaciones seleccionadas
    for app in selected_apps:
        console.print(f"[yellow]Instalando {app['name']}...[/yellow]")
        if run_command(app["command"], f"Instalando {app['name']}"):
            # Añadir al PATH si es un lenguaje
            if "path" in app:
                add_to_path(app)
        else:
            console.print(f"[red]Error instalando {app['name']}.[/red]")

def hide_dock() -> None:
    """Oculta el Dock de macOS si el usuario lo desea."""
    if Confirm.ask("¿Ocultar el Dock de macOS?", default=True):
        console.print("[yellow]Ocultando el Dock...[/yellow]")
        run_command(
            "defaults write com.apple.dock autohide -bool true; killall Dock",
            "Ocultando Dock"
        )
        console.print("[green]Dock ocultado.[/green]")
    else:
        console.print("[green]El Dock permanecerá visible.[/green]")

def main() -> None:
    console.print(Panel("Script de Configuración para macOS", style="bold green", expand=False))

    # Verificar Homebrew y Git
    if not check_brew() or not check_git():
        console.print("[red]Error en la configuración inicial.[/red]")
        sys.exit(1)

    # Configurar ZSH y Oh My Zsh
    install_oh_my_zsh()

    # Seleccionar e instalar aplicaciones
    selected_apps = select_apps()
    install_apps(selected_apps)

    # Ocultar Dock
    hide_dock()

    # Resumen final
    console.print(Panel(
        "¡Configuración completada! Por favor, reinicia tu terminal para aplicar los cambios.",
        style="bold green"
    ))

if __name__ == "__main__":
    main()