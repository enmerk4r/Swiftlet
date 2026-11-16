var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper2");
dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic d=editor.Canvas.Document;
foreach(dynamic c in (System.Collections.IEnumerable)d.Objects.Forwards) if(c.InstanceId.ToString()=="__TARGET_ID__") {
 var previous=c.Parameters.Output(2).State.CurrentId;
 c.Parameters.Input(0).Set(new string[]{"__HTTP_BASE__/slow"});c.Parameters.Input(0).Expire();c.Expire();
 using(var cts=new System.Threading.CancellationTokenSource()) {
 var start=((System.Reflection.MethodInfo[])d.Solution.GetType().GetMethods()).Single(m=>m.Name=="Start"&&m.GetParameters().Length==2);
 var mode=Enum.ToObject(start.GetParameters()[1].ParameterType,0);
 var watch=System.Diagnostics.Stopwatch.StartNew();
 var task=(System.Threading.Tasks.Task)start.Invoke(d.Solution,new object[]{cts,mode});cts.CancelAfter(300);
 try { task.GetAwaiter().GetResult(); } catch(OperationCanceledException){}
 if(watch.Elapsed.TotalSeconds>3)throw new Exception("Cancellation was too slow");
 Console.WriteLine("CANCEL ms="+watch.ElapsedMilliseconds+" componentPhase="+c.Phase);
 var tree=c.Parameters.Output(2).State.Data?.Tree();
 Console.WriteLine("responseItems="+(tree==null?0:tree.ItemCount));
 if(c.Phase.ToString()!="Cancelled" || !previous.Equals(c.Parameters.Output(2).State.CurrentId)) throw new Exception("Canceled request published new successful data");
 Console.WriteLine("PASS in-flight cancellation; last successful cache unchanged");
 }
}