// Runtime dump of the built Swiftlet.Gh2 assembly for build/parity/build_parity_gh2.py.
// Runs inside Rhino 9 through the Rhino AI run_csharp tool after tests/Swiftlet.Cases/cases.py --load
// (GH1 and GH2 Swiftlet loaded from one folder). Prints one SWIFTLET_GH2_CATALOG line of JSON:
//   objects:    every GH2 component/parameter class with [IoId]: Nomen, and per pin name, user name,
//               access, requirement, element type and persistent default values;
//   migrations: every GH1 -> GH2 rule (IMigrationTable rows and IMigrateComponent/IMigrateParameter
//               classes), with the GH2 class it produces (found by running it on a fresh GH1 object).
using System; using System.Collections; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Text.Json;
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == n);
var gh1 = A("Grasshopper"); var gh2 = A("Grasshopper2"); var gio = A("GrasshopperIO"); var sw1 = A("Swiftlet.Gh.Rhino8"); var sw2 = A("Swiftlet.Gh2");
var ioIdAttr = gio.GetType("GrasshopperIO.IoIdAttribute", true);
var compT = gh2.GetType("Grasshopper2.Components.Component", true);
var paramT = gh2.GetType("Grasshopper2.Parameters.IParameter", true);
string S(object o) => o?.ToString();
object P(object o, string name) { try { return o?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(o); } catch { return null; } }

Type ElementType(object p)
{
    for (var t = p.GetType(); t != null; t = t.BaseType)
        if (t.IsGenericType && t.GetGenericTypeDefinition().Name == "Parameter`1") return t.GetGenericArguments()[0];
    return null;
}

List<object> Defaults(object p)
{
    var values = new List<object>();
    object tree = P(p, "PersistentDataWeak");
    if (tree == null) return values;
    foreach (dynamic twig in (IEnumerable)((dynamic)tree).AllTwigs)
        for (int i = 0; i < (int)twig.LeafCount; i++)
            values.Add(twig.NullAt(i) ? null : Text(twig.ItemAt(i)));
    return values;
}

string Text(object v) => v switch
{
    null => null,
    bool b => b ? "True" : "False",
    byte[] bytes => "base64:" + Convert.ToBase64String(bytes),
    double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
    IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
    _ => v.GetType().Namespace == "Swiftlet.Core.Json" ? S(v.GetType().GetProperty("Json")?.GetValue(v)) : v.ToString(),
};

Dictionary<string, object> Pin(object p) => new()
{
    ["name"] = S(P(P(p, "Nomen"), "Name")),
    ["userName"] = S(P(p, "UserName")) ?? S(P(p, "FallbackName")),
    ["info"] = S(P(P(p, "Nomen"), "Info")),
    ["access"] = S(P(p, "Access")),
    ["requirement"] = S(P(p, "Requirement")),
    ["type"] = ElementType(p)?.FullName,
    ["paramClass"] = p.GetType().FullName,
    ["default"] = Defaults(p),
};

var objects = new List<object>();
foreach (var t in sw2.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute(ioIdAttr) != null && t.GetConstructor(Type.EmptyTypes) != null).OrderBy(t => t.FullName))
{
    var e = new Dictionary<string, object> { ["class"] = t.Name, ["fullName"] = t.FullName, ["ioId"] = S(P(t.GetCustomAttribute(ioIdAttr), "Id"))?.ToLowerInvariant() };
    try
    {
        object o = Activator.CreateInstance(t);
        e["kind"] = compT.IsAssignableFrom(t) ? "component" : paramT.IsAssignableFrom(t) ? "param" : "other";
        object nomen = P(o, "Nomen");
        e["name"] = S(P(nomen, "Name")); e["info"] = S(P(nomen, "Info")); e["chapter"] = S(P(nomen, "Chapter")); e["section"] = S(P(nomen, "Section"));
        e["slot"] = P(nomen, "Slot"); e["rank"] = S(P(nomen, "Rank"));
        if (e["kind"].Equals("component"))
        {
            dynamic prm = ((dynamic)o).Parameters;
            var ins = new List<object>(); for (int i = 0; i < (int)prm.InputCount; i++) ins.Add(Pin((object)prm.Input(i)));
            var outs = new List<object>(); for (int i = 0; i < (int)prm.OutputCount; i++) outs.Add(Pin((object)prm.Output(i)));
            e["inputs"] = ins; e["outputs"] = outs;
        }
        else if (e["kind"].Equals("param"))
        {
            e["type"] = ElementType(o)?.FullName; e["access"] = S(P(o, "Access"));
        }
    }
    catch (Exception ex) { e["error"] = (ex.InnerException ?? ex).GetType().Name + ": " + (ex.InnerException ?? ex).Message; }
    objects.Add(e);
}

