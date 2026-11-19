var ghAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Grasshopper2");
dynamic editor = ghAssembly.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic document = editor.Canvas.Document;
var outputs = new List<object>();
bool found = false;
foreach (dynamic obj in document.Objects.Forwards)
{
    if (obj.InstanceId.ToString() != "__TARGET_ID__") continue;
    found = true;
    foreach (dynamic parameter in obj.Parameters.Outputs)
    {
        dynamic tree = parameter.State.Data.Tree();
        if (tree == null) throw new InvalidOperationException("No GH2 output tree");
        var branches = new List<object>();
        int leafCount = 0;
        var twigs = ((System.Collections.IEnumerable)tree.AllTwigs).Cast<object>().ToArray();
        for (int branch = 0; branch < tree.PathCount; branch++)
        {
            dynamic path = tree.Paths[branch];
            dynamic twig = twigs[branch];
            var values = new List<object>();
            // ItemCount excludes nulls in GH2. LeafCount preserves their positions.
            for (int index = 0; index < twig.LeafCount; index++)
                values.Add(CanonicalValue(twig.NullAt(index) ? null : twig.ItemAt(index)));
            leafCount += values.Count;
            branches.Add(new Dictionary<string, object> { ["path"] = (int[])path.ToArray(), ["values"] = values });
        }
        if (leafCount != (int)tree.LeafCount) throw new InvalidOperationException("Incomplete GH2 extraction");
        outputs.Add(new Dictionary<string, object> { ["name"] = (string)parameter.Nomen.Name, ["branches"] = branches });
    }
}
if (!found) throw new InvalidOperationException("GH2 target no longer exists");
Console.WriteLine("SWIFTLET_PARITY_JSON:" + System.Text.Json.JsonSerializer.Serialize(outputs));
