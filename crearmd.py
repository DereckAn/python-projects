# created by @dereckangeles
# 2024-06-13
# To make documentation faster

import flet as ft
import json
from tabulate import tabulate

class TSScripts():
  def __init__(self, title, examples):
    super().__init__()
    examples_text = ""
    markdown_rows = ""
    
    
    def json2jsquery(json):
      js = """const q = ml.query();
    """
      js += f'q.from("{json["table"]["name"]}");\n'
      if 'sqlselect' in json:
        selections = ', '.join([f'"{sel}"' for sel in json['sqlselect']])
        js += f'q.select({selections});'
      if 'take' in json:
        js += f'q.take({json["take"]});\n'
      if 'withgeo' in json:
        js += 'q.withgeo(true);\n'
      return js

    
    def json2sql(json):
      table_name = json['table']['name'].replace('/', '.')
      if 'sqlselect' in json:
        selections = ',\n '.join([f'"{sel}"' for sel in json['sqlselect']])
        
      sql = f"""SELECT {selections}
"""
      sql += f'FROM {table_name}'
      if 'take' in json:
        sql += f'LIMIT {json["take"]}'
      if 'withgeo' in json:
        sql += 'WITH GEO'
      return sql
    
    for i, exa in enumerate(examples):
      example_json = json.loads(exa[0])
      print(i, example_json)
      sql_example = json2sql(example_json)
      js_example = json2jsquery(example_json)
      
      examples_text += f"""{{
      name: "snippetGroup{i+1}",
      sources: [
        {{
          code: `{example_json}`,
          language: "json",
          label: "JSON",
          executionMode: "query",
          runnable: true,
          readOnly: false,
          syntaxHighlightingModelUri: CodeEditorMonaco.modelUris.json.query,
        }},
        {{
          code: `{sql_example}`,
          language: "sql",
          label: "SQL",
          executionMode: "query",
          runnable: true,
          readOnly: false,
        }},
        {{
          code: `{js_example}`,
          language: "javascript",
          label: "JavaScript",
          executionMode: "javascript-query",
          runnable: true,
          readOnly: false,
        }},
      ],
    }},
    """

      markdown_rows += f"""markdownRow(`
### Examples

#### {exa[1]}
{exa[2]}
`,
        s
      );

      s.row().contentTemplates((s) => {{
        s.column({{
          columnLg: 10,
          border: {{ color: "secondary", width: 1, rounded: true }},
        }}).contentTemplates((s) => {{
          this._codeExampleViewModels["snippetGroup1"].scriptView(
            s,
            "codeExampleViewModels.snippetGroup1"
          );
        }});
      }});
      """
      
      
    typescript_code = f"""
import type {{ ILocatedRoute, RaptorEngine, ViewDefinitions }} from "index";
import {{ registerPublicDashboard, RSScriptor }} from "index";
import {{ CodeEditorMonaco }} from "raptor/raptorDom/controls/CodeEditorMonaco/CodeEditorMonaco";
import type {{ IContainerScriptor }} from "raptor/renderer/RSScriptorInterfaces";
import {{ ensureMonacoEditorFramework }} from "util/util";

import type {{ CodeExampleViewModel }} from "./controls/CodeExample/CodeExampleViewModel";
import type {{ ICodeExample }} from "./controls/CodeExample/Interfaces";
import {{ MultiCodeExampleViewModel }} from "./controls/CodeExample/MultiCodeExampleViewModel";
import {{ RaptorDashboard }} from "./raptor/RaptorDashboard";

export async function initModule(
  container: HTMLElement,
  route: ILocatedRoute
): Promise<RaptorEngine> {{
  const dash = await new RaptorDashboard(container, route).init();
  await ensureMonacoEditorFramework();

  const vm = getViewModel(dash);
  dash.addView(vm.generateView());

  await dash.addViewModel(vm);
  const engine = await dash.render();

  return engine;
}}

registerPublicDashboard({{
  id: "ext/ml-docs-query/QueryExpression-{title}",
  name: "QueryExpression-{title}",
  description: "{title} query expression documentation",
}});

function getViewModel(dash: RaptorDashboard): InscribedCirculeViewModel {{
  return new InscribedCirculeViewModel(dash, [
    {examples_text}
  ]);
}}

function markdownRow(
  markdown: string,
  s: IContainerScriptor<CodeExampleViewModel>
): void {{
  s.row().contentTemplates((s) => {{
    s.column({{ columnLg: 10, padding: {{ topLg: 3 }} }}).contentTemplates((s) => {{
      s.paragraph({{ text: " " }});
      s.markdown({{
        markdown: markdown,
      }});
    }});
  }});
}}

export class InscribedCirculeViewModel extends MultiCodeExampleViewModel {{
  constructor(dash: RaptorDashboard, sources: ICodeExample[]) {{
    super(dash, sources);
  }}

  public onAfterRender(): void {{
    super.onAfterRender();

    // fetch markdown content
    ml.fetch("/ext/ml-docs-query/QueryExpression-{title}.md", {{
      method: "GET",
      credentials: "include",
    }})
      .then((resp) => resp.text())
      .then((md) => {{
        this.markdownContent = md;
        this.update();
      }});

    this.update();
  }}

  public generateView(): ViewDefinitions.IRenderingDefinition {{
    const scriptor = RSScriptor.create<CodeExampleViewModel>();
    const s = scriptor.page("page", "page");
    s.container({{
      fluid: true,
      overflow: "auto",
      //heightVH: 100,
      customCssClasses: "ml-dash-md",
      margin: {{
        topLg: 4,
      }},
    }}).contentTemplates((s) => {{
      s.row().contentTemplates((s) => {{
        s.column({{ columnLg: 10 }}).contentTemplates((s) => {{
          s.markdown({{
            markdown: "",
            bindings:{{
              text: "markdownContent",
            }},
          }});
        }});
      }});

      {markdown_rows}
      
    }});

    const viewDef = scriptor.commitPage();
    return viewDef;
  }}
}} 
    """


    with open(f'QueryExpression-{title}.ts', 'w') as f:
        f.write(typescript_code)
    
