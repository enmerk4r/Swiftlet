var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper2");
dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic doc=editor.Canvas.Document;
var found=false;
foreach(dynamic c in (System.Collections.IEnumerable)doc.Objects.Forwards) if(c.InstanceId.ToString()=="__TARGET_ID__") {
 found=true;
 c.Parameters.Input(0).Set(new object[]{"kept"});c.Parameters.Input(0).Expire();
 c.Parameters.Input(1).Set(new int[]{10000});c.Parameters.Input(1).Expire();c.Expire();
 using var cts=new System.Threading.CancellationTokenSource();
 var start=((System.Reflection.MethodInfo[])doc.Solution.GetType().GetMethods()).Single(m=>m.Name=="Start"&&m.GetParameters().Length==2);
 var watch=System.Diagnostics.Stopwatch.StartNew();
 var task=(System.Threading.Tasks.Task)start.Invoke(doc.Solution,new object[]{cts,Enum.ToObject(start.GetParameters()[1].ParameterType,0)});cts.CancelAfter(300);
 try {task.GetAwaiter().GetResult();}catch(OperationCanceledException){}
 if(watch.Elapsed.TotalSeconds>3||c.Phase.ToString()!="Cancelled")throw new Exception("Throttle did not cancel promptly: "+c.Phase);
 Console.WriteLine("PASS Throttle cancellation "+watch.ElapsedMilliseconds+"ms");
}
if(!found)throw new Exception("Missing Throttle component");
