# created by @dereckangeles
# 2024-06-13
# To make the documentation faster

import flet as ft

class Parameter(ft.Column):
    def __init__(self, parameter_name, 
                 parameter_required, 
                 parameter_delete, 
                 parameter_default, 
                 parameter_null,
                 parameter_types):
        super().__init__()
        # self.completed = False
        
        self.parameter_name = parameter_name
        self.parameter_required = parameter_required
        self.parameter_types = parameter_types
        self.parameter_default = parameter_default
        self.parameter_null = parameter_null
        self.parameter_delete = parameter_delete
        
        # self.display_task = ft.Checkbox(
        #     value=False, label=self.task_name, on_change=self.status_changed
        # )
        
        self.edit_parameter_name = ft.TextField(expand=1)

        self.display_view = ft.Row(
            alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
            vertical_alignment=ft.CrossAxisAlignment.CENTER,
            controls=[
                self.edit_parameter_name,
                ft.Row(
                    spacing=0,
                    controls=[
                        ft.IconButton(
                            icon=ft.icons.CREATE_OUTLINED,
                            tooltip="Edit To-Do",
                            on_click=self.edit_clicked,
                        ),
                        ft.IconButton(
                            ft.icons.DELETE_OUTLINE,
                            tooltip="Delete To-Do",
                            on_click=self.delete_clicked,
                        ),
                    ],
                ),
            ],
        )

        self.edit_view = ft.Row(
            visible=False,
            alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
            vertical_alignment=ft.CrossAxisAlignment.CENTER,
            controls=[
                self.edit_name,
                ft.IconButton(
                    icon=ft.icons.DONE_OUTLINE_OUTLINED,
                    icon_color=ft.colors.GREEN,
                    tooltip="Update To-Do",
                    on_click=self.save_clicked,
                ),
            ],
        )
        self.controls = [self.display_view, self.edit_view]



