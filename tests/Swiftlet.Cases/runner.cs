// GH1/GH2 case runner, executed inside Rhino through the Rhino AI `run_csharp` tool.
// cases.py substitutes __CASES_JSON__ with a JSON array of cases and parses the
// SWIFTLET_CASES_JSON line this script prints. Everything goes through reflection on the
// already-loaded assemblies, so the script needs no #r references.
//
// Per case and host, the component is added to that host's active document, literal inputs are
// supplied (GH1: a generic persistent source param wired into the input; GH2: persistent data set
// on the input parameter), the document is solved synchronously, outputs and messages are read
// and canonicalised, and every object the case created is removed again.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

Assembly Asm(string name) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name)
    ?? throw new InvalidOperationException("Assembly not loaded: " + name);

var gh1 = Asm("Grasshopper");
var gh2 = Asm("Grasshopper2");
var sw1 = Asm("Swiftlet.Gh.Rhino8");
var sw2 = Asm("Swiftlet.Gh2");
var core = Asm("Swiftlet.Core");
var gio = Asm("GrasshopperIO");

var cases = JsonNode.Parse(@"__CASES_JSON__")!.AsArray();
var results = new List<object>();

// ---------------------------------------------------------------- value construction
// A value spec is a JSON object with exactly one key naming its kind.
object CoreValue(JsonObject spec)
{
    var (kind, v) = spec.Single();
    switch (kind)
    {
        case "text": return v is null ? null : (string)v;
        case "int": return (int)v!;
        case "num": return (double)v!;
        case "bool": return (bool)v!;
        case "query": return Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.QueryParameter", true), (string)v![0], (string)v[1]);
        case "header": return Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.HttpHeader", true), (string)v![0], (string)v[1]);
        case "bytes": return Convert.FromBase64String((string)v!);
        case "json": case "jobject": case "jarray": case "jvalue":
            return JsonForHost(kind, (string)v!, 2);
        default: throw new NotSupportedException("value kind " + kind);
    }
}

// JSON: GH1 Goo wrap System.Text.Json nodes; GH2 uses the immutable Swiftlet.Core.Json wrappers.
object JsonForHost(string kind, string text, int host)
{
    if (host == 1) return JsonNode.Parse(text);
    string typeName = kind switch { "jobject" => "JsonObjectData", "jarray" => "JsonArrayData", "jvalue" => "JsonValueData", _ => "JsonToken" };
    var t = core.GetType("Swiftlet.Core.Json." + typeName, true);
    var parse = t.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
        ?? throw new MissingMethodException(t.Name + ".Parse(string)");
    return parse.Invoke(null, new object[] { text });
}

object Gh1Goo(JsonObject spec)
{
    var (kind, v) = spec.Single();
    switch (kind)
    {
        case "text": return Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_String", true), (string)v!);
        case "int": return Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_Integer", true), (int)v!);
        case "num": return Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_Number", true), (double)v!);
        case "bool": return Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_Boolean", true), (bool)v!);
        case "query": return Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Goo.QueryParameterGoo", true), CoreValue(spec));
        case "header": return Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Goo.HttpHeaderGoo", true), CoreValue(spec));
        case "bytes": return Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Goo.ByteArrayGoo", true), CoreValue(spec));
        case "json": case "jobject": case "jarray": case "jvalue":
            string goo = kind switch { "jobject" => "JsonObjectGoo", "jarray" => "JsonArrayGoo", "jvalue" => "JsonValueGoo", _ => "JsonNodeGoo" };
            return Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Goo." + goo, true), JsonForHost(kind, (string)v!, 1));
        default: throw new NotSupportedException("value kind " + kind);
    }
}

