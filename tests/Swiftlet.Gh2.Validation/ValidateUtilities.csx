// Run through Rhino AI run_csharp in a process that has BOTH Swiftlet shells loaded (cases.py --load).
// Covers what the data-driven cases cannot express: tree inputs, random output, saved component
// state and its Grasshopper 1 migration. Prints PASS lines; throws on failure.
using System; using System.Linq; using System.Reflection; using System.Collections; using System.Collections.Generic;
var asm = AppDomain.CurrentDomain.GetAssemblies();
var gh = asm.First(x => x.GetName().Name == "Grasshopper2");
var io = asm.First(x => x.GetName().Name == "GrasshopperIO").GetType("GrasshopperIO.IO");
var plug = asm.First(x => x.GetName().Name == "Swiftlet.Gh2");
var sw1 = asm.First(x => x.GetName().Name == "Swiftlet.Gh.Rhino8");
var core = asm.First(x => x.GetName().Name == "Swiftlet.Core");
void Check(bool ok, string what) { if (!ok) throw new Exception("FAIL " + what); Console.WriteLine("PASS " + what); }
Type C(string n) => plug.GetType("Swiftlet.Gh2.Components." + n, true);
var garden = gh.GetType("Grasshopper2.Data.Garden", true);

// Uses the open GH2 canvas document (start GH2 first); everything added here is removed at the end.
dynamic editor = gh.GetType("Grasshopper2.UI.Editor", true).GetProperty("Instance").GetValue(null)
    ?? throw new InvalidOperationException("GH2 editor not running (call g2_start first)");
dynamic doc = editor.Canvas.Document;
var added = new List<object>();
dynamic Add(string cls) { dynamic c = Activator.CreateInstance(C(cls)); doc.Objects.Add(c); added.Add(c); return c; }
void Solve() { doc.Solution.StartWait(); }
object[] Items(dynamic component, int output) => ((IEnumerable)component.Parameters.Output(output).State.Data.Tree().AllItems).Cast<object>().ToArray();

// ---- Generate GUID: fresh, well-formed value in the requested format
dynamic guid = Add("GenerateGuidComponent");
guid.Parameters.Input(1).Set(new string[] { "N" });
Solve();
var g1 = (string)Items(guid, 0).Single();
Check(g1.Length == 32 && Guid.TryParseExact(g1, "N", out _), "GenerateGuid produces format N: " + g1);
guid.Parameters.Input(0).Expire(); guid.Expire(); Solve();
var g2 = (string)Items(guid, 0).Single();
Check(g2 != g1, "GenerateGuid produces a new value on each solve");

// ---- Replace Empty Branches: only empty twigs are replaced, paths are kept
dynamic reb = Add("ReplaceEmptyBranchesComponent");
var arrays = new string[][] { new[] { "a", "b" }, new string[0], new[] { "c" } };
var treeFromArrays = garden.GetMethods().Single(m => m.Name == "TreeFromArrays" && m.GetParameters().Length == 3).MakeGenericMethod(typeof(string));
object tree = treeFromArrays.Invoke(null, new object[] { arrays, null, null });
dynamic inTree = reb.Parameters.Input(0); dynamic inRep = reb.Parameters.Input(1);
((object)inTree).GetType().GetMethods().Single(m => m.Name == "Set" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == "ITree").Invoke((object)inTree, new object[] { tree });
inRep.Set(new object[] { "x", "y" });
inTree.Expire(); inRep.Expire(); reb.Expire(); Solve();
object outTree = reb.Parameters.Output(0).State.Data.Tree();
var iTree = gh.GetType("Grasshopper2.Data.ITree", true); var iTwig = gh.GetType("Grasshopper2.Data.ITwig", true);
var twigs = ((IEnumerable)iTree.GetProperty("AllTwigs").GetValue(outTree)).Cast<object>()
    .Select(tw => string.Join(",", ((IEnumerable)iTwig.GetProperty("Items").GetValue(tw)).Cast<object>())).ToList();
object paths = iTree.GetProperty("Paths").GetValue(outTree);
var pathTexts = ((IEnumerable)paths.GetType().GetProperty("Forwards").GetValue(paths)).Cast<object>().Select(x => x.ToString()).ToList();
Console.WriteLine("   " + string.Join(" | ", pathTexts.Zip(twigs, (x, y) => x + "=" + y)));
Check(twigs.Count == 3 && twigs[0] == "a,b" && twigs[1] == "x,y" && twigs[2] == "c" && pathTexts.Distinct().Count() == 3,
    "ReplaceEmptyBranches pads only the empty branch and keeps paths");

// ---- Save Web Response: saved Text/Binary choice survives serialization
dynamic swr = Activator.CreateInstance(C("SaveWebResponseComponent"));
Check((bool)swr.SaveAsText == false, "SaveWebResponse defaults to binary");
swr.SaveAsText = true;
var dup = io.GetMethods().Single(m => m.Name == "Duplicate" && !m.IsGenericMethod);
dynamic swrClone = dup.Invoke(null, new object[] { swr });
Check((bool)swrClone.SaveAsText, "SaveWebResponse text mode survives a serialization round-trip");

// ---- Save Web Response: Grasshopper 1 saved state migrates
var gh1Type = sw1.GetType("Swiftlet.Gh.Rhino8.Components.SaveWebResponseComponent", true);
foreach (bool text in new[] { true, false })
{
    object legacy = Activator.CreateInstance(gh1Type);
    gh1Type.GetField("_textChecked", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(legacy, text);
    gh1Type.GetField("_binaryChecked", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(legacy, !text);
    var migratorType = plug.GetType("Swiftlet.Gh2.Migration.SaveWebResponseMigration", true);
    object migrator = Activator.CreateInstance(migratorType);
    var migrate = migratorType.GetMethod("Migrate");
    ((dynamic)legacy).CreateAttributes();
    object wrapped = Activator.CreateInstance(gh.GetType("Grasshopper2.Interop.IGH_Component", true), new object[] { legacy });
    dynamic migrated = migrate.Invoke(migrator, new object[] { wrapped });
    Check((bool)migrated.SaveAsText == text && (int)migrated.Parameters.InputCount == 2 && (int)migrated.Parameters.OutputCount == 1,
        "SaveWebResponse migrates GH1 state " + (text ? "Text" : "Binary"));
}
foreach (dynamic o in added) doc.Objects.Remove(o);
Console.WriteLine("ALL UTILITY CHECKS PASSED");
