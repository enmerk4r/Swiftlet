"""Live check of the GH2 network components: HTTP Listener, UDP Listener, WebSocket Server / Client / Send.

    python tests/Swiftlet.Cases/checks/validate_network.py --port <slot port>

Needs a slot with Swiftlet.Gh2 loaded (cases.py --load) and GH2 open. The script places the components
on the GH2 canvas, talks to them over real sockets from this process, checks their outputs, deletes
them and verifies that every port has been released. It removes what it adds.
"""
import argparse, json, socket, sys, time, urllib.request, uuid

ap = argparse.ArgumentParser()
ap.add_argument('--port', type=int, required=True)
args = ap.parse_args()


def call(tool, arguments, timeout=300):
    body = {"jsonrpc": "2.0", "id": str(uuid.uuid4()), "method": "tools/call", "params": {"name": tool, "arguments": arguments}}
    req = urllib.request.Request(f"http://127.0.0.1:{args.port}/", data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        raw = r.read().decode()
    if raw.lstrip().startswith(("event:", "data:")):
        raw = next(x[5:].strip() for x in raw.splitlines() if x.startswith("data:"))
    d = json.loads(raw)
    if "error" in d:
        raise RuntimeError(d["error"])
    return "".join(c.get("text", "") for c in d["result"].get("content", []) if c.get("type") == "text")


def cs(script):
    text = call("run_csharp", {"script": PRELUDE + script})
    payload = json.loads(text.splitlines()[-1])
    if payload.get("stderr"):
        raise RuntimeError(payload["stderr"])
    return payload.get("stdout", "")


def free_port(kind=socket.SOCK_STREAM):
    with socket.socket(socket.AF_INET, kind) as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def can_bind(port, kind=socket.SOCK_STREAM):
    try:
        with socket.socket(socket.AF_INET, kind) as s:
            s.bind(("0.0.0.0", port))
        return True
    except OSError:
        return False


def listening(port):
    """True when something accepts TCP connections on localhost:port (binding tests are unreliable on Windows)."""
    for family, address in ((socket.AF_INET, "127.0.0.1"), (socket.AF_INET6, "::1")):
        try:
            with socket.socket(family, socket.SOCK_STREAM) as s:
                s.settimeout(1)
                if s.connect_ex((address, port)) == 0:
                    return True
        except OSError:
            pass
    return False


def check(ok, what):
    print(("PASS " if ok else "FAIL ") + what, flush=True)
    if not ok:
        failures.append(what)


def wait_for(read, predicate, seconds=10):
    end = time.time() + seconds
    value = read()
    while not predicate(value) and time.time() < end:
        time.sleep(0.25)
        value = read()
    return value


PRELUDE = r'''
using System; using System.Linq; using System.Collections; using System.Collections.Generic; using System.Text.Json;
var __asm = AppDomain.CurrentDomain.GetAssemblies();
var gh = __asm.First(x => x.GetName().Name == "Grasshopper2");
var plug = __asm.First(x => x.GetName().Name == "Swiftlet.Gh2");
var core = __asm.First(x => x.GetName().Name == "Swiftlet.Core");
dynamic editor = gh.GetType("Grasshopper2.UI.Editor", true).GetProperty("Instance").GetValue(null) ?? throw new InvalidOperationException("GH2 editor not running");
dynamic doc = editor.Canvas.Document;
dynamic Find(string id) { foreach (dynamic o in (IEnumerable)doc.Objects.Forwards) if (o.InstanceId.ToString() == id) return o; throw new Exception("object not found " + id); }
dynamic Add(string cls) { dynamic c = Activator.CreateInstance(plug.GetType("Swiftlet.Gh2.Components." + cls, true)); doc.Objects.Add(c); return c; }
void SetIn(dynamic c, int i, object values) { dynamic p = c.Parameters.Input(i); p.Set((dynamic)values); p.Expire(); c.Expire(); }
object[] Out(dynamic c, int i) => ((IEnumerable)c.Parameters.Output(i).State.Data.Tree().AllItems).Cast<object>().ToArray();
string Show(object x) { if (x == null) return ""; var k = x.GetType().GetProperty("Key"); var v = x.GetType().GetProperty("Value"); return k != null && v != null ? k.GetValue(x) + "=" + v.GetValue(x) : x.ToString(); }
string Text(dynamic c, int i) => string.Join("|", Out(c, i).Select(Show));
'''

failures = []
http_port, ws_port = free_port(), free_port()
udp_port = free_port(socket.SOCK_DGRAM)

ids = json.loads(cs(r'''
var body = Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.RequestBodyText", true), "text/plain", "listener ok");
var bodies = Array.CreateInstance(core.GetType("Swiftlet.Core.Http.IRequestBody", true), 1); bodies.SetValue(body, 0);
dynamic http = Add("HttpListenerComponent"); SetIn(http, 0, new int[] { %d }); SetIn(http, 1, new string[] { "hook" }); SetIn(http, 2, bodies);
dynamic udp = Add("UdpListenerComponent"); SetIn(udp, 0, new int[] { %d });
dynamic server = Add("WebSocketServerComponent"); SetIn(server, 0, new int[] { %d }); SetIn(server, 1, new bool[] { true });
dynamic client = Add("WebSocketClientComponent"); SetIn(client, 0, new string[] { "ws://localhost:%d/" }); SetIn(client, 2, new bool[] { false });
dynamic sendFromClient = Add("WebSocketSendComponent"); SetIn(sendFromClient, 1, new string[] { "hello from client" }); SetIn(sendFromClient, 2, new bool[] { false });
dynamic sendFromServer = Add("WebSocketSendComponent"); SetIn(sendFromServer, 1, new string[] { "hello from server" }); SetIn(sendFromServer, 2, new bool[] { false });
doc.Solution.StartWait();
Console.WriteLine(JsonSerializer.Serialize(new Dictionary<string, string> {
    ["http"] = http.InstanceId.ToString(), ["udp"] = udp.InstanceId.ToString(), ["server"] = server.InstanceId.ToString(),
    ["client"] = client.InstanceId.ToString(), ["sendFromClient"] = sendFromClient.InstanceId.ToString(), ["sendFromServer"] = sendFromServer.InstanceId.ToString() }));
''' % (http_port, udp_port, ws_port, ws_port)).strip().splitlines()[-1])


def outputs(key, count):
    script = 'dynamic c = Find("%s"); Console.WriteLine(JsonSerializer.Serialize(Enumerable.Range(0, %d).Select(i => Text(c, i)).ToArray()));' % (ids[key], count)
    return json.loads(cs(script).strip().splitlines()[-1])


# ---------------------------------------------------------------- HTTP Listener
check(outputs("http", 4)[0] == "", "HTTP Listener publishes nothing before a request arrives")
with urllib.request.urlopen(f"http://localhost:{http_port}/hook/?a=1&b=two", timeout=10) as r:
    reply = r.read().decode()
check(reply == "listener ok", "HTTP Listener returns the pre-canned response body: " + reply)
got = wait_for(lambda: outputs("http", 4), lambda o: o[0] == "GET")
check(got[0] == "GET", "HTTP Listener recomputed itself with the request method: " + got[0])
check(got[2] == "a=1|b=two", "HTTP Listener outputs the query parameters: " + got[2])
req = urllib.request.Request(f"http://localhost:{http_port}/hook/", data="posted λ".encode("utf-8"), method="POST")
with urllib.request.urlopen(req, timeout=10) as r:
    r.read()
got = wait_for(lambda: outputs("http", 4), lambda o: o[0] == "POST")
check(got[0] == "POST" and got[3] == "posted λ", "HTTP Listener outputs POST content: " + got[3])

# ---------------------------------------------------------------- UDP Listener
with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
    s.sendto(bytes([1, 2, 3, 250]), ("127.0.0.1", udp_port))
    sender = s.getsockname()[1]
got = wait_for(lambda: outputs("udp", 2), lambda o: o[0] != "")
remote = got[1]
data = cs('dynamic c = Find("%s"); Console.WriteLine(string.Join(",", ((byte[])Out(c, 0).Single())));' % ids["udp"]).strip()
check(data == "1,2,3,250", "UDP Listener outputs the received datagram: " + data)
check(remote.endswith(":%d" % sender), "UDP Listener outputs the sender endpoint: " + remote)

# ---------------------------------------------------------------- WebSocket server <-> client
got = outputs("server", 5)
check(got[4] == "Listening on ws://localhost:%d/" % ws_port and got[3] == "0", "WebSocket Server is listening with 0 clients: %s" % got[3:])
cs('dynamic c = Find("%s"); SetIn(c, 2, new bool[] { true }); doc.Solution.StartWait();' % ids["client"])
got = wait_for(lambda: outputs("client", 4), lambda o: o[3] == "Connected")
check(got[3] == "Connected", "WebSocket Client connects: " + got[3])
got = wait_for(lambda: outputs("server", 5), lambda o: o[3] == "1")
check(got[3] == "1", "WebSocket Server counts the client: " + got[3])

call("g2_connect", {"src_id": ids["client"], "src": "Connection", "dst_id": ids["sendFromClient"], "dst": "Connection", "solve": False})
cs('dynamic c = Find("%s"); SetIn(c, 2, new bool[] { true }); doc.Solution.StartWait();' % ids["sendFromClient"])
got = wait_for(lambda: outputs("server", 5), lambda o: o[1] == "hello from client")
check(got[1] == "hello from client", "WebSocket Server receives the client's message: " + got[1])
cs('dynamic c = Find("%s"); SetIn(c, 2, new bool[] { false }); doc.Solution.StartWait();' % ids["sendFromClient"])

call("g2_connect", {"src_id": ids["server"], "src": "Connection", "dst_id": ids["sendFromServer"], "dst": "Connection", "solve": False})
cs('dynamic c = Find("%s"); SetIn(c, 2, new bool[] { true }); doc.Solution.StartWait();' % ids["sendFromServer"])
got = wait_for(lambda: outputs("client", 4), lambda o: o[1] == "hello from server")
check(got[1] == "hello from server", "WebSocket Client receives the server's message: " + got[1])
cs('dynamic c = Find("%s"); SetIn(c, 2, new bool[] { false }); doc.Solution.StartWait();' % ids["sendFromServer"])

# Stopping through the Run inputs releases the port while the component stays on the canvas.
cs('dynamic c = Find("%s"); SetIn(c, 2, new bool[] { false }); dynamic s = Find("%s"); SetIn(s, 1, new bool[] { false }); doc.Solution.StartWait();' % (ids["client"], ids["server"]))
got = wait_for(lambda: outputs("server", 5), lambda o: o[4] == "Stopped")
check(got[4] == "Stopped", "WebSocket Server reports Stopped when Run is false: " + got[4])
check(not wait_for(lambda: listening(ws_port), lambda on: not on), "WebSocket Server stops accepting connections after Run = false")
cs('dynamic s = Find("%s"); SetIn(s, 1, new bool[] { true }); doc.Solution.StartWait();' % ids["server"])
check(wait_for(lambda: listening(ws_port), lambda on: on), "WebSocket Server listens again when Run goes back to true")

# ---------------------------------------------------------------- deletion releases everything
check(listening(http_port), "HTTP Listener is accepting connections while it is on the canvas")
check(not can_bind(udp_port, socket.SOCK_DGRAM), "UDP Listener holds its port while it is on the canvas")
cs('foreach (var id in new[] { %s }) doc.Objects.Remove(Find(id)); doc.Solution.StartWait();' % ", ".join('"%s"' % v for v in ids.values()))
check(not wait_for(lambda: listening(http_port), lambda on: not on), "HTTP Listener stops accepting connections after deletion")
check(wait_for(lambda: can_bind(udp_port, socket.SOCK_DGRAM), lambda ok: ok), "UDP Listener port is free after deletion")
check(not wait_for(lambda: listening(ws_port), lambda on: not on), "WebSocket Server stops accepting connections after deletion")
bridges = cs('Console.WriteLine(System.Diagnostics.Process.GetProcessesByName("SwiftletBridge").Length);').strip()
print("bridge processes still running on this machine: " + bridges + " (other agents may own some)")

print()
print("ALL NETWORK CHECKS PASSED" if not failures else "%d FAILED: %s" % (len(failures), failures))
sys.exit(1 if failures else 0)
