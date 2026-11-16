using System; using System.Linq; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Text.Json; using System.IO;
var command=JsonDocument.Parse(@"__COMMAND__").RootElement;
string action=command.GetProperty("action").GetString();
Assembly A(string n)=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==n);
var gh=A("Grasshopper2");var sw=A("Swiftlet.Gh2");
dynamic doc=AppDomain.CurrentDomain.GetData("swiftlet.settings.doc");
dynamic component=AppDomain.CurrentDomain.GetData("swiftlet.settings.component");
IEnumerable<object> Children(object c){
 var controls=c.GetType().GetProperty("Controls");
 if(controls?.GetValue(c) is IEnumerable list)foreach(var child in list)yield return child;
}
IEnumerable<object> Walk(object root){yield return root;foreach(var c in Children(root))foreach(var x in Walk(c))yield return x;}
void SetInput(dynamic c,int index,object[] values,Type type){Array typed=Array.CreateInstance(type,values.Length);for(int i=0;i<values.Length;i++)typed.SetValue(values[i],i);dynamic pin=c.Parameters.Input(index);pin.Set((dynamic)typed);pin.Expire();}
if(action=="setup"){
 if(doc!=null)doc.Close();
 doc=gh.GetType("Grasshopper2.Doc.Document").GetMethod("NewActiveDocument").Invoke(null,null);
 dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);editor.Canvas.Document=doc;
 string name=command.GetProperty("component").GetString();component=Activator.CreateInstance(sw.GetTypes().Single(t=>t.Name==name));doc.Objects.Add(component);
 if(name=="CreateTextBodyComponent")SetInput(component,0,new object[]{"UI fixture"},typeof(object));
 else if(name=="SaveWebResponseComponent"){
  var type=A("Swiftlet.Core").GetType("Swiftlet.Core.Http.HttpResponseData");
  object response=Activator.CreateInstance(type,new object[]{"1.1",200,"OK",null,true,"text fixture",new byte[]{1,2,3}});
  SetInput(component,0,new[]{response},type);SetInput(component,1,new object[]{command.GetProperty("file").GetString()},typeof(string));
 }else{
  SetInput(component,0,new object[]{command.GetProperty("url").GetString()},typeof(string));
  if(name=="HttpRequestComponent")SetInput(component,1,new object[]{"GET"},typeof(string));
  if(new[]{"PostRequestComponent","PutRequestComponent","PatchRequestComponent"}.Contains(name)){
   var bodyType=A("Swiftlet.Core").GetType("Swiftlet.Core.Http.RequestBodyText");object body=Activator.CreateInstance(bodyType,new object[]{"text/plain","ui"});
   SetInput(component,1,new[]{body},A("Swiftlet.Core").GetType("Swiftlet.Core.Http.IRequestBody"));
  }
 }
 component.Expire();doc.Solution.StartWait();
 dynamic panel=Activator.CreateInstance(gh.GetType("Grasshopper2.UI.InputPanel.InputPanel"));component.AppendToInputPanel(panel);object control=panel.ToEtoControl();
 var pickers=Walk(control).Where(x=>x.GetType().FullName=="Eto.Forms.DropDown").ToArray();
 if(pickers.Length!=1)throw new Exception("Expected one actual Eto picker; found "+pickers.Length+" controls "+string.Join(",",Walk(control).Select(x=>x.GetType().Name)));
 AppDomain.CurrentDomain.SetData("swiftlet.settings.doc",doc);AppDomain.CurrentDomain.SetData("swiftlet.settings.component",component);AppDomain.CurrentDomain.SetData("swiftlet.settings.picker",pickers[0]);
}
if(action=="change"){dynamic picker=AppDomain.CurrentDomain.GetData("swiftlet.settings.picker");picker.SelectedIndex=command.GetProperty("index").GetInt32();}
if(action=="undo"){doc.Undo.Undo();}
if(action=="redo"){doc.Undo.Redo();}
string property=component.GetType().Name=="CreateTextBodyComponent"?"ContentType":component.GetType().Name=="SaveWebResponseComponent"?"SaveAsText":"TimeoutSeconds";
object value=component.GetType().GetProperty(property).GetValue(component);
var io=A("GrasshopperIO").GetType("GrasshopperIO.IO");object clone=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod).Invoke(null,new object[]{component});
object archived=clone.GetType().GetProperty(property).GetValue(clone);
dynamic data=component.State.Data;
var outputs=new List<object>();foreach(dynamic pin in component.Parameters.Outputs){var tree=data?.Tree((Guid)pin.InstanceId);outputs.Add(tree==null?null:((IEnumerable)tree.AllItems).Cast<object>().ToArray());}
Console.WriteLine("SETTINGS_JSON:"+JsonSerializer.Serialize(new{component=component.GetType().Name,phase=component.Phase.ToString(),solution=doc.Solution.State.Phase.ToString(),running=doc.Solution.State.CurrentlyRunning!=null,state=value,archived,outputs,undo=doc.Undo.FirstUndo!=null,redo=doc.Undo.FirstRedo!=null}));
