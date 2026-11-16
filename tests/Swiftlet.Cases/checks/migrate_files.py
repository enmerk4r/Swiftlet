"""Whole-file GH1 -> GH2 migration report for saved definitions (release gate).

For every .gh/.ghx file given (default: everything under Examples/), inside a slot with GH1 and GH2
Swiftlet loaded (cases.py --load):
  1. open it with GH1's GH_DocumentIO (no solve, nothing is sent anywhere);
  2. convert it with GH2's DocumentIO.Convert1(IGH_Document, MigrationLevel.Total), the same conversion
     the GH2 editor runs when a user opens a GH1 file;
  3. classify every resulting object: native Swiftlet GH2, other native GH2 (standard components,
     panels, sliders, relays...), or a GH1 interop host; and compare wire counts.
A file passes when no Swiftlet object is left as GH1 interop, every GH1 Swiftlet object has a native
counterpart, and no wire into a Swiftlet object is lost. Wires into other objects, objects from plugins
that are not installed (skipped before conversion; GH1 hangs on them) and objects GH1 itself cannot read
are reported but belong to the host.

    python tests/Swiftlet.Cases/checks/migrate_files.py --port N [files...] [--report out.json]
"""
import argparse, json, pathlib, sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from cases import REPO, run_csharp  # noqa: E402

