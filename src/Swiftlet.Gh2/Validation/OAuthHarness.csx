using System; using System.Linq; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Text.Json;
var command=JsonDocument.Parse(@"__COMMAND_JSON__").RootElement;
int host=command.GetProperty("host").GetInt32();string action=command.GetProperty("action").GetString();
Assembly A(string name)=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==name);
var gh=A(host==1?"Grasshopper":"Grasshopper2");var sw=A(host==1?"Swiftlet.Gh.Rhino8":"Swiftlet.Gh2");
dynamic doc;
if(host==1){dynamic canvas=gh.GetType("Grasshopper.Instances").GetProperty("ActiveCanvas").GetValue(null);doc=canvas.Document;}
else {dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);doc=editor.Canvas.Document;if(doc==null||doc.State.ToString()=="Closed"){doc=gh.GetType("Grasshopper2.Doc.Document").GetMethod("NewActiveDocument").Invoke(null,null);editor.Canvas.Document=doc;}}
dynamic component=null;
if(action=="create") {
 string name=command.GetProperty("class").GetString();var type=sw.GetType((host==1?"Swiftlet.Gh.Rhino8.Components.":"Swiftlet.Gh2.Components.")+name);
 if(name=="OAuthAuthorizeComponent") {
  var desktop=Assembly.Load("Swiftlet.Hosts.Desktop");var headless=Assembly.Load("Swiftlet.Hosts.Headless");
  var manual=Activator.CreateInstance(headless.GetType("Swiftlet.Hosts.Headless.ManualBrowserLauncher"));
  var services=Activator.CreateInstance(desktop.GetType("Swiftlet.Hosts.Desktop.DesktopHostServices"),new object[]{manual,null,null,null});
  component=Activator.CreateInstance(type,new object[]{services});
 } else component=Activator.CreateInstance(type);
 if(host==1){component.CreateAttributes();doc.AddObject(component,false,int.MaxValue);}else doc.Objects.Add(component);
} else {
 string id=command.GetProperty("id").GetString();
 IEnumerable objects=host==1?(IEnumerable)doc.Objects:(IEnumerable)doc.Objects.Forwards;
 foreach(dynamic candidate in objects)if((host==1?candidate.InstanceGuid.ToString():candidate.InstanceId.ToString())==id){component=candidate;break;}
 if(component==null)throw new Exception("OAuth target missing");
}
if(action=="close") { if(host!=2)throw new Exception("Document close check is GH2 only");doc.Close();Console.WriteLine("OAUTH_JSON:{}");return; }
if(action=="archive") {
 object restored;
 if(host==2){var io=A("GrasshopperIO").GetType("GrasshopperIO.IO");restored=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod).Invoke(null,new object[]{component});}
 else {dynamic chunk=Activator.CreateInstance(A("GH_IO").GetType("GH_IO.Serialization.GH_LooseChunk"),new object[]{"OAuthArchive"});component.Write(chunk);dynamic copy=Activator.CreateInstance(component.GetType());copy.CreateAttributes();copy.Read(chunk);restored=copy;}
 var flags=BindingFlags.NonPublic|BindingFlags.Instance;
 if(restored.GetType().Name=="OAuthTokenComponent") {dynamic state=restored.GetType().GetField("_session",flags).GetValue(restored);if(!string.IsNullOrEmpty((string)state.Cached.AccessToken))throw new Exception("Token cache serialized");}
 else {dynamic state=restored.GetType().GetField("_controller",flags).GetValue(restored);if(state.Snapshot.Code!=null||state.Snapshot.Verifier!=null)throw new Exception("Authorization state serialized");}
 Console.WriteLine("OAUTH_JSON:{\"archived\":true}");return;
}
if(action=="remove") {
 if(host==1){var sources=new List<object>();foreach(dynamic pin in component.Params.Input)foreach(object source in pin.Sources)sources.Add(source);doc.RemoveObject(component,false);foreach(dynamic source in sources)doc.RemoveObject(source,false);}
 else doc.Objects.Remove(component);
 Console.WriteLine("OAUTH_JSON:{}");return;
}
if(action=="disable"||action=="enable") {
 if(host!=2)throw new Exception("Activity test is GH2 only");
 var activity=gh.GetType("Grasshopper2.Doc.ObjectActivity");component.GetType().GetProperty("Activity").SetValue(component,Enum.Parse(activity,action=="disable"?"Disabled":"Enabled"));
}
if(command.TryGetProperty("inputs",out var inputs))foreach(var item in inputs.EnumerateObject()) {
 int index=int.Parse(item.Name);var value=item.Value;
 if(host==1){
  dynamic pin=component.Params.Input[index];dynamic source;
  if(pin.SourceCount==0){source=Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Parameters.Param_GenericObject"));source.CreateAttributes();doc.AddObject(source,false,int.MaxValue);pin.AddSource(source);}
  else source=pin.Sources[0];
  source.PersistentData.Clear();
  var items=value.ValueKind==JsonValueKind.Array?value.EnumerateArray().ToArray():new[]{value};
  foreach(var literal in items){object goo=literal.ValueKind switch {
   JsonValueKind.String=>Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Types.GH_String"),new object[]{literal.GetString()}),
   JsonValueKind.Number=>Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Types.GH_Integer"),new object[]{literal.GetInt32()}),
   _=>Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Types.GH_Boolean"),new object[]{literal.GetBoolean()})};
   ((object)source.PersistentData).GetType().GetMethod("Append",new[]{gh.GetType("Grasshopper.Kernel.Types.IGH_Goo")}).Invoke(source.PersistentData,new[]{goo});
  }source.ExpireSolution(false);
 }else{
  dynamic pin=component.Parameters.Input(index);var items=value.ValueKind==JsonValueKind.Array?value.EnumerateArray().ToArray():new[]{value};
  if(items.Length==0)pin.Set(Array.Empty<string>());
  else if(items[0].ValueKind==JsonValueKind.String)pin.Set(items.Select(x=>x.GetString()).ToArray());
  else if(items[0].ValueKind==JsonValueKind.Number)pin.Set(items.Select(x=>x.GetInt32()).ToArray());
  else pin.Set(items.Select(x=>x.GetBoolean()).ToArray());pin.Expire();
 }
}
if(action=="cancel") {
 if(host!=2)throw new Exception("Cancellation check is GH2 only");
 component.Expire();using var cts=new System.Threading.CancellationTokenSource();
 var start=((MethodInfo[])doc.Solution.GetType().GetMethods()).Single(m=>m.Name=="Start"&&m.GetParameters().Length==2);
 var watch=System.Diagnostics.Stopwatch.StartNew();
 var task=(System.Threading.Tasks.Task)start.Invoke(doc.Solution,new object[]{cts,Enum.ToObject(start.GetParameters()[1].ParameterType,0)});cts.CancelAfter(300);
 try{task.GetAwaiter().GetResult();}catch(OperationCanceledException){}
 if(component.Phase.ToString()!="Cancelled"||watch.Elapsed.TotalSeconds>3)throw new Exception("OAuth cancellation failed");
 Console.WriteLine("OAUTH_JSON:"+JsonSerializer.Serialize(new{cancelled=true,elapsed=watch.ElapsedMilliseconds}));return;
}
if(action!="disable") {
 if(host==1){component.ExpireSolution(false);doc.NewSolution(false);}else{component.Expire();doc.Solution.StartWait();}
}
var values=new List<object>();
if(host==1)foreach(dynamic pin in component.Params.Output){var list=((IEnumerable)pin.VolatileData.AllData(true)).Cast<object>().ToArray();values.Add(list.Length==0||list[0]==null?null:((dynamic)list[0]).Value);}
else foreach(dynamic pin in component.Parameters.Outputs){var data=pin.State.Data?.Tree();var list=data==null?Array.Empty<object>():((IEnumerable)data.AllItems).Cast<object>().ToArray();values.Add(list.Length==0?null:list[0]);}
string targetId=host==1?component.InstanceGuid.ToString():component.InstanceId.ToString();
Console.WriteLine("OAUTH_JSON:"+JsonSerializer.Serialize(new{id=targetId,values=values}));
