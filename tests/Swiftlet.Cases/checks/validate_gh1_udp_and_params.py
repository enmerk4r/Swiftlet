"""Live check, in a slot with both shells loaded (cases.py --load):

1. The Grasshopper 1 UDP Listener still works after moving its socket loop into the shared
   Swiftlet.Runtime.UdpListenerSession (receive, self-recompute, port release on removal).
2. Every free-floating Grasshopper 1 Swiftlet parameter with a ParameterMigrationTable rule migrates to
   its Grasshopper 2 parameter.

    python tests/Swiftlet.Cases/checks/validate_gh1_udp_and_params.py --port <slot port>
"""
import argparse, json, socket, sys, time, urllib.request, uuid

ap = argparse.ArgumentParser()
ap.add_argument('--port', type=int, required=True)
args = ap.parse_args()
failures = []


def cs(script):
    body = {"jsonrpc": "2.0", "id": str(uuid.uuid4()), "method": "tools/call", "params": {"name": "run_csharp", "arguments": {"script": script}}}
    req = urllib.request.Request(f"http://127.0.0.1:{args.port}/", data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream"})
    with urllib.request.urlopen(req, timeout=300) as r:
        raw = r.read().decode()
    if raw.lstrip().startswith(("event:", "data:")):
        raw = next(x[5:].strip() for x in raw.splitlines() if x.startswith("data:"))
    text = "".join(c.get("text", "") for c in json.loads(raw)["result"].get("content", []) if c.get("type") == "text")
    payload = json.loads(text.splitlines()[-1])
    if payload.get("stderr"):
        raise RuntimeError(payload["stderr"])
    return payload.get("stdout", "").strip()


def check(ok, what):
    print(("PASS " if ok else "FAIL ") + what, flush=True)
    if not ok:
        failures.append(what)


def can_bind_udp(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
            s.bind(("0.0.0.0", port))
        return True
    except OSError:
        return False


with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
    s.bind(("127.0.0.1", 0))
    udp_port = s.getsockname()[1]

GH1 = r'''
using System; using System.Linq;
using Grasshopper.Kernel; using Grasshopper.Kernel.Types; using Grasshopper.Kernel.Parameters;
var sw1 = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Swiftlet.Gh.Rhino8");
var canvas = Grasshopper.Instances.ActiveCanvas ?? throw new InvalidOperationException("GH1 not open");
GH_Document doc = canvas.Document;
if (doc == null) { doc = new GH_Document(); Grasshopper.Instances.DocumentServer.AddDocument(doc); canvas.Document = doc; }
IGH_Component Udp() => doc.Objects.OfType<IGH_Component>().FirstOrDefault(c => c.GetType().Name == "UdpListenerComponent");
'''

cs(GH1 + r'''
var comp = (IGH_Component)Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Components.UdpListenerComponent", true));
comp.CreateAttributes(); doc.AddObject(comp, false);
var port = (Param_Integer)comp.Params.Input[0]; port.PersistentData.Append(new GH_Integer(%d));
doc.NewSolution(false, GH_SolutionMode.Silent);
''' % udp_port)
check(not can_bind_udp(udp_port), "GH1 UDP Listener holds its port")

with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
    s.sendto(bytes([9, 8, 7]), ("127.0.0.1", udp_port))

READ = GH1 + r'''
var c = Udp();
var data = c.Params.Output[0].VolatileData.AllData(true).Select(g => string.Join(",", (byte[])g.GetType().GetProperty("Value").GetValue(g))).FirstOrDefault();
Console.WriteLine((data ?? "") + ";" + string.Join("|", c.Params.Output[1].VolatileData.AllData(true).Select(g => g.ToString())));
'''
end = time.time() + 10
got = cs(READ)
while not got.startswith("9,8,7") and time.time() < end:
    time.sleep(0.25)
    got = cs(READ)
check(got.startswith("9,8,7;127.0.0.1:"), "GH1 UDP Listener receives a datagram and recomputes itself: " + got)

cs(GH1 + 'doc.RemoveObject(Udp(), false);')
end = time.time() + 10
while not can_bind_udp(udp_port) and time.time() < end:
    time.sleep(0.25)
check(can_bind_udp(udp_port), "GH1 UDP Listener releases its port when removed")

rows = json.loads(cs(r'''
using System; using System.Linq; using System.Collections.Generic; using System.Reflection; using System.Text.Json;
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == n);
var gh1 = A("Grasshopper"); var gh2 = A("Grasshopper2"); var sw1 = A("Swiftlet.Gh.Rhino8"); var sw2 = A("Swiftlet.Gh2");
var migParamT = gh2.GetType("Grasshopper2.Doc.Migration.IMigrateParameter", true);
var wrapT = gh2.GetType("Grasshopper2.Interop.IGH_Param", true);
var paramBase = gh1.GetType("Grasshopper.Kernel.IGH_Param", true);
var byGuid = new Dictionary<Guid, Type>();
foreach (var t in sw1.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "Swiftlet.Gh.Rhino8.Params" && paramBase.IsAssignableFrom(t)))
    byGuid[(Guid)t.GetProperty("ComponentGuid").GetValue(Activator.CreateInstance(t))] = t;
var table = Activator.CreateInstance(sw2.GetType("Swiftlet.Gh2.Migration.ParameterMigrationTable", true));
var rows = new List<object>();
foreach (var m in (System.Collections.IEnumerable)table.GetType().GetMethod("CreateMigrators").Invoke(table, null))
{
    var id = (Guid)new[] { migParamT }.Concat(migParamT.GetInterfaces()).Select(i => i.GetProperty("Grasshopper1Id")).First(p => p != null).GetValue(m);
    var row = new Dictionary<string, string> { ["guid"] = id.ToString() };
    try
    {
        if (!byGuid.TryGetValue(id, out var t1)) throw new Exception("no GH1 parameter with this GUID");
        dynamic src = Activator.CreateInstance(t1); src.CreateAttributes();
        row["gh1"] = t1.Name;
        var wrapper = Activator.CreateInstance(wrapT, new object[] { src });
        dynamic target = migParamT.GetMethod("Migrate").Invoke(m, new[] { wrapper });
        row["gh2"] = ((object)target).GetType().Name;
    }
    catch (Exception e) { var x = e is TargetInvocationException ti && ti.InnerException != null ? ti.InnerException : e; row["error"] = x.GetType().Name + ": " + x.Message; }
    rows.Add(row);
}
Console.WriteLine(JsonSerializer.Serialize(new { rows, gh1Params = byGuid.Values.Select(t => t.Name).OrderBy(n => n).ToArray() }));
''').splitlines()[-1])
covered = set()
for row in rows["rows"]:
    ok = "error" not in row and row.get("gh2", "")[:-len("eter")] == row.get("gh1")
    covered.add(row.get("gh1"))
    check(ok, "parameter migration %s -> %s%s" % (row.get("gh1"), row.get("gh2"), "" if ok else " " + row.get("error", "")))
print("GH1 parameters without a rule in ParameterMigrationTable (expected: those handled by the Server tables): %s"
      % sorted(set(rows["gh1Params"]) - covered))
print()
print("ALL CHECKS PASSED" if not failures else "%d FAILED: %s" % (len(failures), failures))
sys.exit(1 if failures else 0)
