// Run through Rhino AI run_csharp in a GH2 process that has Swiftlet.Gh2 loaded.
// Checks every shared data type: persistent parameter round-trip through GrasshopperIO, Copy
// semantics, Format text, and the registered JSON conversions. Prints PASS lines; throws on failure.
using System; using System.Linq; using System.Reflection; using System.Collections;
var asm = AppDomain.CurrentDomain.GetAssemblies();
var gh = asm.First(x => x.GetName().Name == "Grasshopper2");
var io = asm.First(x => x.GetName().Name == "GrasshopperIO").GetType("GrasshopperIO.IO");
var plug = asm.First(x => x.GetName().Name == "Swiftlet.Gh2");
var core = asm.First(x => x.GetName().Name == "Swiftlet.Core");
var dup = io.GetMethods().Single(m => m.Name == "Duplicate" && !m.IsGenericMethod);

object RoundTrip(string parameterClass, object value)
{
    dynamic p = Activator.CreateInstance(plug.GetType("Swiftlet.Gh2.Parameters." + parameterClass));
    Type itemType = p.GetType().BaseType.GetGenericArguments()[0];
    var values = Array.CreateInstance(itemType, 1); values.SetValue(value, 0);
    MethodInfo setter = ((MethodInfo[])p.GetType().GetMethods()).Single(m => m.Name == "Set" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == values.GetType());
    setter.Invoke(p, new object[] { values });
    dynamic restored = dup.Invoke(null, new object[] { p });
    var items = ((IEnumerable)restored.PersistentData.AllItems).Cast<object>().ToArray();
    if (items.Length != 1) throw new Exception(parameterClass + ": expected 1 restored item, got " + items.Length);
    return items[0];
}
void Check(bool ok, string what) { if (!ok) throw new Exception("FAIL " + what); Console.WriteLine("PASS " + what); }
object Assistant(string name) => Activator.CreateInstance(plug.GetType("Swiftlet.Gh2.Types." + name));

// Byte array
var bytes = new byte[] { 0, 1, 2, 250, 255 };
var bytesBack = (byte[])RoundTrip("ByteArrayParameter", bytes);
Check(bytesBack.SequenceEqual(bytes) && !ReferenceEquals(bytesBack, bytes), "ByteArray round-trip");
dynamic ba = Assistant("ByteArrayTypeAssistant");
Check((string)ba.Format(bytes) == "BYTE ARRAY [ 5 ]", "ByteArray format");

// JSON
Type T(string n) => core.GetType("Swiftlet.Core.Json." + n);
dynamic token = T("JsonToken").GetMethod("Parse").Invoke(null, new object[] { "{\"a\":1,\"b\":[true,null,\"λ\"]}" });
dynamic tokenBack = RoundTrip("JsonNodeParameter", (object)token);
Check((string)tokenBack.Json == (string)token.Json, "JsonToken round-trip " + tokenBack.Json);
dynamic nullBack = RoundTrip("JsonNodeParameter", T("JsonToken").GetField("Null").GetValue(null));
Check((bool)nullBack.IsNull, "JsonToken JSON null survives as a value");
var tryObj = new object[] { (object)token, null }; T("JsonObjectData").GetMethod("TryFrom").Invoke(null, tryObj);
dynamic objBack = RoundTrip("JsonObjectParameter", tryObj[1]);
Check(objBack.ToString() == "JSON Object [2 keys]", "JsonObject round-trip");
dynamic arrTok = T("JsonToken").GetMethod("Parse").Invoke(null, new object[] { "[1,2,3]" });
var tryArr = new object[] { (object)arrTok, null }; T("JsonArrayData").GetMethod("TryFrom").Invoke(null, tryArr);
dynamic arrBack = RoundTrip("JsonArrayParameter", tryArr[1]);
Check(arrBack.ToString() == "JSON Array [3 items]", "JsonArray round-trip");
dynamic valBack = RoundTrip("JsonValueParameter", T("JsonValueData").GetField("Null").GetValue(null));
Check((bool)valBack.IsNull, "JsonValue null round-trip");

// JSON conversions registered with the GH2 conversion server
var conv = plug.GetType("Swiftlet.Gh2.Types.JsonConversions");
var toToken = conv.GetMethods().Single(m => m.Name == "Convert" && m.GetParameters()[0].ParameterType == T("JsonObjectData") );
var a1 = new object[] { tryObj[1], null }; toToken.Invoke(null, a1);
Check(((dynamic)a1[1]).Json == (string)token.Json, "object -> token conversion");
var toArray = conv.GetMethods().Single(m => m.Name == "Convert" && m.GetParameters()[0].ParameterType == T("JsonToken") && m.GetParameters()[1].ParameterType.GetElementType() == T("JsonArrayData"));
bool threw = false; try { toArray.Invoke(null, new object[] { (object)token, null }); } catch (TargetInvocationException) { threw = true; }
Check(threw, "token(object) -> array conversion is rejected");

