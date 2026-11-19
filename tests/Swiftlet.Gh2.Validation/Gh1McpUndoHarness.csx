using System;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;using System.Text.Json;
var cmd=JsonDocument.Parse(@"__COMMAND__").RootElement;string action=cmd.GetProperty("action").GetString();
Assembly A(string n)=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==n);
var gh=A("Grasshopper");var sw=A("Swiftlet.Gh.Rhino8");dynamic doc=AppDomain.CurrentDomain.GetData("swiftlet.gh1undo.doc");dynamic c=AppDomain.CurrentDomain.GetData("swiftlet.gh1undo.component");dynamic source=AppDomain.CurrentDomain.GetData("swiftlet.gh1undo.source");dynamic sink=AppDomain.CurrentDomain.GetData("swiftlet.gh1undo.sink");
if(action=="setup"){
 doc=Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.GH_Document"));dynamic canvas=gh.GetType("Grasshopper.Instances").GetProperty("ActiveCanvas").GetValue(null);canvas.Document=doc;
 c=Activator.CreateInstance(sw.GetType("Swiftlet.Gh.Rhino8.Components.McpServerComponent"));c.CreateAttributes();doc.AddObject(c,false,int.MaxValue);
 var tools=new List<object>();
 foreach(string name in new[]{"first","second"}){
  dynamic tool=Activator.CreateInstance(sw.GetType("Swiftlet.Gh.Rhino8.Components.DefineToolComponent"));tool.CreateAttributes();doc.AddObject(tool,false,int.MaxValue);
  var textType=gh.GetType("Grasshopper.Kernel.Types.GH_String");
  for(int i=0;i<2;i++){dynamic input=tool.Params.Input[i];((object)input.PersistentData).GetType().GetMethod("Append",new[]{textType}).Invoke(input.PersistentData,new[]{Activator.CreateInstance(textType,new object[]{name})});}
  c.Params.Input[1].AddSource(tool.Params.Output[0]);tools.Add(tool);
 }
 source=tools[1];
 dynamic pin=c.Params.Input[0];pin.PersistentData.Clear();var it=gh.GetType("Grasshopper.Kernel.Types.GH_Integer");((object)pin.PersistentData).GetType().GetMethod("Append",new[]{it}).Invoke(pin.PersistentData,new[]{Activator.CreateInstance(it,new object[]{cmd.GetProperty("port").GetInt32()})});
 c.ExpireSolution(false);doc.NewSolution(false);
 AppDomain.CurrentDomain.SetData("swiftlet.gh1undo.doc",doc);AppDomain.CurrentDomain.SetData("swiftlet.gh1undo.component",c);AppDomain.CurrentDomain.SetData("swiftlet.gh1undo.source",source);
}
if(action=="wire"){sink=Activator.CreateInstance(sw.GetType("Swiftlet.Gh.Rhino8.Params.McpToolCallRequestParam"));sink.CreateAttributes();doc.AddObject(sink,false,int.MaxValue);sink.AddSource(c.Params.Output[1]);AppDomain.CurrentDomain.SetData("swiftlet.gh1undo.sink",sink);}
if(action=="tools"){
 doc.UndoUtil.RecordWireEvent("Change Tools",c.Params.Input[1]);c.Params.Input[1].RemoveSource(source.Params.Output[0]);c.ExpireSolution(false);doc.NewSolution(false);
}
if(action=="undo"){doc.Undo();doc.NewSolution(false);}
if(action=="redo"){doc.Redo();doc.NewSolution(false);}
if(action=="close"){doc.RemoveObject(c,false);doc.Dispose();Console.WriteLine("GH1UNDO_JSON:{}");return;}
var pins=new List<object>();foreach(dynamic pin in c.Params.Output)pins.Add(new{id=pin.InstanceGuid.ToString(),name=(string)pin.NickName});
Console.WriteLine("GH1UNDO_JSON:"+JsonSerializer.Serialize(new{pins,wires=sink==null?0:(int)sink.SourceCount}));