class MDScripts():
  def __init__(self, title, function, description, retorno, select, where, custom_usage, versions, parameters, notes):
    super().__init__()
    parameters_list = [[f"`{p[0]}`", f'**{p[1]}**' if p[1] == "Required" else 'Optional', f"{', '.join(f'`{x.strip()}`' for x in p[2].value.split(','))}", p[3], p[4], p[5]] for p in parameters]
    
    table = tabulate(parameters_list, headers=["Parameter", "Required", "Type(s)", "Description", "`null` Behavior", "Default"], tablefmt="pipe", stralign="left")

    versions_text = f"## Versions\n{versions}" if len(versions) > 0 else ""
    notes_text = "\n".join([f"- {note}" for note in notes]) if len(notes) > 0 else ""
    notes_text = f"### Notes\n{notes_text}" if len(notes) > 0 else ""
    
    usage_text = f"`{title}` may be used in the query"
    usage_text += " SELECT" if select else ""
    usage_text += " and WHERE" if where else ""
    usage_text += custom_usage if custom_usage else " clauses for analyzing data and applying conditional logic."
    

    info = f"""# {title}

## Description
`{function}`

{description}
"""

    if len(versions) > 0:
        info += f"""{versions_text}"""
        
    info += f"""
### Return Type
`{retorno}` (see [Type Conversions](/docs/QueryExpression-Types))

## Parameters
{table}

## Usage
{usage_text}
"""
    if notes_text:
      info += f"{notes_text}\n"

    with open(f'QueryExpression-{title}.md', 'w') as f:
        f.write(info)