// Request body + multipart
Type H(string n) => core.GetType("Swiftlet.Core.Http." + n);
dynamic text = Activator.CreateInstance(H("RequestBodyText"), "application/json", "{\"k\":\"λ\"}");
dynamic textBack = RoundTrip("RequestBodyParameter", (object)text);
Check(textBack.GetType().Name == "RequestBodyText" && (string)textBack.Text == (string)text.Text && (string)textBack.ContentType == "application/json", "RequestBodyText round-trip");
dynamic raw = Activator.CreateInstance(H("RequestBodyBytes"), "application/octet-stream", bytes);
dynamic rawBack = RoundTrip("RequestBodyParameter", (object)raw);
Check(rawBack.GetType().Name == "RequestBodyBytes" && ((byte[])rawBack.ToByteArray()).SequenceEqual(bytes), "RequestBodyBytes round-trip");
dynamic fieldText = Activator.CreateInstance(H("MultipartField"), "note", "hello λ", null, null);
dynamic fieldBytes = Activator.CreateInstance(H("MultipartField"), "file", bytes, "a.bin", "application/x-test");
dynamic ftBack = RoundTrip("MultipartFieldParameter", (object)fieldText);
Check((bool)ftBack.IsText && (string)ftBack.Text == "hello λ" && (string)ftBack.Name == "note", "MultipartField text round-trip");
dynamic fbBack = RoundTrip("MultipartFieldParameter", (object)fieldBytes);
Check(!(bool)fbBack.IsText && (string)fbBack.FileName == "a.bin" && (string)fbBack.ContentType == "application/x-test" && ((byte[])fbBack.Bytes).SequenceEqual(bytes), "MultipartField bytes round-trip");
var fieldList = Array.CreateInstance(H("MultipartField"), 2); fieldList.SetValue((object)fieldText, 0); fieldList.SetValue((object)fieldBytes, 1);
dynamic multi = H("RequestBodyMultipartForm").GetConstructors().Single(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.IsGenericType && c.GetParameters()[0].ParameterType.GetGenericArguments()[0] == H("MultipartField")).Invoke(new object[] { fieldList });
dynamic multiBack = RoundTrip("RequestBodyParameter", (object)multi);
Check(multiBack.GetType().Name == "RequestBodyMultipartForm" && (int)multiBack.Fields.Count == 2 && (string)multiBack.Fields[1].FileName == "a.bin", "RequestBodyMultipartForm round-trip");

// HTML / XML nodes
var hap = asm.First(x => x.GetName().Name == "HtmlAgilityPack");
dynamic hdoc = Activator.CreateInstance(hap.GetType("HtmlAgilityPack.HtmlDocument"));
hdoc.LoadHtml("<div id=\"x\"><p>hi</p></div>");
dynamic div = hdoc.DocumentNode.ChildNodes[0];
dynamic divBack = RoundTrip("HtmlNodeParameter", (object)div);
Check((string)divBack.Name == "div" && (string)divBack.OuterHtml == (string)div.OuterHtml, "HtmlNode round-trip");
var xdoc = new System.Xml.XmlDocument(); xdoc.LoadXml("<root a=\"1\"><child>t</child></root>");
dynamic xBack = RoundTrip("XmlNodeParameter", xdoc.DocumentElement);
Check((string)xBack.Name == "root" && (string)xBack.OuterXml == xdoc.DocumentElement.OuterXml, "XmlNode round-trip");
// Nodes that are not elements (what XPath or Get Child Nodes can output) keep their kind and content.
var mixed = new System.Xml.XmlDocument(); mixed.LoadXml("<r xmlns:p=\"urn:p\" a=\"1\" p:b=\"2\">hello<!--c--><![CDATA[x<y]]><?pi d?></r>");
var ns = new System.Xml.XmlNamespaceManager(mixed.NameTable); ns.AddNamespace("p", "urn:p");
foreach (string xpath in new[] { "text()", "@a", "@p:b", "comment()", "processing-instruction()" })
{
    System.Xml.XmlNode node = mixed.DocumentElement.SelectSingleNode(xpath, ns);
    var back = (System.Xml.XmlNode)RoundTrip("XmlNodeParameter", node);
    Check(back.NodeType == node.NodeType && back.Name == node.Name && back.NamespaceURI == node.NamespaceURI && back.Value == node.Value, "XmlNode " + node.NodeType + " " + node.Name + " round-trip");
}
var cdataNode = mixed.DocumentElement.ChildNodes.Cast<System.Xml.XmlNode>().Single(n => n.NodeType == System.Xml.XmlNodeType.CDATA);
var cdataBack = (System.Xml.XmlNode)RoundTrip("XmlNodeParameter", cdataNode);
Check(cdataBack.NodeType == System.Xml.XmlNodeType.CDATA && cdataBack.Value == "x<y", "XmlNode CDATA round-trip");
Console.WriteLine("ALL TYPE CHECKS PASSED");
