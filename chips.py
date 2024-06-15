from time import sleep
import flet as ft

def main(page: ft.Page):
    page.title = "Auto-scrolling ListView in AlertDialog"

    lv = ft.ListView(expand=1, spacing=10, padding=20, auto_scroll=True)
    param_types = ["String", "Col<String>", "Double", "Col<Double>", "Int32", "Col<Int32>"]
    count = 1

    for param_type in param_types:
        lv.controls.append(ft.Checkbox(param_type))

    dialog = ft.AlertDialog(
        title="My Dialog",
        content=lv,
        actions=[
            ft.TextButton("Close", on_click=lambda: dialog.close()),
        ],
    )

    page.add(dialog)

ft.app(target=main)
