var gh1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper");
var gh2=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2");
var sw1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh.Rhino8");
dynamic doc1=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document"));
dynamic legacy=Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Components.McpToolResponseComponent_ARCHIVED"));legacy.CreateAttributes();legacy.NickName="Legacy exact response";doc1.AddObject(legacy,false);
for(int index=0;index<3;index++)
{
    dynamic source=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Parameters.Param_GenericObject"));
    source.CreateAttributes();dynamic value=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_String"),new object[]{"wire_"+index});source.PersistentData.Append(value);source.NickName="legacy_source_"+index;doc1.AddObject(source,false);legacy.Params.Input[index].AddSource(source);
}
dynamic sink=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Parameters.Param_GenericObject"));sink.CreateAttributes();sink.NickName="legacy_success_sink";doc1.AddObject(sink,false);sink.AddSource(legacy.Params.Output[0]);
var ghio=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GH_IO");dynamic archive=Activator.CreateInstance(ghio.GetType("GH_IO.Serialization.GH_Archive"));
if(!(bool)archive.AppendObject(doc1,"Definition"))throw new Exception("Append GH1 archive failed");
dynamic saved=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document"));if(!(bool)archive.ExtractObject(saved,"Definition"))throw new Exception("Extract GH1 archive failed");
object wrapped=Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Document"),new object[]{saved});
var convert=gh2.GetType("Grasshopper2.Doc.DocumentIO").GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Instance).First(m=>m.Name=="Convert1");
dynamic migrated=convert.Invoke(convert.IsStatic?null:Activator.CreateInstance(convert.DeclaringType,true),new[]{wrapped,Enum.Parse(gh2.GetType("Grasshopper2.Doc.Migration.MigrationLevel"),"Total")});
var objects=((System.Collections.IEnumerable)migrated.Objects.Forwards).Cast<object>().ToArray();
dynamic target=objects.Single(o=>o.GetType().FullName=="Swiftlet.Gh2.Components.McpToolResponseComponent_ARCHIVED");
if(!target.Obsolete||target.Nomen.Rank.ToString()!="Hidden")throw new Exception("Legacy component is visible or not obsolete");
if(target.Parameters.InputCount!=3||target.Parameters.OutputCount!=1)throw new Exception("Legacy pin counts changed");
for(int index=0;index<3;index++)
{
    dynamic source=objects.Single(o=>o.GetType().Name=="GenericParameter"&&((System.Collections.IEnumerable)((dynamic)o).PersistentDataWeak.AllItems).Cast<object>().Any(value=>value?.ToString()=="wire_"+index));
    if(target.Parameters.Input(index).Inputs.Count!=1||target.Parameters.Input(index).Inputs[0]!=source.InstanceId)throw new Exception("Legacy source wire changed at pin "+index);
}
dynamic newSink=objects.Single(o=>o.GetType().Name=="GenericParameter"&&(int)((dynamic)o).Inputs.Count==1);
if(newSink.Inputs.Count!=1||newSink.Inputs[0]!=target.Parameters.Output(0).InstanceId)throw new Exception("Legacy success wire changed");
Console.WriteLine("PASS archived MCP response whole-document migration keeps all three ordered input wires, success output wire, Hidden rank and Obsolete flag");
migrated.Close();saved.Dispose();doc1.Dispose();

