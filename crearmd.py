# created by @dereckangeles
# 2024-06-13
# To make the documentation faster

import flet as ft

class Notes(ft.Column):
    def __init__(self, notes_delete, nnotes=2):
        super().__init__()
        self.display_notes = ft.Row(controls=[
            ft.TextField(label=f"Note {nnotes}", multiline=True, expand=1), 
            ft.IconButton(
                ft.icons.DELETE_OUTLINE, 
                tooltip=f"Delete Note {nnotes}", 
                on_click=self.delete_notes,
                icon_color="red",),])
        self.notes_delete = notes_delete
        
        self.controls = [self.display_notes]
    
    def delete_notes(self, e):
        self.notes_delete(self)
        
class Version(ft.Column):
    def __init__(self, version_delete, vnum=2):
        super().__init__()
        self.display_version_name = ft.TextField(label=f"Version {vnum} Name")
        self.display_version_return = ft.TextField(label=f"Version {vnum} Return Type")
        self.display_version_description = ft.TextField(label=f"Version {vnum} Description", expand=1)
        self.version_delete = version_delete
        
        self.display_version = ft.Container(
            margin=10,
            padding=10,
            border_radius=10,
            bgcolor=ft.colors.GREY_900,
            content=ft.Column(
                controls=[
                    ft.Text(value=f"Version {vnum}", theme_style=ft.TextThemeStyle.HEADLINE_SMALL, color="green"),
                    self.display_version_name,
                    ft.Row(
                        controls=[
                            self.display_version_description, 
                            self.display_version_return,
                            ft.IconButton(
                                ft.icons.DELETE_OUTLINE,
                                tooltip=f"Delete Version {vnum}",
                                on_click=self.delete_version,
                                icon_color="red",
                            ),
                       ],
                    )
                ]
            )
        )
        
        self.controls = [self.display_version]
    
    def delete_version(self, e):
        self.version_delete(self)

class Parameter(ft.Column):
    def __init__(self, parameter_name, 
                 parameter_required, 
                 parameter_types,
                 parameter_null,
                 parameter_default, 
                 parameter_delete):
        super().__init__()
        
        self.display_parameter_name = ft.Text(parameter_name)
        self.display_parameter_required = ft.Text("Required" if parameter_required else "Optional")
        self.display_parameter_default = ft.Text(parameter_default if parameter_default else "N/A")
        self.display_parameter_null = ft.Text(parameter_null if parameter_null else "Returns `null`")
        self.display_parameter_types = ft.Text(", ".join(parameter_types))
        self.parameter_delete = parameter_delete
        
        # self.display_task = ft.Checkbox(
        #     value=False, label=self.task_name, on_change=self.status_changed
        # )
        
        # self.edit_parameter_name = ft.TextField(expand=1)
        # self.edit_parameter_required = ft.Checkbox()
        # self.edit_parameter_default = ft.TextField(expand=1)
        # self.edit_parameter_null = ft.TextField(expand=1)
        # self.edit_parameter_types = ft.Container(
        #                 width=200,
        #                 # content=
        #                 # ft.ExpansionTile(
                            
        #                 #     title=ft.Text("Types"),
        #                 #     affinity=ft.TileAffinity.LEADING,
        #                 #     initially_expanded=False,
        #                 #     collapsed_text_color=ft.colors.BLUE,
        #                 #     text_color=ft.colors.BLUE,
        #                 #     controls=[
        #                 #         ft.ListTile(title=ft.Container(
        #                 #             height=100,
        #                 #             content=lv))
        #                 #     ]
        #                 # )
        #             ),
        

        self.display_view = ft.Row(
            alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
            vertical_alignment=ft.CrossAxisAlignment.CENTER,
            controls=[
                self.display_parameter_name,
                self.display_parameter_required,
                self.display_parameter_types,
                self.display_parameter_default,
                self.display_parameter_null,
                ft.Row(
                    spacing=0,
                    controls=[
                        ft.IconButton(
                            icon=ft.icons.CREATE_OUTLINED,
                            tooltip="Edit To-Do",
                            # on_click=self.edit_param,
                        ),
                        ft.IconButton(
                            ft.icons.DELETE_OUTLINE,
                            tooltip="Delete To-Do",
                            on_click=self.delete_param,
                        ),
                    ],
                ),
                
            ],
        )

        # self.edit_view = ft.Row(
        #     visible=False,
        #     alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
        #     vertical_alignment=ft.CrossAxisAlignment.CENTER,
        #     controls=[
        #         self.edit_parameter_name,
        #         self.edit_parameter_required,
        #         self.edit_parameter_types,
        #         self.edit_parameter_default,
        #         self.edit_parameter_null,
        #         ft.IconButton(
        #             icon=ft.icons.DONE_OUTLINE_OUTLINED,
        #             icon_color=ft.colors.GREEN,
        #             tooltip="Update To-Do",
        #             on_click=self.save_edited_param,
        #         ),
        #     ],
        # )
        
        self.controls = [self.display_view, ]
        
    def delete_param(self, e):
        self.parameter_delete(self)
    
    # def edit_param(self, e):
    #     self.edit_parameter_name.value = self.display_parameter_name.value
    #     self.edit_parameter_required.value = self.display_parameter_required.value
    #     self.edit_parameter_default.value = self.display_parameter_default.value
    #     self.edit_parameter_null.value = self.display_parameter_null.value
    #     self.edit_parameter_types.value = self.display_parameter_types
    #     self.display_view.visible = False
    #     self.edit_view.visible = True
    #     self.update()
        
    def save_edited_param(self, e):
        self.display_parameter_name.value = self.edit_parameter_name.value
        self.display_parameter_required.value = self.edit_parameter_required.value
        self.display_parameter_default.value = self.edit_parameter_default.value
        self.display_parameter_null.value = self.edit_parameter_null.value
        self.display_parameter_types = self.edit_parameter_types.value
        self.display_view.visible = True
        self.edit_view.visible = False
        self.update()

