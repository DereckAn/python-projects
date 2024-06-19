import type { ILocatedRoute, RaptorEngine, ViewDefinitions } from "index";
import { registerPublicDashboard, RSScriptor } from "index";
import { CodeEditorMonaco } from "raptor/raptorDom/controls/CodeEditorMonaco/CodeEditorMonaco";
import type { IContainerScriptor } from "raptor/renderer/RSScriptorInterfaces";
import { ensureMonacoEditorFramework } from "util/util";

import type { CodeExampleViewModel } from "./controls/CodeExample/CodeExampleViewModel";
import type { ICodeExample } from "./controls/CodeExample/Interfaces";
import { MultiCodeExampleViewModel } from "./controls/CodeExample/MultiCodeExampleViewModel";
import { RaptorDashboard } from "./raptor/RaptorDashboard";

export async function initModule(
  container: HTMLElement,
  route: ILocatedRoute
): Promise<RaptorEngine> {
  const dash = await new RaptorDashboard(container, route).init();
  await ensureMonacoEditorFramework();

  const vm = getViewModel(dash);
  dash.addView(vm.generateView());

  await dash.addViewModel(vm);
  const engine = await dash.render();

  return engine;
}

registerPublicDashboard({
  id: "ext/ml-docs-query/QueryExpression-GetUTMZoneBounds",
  name: "QueryExpression-GetUTMZoneBounds",
  description: "GetUTMZoneBounds query expression documentation",
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
    "sqlselect": [
        "MultipointFromWKT(textmultiPoint) as MultipointFromWKT"
    ]
}`,
          language: "json",
          label: "JSON",
          executionMode: "query",
          runnable: true,
          readOnly: false,
          syntaxHighlightingModelUri: CodeEditorMonaco.modelUris.json.query,
        },
        {
          code: `SELECT MultipointFromWKT(textmultiPoint) AS MultipointFromWKT
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
q.select("MultipointFromWKT(textmultiPoint) as MultipointFromWKT");`,
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
        "MultipointFromWKT('(-94.45312410593034 38.318673559287284),(-90.58593660593034 38.04232235983689), (-86.71874910593034 38.18062898005877)') as MultipointFromWKT"
    ]
}`,
          language: "json",
          label: "JSON",
          executionMode: "query",
          runnable: true,
          readOnly: false,
        },
        {
          code: `SELECT MultipointFromWKT('(-94.45312410593034 38.318673559287284),(-90.58593660593034 38.04232235983689), (-86.71874910593034 38.18062898005877)') AS MultipointFromWKT
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
q.select("MultipointFromWKT('(-94.45312410593034 38.318673559287284),(-90.58593660593034 38.04232235983689),(-86.71874910593034 38.18062898005877)') as MultipointFromWKT");
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

function markdownRow(
  markdown: string,
  s: IContainerScriptor<CodeExampleViewModel>
): void {
  s.row().contentTemplates((s) => {
    s.column({ columnLg: 10, padding: { topLg: 3 } }).contentTemplates((s) => {
      s.paragraph({ text: " " });
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
    ml.fetch("/ext/ml-docs-query/QueryExpression-GetUTMZoneBounds.md", {
      method: "GET",
      credentials: "include",
    })
      .then((resp) => resp.text())
      .then((md) => {
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
    }).contentTemplates((s) => {
      s.row().contentTemplates((s) => {
        s.column({ columnLg: 10 }).contentTemplates((s) => {
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
#### Creates a Multipoint from a WKT string column.
Returns a Multipoint from a WKT string column. The WKT string is defined in the \`test/allshapes\` table.
`,
        s
      );

      s.row().contentTemplates((s) => {
        s.column({
          columnLg: 10,
          border: { color: "secondary", width: 1, rounded: true },
        }).contentTemplates((s) => {
          this._codeExampleViewModels["snippetGroup1"].scriptView(
            s,
            "codeExampleViewModels.snippetGroup1"
          );
        });
      });

      markdownRow(
        `
#### Use a static WKT string to create a Multipoint.
Returns a Multipoint from a hard coded WKT string formated as a Multipoint.
`,
        s
      );

      s.row().contentTemplates((s) => {
        s.column({
          columnLg: 10,
          border: { color: "secondary", width: 1, rounded: true },
        }).contentTemplates((s) => {
          this._codeExampleViewModels["snippetGroup2"].scriptView(
            s,
            "codeExampleViewModels.snippetGroup2"
          );
        });
      });
    });

    const viewDef = scriptor.commitPage();
    return viewDef;
  }
}
