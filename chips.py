import flet as ft

def main(page: ft.Page):
    page.title = "AlertDialog examples"
    param_t = []
    parame_selected = []
    param_types = ["String", "Column<String>", 
                            "Double", "Column<Double>", 
                            "Int32", "Columnumn<Int32>", 
                            "Int64", "Column<Int64>", 
                            "Boolean", "Column<String>('true' or 'false')", 
                            "DateTime",	"Column<DateTime>",
                            "Guid",	"Column<Guid>",
                            "TimeSpan",
                            "Point", "Column<XY>",
                            "Line",	"Column<Line>",
                            "Multipoint", "Column<Multipoint>",
                            "Shape", "Column<Poly>"]
    
    for param_type in param_types:
        param_t.append(ft.Chip(label=ft.Text(param_type),  bgcolor=ft.colors.GREEN_200,
                disabled_color=ft.colors.GREEN_100,
                autofocus=True,
                on_select=lambda e, param_type=param_type: 
                    parame_selected.remove(param_type) if param_type in parame_selected else parame_selected.append(param_type),))
        

    dlg = ft.AlertDialog(
        title=ft.Text("Hello, you!"), on_dismiss=lambda e: print("Dialog dismissed!")
    )

    def close_dlg(e):
        dlg_modal.open = False
        page.update()

    dlg_modal = ft.AlertDialog(
        title=ft.Text("Please confirm"),
        content=ft.Container(
            content=ft.Row(
                wrap=True,
                controls=param_t,
            )
            ),
        actions=[
            ft.TextButton("Cancel", on_click=close_dlg),
        ],
        on_dismiss=lambda e: print("Modal dialog dismissed!", print(parame_selected) ),
    )

    def open_dlg(e):
        page.dialog = dlg
        dlg.open = True
        page.update()

    def open_dlg_modal(e):
        page.dialog = dlg_modal
        dlg_modal.open = True
        page.update()

    page.add(
        ft.ElevatedButton("Open dialog", on_click=open_dlg),
        ft.ElevatedButton("Open modal dialog", on_click=open_dlg_modal),
    )

ft.app(target=main)