// ---------------------------------------------------------------- canonical output values
object Canon(object o, int depth = 0)
{
    if (o is null) return null;
    var t = o.GetType();
    // GH1 goo -> underlying value
    if (t.GetInterface("Grasshopper.Kernel.Types.IGH_Goo") != null)
    {
        var sv = t.GetMethod("ScriptVariable")?.Invoke(o, null);
        if (sv != null && !ReferenceEquals(sv, o)) return Canon(sv, depth);
        var val = t.GetProperty("Value")?.GetValue(o);
        return Canon(val, depth);
    }
    switch (o)
    {
        case string s: return new Dictionary<string, object> { ["text"] = s };
        case bool b: return new Dictionary<string, object> { ["bool"] = b };
        case int i: return new Dictionary<string, object> { ["int"] = i };
        case long l: return new Dictionary<string, object> { ["int"] = l };
        case double d: return new Dictionary<string, object> { ["num"] = d };
        case byte[] bytes: return new Dictionary<string, object> { ["bytes"] = Convert.ToBase64String(bytes) };
        case JsonNode node: return new Dictionary<string, object> { ["json"] = node.ToJsonString() };
    }
    if (t.Namespace == "Swiftlet.Core.Json")
    {
        // Immutable wrappers: compare by their compact JSON text (JSON null -> "null").
        var toNode = t.GetMethod("ToNode", Type.EmptyTypes);
        var node = toNode?.Invoke(o, null) as JsonNode;
        return new Dictionary<string, object> { ["json"] = node?.ToJsonString() ?? "null" };
    }
    if (depth > 3) return new Dictionary<string, object> { ["opaque"] = t.FullName };
    // Plain domain objects (Core types): public readable properties, recursively.
    var props = new SortedDictionary<string, object>(StringComparer.Ordinal);
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0))
    {
        object v;
        try { v = p.GetValue(o); } catch (Exception e) { v = "ERR " + (e.InnerException ?? e).GetType().Name; }
        props[p.Name] = v is IEnumerable en && v is not string && v is not byte[]
            ? en.Cast<object>().Select(x => Canon(x, depth + 1)).ToList()
            : Canon(v, depth + 1);
    }
    return new Dictionary<string, object> { ["type"] = t.Name, ["props"] = props };
}

// ---------------------------------------------------------------- GH1
dynamic Gh1Doc()
{
    var canvas = gh1.GetType("Grasshopper.Instances", true).GetProperty("ActiveCanvas")!.GetValue(null);
    dynamic doc = canvas != null ? ((dynamic)canvas).Document : null;
    if (doc == null)
    {
        doc = Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document", true));
        gh1.GetType("Grasshopper.Instances", true).GetProperty("DocumentServer")!.GetValue(null)!
            .GetType().GetMethod("AddDocument")!.Invoke(gh1.GetType("Grasshopper.Instances").GetProperty("DocumentServer")!.GetValue(null), new object[] { doc });
    }
    return doc;
}

Dictionary<string, object> RunGh1(JsonObject c)
{
    dynamic doc = Gh1Doc();
    var created = new List<object>();
    var compType = sw1.GetType("Swiftlet.Gh.Rhino8.Components." + (string)c["gh1"]!, true);
    dynamic comp = Activator.CreateInstance(compType);
    comp.CreateAttributes();
    doc.AddObject(comp, false, int.MaxValue);
    created.Add(comp);
    try
    {
        var inputs = c["inputs"]!.AsArray();
        var genericT = gh1.GetType("Grasshopper.Kernel.Parameters.Param_GenericObject", true);
        for (int i = 0; i < inputs.Count; i++)
        {
            var spec = inputs[i];
            if (spec is null) continue; // missing input
            dynamic src = Activator.CreateInstance(genericT);
            src.CreateAttributes();
            doc.AddObject(src, false, int.MaxValue);
            created.Add(src);
            var items = spec is JsonArray arr ? arr.Select(x => x!.AsObject()).ToList() : new List<JsonObject> { spec.AsObject() };
            object pd = src.PersistentData;
            var append = pd.GetType().GetMethod("Append", new[] { gh1.GetType("Grasshopper.Kernel.Types.IGH_Goo", true) });
            foreach (var item in items) append!.Invoke(pd, new[] { Gh1Goo(item) });
            comp.Params.Input[i].AddSource(src);
        }
        comp.ExpireSolution(false);
        var modeT = gh1.GetType("Grasshopper.Kernel.GH_SolutionMode", true);
        ((object)doc).GetType().GetMethod("NewSolution", new[] { typeof(bool), modeT })!
            .Invoke(doc, new object[] { false, Enum.Parse(modeT, "Silent") });
        var outputs = new List<object>();
        foreach (dynamic p in comp.Params.Output)
        {
            var branches = new List<object>();
            dynamic tree = p.VolatileData;
            foreach (dynamic path in tree.Paths)
            {
                var vals = new List<object>();
                foreach (object item in tree.get_Branch(path)) vals.Add(Canon(item));
                branches.Add(new Dictionary<string, object> { ["path"] = (int[])path.Indices, ["values"] = vals });
            }
            outputs.Add(branches);
        }
        var msgs = new List<object>();
        foreach (var level in new[] { "Error", "Warning" })
        {
            var lvT = gh1.GetType("Grasshopper.Kernel.GH_RuntimeMessageLevel", true);
            var rm = ((object)comp).GetType().GetMethod("RuntimeMessages", new[] { lvT })!;
            foreach (string m in (IEnumerable)rm.Invoke(comp, new[] { Enum.Parse(lvT, level) })!) msgs.Add(level + ": " + m);
        }
        return new Dictionary<string, object> { ["outputs"] = outputs, ["messages"] = msgs };
    }
    finally
    {
        foreach (dynamic o in created) doc.RemoveObject(o, false);
    }
}

