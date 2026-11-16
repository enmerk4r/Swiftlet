"""Live check that GH1 Create Text Body keeps its menu choice when typed content is plugged in.

Sets the menu to Text, then solves with plain text, a JSON object, and plain text again.
The JSON object must go out as application/json; plain text must go out as text/plain both
times, and the saved "IsTextChecked" flag must survive.

    python tests/Swiftlet.Cases/checks/gh1_text_body_setting.py --port 10500   (after cases.py --load)
"""
import argparse, pathlib, sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
from cases import run_csharp  # noqa: E402

SCRIPT = r'''
using System; using System.Linq; using System.Reflection;
using Grasshopper.Kernel; using Grasshopper.Kernel.Types; using Grasshopper.Kernel.Parameters;
var shell = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Swiftlet.Gh.Rhino8");
var type = shell.GetType("Swiftlet.Gh.Rhino8.Components.CreateTextBodyComponent", true);
var comp = (GH_Component)Activator.CreateInstance(type);
var doc = new GH_Document { Enabled = true };
comp.CreateAttributes();
doc.AddObject(comp, false);
var source = new Param_GenericObject();
source.CreateAttributes();
doc.AddObject(source, false);
comp.Params.Input[0].AddSource(source);
var flags = BindingFlags.Instance | BindingFlags.NonPublic;
type.GetMethod("MenuTextClick", flags).Invoke(comp, new object[] { null, EventArgs.Empty });
string Solve(IGH_Goo goo) {
  source.PersistentData.Clear(); source.PersistentData.Append(goo);
  source.ExpireSolution(false); doc.NewSolution(false, GH_SolutionMode.Silent);
  if (comp.Params.Output[0].VolatileData.DataCount == 0)
    throw new Exception("no output; phase=" + comp.Phase + " src=" + source.VolatileData.DataCount + " enabled=" + doc.Enabled + " msgs=" + string.Join(";", comp.RuntimeMessages(GH_RuntimeMessageLevel.Error).Concat(comp.RuntimeMessages(GH_RuntimeMessageLevel.Warning))));
  dynamic body = comp.Params.Output[0].VolatileData.AllData(true).Cast<dynamic>().Single().Value;
  return (string)body.ContentType + "|" + comp.Message + "|" + (bool)type.GetField("_isTextChecked", flags).GetValue(comp);
}
var jsonGoo = (IGH_Goo)Activator.CreateInstance(shell.GetType("Swiftlet.Gh.Rhino8.Goo.JsonObjectGoo", true));
Console.WriteLine("text1=" + Solve(new GH_String("hi")));
Console.WriteLine("json=" + Solve(jsonGoo));
Console.WriteLine("text2=" + Solve(new GH_String("hi")));
doc.Dispose();
'''

EXPECT = {
    "text1": "text/plain|Text|True",
    "json": "application/json|JSON|True",
    "text2": "text/plain|Text|True",
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    a = ap.parse_args()
    out = run_csharp(a.port, SCRIPT)
    text = out if isinstance(out, str) else str(out)
    got = dict(line.split("=", 1) for line in text.replace("\\r\\n", "\n").replace("\\n", "\n").splitlines() if "=" in line and line.split("=", 1)[0] in EXPECT)
    failed = False
    for key, want in EXPECT.items():
        ok = got.get(key, "").strip().strip('"') == want
        failed |= not ok
        print(("PASS " if ok else "FAIL ") + key + ": " + got.get(key, "<missing>") + ("" if ok else "  (expected " + want + ")"))
    if failed:
        print(text[:3000])
        sys.exit(1)


if __name__ == "__main__":
    main()
