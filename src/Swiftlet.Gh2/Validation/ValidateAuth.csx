var gh2=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper2");
dynamic editor=gh2.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
var gh1=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Grasshopper");
var plug=System.Reflection.Assembly.LoadFrom(__GH1_PATH_JSON__);
dynamic doc=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document"));
dynamic legacy=Activator.CreateInstance(plug.GetType("Swiftlet.Gh.Rhino8.Components.__CLASS__"));
legacy.CreateAttributes(); doc.AddObject(legacy,false);
string[] inputs=__INPUTS_JSON__;
for(int i=0;i<inputs.Length;i++) {
 dynamic param=legacy.Params.Input[i];
 dynamic goo=Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_String"),new object[]{inputs[i]});
 param.PersistentData.Append(goo);
}
doc.NewSolution(false);
var found=false;
foreach(dynamic c in (System.Collections.IEnumerable)editor.Canvas.Document.Objects.Forwards) if(c.InstanceId.ToString()=="__TARGET_ID__") {
 found=true;
 for(int pin=0;pin<(int)legacy.Params.Output.Count;pin++) {
  var values=((System.Collections.IEnumerable)c.Parameters.Output(pin).State.Data.Tree().AllItems).Cast<object>().ToArray();
  var previous=((System.Collections.IEnumerable)legacy.Params.Output[pin].VolatileData.AllData(true)).Cast<object>().ToArray();
  if(values.Length!=1||previous.Length!=1)throw new Exception("Missing Auth output");
  dynamic actual=values[0];dynamic expected=((dynamic)previous[0]).Value;
  if(actual.Key!=expected.Key||actual.Value!=expected.Value)throw new Exception("Auth parity mismatch");
  string[] expectedPair=pin==0?__EXPECTED_HEADER__:__EXPECTED_QUERY__;
  if(actual.Key!=expectedPair[0]||actual.Value!=expectedPair[1])throw new Exception("Auth expected-value mismatch");
 }
 var io=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GrasshopperIO").GetType("GrasshopperIO.IO");
 var duplicate=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod);
 dynamic copy=duplicate.Invoke(null,new object[]{c});
 if(copy.Parameters.InputCount!=c.Parameters.InputCount||copy.Parameters.OutputCount!=c.Parameters.OutputCount)throw new Exception("Archive pin mismatch");
 Console.WriteLine("PASS __CLASS__ GH1/GH2 exact typed output + expected values + archive");
}
doc.Dispose();
if(!found)throw new Exception("Auth test target not found");
