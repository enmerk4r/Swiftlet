using System;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Text.Json;
var cmd=JsonDocument.Parse(@"__COMMAND__").RootElement;string action=cmd.GetProperty("action").GetString();
Assembly A(string n)=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==n);
var gh=A("Grasshopper2");var sw=A("Swiftlet.Gh2");dynamic doc=AppDomain.CurrentDomain.GetData("swiftlet.variable.doc");dynamic c=AppDomain.CurrentDomain.GetData("swiftlet.variable.component");
dynamic Verb(string verb,string noun)=>Activator.CreateInstance(gh.GetType("Grasshopper2.Undo.VerbNoun"),new object[]{verb,noun});
void SetTools(dynamic target,string[] names){var type=A("Swiftlet.Core").GetType("Swiftlet.Core.Mcp.McpToolDefinition");Array values=Array.CreateInstance(type,names.Length);for(int i=0;i<names.Length;i++)values.SetValue(Activator.CreateInstance(type,new object[]{names[i],names[i],null}),i);target.Parameters.Input(1).Set((dynamic)values);target.Parameters.Input(1).Expire();}
if(action=="wire"){
 var type=sw.GetTypes().Single(t=>t.Name==(c.GetType().Name=="McpServerComponent"?"McpToolCallRequestParameter":"ListenerRequestParameter"));
 dynamic sink=Activator.CreateInstance(type);doc.Objects.Add(sink);
 gh.GetType("Grasshopper2.Parameters.Connections").GetMethods().Single(m=>m.Name=="Connect"&&m.GetParameters().Length==3).Invoke(null,new object[]{c.Parameters.Output(1),sink,null});
 AppDomain.CurrentDomain.SetData("swiftlet.variable.sink",sink);
}
if(action=="setup"){
 AppDomain.CurrentDomain.SetData("swiftlet.variable.sink",null);
 if(doc!=null)doc.Close();doc=gh.GetType("Grasshopper2.Doc.Document").GetMethod("NewActiveDocument").Invoke(null,null);dynamic ed=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);ed.Canvas.Document=doc;
 c=Activator.CreateInstance(sw.GetTypes().Single(t=>t.Name==cmd.GetProperty("component").GetString()));doc.Objects.Add(c);c.Parameters.Input(0).Set(new[]{cmd.GetProperty("port").GetInt32()});c.Parameters.Input(0).Expire();
 if(c.GetType().Name=="McpServerComponent")SetTools(c,new[]{"first"});
 c.Expire();doc.Solution.Start();AppDomain.CurrentDomain.SetData("swiftlet.variable.doc",doc);AppDomain.CurrentDomain.SetData("swiftlet.variable.component",c);
}
if(action=="add"||action=="remove"){
 dynamic side=Enum.Parse(gh.GetType("Grasshopper2.Components.Side"),"Output");
 dynamic undo=Activator.CreateInstance(gh.GetType("Grasshopper2.Undo.ActionList"),new object[]{Array.CreateInstance(gh.GetType("Grasshopper2.Undo.Action"),0)});
 if(action=="add")c.DoCreateParameter(side,1,undo);else c.DoRemoveParameter(side,1,undo);
 c.VariableParameterMaintenance();doc.Undo.Do(Verb(action,"route"),undo);c.Expire();doc.Solution.Start();
}
if(action=="tools"||action=="port"||action=="name"){
 int index=action=="tools"?1:action=="port"?0:2;
 dynamic undo=Activator.CreateInstance(gh.GetType("Grasshopper2.Undo.Actions.PersistentDataAction"),new object[]{c.Parameters.Input(index),Guid.NewGuid()});
 c.AddUndoRecord(Verb("Change",action),undo);
 if(action=="tools")SetTools(c,cmd.GetProperty("tools").EnumerateArray().Select(x=>x.GetString()).ToArray());
 else if(action=="port")c.Parameters.Input(index).Set(new[]{cmd.GetProperty("port").GetInt32()});
 else c.Parameters.Input(index).Set(new[]{cmd.GetProperty("name").GetString()});
 c.Parameters.Input(index).Expire();c.Expire();doc.Solution.Start();
}
if(action=="undo")doc.Undo.Undo();if(action=="redo")doc.Undo.Redo();
if(action=="close"){doc.Close();Console.WriteLine("VARIABLE_JSON:{}");return;}
var pins=new List<object>();dynamic data=c.State.Data;
foreach(dynamic pin in c.Parameters.Outputs){var values=new List<object>();var tree=data?.Tree((Guid)pin.InstanceId);if(tree!=null)foreach(dynamic value in tree.NonNullItems){
 if(c.GetType().Name=="ServerInputComponent"){
  values.Add(new{path=(string)value.Path,route=(string)value.MatchedRoute,responded=(bool)value.HasResponded});
  if(action=="respond"&&!value.HasResponded){object reply=A("Swiftlet.Runtime").GetType("Swiftlet.Runtime.ModernServerHttpResponse").GetMethod("PlainText").Invoke(null,new object[]{200,(string)value.MatchedRoute,null});value.TrySetResponse((dynamic)reply);}
 }
 }pins.Add(new{id=pin.InstanceId.ToString(),name=(string)pin.UserName,values});}
var io=A("GrasshopperIO").GetType("GrasshopperIO.IO");dynamic clone=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod).Invoke(null,new object[]{c});
var archived=new List<string>();foreach(dynamic pin in clone.Parameters.Outputs)archived.Add((string)pin.UserName);
dynamic sinkState=AppDomain.CurrentDomain.GetData("swiftlet.variable.sink");
object session=c.GetType().GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(c);
int? port=session==null?null:(int)session.GetType().GetProperty("Port").GetValue(session);
string serverName=session?.GetType().GetProperty("ServerName")?.GetValue(session) as string;
Console.WriteLine("VARIABLE_JSON:"+JsonSerializer.Serialize(new{port,serverName,wires=sinkState==null?0:(int)sinkState.Inputs.Count,component=c.GetType().Name,phase=c.Phase.ToString(),running=doc.Solution.State.CurrentlyRunning!=null,pins,archived}));