class MakerMarkerDown(ft.Column):
    # application's root control is a Column containing all other controls
    def __init__(self):
        super().__init__()
        
        self.vnum = 1
        self.nnotes =1
        self.width = 800
        self.param_types = ["String", "Col<String>", "Double", "Col<Double>", "Int32", "Col<Int32>"]
        
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
                ft.DataColumn(ft.Text("Edit")),
            ],
            rows=[],
        )
        self.parameters = ft.Column()
        self.versions = ft.Column(
            controls=[
                ft.Container(content=ft.Column(
                    controls=[
                        self.titlee("Version 1", "Blue"),
                        ft.Row(
                            controls=[ 
                                self.func_function,
                                self.func_return
                            ],
                        ),
                        ft.Row( controls=[self.func_description,]),
                            ]
                        ))
            ]
        )
        self.notes=ft.Column()
        
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
        
        self.controls = [
            self.titlee("Markdown Maker", "Red"),
            ft.Row(
                controls=[
                    self.func_title,
                ],
            ),
            ft.Divider(height=1, color="gray"),
            self.versions,
            ft.CupertinoFilledButton(text="Add Version", on_click=self.add_version, padding=10,),
            ft.Divider(height=1, color="white"),
            self.titlee("Parameters", "blue"),
            self.table,
            ft.Divider(height=1, color="gray"),
            self.parameters,
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
            ft.CupertinoFilledButton(text="Add Parameter", on_click=self.add_parameter, padding=10, ),
            ft.Divider(height=1, color="white"),
            self.titlee("Query Builder", "blue"),
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
            ft.Divider(height=1, color="white"),
            self.titlee("Notes", "blue"),
            ft.TextField(label="NOTES", hint_text="- Add a note, if any"),
            self.notes,
            ft.CupertinoFilledButton(text="Add Note", on_click=self.add_notes, padding=10, ),
            
            

        ]
        
    def titlee(self, text, color):
        return ft.Row(
                [ft.Text(value=text, theme_style=ft.TextThemeStyle.HEADLINE_LARGE, color=color)],
                alignment=ft.MainAxisAlignment.CENTER,
            )
    
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
        print(f'checkboxes: {self.checkboxes}')
        if self.parameter_name.value and self.parameter_types:
            param = Parameter(self.parameter_name.value, 
                              self.parameter_required.value, 
                              self.parameter_types, 
                              self.parameter_null.value, 
                              self.parameter_default.value, 
                              self.parameter_delete)
            self.parameters.controls.append(param)
            self.parameter_name.value = ""
            self.parameter_required.value = False
            self.parameter_null.value = ""
            self.parameter_default.value = ""   
            self.parameter_types = []
            self.update()
        else:
            self.alerts("Parameter Name and Type(s) are required")
            
    def alerts(self, text):
        self.page.snack_bar = ft.SnackBar(
            ft.Text(text),
            bgcolor="red",
        )
        self.page.snack_bar.open = True
        self.page.update()

    def add_version(self, e):
        self.vnum += 1
        vers = Version(self.version_delete, self.vnum)
        self.versions.controls.append(vers)
        self.update()
        
    def parameter_delete(self, param):
        self.parameters.controls.remove(param)
        self.update()

    def version_delete(self, version):
        self.versions.controls.remove(version)
        self.vnum -= 1
        self.update()

    def add_notes(self, e):
        self.nnotes += 1
        notes = Notes(self.delete_notes, self.nnotes)
        self.notes.controls.append(notes)
        self.update()

    def delete_notes(self, e):
        self.notes.controls.remove(e)
        self.nnotes -= 1
        self.update()
    
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
