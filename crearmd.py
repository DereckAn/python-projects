# created by @dereckangeles
# 2024-06-13
# To make the documentation faster

import flet as ft

class Example(ft.Column):
    def __init__(self, example_delete):
        super().__init__()
        self.example_delete = example_delete
        self.example = ft.TextField(label="Example", multiline=True, expand=1)
        self.display_example = ft.Row(controls=[
            self.example, 
            ft.IconButton(
                ft.icons.DELETE_OUTLINE, 
                tooltip="Delete Example", 
                on_click=self.delete_example,
                icon_color="red",),
        ])  

        self.controls = [self.display_example]
    
    def delete_example(self, e):
        self.example_delete(self)

class Notes(ft.Column):
    def __init__(self, notes_delete, nnotes=2):
        super().__init__()
        self.notes_delete = notes_delete
        self.note = ft.TextField(label=f"Note {nnotes}", multiline=True, expand=1)
        self.display_notes = ft.Row(controls=[
            self.note,
            ft.IconButton(
                ft.icons.DELETE_OUTLINE, 
                tooltip=f"Delete Note {nnotes}", 
                on_click=self.delete_notes,
                icon_color="red",),])
        
        
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
                 parameter_description,
                 parameter_null,
                 parameter_default, 
                 parameter_delete):
        super().__init__()
        
        self.display_parameter_name = ft.Text(parameter_name)
        self.display_parameter_required = ft.Text("Required" if parameter_required else "Optional")
        self.display_parameter_default = ft.Text(parameter_default if parameter_default else "N/A")
        self.display_parameter_null = ft.Text(parameter_null if parameter_null else "Returns `null`")
        self.display_parameter_types = ft.Text(", ".join(parameter_types))
        self.display_parameter_description = ft.Text(parameter_description)
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
                self.display_parameter_description,
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
        self.nnotes = 0
        self.width = 1000
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
                ft.DataColumn(ft.Text("Description")),
                ft.DataColumn(ft.Text("`null` Behavior")),
                ft.DataColumn(ft.Text("Default")),
                ft.DataColumn(ft.Text("Edit")),
            ],
            rows=[],
        )
        self.parameters = ft.Column()
        self.versions = ft.Column()
        self.notes=ft.Column()
        self.examples = ft.Column()
        self.select = ft.Checkbox(label="SELECT", value=True)
        self.where = ft.Checkbox(label="WHERE", value=True)
        self.custom_usage = ft.TextField(label="Default SELECT/WHERE",hint_text="may be used in the query SELECT and WHERE clauses for analyzing data and applying conditional logic.")
        self.version1 = ft.Container(content=ft.Column(
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
        
        self.parameter_name = ft.TextField(width=125, label="Parameter")
        self.parameter_required = ft.Checkbox( label="")
        self.parameter_types = []
        self.parameter_description = ft.TextField(label="Description")
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
            self.version1,
            self.versions,
            ft.FilledTonalButton("Version", icon="ad    d", on_click=self.add_version),
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
                    self.parameter_description,
                    self.parameter_null,
                    self.parameter_default,
                ]),
            ft.FilledTonalButton("Parameter", icon="add", on_click=self.add_parameter),
            self.titlee("Query Builder", "blue"),
            ft.Row(
                alignment=ft.MainAxisAlignment.SPACE_BETWEEN,  # Alineación para distribuir los controles
                controls=[
                    ft.Row(  # Columna para los checkboxes
                        controls=[
                            self.select,
                            self.where
                        ],
                    ),
                    self.custom_usage,
                ],
            ),  
            self.titlee("Notes", "blue"),
            self.notes,
            ft.FilledTonalButton("Note", icon="add", on_click=self.add_notes),
            self.titlee("Examples", "blue"),
            self.examples,
            ft.FilledTonalButton("Example", icon="add", on_click=self.add_example),
            ft.Divider(height=10, color="red"),
            ft.Row(
                [ft.CupertinoFilledButton(content=ft.Text("Create Scripts", ), on_click=self.create_scripts),],
                alignment=ft.MainAxisAlignment.CENTER,  # Alineación para distribuir los controles
            )
        ]
        
    def titlee(self, text, color):
        return ft.Column(
            alignment=ft.CrossAxisAlignment.CENTER,
            controls=[
                        ft.Divider(height=10, color="white"),
                        ft.Row(
                            [ft.Text(value=text, theme_style=ft.TextThemeStyle.HEADLINE_LARGE, color=color, ),], alignment=ft.MainAxisAlignment.CENTER,
                               ),
                        
                    ]
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
        if self.parameter_name.value and self.parameter_types:
            param = Parameter(self.parameter_name.value, 
                              self.parameter_required.value, 
                              self.parameter_types, 
                              self.parameter_description.value,
                              self.parameter_null.value, 
                              self.parameter_default.value, 
                              self.parameter_delete)
            self.parameters.controls.append(param)
            self.parameter_name.value = ""
            self.parameter_required.value = False
            self.parameter_null.value = ""
            self.parameter_default.value = ""   
            self.parameter_types = []
            self.parameter_description.value = ""
            self.update()
        else:
            self.alerts("Parameter Name and Type(s) are required")
            
    def alerts(self, text, color="red"):
        self.page.snack_bar = ft.SnackBar(
            ft.Text(text),
            bgcolor=color,
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
    
    def add_example(self, e):
        example = Example(self.delete_example)
        self.examples.controls.append(example)
        self.update()

    def delete_example(self, e):
        self.examples.controls.remove(e)
        self.update()
    
    def create_scripts(self, e):
        example_list = []
        notes_list = []
        version_list = []
        parameters_list = []
        
        for exa in self.examples.controls:
            example_list.append(exa.example.value)
        
        for note in self.notes.controls:
            notes_list.append(note.note.value)
        
        for ver in self.versions.controls:
            version_list.append([
                ver.display_version_name.value, 
                ver.display_version_return.value, 
                ver.display_version_description.value])
        
        for param in self.parameters.controls:
            parameters_list.append([
                param.display_parameter_name.value, 
                param.display_parameter_required.value, 
                param.display_parameter_types, 
                param.display_parameter_null.value, 
                param.display_parameter_default.value])
        
        print("title: ", self.func_title.value)
        print("function: ", self.func_function.value)
        print("description: ", self.func_description.value)
        print("return: ", self.func_return.value)
        print("parameters_list: ", parameters_list)
        print("example_list: ", example_list)
        print("notes_list: ", notes_list)
        print("version_list: ", version_list)
        
        self.md_scripts(self.func_title.value,
                           self.func_function.value,
                           self.func_description.value,
                           self.func_return.value,
                           self.select.value,
                           self.where.value,
                           self.custom_usage.value,
                           version_list,
                           parameters_list,
                           notes_list
                           )
        
        self.ts_scripts(self.func_title.value,
                           example_list
                           )
        
        self.alerts("Creating Scripts", "green")
        
    def md_scripts(self, title, function, description, retorno, select, where, custom_usage, versions, parameters, notes):
        
        rows = '\n'.join([f"| `{p[0]}`| {'**Required**' if p[1] == "Required" else 'Optional'} | {', '.join(f'`{x.strip()}`' for x in p[2].value.split(','))} | {p[3]} | {p[4]} |" for p in parameters])
        versions_text = f"## Versions\n{versions}" if len(versions) > 0 else ""
        notes_text = f"### Notes\n- {notes}" if len(notes) > 0 else ""
        
        usage_text = f"`{title}` may be used in the query"
        usage_text += " SELECT" if select else ""
        usage_text += " and WHERE" if where else ""
        usage_text += custom_usage if custom_usage else " clauses for analyzing data and applying conditional logic."
        

        info = f"""# {title}

## Description
`{function}`

{description}

{versions_text}

### Return Type
`{retorno}` (see [Type Conversions](/docs/QueryExpression-Type))

## Parameters
| Parameter | Required | Type(s) | Description | `null` Behavior | Default |
| :-------- | :------- | :------ | :---------- | :-------------- | :------ |
{rows}

## Usage
{usage_text}

{notes_text}

"""

        # Abre el archivo en modo de escritura. Si el archivo no existe, se creará.
        with open(f'QueryExpression-{title}.md', 'w') as f:
            # Escribe la información en el archivo
            f.write(info)

            # Cierra el archivo
        
    def ts_scripts(self, title, examples):
        pass
    
def main(page: ft.Page):
    page.title = "Create Scritps"
    page.horizontal_alignment = ft.CrossAxisAlignment.CENTER
    page.scroll = ft.ScrollMode.ADAPTIVE

    # create app control and add it to the page
    page.add(MakerMarkerDown())

ft.app(main)

