"""Build a run_csharp payload that dumps every GH1 Swiftlet component/param at runtime.

Usage: python gh1_catalog_script.py <path to built Swiftlet.Gh.Rhino8.dll (copy it out of bin first)> <out.json>
Then send <out.json>.args.json to the Rhino MCP run_csharp tool; the dump is written to <out.json>.
"""
dll, out = sys.argv[1], sys.argv[2]
script = r'''
using System; using System.IO; using System.Linq; using System.Reflection; using System.Collections.Generic; using System.Text.Json;
var log = new List<string>();
if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Grasshopper")) {
  Rhino.PlugIns.PlugIn.LoadPlugIn(new Guid("b45a29b1-4343-4035-989e-044e8580d9cf"));
}
var gh = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Grasshopper");
var docObj = gh.GetType("Grasshopper.Kernel.GH_DocumentObject", true);
var compT = gh.GetType("Grasshopper.Kernel.GH_Component", true);
var paramIface = gh.GetType("Grasshopper.Kernel.IGH_Param", true);
var varIface = gh.GetType("Grasshopper.Kernel.IGH_VariableParameterComponent", true);
var asm = Assembly.LoadFrom(@"%DLL%");
Type[] types; try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); foreach (var le in e.LoaderExceptions) log.Add("LOADER " + le.Message); }
object P(object o, string n) { try { return o.GetType().GetProperty(n, BindingFlags.Public|BindingFlags.Instance)?.GetValue(o); } catch (Exception e) { return "ERR:" + (e.InnerException ?? e).Message; } }
string S(object o) => o?.ToString();
Dictionary<string, object> Param(object p) {
  var d = new Dictionary<string, object> {
    ["name"] = S(P(p,"Name")), ["nickname"] = S(P(p,"NickName")), ["description"] = S(P(p,"Description")),
    ["access"] = S(P(p,"Access")), ["optional"] = P(p,"Optional"), ["typeName"] = S(P(p,"TypeName")),
    ["paramClass"] = p.GetType().FullName, ["dataMapping"] = S(P(p,"DataMapping")),
  };
  try {
    var pd = p.GetType().GetProperty("PersistentData")?.GetValue(p);
    if (pd != null) {
      var cnt = (int)pd.GetType().GetProperty("DataCount").GetValue(pd);
      if (cnt > 0) {
        var all = (System.Collections.IEnumerable)pd.GetType().GetProperty("AllData", new Type[]{typeof(bool)})?.GetValue(pd, new object[]{true});
        var vals = new List<string>(); if (all != null) foreach (var v in all) vals.Add(S(v));
        if (all == null) { var m = pd.GetType().GetMethod("AllData"); foreach (var v in (System.Collections.IEnumerable)m.Invoke(pd, new object[]{true})) vals.Add(S(v)); }
        d["default"] = vals;
      }
    }
  } catch (Exception e) { d["defaultError"] = (e.InnerException ?? e).Message; }
  return d;
}
var result = new List<object>();
foreach (var t in types.Where(t => t.IsClass && !t.IsAbstract && docObj.IsAssignableFrom(t)).OrderBy(t => t.FullName)) {
  var e = new Dictionary<string, object> { ["class"] = t.Name, ["fullName"] = t.FullName, ["baseClass"] = t.BaseType?.Name };
  try {
    var ctor = t.GetConstructor(Type.EmptyTypes); if (ctor == null) { e["error"] = "no parameterless ctor"; result.Add(e); continue; }
    var o = ctor.Invoke(null);
    e["kind"] = compT.IsAssignableFrom(t) ? "component" : (paramIface.IsAssignableFrom(t) ? "param" : "other");
    e["guid"] = S(P(o,"ComponentGuid")); e["name"] = S(P(o,"Name")); e["nickname"] = S(P(o,"NickName"));
    e["description"] = S(P(o,"Description")); e["category"] = S(P(o,"Category")); e["subcategory"] = S(P(o,"SubCategory"));
    e["exposure"] = S(P(o,"Exposure")); e["obsolete"] = P(o,"Obsolete");
    e["variableParameters"] = varIface.IsAssignableFrom(t);
    if (e["kind"].Equals("component")) {
      var prm = P(o,"Params");
      e["inputs"] = ((System.Collections.IEnumerable)P(prm,"Input")).Cast<object>().Select(Param).ToList();
      e["outputs"] = ((System.Collections.IEnumerable)P(prm,"Output")).Cast<object>().Select(Param).ToList();
    } else if (e["kind"].Equals("param")) {
      e["typeName"] = S(P(o,"TypeName")); e["access"] = S(P(o,"Access"));
    }
  } catch (Exception ex) { e["error"] = (ex.InnerException ?? ex).GetType().Name + ": " + (ex.InnerException ?? ex).Message; }
  result.Add(e);
}
File.WriteAllText(@"%OUT%", JsonSerializer.Serialize(new { log, objects = result }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("objects " + result.Count + " errors " + result.Count(r => ((Dictionary<string,object>)r).ContainsKey("error")) + " loaderLog " + log.Count);
'''
json.dump({"script": script.replace("%DLL%", dll).replace("%OUT%", out)}, open(out + ".args.json", "w"))