// ---------------------------------------------------------------- GH2
dynamic Gh2Doc()
{
    dynamic editor = gh2.GetType("Grasshopper2.UI.Editor", true).GetProperty("Instance")!.GetValue(null)
        ?? throw new InvalidOperationException("GH2 editor not running (call g2_start first)");
    return editor.Canvas.Document;
}

void SetGh2(dynamic param, JsonNode spec)
{
    var items = spec is JsonArray arr ? arr.Select(x => CoreValue(x!.AsObject())).ToList() : new List<object> { CoreValue(spec.AsObject()) };
    // Parameter<T>.Set(T[]) with T taken from the parameter's generic base type.
    Type pt = ((object)param).GetType();
    while (pt != null && !(pt.IsGenericType && pt.GetGenericTypeDefinition().Name == "Parameter`1")) pt = pt.BaseType;
    var elem = pt?.GetGenericArguments()[0] ?? typeof(object);
    var array = Array.CreateInstance(elem, items.Count);
    for (int k = 0; k < items.Count; k++) array.SetValue(items[k] is IConvertible && elem.IsPrimitive ? Convert.ChangeType(items[k], elem) : items[k], k);
    var set = ((object)param).GetType().GetMethods().Single(m => m.Name == "Set" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == array.GetType());
    set.Invoke(param, new object[] { array });
    param.Expire();
}

Dictionary<string, object> RunGh2(JsonObject c)
{
    dynamic doc = Gh2Doc();
    var compType = sw2.GetType("Swiftlet.Gh2.Components." + (string)(c["gh2"] ?? c["gh1"])!, true);
    dynamic comp = Activator.CreateInstance(compType);
    doc.Objects.Add(comp);
    try
    {
        var inputs = c["inputs"]!.AsArray();
        for (int i = 0; i < inputs.Count; i++)
            if (inputs[i] is not null) SetGh2(comp.Parameters.Input(i), inputs[i]);
        comp.Expire();
        doc.Solution.StartWait();
        var outputs = new List<object>();
        foreach (dynamic p in comp.Parameters.Outputs)
        {
            var branches = new List<object>();
            dynamic tree = p.State.Data?.Tree();
            if (tree != null)
            {
                var twigs = ((IEnumerable)tree.AllTwigs).Cast<object>().ToArray();
                for (int b = 0; b < tree.PathCount; b++)
                {
                    dynamic twig = twigs[b];
                    var vals = new List<object>();
                    for (int k = 0; k < twig.LeafCount; k++) vals.Add(twig.NullAt(k) ? null : Canon(twig.ItemAt(k)));
                    branches.Add(new Dictionary<string, object> { ["path"] = (int[])tree.Paths[b].ToArray(), ["values"] = vals });
                }
            }
            outputs.Add(branches);
        }
        var msgs = new List<object>();
        dynamic data = comp.State.Data;
        dynamic messages = data?.Messages;
        if (messages != null)
            for (int k = 0; k < (int)messages.Count; k++)
            {
                dynamic m = messages[k];
                string level = m.Level.ToString();
                if (level == "Error" || level == "Warning") msgs.Add(level + ": " + (string)m.Text + " | " + (string)m.Info);
            }
        string phase = comp.State.Phase.ToString();
        return new Dictionary<string, object> { ["outputs"] = outputs, ["messages"] = msgs, ["phase"] = phase };
    }
    finally
    {
        doc.Objects.Remove(comp);
    }
}

foreach (var node in cases)
{
    var c = node!.AsObject();
    var r = new Dictionary<string, object> { ["name"] = (string)c["name"]! };
    foreach (var (host, run) in new (string, Func<JsonObject, Dictionary<string, object>>)[] { ("gh1", RunGh1), ("gh2", RunGh2) })
    {
        if (c[host + "Skip"] is not null) { r[host] = new Dictionary<string, object> { ["skipped"] = (string)c[host + "Skip"]! }; continue; }
        try { r[host] = run(c); }
        catch (Exception e) { var x = e is TargetInvocationException t && t.InnerException != null ? t.InnerException : e; r[host] = new Dictionary<string, object> { ["exception"] = x.GetType().Name + ": " + x.Message }; }
    }
    results.Add(r);
}
Console.WriteLine("SWIFTLET_CASES_JSON:" + JsonSerializer.Serialize(results));
