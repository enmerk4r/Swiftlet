"""Live check that GH2 converts between the JSON parameter types when they are actually wired.

Builds three small chains on the GH2 canvas with g2_apply_graph (real wires, real solve):
  Create JSON Object  --JsonObjectData--> Read JSON Object   (object output -> token input)
  Create JSON Value   --JsonValueData---> Stringify JSON Token (value output -> token input, JSON null)
  Parse JSON String   --JsonToken-------> Get JSON Object Key (token output -> object input)
sets the literal inputs by script, solves, asserts the typed outputs, and deletes what it created.

    python tests/Swiftlet.Cases/checks/json_chain.py --port 10502      (after cases.py --load)
"""
import argparse, json, pathlib, sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
from cases import call, run_csharp  # noqa: E402

IDS = {  # GH2 IoIds (src/Swiftlet.Gh2/Ids)
    "CreateJsonObject": "812cf656-b567-4ca6-8d20-5505f6515b5f",
    "ReadJsonObject": "f17d398a-e127-49e3-be13-5677e712ea44",
    "CreateJsonValue": "8a3f63de-4907-4086-930f-71d758c42584",
    "StringifyJsonToken": "f24c685d-c3ad-492d-8b2c-5e7cf14c5767",
    "ParseJsonString": "680fd893-664b-425a-8450-9346160e52f0",
    "GetJsonObjectKey": "49343eee-d7f1-48b8-b225-085d78d7f0cc",
}

SCRIPT = r'''
using System; using System.Collections; using System.Linq; using System.Reflection;
var gh2 = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Grasshopper2");
var core = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Swiftlet.Core");
dynamic doc = ((dynamic)gh2.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null)).Canvas.Document;
dynamic Find(string id) { foreach (dynamic o in doc.Objects.Forwards) if (o.InstanceId.ToString() == id) return o; throw new Exception("missing " + id); }
void Set(dynamic p, Array values) {
  var set = ((object)p).GetType().GetMethods().Single(m => m.Name == "Set" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == values.GetType());
  set.Invoke(p, new object[] { values }); p.Expire(); }
string Dump(dynamic comp, int output) {
  dynamic tree = comp.Parameters.Output(output).State.Data?.Tree();
  if (tree == null) return "<no data>";
  var items = ((IEnumerable)tree.AllTwigs).Cast<dynamic>().SelectMany(t => Enumerable.Range(0, (int)t.LeafCount).Select(i => t.NullAt(i) ? "<null>" : Describe(t.ItemAt(i))));
  return "[" + string.Join(", ", items) + "]"; }
string Describe(object o) => o.GetType().Namespace == "Swiftlet.Core.Json" ? o.GetType().Name + ":" + o.GetType().GetProperty("Json").GetValue(o) : o.GetType().Name + ":" + o;
var token = core.GetType("Swiftlet.Core.Json.JsonToken", true);
var parse = token.GetMethod("Parse", new[] { typeof(string) });
if ("__MODE__" == "set") {
  dynamic createObject = Find("__CreateJsonObject__");
  Set(createObject.Parameters.Input(0), new[] { "a", "b" });
  var vals = Array.CreateInstance(token, 2); vals.SetValue(parse.Invoke(null, new object[] { "1" }), 0); vals.SetValue(parse.Invoke(null, new object[] { "[true]" }), 1);
  Set(createObject.Parameters.Input(1), vals);
  dynamic parseString = Find("__ParseJsonString__");
  Set(parseString.Parameters.Input(0), new[] { "{\"k\":42}" });
  dynamic getKey = Find("__GetJsonObjectKey__");
  Set(getKey.Parameters.Input(1), new[] { "k" });
  createObject.Expire(); parseString.Expire(); getKey.Expire();
  Console.WriteLine("inputs set");
} else if ("__MODE__" == "read") {
  Console.WriteLine("ReadJsonObject.Keys=" + Dump(Find("__ReadJsonObject__"), 0));
  Console.WriteLine("ReadJsonObject.JTokens=" + Dump(Find("__ReadJsonObject__"), 1));
  Console.WriteLine("Stringify.Text=" + Dump(Find("__StringifyJsonToken__"), 0));
  Console.WriteLine("GetJsonObjectKey.JToken=" + Dump(Find("__GetJsonObjectKey__"), 0));
} else {
  foreach (var id in "__ALL__".Split(',')) doc.Objects.Remove(Find(id));
  Console.WriteLine("removed");
}
'''

EXPECT = {
    "ReadJsonObject.Keys": "[String:a, String:b]",
    "ReadJsonObject.JTokens": "[JsonToken:1, JsonToken:[true]]",
    "Stringify.Text": "[<null>]",
    "GetJsonObjectKey.JToken": "[JsonToken:42]",
}


def script(mode, keys):
    s = SCRIPT.replace("__MODE__", mode).replace("__ALL__", ",".join(keys.values()))
    for k, v in keys.items():
        s = s.replace(f"__{k}__", v)
    return s


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    a = ap.parse_args()
    comps = [{"Key": k, "Selector": v, "X": 100 + 250 * (i % 2), "Y": 100 + 120 * (i // 2)} for i, (k, v) in enumerate(IDS.items())]
    wires = [
        {"SrcKey": "CreateJsonObject", "Src": "0", "DstKey": "ReadJsonObject", "Dst": "0"},
        {"SrcKey": "CreateJsonValue", "Src": "0", "DstKey": "StringifyJsonToken", "Dst": "0"},
        {"SrcKey": "ParseJsonString", "Src": "0", "DstKey": "GetJsonObjectKey", "Dst": "0"},
    ]
    result, text = call(a.port, "g2_apply_graph", {"sliders": [], "components": comps, "wires": wires, "solve": False})
    graph = json.loads(text)
    keys = {p["key"]: p["id"] for p in graph.get("placed", [])}
    if len(keys) != len(IDS) or graph.get("wiresOk") != len(wires):
        if keys:
            run_csharp(a.port, script("remove", keys))
        sys.exit("placement/wiring failed: " + text[:2000])
    print(f"placed {len(keys)} components, {graph['wiresOk']} wires")
    try:
        print(run_csharp(a.port, script("set", keys)))
        _, solved = call(a.port, "g2_solve_canvas", {})
        print("solve", solved[:600])
        out = run_csharp(a.port, script("read", keys))
        print(out)
        got = dict(line.split("=", 1) for line in out.strip().splitlines() if "=" in line)
        bad = [f"{k}: expected {v}, got {got.get(k)}" for k, v in EXPECT.items() if got.get(k, "").strip() != v]
        for b in bad:
            print("FAIL", b)
        print("ALL PASS" if not bad else f"{len(bad)} FAILURES")
        return 1 if bad else 0
    finally:
        print(run_csharp(a.port, script("remove", keys)))


if __name__ == "__main__":
    sys.exit(main())
