using System;using System.Linq;using System.Reflection;using System.Collections;
var assemblies=AppDomain.CurrentDomain.GetAssemblies();
var gh2=assemblies.Single(a=>a.GetName().Name=="Grasshopper2");
var plugin=assemblies.Single(a=>a.GetName().Name=="Swiftlet.Gh2");
var core=assemblies.Single(a=>a.GetName().Name=="Swiftlet.Core");
var runtime=assemblies.Single(a=>a.GetName().Name=="Swiftlet.Runtime");
var io=assemblies.Single(a=>a.GetName().Name=="GrasshopperIO").GetType("GrasshopperIO.IO");
var duplicate=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod);
Type M(string name)=>core.GetType("Swiftlet.Core.Mcp."+name,true);
object RoundTrip(string parameter,object value)
{
 dynamic p=Activator.CreateInstance(plugin.GetType("Swiftlet.Gh2.Parameters."+parameter));
 Type itemType=p.GetType().BaseType.GetGenericArguments()[0];
 var items=Array.CreateInstance(itemType,1);items.SetValue(value,0);
 var setter=((MethodInfo[])p.GetType().GetMethods()).Single(m=>m.Name=="Set"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==items.GetType());
 setter.Invoke(p,new object[]{items});dynamic restored=duplicate.Invoke(null,new object[]{p});
 var result=((IEnumerable)restored.PersistentData.AllItems).Cast<object>().ToArray();
 if(result.Length!=1)throw new Exception(parameter+" lost persistent value");return result[0];
}
string Json(object value)=>(string)value.GetType().GetMethod("ToJson").Invoke(value,null).GetType().GetMethod("ToJsonString",new[]{typeof(System.Text.Json.JsonSerializerOptions)}).Invoke(value.GetType().GetMethod("ToJson").Invoke(value,null),new object[]{null});
var values=new object[]{
 Activator.CreateInstance(M("McpTextContentBlock"),new object[]{"hello λ"}),
 Activator.CreateInstance(M("McpImageContentBlock"),new object[]{"image/png","AAEC/v8="}),
 Activator.CreateInstance(M("McpResourceLinkContentBlock"),new object[]{"file:///a","name","title","description","text/plain"}),
 Activator.CreateInstance(M("McpEmbeddedTextResourceContentBlock"),new object[]{"file:///a.txt","text λ","text/plain"}),
 Activator.CreateInstance(M("McpEmbeddedBinaryResourceContentBlock"),new object[]{"file:///a.bin",new byte[]{0,1,2,254,255},"application/octet-stream"})};
foreach(object value in values)
{
 object restored=RoundTrip("McpContentBlockParameter",value);
 if(Json(value)!=Json(restored)||value.GetType()!=restored.GetType())throw new Exception("Content archive mismatch "+value.GetType());
 Console.WriteLine("PASS "+value.GetType().Name+" exact content archive");
}
object parameter=Activator.CreateInstance(M("McpToolParameter"),new object[]{"n","int","description λ",false});
dynamic restoredParameter=RoundTrip("McpToolParameterParameter",parameter);
if(restoredParameter.Name!="n"||restoredParameter.Type!="integer"||restoredParameter.Description!="description λ"||restoredParameter.Required)throw new Exception("Parameter archive mismatch");
Console.WriteLine("PASS MCP parameter normalization and archive");
var parameters=Array.CreateInstance(M("McpToolParameter"),2);parameters.SetValue(parameter,0);parameters.SetValue(parameter,1);
object definition=Activator.CreateInstance(M("McpToolDefinition"),new object[]{"count","description",parameters});
dynamic restoredDefinition=RoundTrip("McpToolDefinitionParameter",definition);
if(((IEnumerable)restoredDefinition.Parameters).Cast<object>().Count()!=2||restoredDefinition.Name!="count"||restoredDefinition.Description!="description")throw new Exception("Tool archive lost duplicate parameter entries");
if(Json(definition)!=Json(restoredDefinition))throw new Exception("Tool schema changed");
Console.WriteLine("PASS MCP tool archive retains ordered duplicate parameter entries");
// Native image component shares the exact Runtime encoder with GH1.
var image=assemblies.Single(a=>a.GetName().Name=="Swiftlet.Imaging");
object bitmap=Activator.CreateInstance(image.GetType("Swiftlet.Imaging.SwiftletImage"),new object[]{1,1,new byte[]{255,0,0,255}});
var create=runtime.GetType("Swiftlet.Runtime.McpContentFactory").GetMethod("CreateImage");
foreach(string format in new[]{"png","jpeg","gif","tiff","bmp"})
{
 dynamic block=create.Invoke(null,new object[]{bitmap,format});
 if(string.IsNullOrEmpty(block.Data)||!block.MimeType.StartsWith("image/"))throw new Exception("MCP image encoding failed "+format);
}
Console.WriteLine("PASS MCP image encoding supports all five formats");
