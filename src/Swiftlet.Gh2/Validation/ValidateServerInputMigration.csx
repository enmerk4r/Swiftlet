var gh2=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2");
var gh1=System.Reflection.Assembly.LoadFrom(__GH1_PATH_JSON__);
var plugin=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh2");
dynamic source=Activator.CreateInstance(gh1.GetType("Swiftlet.Gh.Rhino8.Components.ServerInputComponent"));
dynamic side=Enum.Parse(AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper").GetType("Grasshopper.Kernel.GH_ParameterSide"),"Output");
dynamic route=source.CreateParameter(side,1);route.NickName="/api/migrated";source.Params.RegisterOutputParam(route,1);
source.NickName="Saved Server";
var g1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper");
dynamic integer=Activator.CreateInstance(g1.GetType("Grasshopper.Kernel.Types.GH_Integer"),new object[]{19477});
dynamic path=Activator.CreateInstance(g1.GetType("Grasshopper.Kernel.Data.GH_Path"),new object[]{0});
source.Params.Input[0].PersistentData.Clear();source.Params.Input[0].PersistentData.Append(integer,path);
var wrapper=Activator.CreateInstance(gh2.GetType("Grasshopper2.Interop.IGH_Component"),new object[]{source});
var migratorType=plugin.GetType("Swiftlet.Gh2.Migration.ServerInputMigration");
foreach(object migrator in new[]{Activator.CreateInstance(migratorType)})
{
    var type=migrator.GetType();if(type.Name!="ServerInputMigration")continue;
    dynamic target=type.GetMethod("Migrate").Invoke(migrator,new object[]{wrapper});
    if(target.Parameters.OutputCount!=2)throw new Exception("Variable output count lost");
    if(target.Parameters.Output(1).UserName!="/api/migrated")throw new Exception("Route name lost");
    if(target.UserName!="Saved Server")throw new Exception("Component nickname lost");
    var values=((System.Collections.IEnumerable)target.Parameters.Input(0).PersistentData.AllItems).Cast<object>().ToArray();
    if(values.Length!=1||Convert.ToInt32(values[0])!=19477)throw new Exception("Saved port lost");
    Console.WriteLine("PASS ServerInput migration preserves variable routes, saved port and nickname");
}
