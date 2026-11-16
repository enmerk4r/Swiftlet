try {
var gh1=System.Reflection.Assembly.LoadFrom(__GH1_PATH_JSON__);
var t=gh1.GetType("Swiftlet.Gh.Rhino8.Components.GetRequestComponent"); dynamic source=Activator.CreateInstance(t); source.CreateAttributes();
var timeout=t.GetProperty("TimeoutSeconds",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);timeout.SetValue(source,30);
var ghio=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GH_IO"); dynamic chunk=Activator.CreateInstance(ghio.GetType("GH_IO.Serialization.GH_LooseChunk"),new object[]{"SavedGet"});source.Write(chunk);
dynamic saved=Activator.CreateInstance(t);saved.CreateAttributes();saved.Read(chunk);
if((int)timeout.GetValue(saved)!=30)throw new Exception("GH1 state not restored");
var gh2=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2"); var wrapper=Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Component"),new object[]{saved});
var plug=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh2");dynamic migrator=Activator.CreateInstance(plug.GetType("Swiftlet.Gh2.Migration.GetRequestMigration"));
dynamic migrated=migrator.Migrate((dynamic)wrapper);
if(migrated.TimeoutSeconds!=30||migrated.Parameters.InputCount!=3||migrated.Parameters.OutputCount!=3)throw new Exception("Migration lost settings or pins");
Console.WriteLine("PASS saved GH1 GET timeout=30 -> native GH2 timeout=30 with 3 inputs/3 outputs");
var panel=Activator.CreateInstance(gh2.GetType("Grasshopper2.UI.InputPanel.InputPanel"));migrated.AppendToInputPanel((dynamic)panel);Console.WriteLine("PASS Eto settings panel construction");
} catch(Exception e) {Console.WriteLine(e.ToString());throw;}