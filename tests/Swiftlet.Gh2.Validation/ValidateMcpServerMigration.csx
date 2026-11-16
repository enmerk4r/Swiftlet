var gh1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper");
var gh2=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2");
var sw1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh.Rhino8");
var sw2=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh2");
var type=sw1.GetType("Swiftlet.Gh.Rhino8.Components.McpServerComponent");
dynamic original=Activator.CreateInstance(type);original.CreateAttributes();original.NickName="Saved MCP Server";
foreach(string name in new[]{"echo","second_tool"})
{
    dynamic output=Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Params.McpToolCallRequestParam"));
    output.Name=name;output.NickName=name;original.Params.RegisterOutputParam(output);
}
dynamic integer=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_Integer"),new object[]{19478});
dynamic path=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Data.GH_Path"),new object[]{0});
original.Params.Input[0].PersistentData.Clear();original.Params.Input[0].PersistentData.Append(integer,path);
var io1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GH_IO");
dynamic chunk=Activator.CreateInstance(io1.GetType("GH_IO.Serialization.GH_LooseChunk"),new object[]{"SavedMcp"});
if(!(bool)original.Write(chunk))throw new Exception("GH1 Write failed");
dynamic saved=Activator.CreateInstance(type);saved.CreateAttributes();if(!(bool)saved.Read(chunk))throw new Exception("GH1 Read failed");
var wrapper=Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Component"),new object[]{saved});
dynamic migrator=Activator.CreateInstance(sw2.GetType("Swiftlet.Gh2.Migration.McpServerMigration"));
dynamic target=migrator.Migrate((dynamic)wrapper);
if(target.Parameters.OutputCount!=2||target.Parameters.Output(0).UserName!="echo"||target.Parameters.Output(1).UserName!="second_tool")throw new Exception("Saved dynamic tool names lost");
if(target.UserName!="Saved MCP Server")throw new Exception("Nickname lost");
var values=((System.Collections.IEnumerable)target.Parameters.Input(0).PersistentData.AllItems).Cast<object>().ToArray();
if(values.Length!=1||Convert.ToInt32(values[0])!=19478)throw new Exception("Saved port lost");
Console.WriteLine("PASS MCP Server saved GH1 archive migration preserves dynamic tools, port and nickname");

dynamic whole=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document"));whole.AddObject(saved,false);
dynamic archive=Activator.CreateInstance(io1.GetType("GH_IO.Serialization.GH_Archive"));archive.AppendObject(whole,"Definition");
dynamic roundtrip=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document"));archive.ExtractObject(roundtrip,"Definition");
var documentWrapper=Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Document"),new object[]{roundtrip});
var convert=gh2.GetType("Grasshopper2.Doc.DocumentIO").GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Instance).First(m=>m.Name=="Convert1");
dynamic doc2=convert.Invoke(convert.IsStatic?null:Activator.CreateInstance(convert.DeclaringType,true),new[]{documentWrapper,Enum.Parse(gh2.GetType("Grasshopper2.Doc.Migration.MigrationLevel"),"Total")});
dynamic server=((System.Collections.IEnumerable)doc2.Objects.Forwards).Cast<object>().Single(o=>o.GetType().FullName=="Swiftlet.Gh2.Components.McpServerComponent");
if(server.Parameters.OutputCount!=2||server.Parameters.Output(0).UserName!="echo"||server.Parameters.Output(1).UserName!="second_tool")throw new Exception("Whole-file conversion lost dynamic tool output names");
var portValues=((System.Collections.IEnumerable)server.Parameters.Input(0).PersistentData.AllItems).Cast<object>().ToArray();
if(portValues.Length!=1||Convert.ToInt32(portValues[0])!=19478)throw new Exception("Whole-file conversion lost saved port");
Console.WriteLine("PASS MCP Server whole-document saved archive conversion preserves dynamic tool names and port");
doc2.Close();roundtrip.Dispose();whole.Dispose();