SCRIPT = r'''
using System; using System.Collections; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Text.Json;
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == n);
var gh1 = A("Grasshopper"); var gh2 = A("Grasshopper2"); var sw1 = A("Swiftlet.Gh.Rhino8");
var levelT = gh2.GetType("Grasshopper2.Doc.Migration.MigrationLevel", true);
var convert = gh2.GetType("Grasshopper2.Doc.DocumentIO", true).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).First(m => m.Name == "Convert1");
var wrapT = gh2.GetType("Grasshopper2.Interop.IGH_Document", true);
var paramT = gh2.GetType("Grasshopper2.Parameters.IParameter", true);
// IParameter.Inputs is a Grasshopper2.Parameters.Connections (has Count, is not enumerable).
int Count(object o) => o is null ? 0 : (int)(o.GetType().GetProperty("Count")?.GetValue(o) ?? 0);
// Wires into an object: component inputs, a floating parameter's own inputs, or a wrapped parameter
// (panels and similar objects expose theirs through a property of type IParameter).
int WiresInto(object o)
{
    var parameters = o.GetType().GetProperty("Parameters")?.GetValue(o);
    if (parameters != null)
    {
        int n = 0, count = (int)parameters.GetType().GetProperty("InputCount")!.GetValue(parameters)!;
        var input = parameters.GetType().GetMethod("Input", new[] { typeof(int) })!;
        for (int i = 0; i < count; i++) { var p = input.Invoke(parameters, new object[] { i }); n += Count(p?.GetType().GetProperty("Inputs")?.GetValue(p)); }
        return n;
    }
    if (paramT.IsInstanceOfType(o)) return Count(o.GetType().GetProperty("Inputs")?.GetValue(o));
    foreach (var prop in o.GetType().GetProperties().Where(p => paramT.IsAssignableFrom(p.PropertyType) && p.GetIndexParameters().Length == 0))
    {
        var p = prop.GetValue(o);
        if (p != null) return Count(p.GetType().GetProperty("Inputs")?.GetValue(p));
    }
    return 0;
}
var results = new List<object>();
foreach (var path in JsonSerializer.Deserialize<string[]>(@"__FILES__"))
{
    var r = new Dictionary<string, object> { ["file"] = path };
    try
    {
        // GH_Archive reads without UI (GH_DocumentIO.Open can raise modal dialogs, e.g. for missing plugins).
        var ghio = A("GH_IO");
        dynamic archive = Activator.CreateInstance(ghio.GetType("GH_IO.Serialization.GH_Archive", true));
        if (!(bool)archive.ReadFromFile(path)) throw new Exception("GH1 could not read the file");
        // Objects from GH1 plugins that are not installed make ExtractObject wait on a hidden prompt.
        // Drop them first and report them (they would stay unrecognised in GH1 and GH2 alike).
        dynamic defObjects = archive.GetRootNode.FindChunk("Definition").FindChunk("DefinitionObjects");
        var loadedLibs = new HashSet<Guid> { Guid.Empty };
        dynamic server = gh1.GetType("Grasshopper.Instances").GetProperty("ComponentServer").GetValue(null);
        foreach (dynamic lib in server.Libraries) { try { loadedLibs.Add((Guid)lib.Id); } catch { } }
        var missing = ((IEnumerable)defObjects.Chunks).Cast<dynamic>()
            .Where(c => (bool)c.ItemExists("Lib") && !loadedLibs.Contains((Guid)c.GetGuid("Lib"))).ToList();
        foreach (var c in missing) defObjects.RemoveChunk(c);
        int chunkCount = ((IEnumerable)defObjects.Chunks).Cast<object>().Count();
        defObjects.RemoveItem("ObjectCount"); defObjects.SetInt32("ObjectCount", chunkCount);
        r["missingPluginObjects"] = missing.Select(c => (string)c.GetString("Name")).ToList();
        dynamic doc1 = Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document", true));
        if (!(bool)archive.ExtractObject(doc1, "Definition")) throw new Exception("GH1 could not extract the definition");
        r["unreadableObjects"] = chunkCount - (int)doc1.ObjectCount;
        var gh1Objects = new List<object>(); int gh1Wires = 0, gh1SwiftletWires = 0;
        foreach (dynamic o in doc1.Objects)
        {
            var t = ((object)o).GetType();
            gh1Objects.Add(new { type = t.Name, swiftlet = t.Assembly == sw1, name = (string)o.Name });
            int w = 0;
            try { foreach (dynamic p in o.Params.Input) w += (int)p.SourceCount; } catch { }
            try { w += (int)o.SourceCount; } catch { }
            gh1Wires += w; if (t.Assembly == sw1) gh1SwiftletWires += w;
        }
        r["gh1Objects"] = gh1Objects; r["gh1Wires"] = gh1Wires; r["gh1SwiftletWires"] = gh1SwiftletWires;

        object wrapped = Activator.CreateInstance(wrapT, new object[] { (object)doc1 });
        object owner = convert.IsStatic ? null : Activator.CreateInstance(convert.DeclaringType, true);
        dynamic doc2 = convert.Invoke(owner, new[] { wrapped, Enum.Parse(levelT, "Total") });
        var gh2Objects = new List<object>(); int gh2Wires = 0, gh2SwiftletWires = 0;
        foreach (dynamic o in doc2.Objects.Forwards)
        {
            var t = ((object)o).GetType();
            string kind = t.Assembly.GetName().Name == "Swiftlet.Gh2" ? "swiftlet" : t.Name.StartsWith("GH1Interop") ? "interop" : "native";
            string gh1Name = null;
            if (kind == "interop") { try { gh1Name = (string)o.Grasshopper1Name; } catch { } }
            gh2Objects.Add(new { type = t.Name, kind, gh1Name, name = (string)o.Nomen.Name });
            int w2 = WiresInto((object)o);
            gh2Wires += w2; if (kind == "swiftlet") gh2SwiftletWires += w2;
        }
        r["gh2Objects"] = gh2Objects; r["gh2Wires"] = gh2Wires; r["gh2SwiftletWires"] = gh2SwiftletWires;
    }
    catch (Exception ex) { var x = ex is TargetInvocationException ti && ti.InnerException != null ? ti.InnerException : ex; r["error"] = x.GetType().Name + ": " + x.Message; }
    results.Add(r);
}
Console.WriteLine("SWIFTLET_FILES:" + JsonSerializer.Serialize(results));
'''


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    ap.add_argument("--report")
    ap.add_argument("files", nargs="*")
    a = ap.parse_args()
    files = [pathlib.Path(f).resolve() for f in a.files] or sorted(
        p for p in (REPO / "Examples").rglob("*") if p.suffix.lower() in (".gh", ".ghx"))
    results = []
    for f in files:  # one call per file, so a problem file cannot take the others down
        print("...", f.relative_to(REPO).as_posix() if f.is_relative_to(REPO) else f, flush=True)
        try:
            out = run_csharp(a.port, SCRIPT.replace("__FILES__", json.dumps([str(f)]).replace('"', '""')), timeout=900)
            line = next((l for l in out.splitlines() if l.startswith("SWIFTLET_FILES:")), None)
            results += json.loads(line[len("SWIFTLET_FILES:"):]) if line else [{"file": str(f), "error": "no result: " + out[:300]}]
        except Exception as ex:  # transport failure: the slot is probably unusable, stop here
            results.append({"file": str(f), "error": f"{type(ex).__name__}: {ex}"})
            print("transport failure on", f, "- stopping")
            break
    failures = 0
    for r in results:
        name = pathlib.Path(r["file"]).relative_to(REPO).as_posix() if pathlib.Path(r["file"]).is_relative_to(REPO) else r["file"]
        if "error" in r:
            failures += 1
            print(f"FAIL {name}: {r['error']}")
            continue
        sw1 = sum(o["swiftlet"] for o in r["gh1Objects"])
        sw2 = sum(o["kind"] == "swiftlet" for o in r["gh2Objects"])
        interop = [o for o in r["gh2Objects"] if o["kind"] == "interop"]
        sw_interop = [o for o in interop if any(g["swiftlet"] and g["name"] == o["gh1Name"] for g in r["gh1Objects"])]
        other_interop = [o for o in interop if o not in sw_interop]
        problems = []
        if sw_interop:
            problems.append("Swiftlet left as GH1 interop: " + ", ".join(sorted({o["gh1Name"] or o["type"] for o in sw_interop})))
        if sw2 < sw1 - len(sw_interop):
            problems.append(f"{sw1} GH1 Swiftlet objects but only {sw2} native GH2 Swiftlet objects")
        if r["gh2SwiftletWires"] < r["gh1SwiftletWires"]:
            problems.append(f"wires into Swiftlet objects {r['gh1SwiftletWires']} in GH1, {r['gh2SwiftletWires']} in GH2")
        other_lost = (r["gh1Wires"] - r["gh1SwiftletWires"]) - (r["gh2Wires"] - r["gh2SwiftletWires"])
        failures += bool(problems)
        print(("FAIL " if problems else "PASS ") + f"{name}: GH1 {len(r['gh1Objects'])} objects ({sw1} Swiftlet), "
              f"GH2 {len(r['gh2Objects'])} objects ({sw2} Swiftlet native, {len(interop)} interop), "
              f"wires {r['gh1Wires']} -> {r['gh2Wires']} (into Swiftlet {r['gh1SwiftletWires']} -> {r['gh2SwiftletWires']})"
              + (f"; {other_lost} wire(s) into non-Swiftlet objects not kept by GH2" if other_lost > 0 else "")
              + (f"; non-Swiftlet interop: {', '.join(sorted({o['gh1Name'] or o['type'] for o in other_interop}))}" if other_interop else "")
              + (f"; skipped (plugin not installed): {', '.join(sorted(set(r['missingPluginObjects'])))}" if r.get("missingPluginObjects") else "")
              + (f"; {r['unreadableObjects']} object(s) GH1 itself could not read" if r.get("unreadableObjects") else ""))
        for p in problems:
            print("     -", p)
    if a.report:
        json.dump(results, open(a.report, "w", encoding="utf-8"), indent=2)
    print(f"\n{len(results) - failures}/{len(results)} files migrate cleanly")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
