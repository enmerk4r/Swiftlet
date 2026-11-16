var gh1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh.Rhino8");
var gh2=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2");
var sw=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh2");
var ghio=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GH_IO");
var io=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GrasshopperIO").GetType("GrasshopperIO.IO");
var dup=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod);
dynamic table=Activator.CreateInstance(sw.GetType("Swiftlet.Gh2.Migration.SendMigrationTable"));
var rules=((System.Collections.IEnumerable)table.CreateMigrators()).Cast<object>().ToList();
rules.Add(Activator.CreateInstance(sw.GetType("Swiftlet.Gh2.Migration.GetRequestMigration")));
foreach(string name in new[]{"GetRequest","PostRequest","PutRequest","PatchRequest","DeleteRequest","HttpRequest"}) {
 var type=gh1.GetType("Swiftlet.Gh.Rhino8.Components."+name+"Component");
 dynamic original=Activator.CreateInstance(type);original.CreateAttributes();
 var timeout=type.GetProperty("TimeoutSeconds",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 timeout.SetValue(original,30);
 dynamic chunk=Activator.CreateInstance(ghio.GetType("GH_IO.Serialization.GH_LooseChunk"),new object[]{"SavedHttp"});original.Write(chunk);
 dynamic saved=Activator.CreateInstance(type);saved.CreateAttributes();saved.Read(chunk);
 var id=(Guid)saved.ComponentGuid;
 var rule=rules.Single(r=>r.GetType().GetProperty("Grasshopper1Id")?.GetValue(r) is Guid guid && guid==id);
 var wrapper=Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Component"),new object[]{saved});
 dynamic target=rule.GetType().GetMethod("Migrate").Invoke(rule,new object[]{wrapper});
 dynamic copy=dup.Invoke(null,new object[]{target});
 if(target.TimeoutSeconds!=30||copy.TimeoutSeconds!=30||copy.Parameters.InputCount!=saved.Params.Input.Count||copy.Parameters.OutputCount!=3)throw new Exception(name+" saved state/pin mismatch");
 Console.WriteLine("PASS "+name+" saved GH1 timeout migration + native archive");
}