// ---------------------------------------------------------------- migrations
var tableT = gh2.GetType("Grasshopper2.Doc.Migration.IMigrationTable", true);
var baseT = gh2.GetType("Grasshopper2.Doc.Migration.IMigrateBase", true);
var migCompT = gh2.GetType("Grasshopper2.Doc.Migration.IMigrateComponent", true);
var migParamT = gh2.GetType("Grasshopper2.Doc.Migration.IMigrateParameter", true);
var gh1Doc = gh1.GetType("Grasshopper.Kernel.GH_DocumentObject", true);
var gh1ByGuid = new Dictionary<Guid, Type>();
foreach (var t in sw1.GetTypes().Where(t => t.IsClass && !t.IsAbstract && gh1Doc.IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null))
    gh1ByGuid[(Guid)t.GetProperty("ComponentGuid")!.GetValue(Activator.CreateInstance(t))!] = t;
MethodInfo Find(Type iface, string name) => new[] { iface }.Concat(iface.GetInterfaces()).Select(i => i.GetMethod(name)).FirstOrDefault(m => m != null);
var rules = new List<(string source, object rule)>();
foreach (var t in sw2.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) != null))
{
    if (tableT.IsAssignableFrom(t))
        foreach (var r in (IEnumerable)tableT.GetMethod("CreateMigrators")!.Invoke(Activator.CreateInstance(t, true), null)!) rules.Add((t.Name, r));
    else if (baseT.IsAssignableFrom(t))
        rules.Add((t.Name, Activator.CreateInstance(t, true)!));
}
var migrations = new List<object>();
foreach (var (source, r) in rules)
{
    var m = new Dictionary<string, object> { ["source"] = source, ["ruleType"] = r.GetType().Name, ["public"] = r.GetType().IsPublic || r.GetType().IsNestedPublic };
    try
    {
        var id = (Guid)baseT.GetProperty("Grasshopper1Id")!.GetValue(r)!;
        m["gh1Guid"] = id.ToString();
        if (!gh1ByGuid.TryGetValue(id, out var t1)) { m["error"] = "no GH1 object with this GUID"; migrations.Add(m); continue; }
        m["gh1Class"] = t1.Name;
        dynamic src = Activator.CreateInstance(t1)!; src.CreateAttributes();
        object target;
        if (migCompT.IsInstanceOfType(r))
            target = Find(migCompT, "Migrate").Invoke(r, new[] { Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Component", true), new object[] { src }) });
        else if (migParamT.IsInstanceOfType(r))
        {
            var migrate = Find(migParamT, "Migrate");
            var wrapT = migrate.GetParameters()[0].ParameterType;
            target = migrate.Invoke(r, new[] { Activator.CreateInstance(wrapT, new object[] { src }) });
        }
        else { m["error"] = "unknown rule kind"; migrations.Add(m); continue; }
        m["gh2Class"] = target?.GetType().Name;
        m["gh2IoId"] = S(P(target?.GetType().GetCustomAttribute(ioIdAttr), "Id"))?.ToLowerInvariant();
    }
    catch (Exception ex) { var x = ex is TargetInvocationException ti && ti.InnerException != null ? ti.InnerException : ex; m["error"] = x.GetType().Name + ": " + x.Message; }
    migrations.Add(m);
}
Console.WriteLine("SWIFTLET_GH2_CATALOG:" + JsonSerializer.Serialize(new { objects, migrations }));
