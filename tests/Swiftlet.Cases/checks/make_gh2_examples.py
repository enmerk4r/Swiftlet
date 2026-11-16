"""Create GH2 (.ghz) counterparts of the GH1 example files and verify them.

For each eligible example (every Swiftlet object migrates natively and nothing is left as GH1 interop):
  1. read the .gh with GH_Archive and convert it with DocumentIO.Convert1(..., MigrationLevel.Total),
     the conversion the GH2 editor runs on open;
  2. recreate the GH1 scribbles (which the converter drops) as GH2 ScribbleObjects with the same text
     at the same canvas position (the converter keeps GH1 coordinates);
  3. save through GH2's own DocumentIO.Save next to the original (<name>.ghz; the .gh is not touched);
  4. read the .ghz back with GrasshopperIO, compare object/scribble/wire counts, put it on the canvas and
     solve it (except files whose endpoints need API keys), and report per-object errors/warnings.

    python tests/Swiftlet.Cases/checks/make_gh2_examples.py --port N [--dry-run]
"""
import argparse, json, pathlib, sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from cases import REPO, run_csharp  # noqa: E402

# Eligible files (migrate_files.py: all Swiftlet native, no GH1 interop) and whether to solve them.
EXAMPLES = {
    "food-for-rhino/swiftlet_examples.gh": "solve",                 # httpbin.org, no key
    "swiftlet-iris-prediction/iris-simple.gh": "solve",             # local 127.0.0.1:8000 (not running here)
    "swiftlet-listeners/swiftlet-http-listener.gh": "solve",        # local listener
    "swiftlet-mcp/mcp-test.gh": "solve",                            # local MCP server
    "swiftlet-openai-api/swiftlet-chat-gpt-example.gh": "no-solve", # api.openai.com needs a key
    "swiftlet-simple-examples/auth.gh": "solve",
    "swiftlet-simple-examples/get-headers.gh": "solve",
    "swiftlet-simple-examples/get-query-params.gh": "solve",
    "swiftlet-simple-examples/get-simple.gh": "solve",
    "swiftlet-simple-examples/html-parsing-simple.gh": "solve",
    "swiftlet-simple-examples/json-casting.gh": "solve",
    "swiftlet-simple-examples/json-construct.gh": "solve",
    "swiftlet-simple-examples/json-parsing-complex.gh": "solve",
    "swiftlet-simple-examples/json-parsing-simple.gh": "solve",
    "swiftlet-simple-examples/post-simple.gh": "solve",
}
SKIPPED = {
    "swiftlet-google-maps-api/swiftlet-google-maps-api.gh": "GhPython Script / Custom Preview stay GH1 interop; uses an uninstalled plugin",
    "swiftlet-iris-prediction/iris-3d.gh": "Brep Edges / Custom Preview / GhPython Script stay GH1 interop",
    "swiftlet-openai-api/swiftlet-dalle-example.gh": "GhPython Script / Import Image stay GH1 interop",
    "swiftlet-server-components/server_demo.gh": "Populate 3D / Voronoi 3D stay GH1 interop",
    "swiftlet-listeners/swiftlet-socket-listener.gh": "archived Socket Listener has no GH2 target",
    "swiftlet-simple-examples/multipart-form-data.gh": "archived Create Multipart Form Body Named/Unnamed have no GH2 target",
}

