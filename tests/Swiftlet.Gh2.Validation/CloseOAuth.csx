using System; using System.Linq; using System.Reflection;
var gh=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2");var sw=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Swiftlet.Gh2");
var desktop=Assembly.Load("Swiftlet.Hosts.Desktop");var headless=Assembly.Load("Swiftlet.Hosts.Headless");
var host=Activator.CreateInstance(desktop.GetType("Swiftlet.Hosts.Desktop.DesktopHostServices"),new object[]{Activator.CreateInstance(headless.GetType("Swiftlet.Hosts.Headless.ManualBrowserLauncher")),null,null,null});
dynamic doc=gh.GetType("Grasshopper2.Doc.Document").GetMethod("NewActiveDocument").Invoke(null,null);
if(__CANVAS__){dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);editor.Canvas.Document=doc;}
dynamic c=Activator.CreateInstance(sw.GetType("Swiftlet.Gh2.Components.OAuthAuthorizeComponent"),new object[]{host});doc.Objects.Add(c);
c.Parameters.Input(0).Set(new[]{"https://auth.example.test/authorize"});c.Parameters.Input(1).Set(new[]{"client"});c.Parameters.Input(3).Set(new[]{__PORT__});c.Parameters.Input(4).Set(new[]{true});
for(int i=0;i<5;i++)c.Parameters.Input(i).Expire();c.Expire();doc.Solution.StartWait();
dynamic controller=c.GetType().GetField("_controller",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(c);
if(!controller.Snapshot.IsWaiting)throw new Exception("Authorization did not start");
doc.Close();Console.WriteLine("PASS waiting OAuth document close PID="+Environment.ProcessId);
