// Live check: a GH1 Create Text Body saved with each menu choice (written by its real GH_IO Write, read
// back by Read) migrates to a GH2 Create Text Body with the same content type, for both the current and
// the archived GH1 component. Run with cases.py --script after --load.
using System; using System.Linq; using System.Reflection;
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == n);
var sw1 = A("Swiftlet.Gh.Rhino8"); var sw2 = A("Swiftlet.Gh2"); var gh2 = A("Grasshopper2"); var ghio = A("GH_IO");
var chunkT = ghio.GetType("GH_IO.Serialization.GH_LooseChunk", true);
var wrapT = gh2.GetType("Grasshopper2.Interop.IGH_Component", true);
var menuKeys = new[] { ("IsTextChecked", "text/plain"), ("IsJavascriptChecked", "text/javascript"), ("IsJsonChecked", "application/json"), ("IsHtmlChecked", "text/html"), ("IsXmlChecked", "application/xml") };
int failures = 0;
foreach (var (gh1Class, migrator) in new[] {
    ("Swiftlet.Gh.Rhino8.Components.CreateTextBodyComponent", "Swiftlet.Gh2.Migration.TextBodyMigration"),
    ("Swiftlet.Gh.Rhino8.Components.CreateTextBodyComponent_ARCHIVED", "Swiftlet.Gh2.Migration.ArchivedTextBodyMigration") })
{
    var t1 = sw1.GetType(gh1Class, true);
    foreach (var (key, expected) in menuKeys)
    {
        // Write a GH1 component whose saved menu flags select `key`, then restore a fresh one from it.
        dynamic writer = Activator.CreateInstance(t1); writer.CreateAttributes();
        foreach (var (k, _) in menuKeys)
            t1.GetField("_" + char.ToLowerInvariant(k[0]) + k.Substring(1), BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(writer, k == key);
        dynamic chunk = Activator.CreateInstance(chunkT, new object[] { "TextBody" });
        writer.Write(chunk);
        dynamic saved = Activator.CreateInstance(t1); saved.CreateAttributes(); saved.Read(chunk);
        dynamic m = Activator.CreateInstance(sw2.GetType(migrator, true));
        dynamic migrated = m.Migrate((dynamic)Activator.CreateInstance(wrapT, new object[] { saved }));
        string got = migrated.ContentType;
        // GH2 persistence: an archive clone (Store + IReader constructor) keeps the setting.
        var dup = A("GrasshopperIO").GetType("GrasshopperIO.IO", true).GetMethods().Single(x => x.Name == "Duplicate" && !x.IsGenericMethod);
        dynamic clone = dup.Invoke(null, new object[] { migrated });
        if ((string)clone.ContentType != got) { failures++; Console.WriteLine("FAIL archive clone lost ContentType " + got + " -> " + (string)clone.ContentType); }
        bool ok = got == expected;
        failures += ok ? 0 : 1;
        Console.WriteLine((ok ? "PASS " : "FAIL ") + t1.Name + " saved " + key + " -> GH2 ContentType " + got + (ok ? "" : " (expected " + expected + ")"));
    }
}
Console.WriteLine(failures == 0 ? "ALL PASS" : failures + " FAILURES");