class MakerMarkerDown(ft.Column):
    # application's root control is a Column containing all other controls
    def __init__(self):
        super().__init__()
        
        self.func_title = self.create_text_field("Title of the Function", "TITLE")
        self.func_function = self.create_text_field("Function", "FUNCTION")
        self.func_description = self.create_text_field("Description of the Function", "DESCRIPTION")
        self.func_return = self.create_text_field("Return Type", "RETURN")
        self.table = ft.DataTable(
            columns=[
                ft.DataColumn(ft.Text("Parameter")),
                ft.DataColumn(ft.Text("Required")),
                ft.DataColumn(ft.Text("Type(s)")),
                ft.DataColumn(ft.Text("`null` Behavior")),
                ft.DataColumn(ft.Text("Default")),
                ft.DataColumn(ft.Text("Delete")),
            ],
            rows=[],
        )
        
        self.param_types = ["String", "Col<String>", "Double", "Col<Double>", "Int32", "Col<Int32>"]
        
        self.parameter_name = ft.TextField(width=125, label="Parameter")
        self.parameter_required = ft.Checkbox( label="Required")
        self.parameter_types = []
        self.parameter_null = ft.TextField(width=150, label="Null Behavior")
        self.parameter_default = ft.TextField(width=150, label="Default")
       
        
        self.checkboxes = {}
        
        lv = ft.ListView(expand=1, spacing=10, padding=20, auto_scroll=False)
        for param_type in self.param_types:
            checkbox = ft.Checkbox(param_type)
            lv.controls.append(checkbox)
            self.checkboxes[param_type] = checkbox
            
        
         
        

        self.width = 800
        self.controls = [
            ft.Row(
                [ft.Text(value="Markdown Maker", theme_style=ft.TextThemeStyle.HEADLINE_MEDIUM, color="blue")],
                alignment=ft.MainAxisAlignment.CENTER,
            ),
            ft.Row(
                controls=[
                    self.func_title,
                ],
            ),
            ft.Row(
                controls=[ 
                    self.func_function,
                    self.func_return
                ],
            ),
            ft.Row( controls=[self.func_description,]),
            self.table,
            ft.Row(
                controls=[
                    self.parameter_name,
                    self.parameter_required,
                    ft.Container(
                        width=200,
                        content=
                        ft.ExpansionTile(
                            
                            title=ft.Text("Types"),
                            affinity=ft.TileAffinity.LEADING,
                            initially_expanded=False,
                            collapsed_text_color=ft.colors.BLUE,
                            text_color=ft.colors.BLUE,
                            controls=[
                                ft.ListTile(title=ft.Container(
                                    height=100,
                                    content=lv))
                            ]
                        )
                    ),
                    self.parameter_null,
                    self.parameter_default,
                ]),
            ft.CupertinoButton(text="Add Parameter", on_click=self.add_parameter, padding=0, ),
            ft.Row(
                alignment=ft.MainAxisAlignment.SPACE_BETWEEN,  # Alineación para distribuir los controles
                controls=[
                    ft.Row(  # Columna para los checkboxes
                        controls=[
                            ft.Checkbox(label="SELECT", value=True),
                            ft.Checkbox(label="WHERE", value=False),
                        ],
                    ),
                    ft.TextField(label="Default SELECT/WHERE",hint_text="may be used in the query SELECT and WHERE clauses for analyzing data and applying conditional logic."),  # TextField a la derecha
                ],
            ),
            ft.Container(
                ft.TextField(label="NOTES", hint_text="- Add a note, if any"),),

        ]
    
    def update_parameters_types(self):
        self.parameter_types.clear()
        
        for param_type, checkbox in self.checkboxes.items():
            if checkbox.value:
                self.parameter_types.append(param_type)
    
    def create_text_field(self, hint_text, label):
        return ft.TextField(
            hint_text=hint_text, 
            # on_submit=self.add_clicked, 
            expand=True, 
            label=label
    )
    def add_parameter(self, e):
        self.update_parameters_types()
        print(self.parameter_name.value)
        print(self.parameter_required.value)
        print(self.parameter_types)
        print(self.parameter_null.value)
        print(self.parameter_default.value)
        # pass
        # if self.new_task.value:
        #     task = Parameter(self.new_task.value, self.task_status_change, self.task_delete)
        #     self.tasks.controls.append(task)
        #     self.new_task.value = ""
        #     self.new_task.focus()
        #     self.update()

    def task_status_change(self, task):
        self.update()

    def task_delete(self, task):
        self.tasks.controls.remove(task)
        self.update()

    def tabs_changed(self, e):
        self.update()

    def clear_clicked(self, e):
        self.page.update()

    def add_row(self, e):
        # Crear una nueva fila con los valores de los TextField
        new_row = ft.DataRow(cells=[
            ft.DataCell(ft.Text(self.param_field.get_text())),
            ft.DataCell(ft.Text(self.required_field.get_text())),
            ft.DataCell(ft.Text(self.type_field.get_text())),
            ft.DataCell(ft.Text(self.null_field.get_text())),
        ])

        # Agregar la nueva fila al DataTable
        self.data_table.rows.append(new_row)

        # Limpiar los TextField
        self.param_field.set_text("")
        self.required_field.set_text("")
        self.type_field.set_text("")
        self.null_field.set_text("")

def main(page: ft.Page):
    page.title = "Create Markdown scritps"
    page.horizontal_alignment = ft.CrossAxisAlignment.CENTER
    page.scroll = ft.ScrollMode.ADAPTIVE

    # create app control and add it to the page
    page.add(MakerMarkerDown())


ft.app(main)


# title = "GetUTMZoneBounds"
# fucntion = "GetUTMZoneBounds(utm)"
# description = "Converts UTM grid zone to geographic bounds."
# retorno = "Int32"

# # Define tus parámetros aquí. Cada diccionario representa una fila.
# parametros = [
#     {"Parameter": "param1", "Required": "Yes", "Type(s)": "int", "Description": "Este es el param1", "`null` Behavior": "N/A"},
#     {"Parameter": "param2", "Required": "No", "Type(s)": "string", "Description": "Este es el param2", "`null` Behavior": "N/A"},
#     # Puedes agregar más parámetros aquí...
# ]

# # Genera las filas de la tabla
# rows = "\n".join(
#     "| `{Parameter}` | {Required} | {Type(s)} | {Description} | {`null` Behavior} |".format(**param)
#     for param in parametros
# )

# info = f"""# {title}

# ## Description
# `{fucntion}`

# {description}

# ### Return Type
# `{retorno}` (see [Type Conversions](/docs/QueryExpression-Type))

# ## Parameters
# | Parameter | Required | Type(s) | Description | `null` Behavior |
# | :-------- | :------- | :------ | :---------- | :-------------- |
# {rows}

# ## Usage
# `{title}` may be used in the query SELECT and WHERE clauses for analyzing data and applying conditional logic.




# """

# # Abre el archivo en modo de escritura. Si el archivo no existe, se creará.
# with open(f'{title}.md', 'w') as f:
#     # Escribe la información en el archivo
#     f.write(info)
