// Run through Rhino AI run_csharp in a process with Swiftlet.Gh2 loaded and GH2 open.
// The ten pins that are optional-with-default in Grasshopper 1 must still let the component run,
// using the same fallback value, when the pin has no data at all (its default was cleared).
using System; using System.Linq; using System.Collections; using System.Collections.Generic;
var asm = AppDomain.CurrentDomain.GetAssemblies();
var gh = asm.First(x => x.GetName().Name == "Grasshopper2");
var plug = asm.First(x => x.GetName().Name == "Swiftlet.Gh2");
dynamic editor = gh.GetType("Grasshopper2.UI.Editor", true).GetProperty("Instance").GetValue(null) ?? throw new InvalidOperationException("GH2 editor not running");
dynamic doc = editor.Canvas.Document;
var added = new List<object>();
int failures = 0;
dynamic Add(string cls) { dynamic c = Activator.CreateInstance(plug.GetTypes().First(t => t.Name == cls)); doc.Objects.Add(c); added.Add(c); return c; }
void SetIn(dynamic c, int i, object values) { dynamic p = c.Parameters.Input(i); p.Set((dynamic)values); p.Expire(); c.Expire(); }
object[] Out(dynamic c, int i) => ((IEnumerable)c.Parameters.Output(i).State.Data.Tree().AllItems).Cast<object>().ToArray();
void Check(bool ok, string what) { Console.WriteLine((ok ? "PASS " : "FAIL ") + what); if (!ok) failures++; }
object HtmlDoc()
{
    var hap = asm.First(a => a.GetName().Name == "HtmlAgilityPack");
    dynamic d = Activator.CreateInstance(hap.GetType("HtmlAgilityPack.HtmlDocument", true));
    d.LoadHtml("<div data-k=\"1\" class=\"y\"><p data-k=\"1\" class=\"y\">deep</p></div>");
    var nodes = Array.CreateInstance(hap.GetType("HtmlAgilityPack.HtmlNode", true), 1); nodes.SetValue((object)d.DocumentNode, 0);
    return nodes;
}

// Recursive defaults to true: the nested <p> is only found by a recursive search from the document node.
dynamic byAttr = Add("GetElementsByAttributeValueComponent"); SetIn(byAttr, 0, HtmlDoc()); SetIn(byAttr, 1, new[] { "data-k" }); SetIn(byAttr, 2, new[] { "1" }); SetIn(byAttr, 3, new bool[0]);
dynamic byClass = Add("GetElementsByClassNameComponent"); SetIn(byClass, 0, HtmlDoc()); SetIn(byClass, 1, new[] { "y" }); SetIn(byClass, 2, new bool[0]);
dynamic byTag = Add("GetElementsByTagNameComponent"); SetIn(byTag, 0, HtmlDoc()); SetIn(byTag, 1, new[] { "p" }); SetIn(byTag, 2, new bool[0]);
dynamic createCsv = Add("CreateCsvLineComponent"); SetIn(createCsv, 0, new[] { "a", "b" }); SetIn(createCsv, 1, new string[0]);
dynamic readCsv = Add("ReadCsvLineComponent"); SetIn(readCsv, 0, new[] { "a,b" }); SetIn(readCsv, 1, new string[0]);
dynamic toText = Add("ByteArrayToTextComponent"); SetIn(toText, 0, new[] { System.Text.Encoding.UTF8.GetBytes("hλ") }); SetIn(toText, 1, new string[0]);
dynamic qr = Add("GenerateQrCodeComponent"); SetIn(qr, 0, new[] { "swiftlet" });
foreach (int i in new[] { 1, 2, 3, 4 }) { dynamic p = qr.Parameters.Input(i); var t = ((object)p).GetType().BaseType.GetGenericArguments()[0]; p.Set((dynamic)Array.CreateInstance(t, 0)); p.Expire(); }
qr.Expire();
dynamic qrDefault = Add("GenerateQrCodeComponent"); SetIn(qrDefault, 0, new[] { "swiftlet" });
doc.Solution.StartWait();

Check(Out(byAttr, 0).Length == 2, "ByAttributeValue with no Recursive data searches recursively: " + Out(byAttr, 0).Length);
Check(Out(byClass, 0).Length == 2, "ByClassName with no Recursive data searches recursively: " + Out(byClass, 0).Length);
Check(Out(byTag, 0).Length == 1, "ByTagName with no Recursive data searches recursively: " + Out(byTag, 0).Length);
Check(Out(createCsv, 0).Length == 1 && (string)Out(createCsv, 0)[0] == "a,b", "CreateCsvLine with no Delimiter data uses a comma");
Check(Out(readCsv, 0).Length == 2, "ReadCsvLine with no Delimiter data uses a comma: " + Out(readCsv, 0).Length);
Check(Out(toText, 0).Length == 1 && (string)Out(toText, 0)[0] == "hλ", "ByteArrayToText with no Encoding data uses UTF8");
dynamic a = Out(qr, 0).FirstOrDefault(), b = Out(qrDefault, 0).FirstOrDefault();
Check(a != null && b != null && a.Width == b.Width && ((byte[])a.GetPixelBytes()).AsSpan().SequenceEqual((byte[])b.GetPixelBytes()),
    "GenerateQrCode with no data on its four optional pins equals the default-valued one");
foreach (dynamic o in added) doc.Objects.Remove(o);
Console.WriteLine(failures == 0 ? "ALL OPTIONAL-DEFAULT CHECKS PASSED" : failures + " FAILED");
if (failures != 0) throw new Exception(failures + " optional-default checks failed");