class Example(ft.Column):
    def __init__(self, example_delete):
        super().__init__()
        self.example_delete = example_delete
        self.example_title = ft.TextField(label="Example Title", multiline=True,  width=420)
        self.example_subtitle = ft.TextField(label="Example Subtitle", multiline=True,  width=420) 
        self.example = ft.TextField(label="Example", multiline=True, width=800,)
        self.div = ft.Divider(height=10, color="grey")
        self.display_example = ft.Row(
            wrap=True,
            controls=[
            self.example_title,
            self.example_subtitle,
            self.example, 
            ft.IconButton(
                ft.icons.DELETE_OUTLINE, 
                tooltip="Delete Example", 
                on_click=self.delete_example,
                icon_color="red",),
            self.div,
            
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
                 types_selected,
                 parameter_description,
                 parameter_null,
                 parameter_default, 
                 parameter_delete):
        super().__init__()
        
        self.display_parameter_name = ft.Text(parameter_name)
        self.display_parameter_required = ft.Text("Required" if parameter_required else "Optional")
        self.display_parameter_default = ft.Text(parameter_default if parameter_default else "N/A")
        self.display_parameter_null = ft.Text(parameter_null if parameter_null else "Returns `null`")
        self.display_types_selected = ft.Text(", ".join(types_selected))
        self.display_parameter_description = ft.Text(parameter_description)
        self.parameter_delete = parameter_delete
        
        self.display_view = ft.Row(
            alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
            controls=[
                self.display_parameter_name,
                self.display_parameter_required,
                self.display_types_selected,
                self.display_parameter_description,
                self.display_parameter_default,
                self.display_parameter_null,
                ft.IconButton(
                            ft.icons.DELETE_OUTLINE,
                            tooltip="Delete To-Do",
                            on_click=self.delete_param,
                            icon_color="red",
                        ),
            ],
        )

        self.controls = [self.display_view, ]
        
    def delete_param(self, e):
        self.parameter_delete(self)
    
    # def save_edited_param(self, e):
    #     self.display_parameter_name.value = self.edit_parameter_name.value
    #     self.display_parameter_required.value = self.edit_parameter_required.value
    #     self.display_parameter_default.value = self.edit_parameter_default.value
    #     self.display_parameter_null.value = self.edit_parameter_null.value
    #     self.display_parameter_types = self.edit_parameter_types.value
    #     self.display_view.visible = True
    #     self.edit_view.visible = False
    #     self.update()

class MakerMarkerDown(ft.Column):
    # application's root control is a Column containing all other controls
    def __init__(self):
        super().__init__()
        
        self.vnum = 1
        self.nnotes = 0
        self.width = 900
        self.types_selected = []
        self.param_types = ["String", "Column<String>", 
                            "Double", "Column<Double>", 
                            "Int32", "Columnumn<Int32>", 
                            "Int64", "Column<Int64>", 
                            "Boolean", 'Column<String>("true" or "false")', 
                            "DateTime",	"Column<DateTime>",
                            "Guid",	"Column<Guid>",
                            "TimeSpan",
                            "Point", "Column<XY>",
                            "Line",	"Column<Line>",
                            "Multipoint", "Column<Multipoint>",
                            "Shape", "Column<Poly>"]
        
        self.func_title = self.create_text_field("Title of the Function", "TITLE")
        self.func_function = self.create_text_field("Function", "FUNCTION")
        self.func_description = self.create_text_field("Description of the Function", "DESCRIPTION")
        self.func_return = self.create_text_field("Return Type", "RETURN")
        
        self.table = ft.DataTable(
            columns=[
                ft.DataColumn(ft.Text("Parameter", color="grey")),
                ft.DataColumn(ft.Text("Required", color="grey")),
                ft.DataColumn(ft.Text("Type(s)", color="grey")),
                ft.DataColumn(ft.Text("Description", color="grey")),
                ft.DataColumn(ft.Text("`null` Behavior", color="grey")),
                ft.DataColumn(ft.Text("Default", color="grey")),
                ft.DataColumn(ft.Text("Edit", color="grey")),
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
        
        self.parameter_types = []
        self.parameter_name = ft.TextField(width=125, label="Parameter")
        self.parameter_required = ft.Checkbox( label="")
        self.parameter_description = ft.TextField(label="Description")
        self.parameter_null = ft.TextField(width=80, label="Null Behavior")
        self.parameter_default = ft.TextField(width=80, label="Default")
        self.parameter_dialog_styles = ft.ElevatedButton("Styles", on_click=self.open_dlg_modal)
        self.dlg_modal = ft.AlertDialog(
            title=ft.Text("Parameter Styles"),
            content=ft.Container(
                content=ft.Row(
                    wrap=True,
                    controls=self.parameter_types,
                  )
                ),
            on_dismiss=lambda e: print("Modal dialog dismissed!", self.types_selected),
        )
      
        
        for param_type in self.param_types:
            self.parameter_types.append(
                ft.Chip(
                    label=ft.Text(param_type),  
                    bgcolor=ft.colors.GREY_700,
                    selected_color=ft.colors.GREEN_700,
                    disabled_color=ft.colors.GREEN_100,
                    autofocus=True,
                    on_select=lambda e, param_type=param_type: 
                        self.types_selected.remove(param_type) if param_type in self.types_selected else self.types_selected.append(param_type),
                    )
                )
        
        self.controls = [
            self.titlee("Markdown Maker", "Red"),
            ft.Row(
                controls=[
                    self.func_title,
                ],
            ),
            self.version1,
            self.versions,
            ft.FilledTonalButton("Version", icon="add", on_click=self.add_version),
            self.titlee("Parameters", "blue"),
            self.table,
            ft.Divider(height=1, color="gray"),
            self.parameters,
            ft.Row(
                controls=[
                    self.parameter_name,
                    self.parameter_required,
                    self.parameter_dialog_styles,
                    self.parameter_description,
                    self.parameter_null,
                    self.parameter_default,
                ]),
            ft.FilledTonalButton("Parameter", icon="add", on_click=self.add_parameter),
            self.titlee("SELECT/WHERE", "blue"),
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
    
    def create_text_field(self, hint_text, label):
        return ft.TextField(
            hint_text=hint_text, 
            # on_submit=self.add_clicked, 
            expand=True,
            label=label
    )
    
    def add_parameter(self, e):
        if self.parameter_name.value and self.types_selected and self.parameter_description.value:
            param = Parameter(self.parameter_name.value, 
                              self.parameter_required.value, 
                              self.types_selected, 
                              self.parameter_description.value,
                              self.parameter_null.value, 
                              self.parameter_default.value, 
                              self.parameter_delete)
            self.parameters.controls.append(param)
            self.parameter_name.value = ""
            self.parameter_required.value = False
            self.parameter_null.value = ""
            self.parameter_default.value = ""   
            self.types_selected.clear()
            self.parameter_description.value = ""
            self.parameter_types.clear()
            self.put_stypes_modal()
            self.update()
        else:
            self.alerts("Parameter Name and Type(s) are required")
            
    def open_dlg_modal(self,e):
        self.page.dialog = self.dlg_modal  
        self.dlg_modal.open = True
        self.page.update()
        
    def put_stypes_modal(self):
      for param_type in self.param_types:
            self.parameter_types.append(
                ft.Chip(
                    label=ft.Text(param_type),  
                    bgcolor=ft.colors.GREY_700,
                    selected_color=ft.colors.GREEN_700,
                    disabled_color=ft.colors.GREEN_100,
                    autofocus=True,
                    on_select=lambda e, param_type=param_type: 
                        self.types_selected.remove(param_type) if param_type in self.types_selected else self.types_selected.append(param_type),
                    )
                )
  
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
          example_list.append([exa.example.value, exa.example_title.value, exa.example_subtitle.value])
      
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
              param.display_types_selected,
              param.display_parameter_description.value,
              param.display_parameter_null.value, 
              param.display_parameter_default.value])
      
      if not self.func_title.value or not self.func_function.value or not self.func_description.value or not self.func_return.value or not parameters_list or not example_list:
        self.alerts("Please complete all the fields", "Red")
        return
      
      print("title: ", self.func_title.value)
      print("function: ", self.func_function.value)
      print("description: ", self.func_description.value)
      print("return: ", self.func_return.value)
      print("parameters_list: ", parameters_list)
      print("example_list: ", example_list)
      print("notes_list: ", notes_list)
      print("version_list: ", version_list)
      
      MDScripts(self.func_title.value,
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
      
      TSScripts(self.func_title.value, example_list)
      
      self.alerts("Creating Scripts", "green")
        
    

def main(page: ft.Page):
    page.title = "Create Scritps"
    page.horizontal_alignment = ft.CrossAxisAlignment.CENTER
    page.scroll = ft.ScrollMode.ADAPTIVE

    # create app control and add it to the page
    page.add(MakerMarkerDown())

ft.app(main)

