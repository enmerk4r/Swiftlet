var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper2");
dynamic editor=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
var io=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="GrasshopperIO").GetType("GrasshopperIO.IO");
var dup=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod);
var plug=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Swiftlet.Gh2");
foreach(dynamic c in (System.Collections.IEnumerable)editor.Canvas.Document.Objects.Forwards) if(c.InstanceId.ToString()=="__TARGET_ID__") {
 var values=((System.Collections.IEnumerable)c.Parameters.Output(2).State.Data.Tree().AllItems).Cast<object>().ToArray();
 if(values.Length!=1)throw new Exception("Missing GET response"); dynamic response=values[0]; if(response.StatusCode!=200)throw new Exception("GET failed");
 using(var json=System.Text.Json.JsonDocument.Parse((string)response.Content)) { if(json.RootElement.GetProperty("path").GetString()!=__EXPECT_PATH_JSON__ || json.RootElement.GetProperty("header").GetString()!=__EXPECT_HEADER_JSON__) throw new Exception("HTTP fixture content mismatch"); } Console.WriteLine("PASS exact GET result: "+response.Content);
 c.TimeoutSeconds=30; dynamic copy=dup.Invoke(null,new object[]{c}); if(copy.TimeoutSeconds!=30)throw new Exception("Timeout lost"); Console.WriteLine("PASS saved timeout roundtrip");
 dynamic p=Activator.CreateInstance(plug.GetType("Swiftlet.Gh2.Parameters.HttpResponseDataParameter")); var arr=Array.CreateInstance(response.GetType(),1);arr.SetValue(response,0);
 ((System.Reflection.MethodInfo[])p.GetType().GetMethods()).Single(m=>m.Name=="Set"&&m.GetParameters()[0].ParameterType==arr.GetType()).Invoke(p,new object[]{arr});
 dynamic restored=dup.Invoke(null,new object[]{p}); dynamic rr=((System.Collections.IEnumerable)restored.PersistentData.AllItems).Cast<object>().Single();
 if(rr.Content!=response.Content||rr.StatusCode!=response.StatusCode||!((byte[])rr.Bytes).SequenceEqual((byte[])response.Bytes))throw new Exception("Response changed");
 Console.WriteLine("PASS response persistence including bytes");
 
}