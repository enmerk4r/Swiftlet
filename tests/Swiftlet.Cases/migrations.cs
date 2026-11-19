// Checks every GH1 -> GH2 migration rule exported by Swiftlet.Gh2 (IMigrationTable classes and
// standalone IMigrateComponent classes). For each rule: instantiate the GH1 component with that GUID,
// wrap it as GH2's IGH_Component interop object, migrate, and report the resulting GH2 type and pin
// counts next to the GH1 pin counts. cases.py --migrations compares them and lists coverage gaps.
using System; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Text.Json;
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == n);
var gh1 = A("Grasshopper"); var gh2 = A("Grasshopper2"); var sw1 = A("Swiftlet.Gh.Rhino8"); var sw2 = A("Swiftlet.Gh2");
var docObj1 = gh1.GetType("Grasshopper.Kernel.GH_Component", true);
var gh1ByGuid = new Dictionary<Guid, Type>();
foreach (var t in sw1.GetTypes().Where(t => t.IsClass && !t.IsAbstract && docObj1.IsAssignableFrom(t)))
    gh1ByGuid[(Guid)t.GetProperty("ComponentGuid")!.GetValue(Activator.CreateInstance(t))!] = t;
var tableT = gh2.GetType("Grasshopper2.Doc.Migration.IMigrationTable", true);
var migCompT = gh2.GetType("Grasshopper2.Doc.Migration.IMigrateComponent", true);
var wrapT = gh2.GetType("Grasshopper2.Interop.IGH_Component", true);
var migrators = new List<(string source, object m)>();
foreach (var t in sw2.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null))
{
    if (tableT.IsAssignableFrom(t))
        foreach (var m in (System.Collections.IEnumerable)tableT.GetMethod("CreateMigrators")!.Invoke(Activator.CreateInstance(t), null)!)
            migrators.Add((t.Name, m));
    else if (migCompT.IsAssignableFrom(t))
        migrators.Add((t.Name, Activator.CreateInstance(t)!));
}
PropertyInfo Prop(Type iface, string name) => new[] { iface }.Concat(iface.GetInterfaces()).Select(i => i.GetProperty(name)).FirstOrDefault(p => p != null)
    ?? throw new MissingMemberException(iface.Name, name);
MethodInfo Meth(Type iface, string name) => new[] { iface }.Concat(iface.GetInterfaces()).Select(i => i.GetMethod(name)).FirstOrDefault(m => m != null)
    ?? throw new MissingMemberException(iface.Name, name);
var rows = new List<object>();
foreach (var (source, m) in migrators)
{
    var row = new Dictionary<string, object> { ["table"] = source };
    try
    {
        if (!migCompT.IsInstanceOfType(m)) { row["kind"] = m.GetType().Name; rows.Add(row); continue; }
        var id = (Guid)Prop(migCompT, "Grasshopper1Id").GetValue(m)!;
        row["gh1Guid"] = id.ToString();
        if (!gh1ByGuid.TryGetValue(id, out var t1)) { row["error"] = "no GH1 component with this GUID"; rows.Add(row); continue; }
        dynamic src = Activator.CreateInstance(t1)!; src.CreateAttributes();
        row["gh1Class"] = t1.Name; row["gh1In"] = (int)src.Params.Input.Count; row["gh1Out"] = (int)src.Params.Output.Count;
        var wrapper = Activator.CreateInstance(wrapT, new object[] { src });
        dynamic target = Meth(migCompT, "Migrate").Invoke(m, new[] { wrapper })!;
        row["gh2Class"] = ((object)target).GetType().Name;
        row["gh2In"] = (int)target.Parameters.InputCount; row["gh2Out"] = (int)target.Parameters.OutputCount;
    }
    catch (Exception e) { var x = e is TargetInvocationException ti && ti.InnerException != null ? ti.InnerException : e; row["error"] = x.GetType().Name + ": " + x.Message; }
    rows.Add(row);
}
Console.WriteLine("SWIFTLET_MIGRATIONS_JSON:" + JsonSerializer.Serialize(rows));
