import type { ILocatedRoute, RaptorEngine, ViewDefinitions } from "index";
import { registerPublicDashboard, RSScriptor } from "index";
import { CodeEditorMonaco } from "raptor/raptorDom/controls/CodeEditorMonaco/CodeEditorMonaco";
import type { IContainerScriptor } from "raptor/renderer/RSScriptorInterfaces";
import { ensureMonacoEditorFramework } from "util/util";

import type { CodeExampleViewModel } from "./controls/CodeExample/CodeExampleViewModel";
import type { ICodeExample } from "./controls/CodeExample/Interfaces";
import { MultiCodeExampleViewModel } from "./controls/CodeExample/MultiCodeExampleViewModel";
import { RaptorDashboard } from "./raptor/RaptorDashboard";

export async function initModule(container: HTMLElement, route: ILocatedRoute): Promise<RaptorEngine> {
    const dash = await new RaptorDashboard(container, route).init();
    await ensureMonacoEditorFramework();

    const vm = getViewModel(dash);
    dash.addView(vm.generateView());

    await dash.addViewModel(vm);
    const engine = await dash.render();

    return engine;
}

registerPublicDashboard({
    id: "ext/ml-docs-query/QueryExpression-CreateShape",
    name: "QueryExpression-CreateShape",
    description: "CreateShape query expression documentation",
});

function getViewModel(dash: RaptorDashboard): InscribedCirculeViewModel {
    return new InscribedCirculeViewModel(dash, [
        {
            name: "snippetGroup1",
            sources: [
                {
                    code: `{
    "table": {
        "name": "test/allshapes"
    },
    "sqlselect": ["CreateShape(allshapes)"]
}`,
                    language: "json",
                    label: "JSON",
                    executionMode: "query",
                    runnable: true,
                    readOnly: false,
                    syntaxHighlightingModelUri: CodeEditorMonaco.modelUris.json.query,
                },
                {
                    code: `SELECT CreateShape(allshapes)
FROM test.allshapes;`,
                    language: "sql",
                    label: "SQL",
                    executionMode: "query",
                    runnable: true,
                    readOnly: false,
                },
                {
                    code: `const q = ml.query();
q.from("test/allshapes");
q.select("CreateShape(allshapes)");`,
                    language: "javascript",
                    label: "JavaScript",
                    executionMode: "javascript-query",
                    runnable: true,
                    readOnly: false,
                },
            ],
        },
        {
            name: "snippetGroup2",
            sources: [
                {
                    code: `{
    "take": 1,
    "table": {
        "name": "test/allshapes"
    },
    "sqlselect": [
        "CreateShape(LineFromWKT('(-112.11914062674626 41.420761289004076,-105.96679687674626 33.8460869780613,-91.90429687674626 34.137572929286634)'))"
    ]
}`,
                    language: "json",
                    label: "JSON",
                    executionMode: "query",
                    runnable: true,
                    readOnly: false,
                },
                {
                    code: `SELECT CreateShape(LineFromWKT('(-112.11914062674626 41.420761289004076,-105.96679687674626 33.8460869780613,-91.90429687674626 34.137572929286634)'))
FROM test.allshapes
LIMIT 1;`,
                    language: "sql",
                    label: "SQL",
                    executionMode: "query",
                    runnable: true,
                    readOnly: false,
                },
                {
                    code: `const q = ml.query();
q.from("test/allshapes");
q.select(
    "CreateShape(LineFromWKT('(-112.11914062674626 41.420761289004076,-105.96679687674626 33.8460869780613,-91.90429687674626 34.137572929286634)'))"
);
q.take(1);`,
                    language: "javascript",
                    label: "JavaScript",
                    executionMode: "javascript-query",
                    runnable: true,
                    readOnly: false,
                },
            ],
        },
    ]);
}

function markdownRow(markdown: string, s: IContainerScriptor<CodeExampleViewModel>): void {
    s.row().contentTemplates(s => {
        s.column({columnLg: 10, padding: {topLg: 3}}).contentTemplates(s => {
            s.paragraph({text: " "});
            s.markdown({
                markdown: markdown,
            });
        });
    });
}

export class InscribedCirculeViewModel extends MultiCodeExampleViewModel {
    constructor(dash: RaptorDashboard, sources: ICodeExample[]) {
        super(dash, sources);
    }

    public onAfterRender(): void {
        super.onAfterRender();

        // fetch markdown content
        ml.fetch("/ext/ml-docs-query/QueryExpression-CreateShape.md", {
            method: "GET",
            credentials: "include",
        })
            .then(resp => resp.text())
            .then(md => {
                this.markdownContent = md;
                this.update();
            });

        this.update();
    }

    public generateView(): ViewDefinitions.IRenderingDefinition {
        const scriptor = RSScriptor.create<CodeExampleViewModel>();
        const s = scriptor.page("page", "page");
        s.container({
            fluid: true,
            overflow: "auto",
            //heightVH: 100,
            customCssClasses: "ml-dash-md",
            margin: {
                topLg: 4,
            },
        }).contentTemplates(s => {
            s.row().contentTemplates(s => {
                s.column({columnLg: 10}).contentTemplates(s => {
                    s.markdown({
                        markdown: "",
                        bindings: {
                            text: "markdownContent",
                        },
                    });
                });
            });

            markdownRow(
                `
### Examples

#### Creates a line from two points
It takes to point columns from table \`test/allshapes\` and returns a LineString.
`,
                s,
            );

            s.row().contentTemplates(s => {
                s.column({
                    columnLg: 10,
                    border: {color: "secondary", width: 1, rounded: true},
                }).contentTemplates(s => {
                    this._codeExampleViewModels["snippetGroup1"].scriptView(s, "codeExampleViewModels.snippetGroup1");
                });
            });

            markdownRow(
                `

#### Creates a line from two static points with a boolean flag
We pass two point functions to make our values static and we pass the Boolean "False" to indicate that we do not want to split the line at the date line.
`,
                s,
            );

            s.row().contentTemplates(s => {
                s.column({
                    columnLg: 10,
                    border: {color: "secondary", width: 1, rounded: true},
                }).contentTemplates(s => {
                    this._codeExampleViewModels["snippetGroup2"].scriptView(s, "codeExampleViewModels.snippetGroup2");
                });
            });
        });

        const viewDef = scriptor.commitPage();
        return viewDef;
    }
}