SCRIPT = r'''
using System; using System.Collections; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Text.Json;
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == n);
var gh1 = A("Grasshopper"); var gh2 = A("Grasshopper2"); var ghio = A("GH_IO"); var gio = A("GrasshopperIO");
string src = @"__SRC__", dst = @"__DST__"; bool solve = __SOLVE__, dryRun = __DRY__;
var r = new Dictionary<string, object> { ["file"] = src };
int Count(object o) => o is null ? 0 : (int)(o.GetType().GetProperty("Count")?.GetValue(o) ?? 0);
var paramT = gh2.GetType("Grasshopper2.Parameters.IParameter", true);
int Wires(object doc) {
    int n = 0;
    foreach (var o in (IEnumerable)((dynamic)doc).Objects.Forwards) {
        var ps = o.GetType().GetProperty("Parameters")?.GetValue(o);
        if (ps != null) { int c = (int)ps.GetType().GetProperty("InputCount").GetValue(ps); var input = ps.GetType().GetMethod("Input", new[] { typeof(int) });
            for (int i = 0; i < c; i++) { var p = input.Invoke(ps, new object[] { i }); n += Count(p.GetType().GetProperty("Inputs")?.GetValue(p)); } }
        else if (paramT.IsInstanceOfType(o)) n += Count(o.GetType().GetProperty("Inputs")?.GetValue(o));
        else foreach (var prop in o.GetType().GetProperties().Where(p => paramT.IsAssignableFrom(p.PropertyType) && p.GetIndexParameters().Length == 0)) { var p = prop.GetValue(o); if (p != null) { n += Count(p.GetType().GetProperty("Inputs")?.GetValue(p)); break; } }
    }
    return n;
}
(int objects, int swiftlet, int interop, int scribbles) Census(object doc) {
    int a = 0, s = 0, i = 0, sc = 0;
    foreach (var o in (IEnumerable)((dynamic)doc).Objects.Forwards) { a++; var t = o.GetType();
        if (t.Assembly.GetName().Name == "Swiftlet.Gh2") s++; else if (t.Name.StartsWith("GH1Interop")) i++; else if (t.Name == "ScribbleObject") sc++; }
    return (a, s, i, sc);
}
try
{
    // 1. GH1 -> GH2
    dynamic archive = Activator.CreateInstance(ghio.GetType("GH_IO.Serialization.GH_Archive", true));
    if (!(bool)archive.ReadFromFile(src)) throw new Exception("GH1 could not read the file");
    dynamic doc1 = Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document", true));
    if (!(bool)archive.ExtractObject(doc1, "Definition")) throw new Exception("GH1 could not extract");
    var scribbles = new List<(string text, float x, float y)>();
    foreach (dynamic o in doc1.Objects) if (((object)o).GetType().Name == "GH_Scribble") scribbles.Add(((string)o.Text, (float)o.Attributes.Pivot.X, (float)o.Attributes.Pivot.Y));
    var convert = gh2.GetType("Grasshopper2.Doc.DocumentIO", true).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).First(m => m.Name == "Convert1");
    dynamic doc2 = convert.Invoke(null, new[] { Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Document", true), new object[] { (object)doc1 }), Enum.Parse(gh2.GetType("Grasshopper2.Doc.Migration.MigrationLevel"), "Total") });
    var before = Census((object)doc2);
    if (before.interop > 0) throw new Exception(before.interop + " objects left as GH1 interop");

    // 2. scribbles
    var scribbleT = gh2.GetType("Grasshopper2.SpecialObjects.ScribbleObject", true);
    var pointF = typeof(Eto.Drawing.PointF);
    var add = ((object)doc2.Objects).GetType().GetMethod("Add", new[] { gh2.GetType("Grasshopper2.Doc.IDocumentObject", true), pointF });
    foreach (var (text, x, y) in scribbles) {
        dynamic sc = Activator.CreateInstance(scribbleT);
        sc.Text = text;
        add.Invoke(doc2.Objects, new object[] { sc, new Eto.Drawing.PointF(x, y) });
    }
    var converted = Census((object)doc2); int wires = Wires((object)doc2);
    r["converted"] = new { converted.objects, converted.swiftlet, converted.scribbles, wires, gh1Scribbles = scribbles.Count };

    // 3. save with GH2's document writer
    if (!dryRun) {
        var ioT = gh2.GetType("Grasshopper2.Doc.DocumentIO", true);
        dynamic io = Activator.CreateInstance(ioT, new object[] { (object)doc2, false, false, false, false });
        var contentsT = gh2.GetType("Grasshopper2.Doc.FileContents", true);
        var save = ioT.GetMethod("Save", new[] { typeof(string), contentsT });
        r["saved"] = save.Invoke(io, new object[] { dst, Enum.Parse(contentsT, "All") });

        // 4. read back, compare, solve
        // A .ghz is a versioned container: read it the way the editor does, through DocumentIO.Open.
        var docT = gh2.GetType("Grasshopper2.Doc.Document", true);
        object back = docT.GetMethod("NewInactiveDocument", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
        dynamic reader = Activator.CreateInstance(ioT, new object[] { back, false, false, false, false });
        var open = ioT.GetMethods().First(m => m.Name == "Open" && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(string));
        var blocking = Array.CreateInstance(docT, 0);
        if (!(bool)open.Invoke(reader, new object[] { dst, blocking, null })) throw new Exception("GH2 could not open the saved .ghz");
        back = reader.Document;
        var c = Census(back); int backWires = Wires(back);
        r["readBack"] = new { c.objects, c.swiftlet, c.scribbles, c.interop, wires = backWires };
        r["roundTrip"] = c.objects == converted.objects && c.swiftlet == converted.swiftlet && c.scribbles == converted.scribbles && backWires == wires && c.interop == 0;
        if (solve) {
            dynamic canvas = ((dynamic)gh2.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null)).Canvas;
            object previous = canvas.Document;
            canvas.Document = (dynamic)back;
            try {
                var started = DateTime.Now;
                ((dynamic)back).Solution.StartWait();
                r["solveSeconds"] = Math.Round((DateTime.Now - started).TotalSeconds, 1);
                var errors = new List<string>(); var warnings = new List<string>(); int faults = 0;
                foreach (dynamic o in ((dynamic)back).Objects.Forwards) {
                    string name; try { name = (string)o.Nomen.Name; } catch { name = ((object)o).GetType().Name; }
                    dynamic state = null; try { state = o.State; } catch { }
                    if (state == null) continue;
                    try { if (state.FaultException != null) { faults++; errors.Add(name + ": fault " + state.FaultException.Message); } } catch { }
                    dynamic msgs = null; try { msgs = state.Data?.Messages; } catch { }
                    if (msgs == null) continue;
                    for (int k = 0; k < (int)msgs.Count; k++) { string lvl = msgs[k].Level.ToString(); string m = name + ": " + (string)msgs[k].Text + " " + (string)msgs[k].Info;
                        if (lvl == "Error") errors.Add(m); else if (lvl == "Warning") warnings.Add(m); }
                }
                // Prove the solve really ran: Swiftlet outputs that hold data afterwards.
                int outputs = 0, withData = 0;
                foreach (dynamic o in ((dynamic)back).Objects.Forwards) {
                    if (((object)o).GetType().Assembly.GetName().Name != "Swiftlet.Gh2") continue;
                    dynamic ps; try { ps = o.Parameters; } catch { continue; }
                    for (int k = 0; k < (int)ps.OutputCount; k++) { outputs++; try { dynamic tree = ps.Output(k).State.Data?.Tree(); if (tree != null && (int)tree.LeafCount > 0) withData++; } catch { } }
                }
                r["solve"] = new { phase = ((object)((dynamic)back).Solution.State).ToString(), faults, errors, warnings, outputs, withData };
            } finally {
                canvas.Document = (dynamic)previous;
                try { ((dynamic)back).Objects.Clear(); } catch { }  // stop listeners/servers the example started
            }
        }
    }
}
catch (Exception ex) { var x = ex is TargetInvocationException ti && ti.InnerException != null ? ti.InnerException : ex; r["error"] = x.GetType().Name + ": " + x.Message; }
Console.WriteLine("SWIFTLET_EXAMPLE:" + JsonSerializer.Serialize(r));
'''


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    ap.add_argument("--dry-run", action="store_true", help="convert and count, do not write .ghz files")
    ap.add_argument("--only")
    ap.add_argument("--report")
    a = ap.parse_args()
    results, bad = [], 0
    for rel, mode in EXAMPLES.items():
        if a.only and a.only not in rel:
            continue
        src = REPO / "Examples" / rel
        dst = src.with_suffix(".ghz")
        script = (SCRIPT.replace("__SRC__", str(src)).replace("__DST__", str(dst))
                  .replace("__SOLVE__", "true" if mode == "solve" else "false").replace("__DRY__", "true" if a.dry_run else "false"))
        try:
            out = run_csharp(a.port, script, timeout=600)
            line = next(l for l in out.splitlines() if l.startswith("SWIFTLET_EXAMPLE:"))
            r = json.loads(line[len("SWIFTLET_EXAMPLE:"):])
        except Exception as ex:
            r = {"file": str(src), "error": f"{type(ex).__name__}: {ex}"}
        results.append(r)
        ok = "error" not in r and (a.dry_run or r.get("roundTrip"))
        solve = r.get("solve")
        if solve and solve["faults"]:
            ok = False
        bad += not ok
        conv = r.get("converted", {})
        print(("PASS " if ok else "FAIL ") + rel + f": {conv.get('swiftlet')} Swiftlet, {conv.get('scribbles')}/{conv.get('gh1Scribbles')} scribbles, {conv.get('wires')} wires"
              + (f"; read back {r['readBack']}" if "readBack" in r and not r.get("roundTrip") else "")
              + (f"; solved ({solve['phase']}) in {r.get('solveSeconds')} s, {solve['withData']}/{solve['outputs']} Swiftlet outputs with data, {len(solve['errors'])} error(s), {len(solve['warnings'])} warning(s)" if solve else ("; not solved (needs API key)" if mode != "solve" else ""))
              + (f"; ERROR {r['error']}" if "error" in r else ""))
        for m in (solve or {}).get("errors", [])[:6]:
            print("      error:", m[:180])
    for rel, why in SKIPPED.items():
        print(f"SKIP {rel}: {why}")
    if a.report:
        json.dump(results, open(a.report, "w", encoding="utf-8"), indent=2)
    print(f"\n{len(results) - bad}/{len(results)} examples written and verified")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
