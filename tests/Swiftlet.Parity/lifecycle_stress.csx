// One dedicated native GH2 canvas; commands are supplied by lifecycle_stress.py.
using System; using System.Collections; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Text.Json;
var command=JsonDocument.Parse(@"__COMMAND__").RootElement;
if(Environment.ProcessId!=command.GetProperty("pid").GetInt32())throw new Exception("Stress slot PID mismatch; no actions performed.");
var gh=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2");
var sw=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh2");
var core=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Core");
var runtime=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Runtime");
dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);dynamic doc=editor.Canvas.Document;
const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
string[] names={"ServerInputComponent","McpServerComponent","HttpListenerComponent","WebSocketServerComponent","OAuthAuthorizeComponent"};
dynamic Find(string name)=>((IEnumerable)doc.Objects.Forwards).Cast<object>().Single(o=>o.GetType().Name==name);
void Set(dynamic component,int index,params object[] values)
{
 object pin=component.Parameters.Input(index);Type type=pin.GetType();while(type!=null&&!(type.IsGenericType&&type.GetGenericTypeDefinition().Name=="Parameter`1"))type=type.BaseType;
 var array=Array.CreateInstance(type.GetGenericArguments()[0],values.Length);for(int i=0;i<values.Length;i++)array.SetValue(values[i],i);
 pin.GetType().GetMethods().Single(m=>m.Name=="Set"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==array.GetType()).Invoke(pin,new object[]{array});
 ((dynamic)pin).Expire();component.Expire();
}
object ManualHost()
{
 var desktop=Assembly.Load("Swiftlet.Hosts.Desktop");var headless=Assembly.Load("Swiftlet.Hosts.Headless");
 return Activator.CreateInstance(desktop.GetType("Swiftlet.Hosts.Desktop.DesktopHostServices"),new object[]{Activator.CreateInstance(headless.GetType("Swiftlet.Hosts.Headless.ManualBrowserLauncher")),null,null,null});
}
void InjectManualHost(dynamic authorizer)
{
 var field=((object)authorizer).GetType().GetField("_controller",Private);dynamic old=field.GetValue(authorizer);old.ResetAsync().GetAwaiter().GetResult();
 object controller=Activator.CreateInstance(runtime.GetType("Swiftlet.Runtime.OAuthAuthorizationController"),new[]{ManualHost()});
 var changed=controller.GetType().GetEvent("Changed");changed.AddEventHandler(controller,Delegate.CreateDelegate(changed.EventHandlerType,(object)authorizer,((object)authorizer).GetType().GetMethod("OnChanged",Private)));
 field.SetValue(authorizer,controller);
}
dynamic Add(string name)
{
 var type=sw.GetType("Swiftlet.Gh2.Components."+name,true);
 dynamic obj=name=="OAuthAuthorizeComponent"?Activator.CreateInstance(type,new[]{ManualHost()}):Activator.CreateInstance(type);
 obj.UserName="stress "+name;doc.Objects.Add(obj);return obj;
}
void Connect(dynamic source,int output,dynamic target,int input)
{
 var method=gh.GetType("Grasshopper2.Parameters.Connections").GetMethods().Single(m=>m.Name=="Connect"&&m.GetParameters().Length==3);
 method.Invoke(null,new object[]{source.Parameters.Output(output),target.Parameters.Input(input),null});
}
void Authorize(bool start){Set(Find("OAuthAuthorizeComponent"),4,start);doc.Solution.StartWait();}
void Solve(){foreach(string name in names)Find(name).Expire();doc.Solution.StartWait();}
string action=command.GetProperty("action").GetString();
if(action=="setup")
{
 doc=gh.GetType("Grasshopper2.Doc.Document").GetMethod("NewActiveDocument").Invoke(null,null);editor.Canvas.Document=doc;
 foreach(string name in names)Add(name);
 var ports=command.GetProperty("ports").EnumerateArray().Select(p=>p.GetInt32()).ToArray();
 for(int i=0;i<names.Length;i++)Set(Find(names[i]),i==4?3:0,ports[i]);
 dynamic server=Find(names[0]);server.DoCreateParameter((dynamic)Enum.Parse(gh.GetType("Grasshopper2.Components.Side"),"Output"),1,null);server.Parameters.Output(1).UserName="/stress";server.VariableParameterMaintenance();
 dynamic mcp=Find(names[1]);var tool=Activator.CreateInstance(core.GetType("Swiftlet.Core.Mcp.McpToolDefinition"),new object[]{"echo","Stress echo",null});Set(mcp,1,tool);
 var p=Activator.CreateInstance(sw.GetType("Swiftlet.Gh2.Parameters.McpToolCallRequestParameter"),new object[]{"echo","echo","Stress tool",Enum.Parse(gh.GetType("Grasshopper2.Parameters.Access"),"Item")});mcp.Parameters.AddOutput((dynamic)p,null);
 dynamic response=Add("ServerResponseComponent");Set(response,1,Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.RequestBodyText"),new object[]{"text/plain","server-ok"}));Connect(server,1,response,0);
 dynamic toolResponse=Add("McpToolResponseComponent");Set(toolResponse,1,Activator.CreateInstance(core.GetType("Swiftlet.Core.Mcp.McpTextContentBlock"),new object[]{"mcp-ok"}));Connect(mcp,0,toolResponse,0);
 Set(Find(names[2]),1,"stress");Set(Find(names[2]),2,Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.RequestBodyText"),new object[]{"text/plain","listener-ok"}));
 dynamic ws=Find(names[3]);Set(ws,1,true);dynamic sender=Add("WebSocketSendComponent");Set(sender,2,true);Connect(ws,0,sender,0);Connect(ws,2,sender,1);
 dynamic oauth=Find(names[4]);Set(oauth,0,"https://auth.example.test/authorize");Set(oauth,1,"stress-client");Set(oauth,4,true);Solve();
}
else if(action=="disable"||action=="enable")
{
 foreach(string name in names)Find(name).Activity=(dynamic)Enum.Parse(gh.GetType("Grasshopper2.Doc.ObjectActivity"),action=="disable"?"Disabled":"Enabled");
 if(action=="enable")Solve();
}
else if(action=="ports")
{
 Authorize(false);var ports=command.GetProperty("ports").EnumerateArray().Select(p=>p.GetInt32()).ToArray();
 for(int i=0;i<names.Length;i++)Set(Find(names[i]),i==4?3:0,ports[i]);Set(Find(names[4]),4,true);Solve();
}
else if(action=="run"){Set(Find(names[3]),1,command.GetProperty("run").GetBoolean());Solve();}
else if(action=="restart-oauth"){Authorize(false);Authorize(true);}
else if(action=="delete")
{
 // Keep deserialization from opening a browser: false is archived; the manual fixture is restored before restarting.
 Authorize(false);
 var objects=Array.CreateInstance(gh.GetType("Grasshopper2.Doc.IDocumentObject"),5);for(int i=0;i<5;i++)objects.SetValue((object)Find(names[i]),i);
 var undo=Activator.CreateInstance(gh.GetType("Grasshopper2.Undo.ActionList"),new object[]{Array.CreateInstance(gh.GetType("Grasshopper2.Undo.Action"),0)});
 doc.Methods.DeleteObjects((dynamic)objects,(dynamic)Array.CreateInstance(gh.GetType("Grasshopper2.Doc.WireEnds"),0),(dynamic)undo);
 var noun=gh.GetType("Grasshopper2.Undo.VerbNoun").GetMethods().Single(m=>m.Name=="op_Implicit"&&m.GetParameters()[0].ParameterType==typeof(string)).Invoke(null,new object[]{"Stress delete services"});
 doc.Undo.Do((dynamic)noun,(dynamic)undo);
}
else if(action=="undo"||action=="redo")
{
 if(action=="redo"){Authorize(false);doc.Undo.Redo();}
 else {doc.Undo.Undo();InjectManualHost(Find(names[4]));Set(Find(names[4]),4,true);Solve();}
}
else if(action=="close"){doc.Close();Console.WriteLine("STRESS:"+JsonSerializer.Serialize(new{pid=Environment.ProcessId,closed=true}));return;}
else if(action=="fixture")
{
 var assembly=Assembly.LoadFrom(command.GetProperty("path").GetString());dynamic fixture=Activator.CreateInstance(assembly.GetType("Swiftlet.Gh2.Fixtures.ControlledPublisherComponent"));doc.Objects.Add(fixture);
 dynamic sender=Find("WebSocketSendComponent");gh.GetType("Grasshopper2.Parameters.Connections").GetMethod("DisconnectAllInputs").Invoke(null,new object[]{sender.Parameters.Input(1),null});Connect(fixture,0,sender,1);doc.Solution.StartWait();
}
else if(action=="fixture-cancel")
{
 dynamic fixture=Find("ControlledPublisherComponent");fixture.Enqueue("mid-process");fixture.CancelAfterPublish=true;fixture.Expire();
 using var cts=new System.Threading.CancellationTokenSource();cts.CancelAfter(200);
 var method=((object)doc.Solution).GetType().GetMethods().Single(m=>m.Name=="StartWait"&&m.GetParameters().Length==2);
 try{method.Invoke(doc.Solution,new object[]{cts,Enum.ToObject(method.GetParameters()[1].ParameterType,0)});}catch(TargetInvocationException error)when(error.InnerException is OperationCanceledException){}
}
else if(action=="resume-ws"||action=="suppress-ws")
{
 dynamic ws=Find(names[3]);object session=((object)ws).GetType().GetField("_session",Private).GetValue(ws);
 var changed=session.GetType().GetEvent("StateChanged");var handler=Delegate.CreateDelegate(changed.EventHandlerType,(object)ws,((object)ws).GetType().GetMethod("OnStateChanged",Private));
 changed.RemoveEventHandler(session,handler);if(action=="resume-ws")changed.AddEventHandler(session,handler);
}
else if(action=="publish-cancel")
{
 Find(names[3]).Expire();using var cts=new System.Threading.CancellationTokenSource();cts.CancelAfter(200);
 var method=((object)doc.Solution).GetType().GetMethods().Single(m=>m.Name=="StartWait"&&m.GetParameters().Length==2);
 try{method.Invoke(doc.Solution,new object[]{cts,Enum.ToObject(method.GetParameters()[1].ParameterType,0)});}catch(TargetInvocationException error)when(error.InnerException is OperationCanceledException){}
}
else if(action=="cancel")doc.Solution.Stop();
else if(action=="unrelated"){Find(names[2]).Expire();doc.Solution.StartWait();}
else if(action=="solve")Solve();
else if(action!="snapshot")throw new Exception("Unknown action "+action);
var diagnostics=new List<object>();
foreach(dynamic obj in ((IEnumerable)doc.Objects.Forwards).Cast<object>())
{
 var values=new List<object>();foreach(dynamic output in obj.Parameters.Outputs){dynamic tree=output.State.Data?.Tree();values.Add(tree==null?Array.Empty<object>():((IEnumerable)tree.AllItems).Cast<object>().Select(value=>value is string||value is bool?value:value?.GetType().Name).ToArray());}
 diagnostics.Add(new{name=((object)obj).GetType().Name,phase=obj.State.Phase.ToString(),current=obj.State.CurrentId.ToString(),process=obj.State.ProcessId.ToString(),fault=((Exception)obj.State.FaultException)?.Message,values});
}
var results=new List<object>();
foreach(string name in names)
{
 dynamic obj=((IEnumerable)doc.Objects.Forwards).Cast<object>().SingleOrDefault(o=>o.GetType().Name==name);if(obj==null)continue;
 int bridge=0;var field=((object)obj).GetType().GetField("_session",Private);
 if(field?.GetValue(obj)is{}session){var process=(System.Diagnostics.Process)session.GetType().GetField("_bridgeProcess",Private)?.GetValue(session);try{if(process!=null&&!process.HasExited)bridge=process.Id;}catch{}}
 object outbox=null;var boxField=((object)obj).GetType().GetField("_received",Private)??((object)obj).GetType().GetField("_requests",Private);
 if(boxField?.GetValue(obj)is{}box&&box.GetType().GetField("_items",Private)is{}itemsField)outbox=new{count=((IList)itemsField.GetValue(box)).Count,published=box.GetType().GetField("_publishedIn",Private).GetValue(box).ToString(),publishedCount=(int)box.GetType().GetField("_publishedCount",Private).GetValue(box)};
 bool waiting=false;string state=null,status=null;
 if(name==names[4]){dynamic c=((object)obj).GetType().GetField("_controller",Private).GetValue(obj);waiting=c.Snapshot.IsWaiting;status=c.Snapshot.Status;dynamic flow=c.GetType().GetField("_flow",Private).GetValue(c);if(flow!=null&&flow.Session!=null)state=flow.Session.State;}
 results.Add(new{name,id=(Guid)obj.InstanceId,activity=obj.Activity.ToString(),phase=obj.State.Phase.ToString(),fault=((Exception)obj.State.FaultException)?.Message,bridge,outbox,waiting,state,status,outputs=((IEnumerable)obj.Parameters.Outputs).Cast<object>().Select(p=>(string)((dynamic)p).UserName).ToArray()});
}
dynamic testFixture=((IEnumerable)doc.Objects.Forwards).Cast<object>().SingleOrDefault(o=>o.GetType().Name=="ControlledPublisherComponent");
object fixtureState=testFixture==null?null:new{attempts=(int)testFixture.Attempts,cancelled=(int)testFixture.CancelledPublications,pending=(bool)testFixture.Pending,phase=testFixture.State.Phase.ToString(),current=testFixture.State.CurrentId.ToString()};
Console.WriteLine("STRESS:"+JsonSerializer.Serialize(new{pid=Environment.ProcessId,root=Environment.GetEnvironmentVariable("DOTNET_ROOT"),components=results,diagnostics,fixture=fixtureState}));
