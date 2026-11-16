"""Live check of the Grasshopper 1 WebSocket Server and Client after the shared-session event changes.

The WebSocket sessions in Swiftlet.Runtime now raise StateChanged only on a real transition (not when an
idle session is stopped or an idle client disconnected). This checks that GH1 still updates itself, without
any solve started by the test, when something really happens:

Server (GH1 WebSocket Server on a free port; a raw-socket client in this process):
  Run = true  -> listening, Clients 0, Message "ws://localhost:P/ (0 clients)"
  connect     -> Clients 1 and Message "(1 clients)"
  send text   -> Message output carries it
  close       -> Clients 0
  Run = false -> Status/Message "Stopped" and the port is released
Client (GH1 WebSocket Client; a raw-socket server in this process):
  Run = true  -> Status Connected
  server sends a text frame -> Message output carries it
  server closes             -> Status leaves Connected
  Run = false -> Message "Disconnected"

    python tests/Swiftlet.Cases/checks/validate_gh1_websocket.py --port <slot port>   (after cases.py --load)
Loopback only. Removes what it adds.
"""
import argparse, base64, hashlib, json, os, socket, struct, sys, threading, time, urllib.request, uuid

ap = argparse.ArgumentParser()
ap.add_argument("--port", type=int, required=True)
args = ap.parse_args()
failures = []
GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"


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


def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def can_bind(port):
    try:
        with socket.socket() as s:
            s.bind(("127.0.0.1", port))
        return True
    except OSError:
        return False


def frame(text, mask):
    data = text.encode()
    head = bytes([0x81])
    n = len(data)
    mbit = 0x80 if mask else 0
    head += bytes([mbit | n]) if n < 126 else bytes([mbit | 126]) + struct.pack(">H", n)
    if not mask:
        return head + data
    key = os.urandom(4)
    return head + key + bytes(b ^ key[i % 4] for i, b in enumerate(data))


def close_frame(mask):
    if not mask:
        return bytes([0x88, 0x02, 0x03, 0xE8])
    key = os.urandom(4)
    return bytes([0x88, 0x82]) + key + bytes(b ^ key[i % 4] for i, b in enumerate(b"\x03\xe8"))


def read_headers(sock):
    buf = b""
    while b"\r\n\r\n" not in buf:
        chunk = sock.recv(4096)
        if not chunk:
            raise ConnectionError("closed during handshake")
        buf += chunk
    return buf.decode(errors="replace")


GH1 = r'''
using System; using System.Linq;
using Grasshopper.Kernel; using Grasshopper.Kernel.Types; using Grasshopper.Kernel.Parameters;
var sw1 = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Swiftlet.Gh.Rhino8");
var canvas = Grasshopper.Instances.ActiveCanvas ?? throw new InvalidOperationException("GH1 not open");
GH_Document doc = canvas.Document;
if (doc == null) { doc = new GH_Document(); Grasshopper.Instances.DocumentServer.AddDocument(doc); canvas.Document = doc; }
IGH_Component Comp(string type) => doc.Objects.OfType<IGH_Component>().First(c => c.GetType().Name == type);
string Out(IGH_Component c, int i) => string.Join("|", c.Params.Output[i].VolatileData.AllData(true).Select(g => g?.ToString() ?? ""));
// Expire the input itself: expiring only the component keeps the pin's old VolatileData (Run stays true).
void SetRun(IGH_Component c, int index, bool run) { var p = (Param_Boolean)c.Params.Input[index]; p.PersistentData.Clear(); p.PersistentData.Append(new GH_Boolean(run)); p.ExpireSolution(true); }
'''


def state(type_name, outputs):
    out = cs(GH1 + 'var c = Comp("%s"); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { message = c.Message, outs = new[] { %s } }));'
             % (type_name, ", ".join(f"Out(c, {i})" for i in outputs)))
    return json.loads(out.splitlines()[-1])


def wait_for(type_name, outputs, pred, seconds=10):
    end = time.time() + seconds
    s = state(type_name, outputs)
    while not pred(s) and time.time() < end:
        time.sleep(0.25)
        s = state(type_name, outputs)
    return s


# Leftovers from an interrupted run would be found instead of the components added below.
cs(GH1 + 'foreach (var c in doc.Objects.OfType<IGH_Component>().Where(c => c.GetType().Name is "WebSocketServerComponent" or "WebSocketClientComponent").ToList()) doc.RemoveObject(c, false);')

# ---------------------------------------------------------------- server
P = free_port()
cs(GH1 + r'''
var comp = (IGH_Component)Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Components.WebSocketServerComponent", true));
comp.CreateAttributes(); doc.AddObject(comp, false);
var port = (Param_Integer)comp.Params.Input[0]; port.PersistentData.Clear(); port.PersistentData.Append(new GH_Integer(%d));
SetRun(comp, 1, true);
''' % P)
S_OUTS = [1, 3, 4]  # Message, Clients, Status
s = wait_for("WebSocketServerComponent", S_OUTS, lambda s: s["outs"][2].startswith("Listening"))
check(s["outs"][2] == f"Listening on ws://localhost:{P}/" and s["outs"][1] == "0" and s["message"] == f"ws://localhost:{P}/ (0 clients)",
      f"GH1 WS Server Run=true: {s}")

