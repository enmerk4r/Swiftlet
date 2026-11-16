// Replace __PLUGIN_PATH_JSON__ with a JSON-escaped C# string literal via the caller.
// This changes only this Rhino process's scoped plugin set. It does not install a user-wide custom plugin.
string path = __PLUGIN_PATH_JSON__;
if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException("GH2 .rhp not found", path);
var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Grasshopper2");
var server = assembly.GetType("Grasshopper2.Framework.PluginServer");
var scoped = server.GetMethod("ScopePlugin").Invoke(null, new object[] { path });
var load = server.GetMethods().Single(m => m.Name == "LoadPlugin" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string));
object[] arguments = { path, null };
bool loaded = (bool)load.Invoke(null, arguments);
var failure = new Dictionary<string, string>();
if (arguments[1] != null)
    foreach (var property in arguments[1].GetType().GetProperties())
        if (property.GetIndexParameters().Length == 0) failure[property.Name] = property.GetValue(arguments[1])?.ToString();
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scoped, loaded, failure }));
if (!loaded) throw new InvalidOperationException("GH2 plugin did not load; see failure details");
