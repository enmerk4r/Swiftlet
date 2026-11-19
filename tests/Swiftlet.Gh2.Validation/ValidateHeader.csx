try {
var gh=AppDomain.CurrentDomain.GetAssemblies().First(x=>x.GetName().Name=="Grasshopper2");
var io=AppDomain.CurrentDomain.GetAssemblies().First(x=>x.GetName().Name=="GrasshopperIO").GetType("GrasshopperIO.IO");
var plug=AppDomain.CurrentDomain.GetAssemblies().First(x=>x.GetName().Name=="Swiftlet.Gh2");
var docs=(System.Collections.IEnumerable)gh.GetType("Grasshopper2.Doc.Document").GetProperty("AllDocuments").GetValue(null);
foreach(dynamic d in docs) foreach(dynamic c in (System.Collections.IEnumerable)d.Objects.Forwards) if(c.GetType().FullName=="Swiftlet.Gh2.Components.CreateHttpHeaderComponent") {
 dynamic tree=c.Parameters.Output(0).State.Data.Tree();
 var items=((System.Collections.IEnumerable)tree.AllItems).Cast<object>().ToArray();
 if(items.Length!=1) throw new Exception("Expected one header");
 dynamic h=items[0]; if(h.Key!="X-Swiftlet"||h.Value!="hello: unicode \u03bb")throw new Exception("Header mismatch");
 Console.WriteLine("PASS exact typed output "+h.Key+"="+h.Value);
 dynamic p=Activator.CreateInstance(plug.GetType("Swiftlet.Gh2.Parameters.HttpHeaderParameter"));
 var values=Array.CreateInstance(h.GetType(),1); values.SetValue(h,0);
 var set=p.GetType().GetMethods();
 System.Reflection.MethodInfo setter=((System.Reflection.MethodInfo[])set).Single(m=>m.Name=="Set"&&m.GetParameters()[0].ParameterType==values.GetType());
 setter.Invoke(p,new object[]{values});
 var dup=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod);
 dynamic restored=dup.Invoke(null,new object[]{p});
 var restoredItems=((System.Collections.IEnumerable)restored.PersistentData.AllItems).Cast<object>().ToArray();
 if(restoredItems.Length!=1)throw new Exception("Persistent header lost");
 dynamic rh=restoredItems[0]; if(rh.Key!=h.Key||rh.Value!=h.Value)throw new Exception("Persistent header changed");
 Console.WriteLine("PASS persistent custom parameter roundtrip");
 dynamic restoredComponent=dup.Invoke(null,new object[]{c});
 if(restoredComponent.Parameters.InputCount!=2||restoredComponent.Parameters.OutputCount!=1)throw new Exception("Pins changed");
 Console.WriteLine("PASS component serialization clone");
 var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"swiftlet-astra-header-document.bin");
 io.GetMethod("WriteStorableToFile").Invoke(null,new object[]{d,path});
 dynamic restoredDocument=io.GetMethods().Single(m=>m.Name=="ReadStorableFromFile"&&m.IsGenericMethod).MakeGenericMethod(gh.GetType("Grasshopper2.Doc.Document")).Invoke(null,new object[]{path});
 if(restoredDocument.Objects.Count!=d.Objects.Count)throw new Exception("Document count changed");
 Console.WriteLine("PASS document file write/read "+path); return;
}
} catch(Exception e) { Console.WriteLine(e.ToString()); throw; }