client = socket.create_connection(("127.0.0.1", P), timeout=10)
key = base64.b64encode(os.urandom(16)).decode()
client.sendall((f"GET / HTTP/1.1\r\nHost: localhost:{P}\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n"
                f"Sec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n").encode())
check(" 101 " in read_headers(client).splitlines()[0], "raw client handshake with the GH1 server")
s = wait_for("WebSocketServerComponent", S_OUTS, lambda s: s["outs"][1] == "1")
check(s["outs"][1] == "1" and s["message"] == f"ws://localhost:{P}/ (1 clients)", f"GH1 WS Server updates on connect: {s}")

client.sendall(frame("hello-gh1", mask=True))
s = wait_for("WebSocketServerComponent", S_OUTS, lambda s: s["outs"][0] == "hello-gh1")
check(s["outs"][0] == "hello-gh1", f"GH1 WS Server updates on a received message: {s}")

client.sendall(close_frame(mask=True))
time.sleep(0.2)
client.close()
s = wait_for("WebSocketServerComponent", S_OUTS, lambda s: s["outs"][1] == "0")
check(s["outs"][1] == "0" and s["message"] == f"ws://localhost:{P}/ (0 clients)", f"GH1 WS Server updates on disconnect: {s}")

cs(GH1 + 'SetRun(Comp("WebSocketServerComponent"), 1, false);')
s = wait_for("WebSocketServerComponent", S_OUTS, lambda s: s["outs"][2] == "Stopped")
end = time.time() + 10
while not can_bind(P) and time.time() < end:
    time.sleep(0.25)
check(s["outs"][2] == "Stopped" and s["message"] == "Stopped" and can_bind(P), f"GH1 WS Server Run=false stops and releases the port: {s}")
cs(GH1 + 'doc.RemoveObject(Comp("WebSocketServerComponent"), false);')

# ---------------------------------------------------------------- client
Q = free_port()
listener = socket.socket()
listener.bind(("127.0.0.1", Q))
listener.listen(1)
accepted = {}


def serve():
    conn, _ = listener.accept()
    headers = read_headers(conn)
    k = next(l.split(":", 1)[1].strip() for l in headers.splitlines() if l.lower().startswith("sec-websocket-key"))
    acc = base64.b64encode(hashlib.sha1((k + GUID).encode()).digest()).decode()
    conn.sendall(("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n"
                  f"Sec-WebSocket-Accept: {acc}\r\n\r\n").encode())
    accepted["conn"] = conn


t = threading.Thread(target=serve, daemon=True)
t.start()
cs(GH1 + r'''
var comp = (IGH_Component)Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Components.WebSocketClientComponent", true));
comp.CreateAttributes(); doc.AddObject(comp, false);
var url = (Param_String)comp.Params.Input[0]; url.PersistentData.Clear(); url.PersistentData.Append(new GH_String("ws://127.0.0.1:%d/"));
SetRun(comp, 2, true);
''' % Q)
t.join(10)
C_OUTS = [1, 3]  # Message, Status
s = wait_for("WebSocketClientComponent", C_OUTS, lambda s: s["outs"][1] == "Connected")
check("conn" in accepted and s["outs"][1] == "Connected" and s["message"] == "Connected", f"GH1 WS Client Run=true connects: {s}")

if "conn" in accepted:
    accepted["conn"].sendall(frame("from-server", mask=False))
s = wait_for("WebSocketClientComponent", C_OUTS, lambda s: s["outs"][0] == "from-server")
check(s["outs"][0] == "from-server", f"GH1 WS Client updates on a received message: {s}")

# Stop listening first, as a server that goes away does: with Run still true the GH1 client reconnects at once,
# synchronously on the UI thread and without a timeout, so a listener that accepts TCP but never completes the
# handshake would freeze Rhino (pre-existing GH1 behaviour, reported separately).
listener.close()
if "conn" in accepted:
    accepted["conn"].sendall(close_frame(mask=False))
    time.sleep(0.2)
    accepted["conn"].close()
s = wait_for("WebSocketClientComponent", C_OUTS, lambda s: s["outs"][1] != "Connected")
check(s["outs"][1] != "Connected" and s["message"] != "Connected", f"GH1 WS Client updates when the server closes: {s}")

cs(GH1 + 'SetRun(Comp("WebSocketClientComponent"), 2, false);')
s = wait_for("WebSocketClientComponent", C_OUTS, lambda s: s["message"] == "Disconnected")
check(s["message"] == "Disconnected", f"GH1 WS Client Run=false: {s}")
cs(GH1 + 'doc.RemoveObject(Comp("WebSocketClientComponent"), false);')

print()
print("ALL CHECKS PASSED" if not failures else "%d FAILED: %s" % (len(failures), failures))
sys.exit(1 if failures else 0)
