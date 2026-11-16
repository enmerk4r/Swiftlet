var ghAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Grasshopper");
dynamic canvas = ghAssembly.GetType("Grasshopper.Instances").GetProperty("ActiveCanvas").GetValue(null);
dynamic document = canvas.Document;
var outputs = new List<object>();
bool found = false;
foreach (dynamic obj in document.Objects)
{
    if (obj.InstanceGuid.ToString() != "__TARGET_ID__") continue;
    found = true;
    foreach (dynamic parameter in obj.Params.Output)
    {
        dynamic tree = parameter.VolatileData;
        if (tree == null) throw new InvalidOperationException("No GH1 output tree");
        var branches = new List<object>();
        foreach (dynamic path in tree.Paths)
        {
            var values = new List<object>();
            foreach (object item in tree.get_Branch(path)) values.Add(CanonicalValue(item));
            branches.Add(new Dictionary<string, object> { ["path"] = (int[])path.Indices, ["values"] = values });
        }
        outputs.Add(new Dictionary<string, object> { ["name"] = (string)parameter.Name, ["branches"] = branches });
    }
}
if (!found) throw new InvalidOperationException("GH1 target no longer exists");
Console.WriteLine("SWIFTLET_PARITY_JSON:" + System.Text.Json.JsonSerializer.Serialize(outputs));
