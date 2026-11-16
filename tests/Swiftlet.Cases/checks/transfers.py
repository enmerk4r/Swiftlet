"""Live GH1/GH2 check for Download File, Upload File and Upload File Multipart.

Starts a loopback HTTP fixture, then for each scenario and each host adds the component, sets its
inputs with Run = true, re-solves every 100 ms until Done/Error (or a timeout), records the outputs,
and removes the component. Asserts GH1 == GH2 outputs, the downloaded bytes, what the server received
for uploads (method, content type, body/multipart parts, headers, query), and that Run = false cancels
a slow download and deletes its partial file.

    python tests/Swiftlet.Cases/checks/transfers.py --port N      (after cases.py --load)
"""
import argparse, cgi, hashlib, io, json, os, pathlib, shutil, sys, tempfile, threading, time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
from cases import run_csharp  # noqa: E402

PAYLOAD = bytes(range(256)) * 1200          # 307,200 bytes
RECEIVED = []                                # uploads seen by the fixture


class Fixture(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def do_GET(self):
        path = self.path.split("?")[0]
        if path == "/file.bin":
            self.send_response(200)
            self.send_header("Content-Length", str(len(PAYLOAD)))
            self.send_header("X-Query", self.path.partition("?")[2])
            self.send_header("X-Got-Header", self.headers.get("X-Test", ""))
            self.end_headers()
            self.wfile.write(PAYLOAD)
        elif path == "/nolen":
            self.send_response(200)
            self.send_header("Transfer-Encoding", "chunked")
            self.end_headers()
            for i in range(0, len(PAYLOAD), 65536):
                chunk = PAYLOAD[i:i + 65536]
                self.wfile.write(b"%x\r\n" % len(chunk) + chunk + b"\r\n")
            self.wfile.write(b"0\r\n\r\n")
        elif path == "/slow":
            self.send_response(200)
            self.send_header("Content-Length", str(10_000_000))
            self.end_headers()
            try:
                for _ in range(200):
                    self.wfile.write(b"x" * 1000)
                    self.wfile.flush()
                    time.sleep(0.05)
            except OSError:
                pass
        else:
            self.send_response(404)
            self.send_header("Content-Length", "0")
            self.end_headers()

    def _upload(self):
        if self.headers.get("Transfer-Encoding", "").lower() == "chunked":
            body = b""
            while True:
                size = int(self.rfile.readline().split(b";")[0].strip() or b"0", 16)
                if size == 0:
                    self.rfile.readline()
                    break
                body += self.rfile.read(size)
                self.rfile.readline()
        else:
            length = int(self.headers.get("Content-Length") or 0)
            body = self.rfile.read(length) if length else b""
        record = {"method": self.command, "path": self.path, "contentType": self.headers.get("Content-Type", ""),
                  "xtest": self.headers.get("X-Test", ""), "length": len(body), "sha": hashlib.sha256(body).hexdigest()}
        if record["contentType"].startswith("multipart/form-data"):
            env = {"REQUEST_METHOD": "POST", "CONTENT_TYPE": record["contentType"], "CONTENT_LENGTH": str(len(body))}
            form = cgi.FieldStorage(fp=io.BytesIO(body), environ=env, keep_blank_values=True)
            parts = []
            for item in form.list or []:
                data = item.value if isinstance(item.value, bytes) else item.value.encode()
                parts.append({"name": item.name, "filename": item.filename, "type": item.type,
                              "sha": hashlib.sha256(data).hexdigest(), "length": len(data)})
            record["parts"] = sorted(parts, key=lambda p: p["name"])
            record.pop("sha"); record.pop("length")
            record["contentType"] = "multipart/form-data"
        RECEIVED.append(record)
        status = 500 if self.path.startswith("/fail") else 200
        reply = json.dumps({"ok": status == 200}).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(reply)))
        self.end_headers()
        self.wfile.write(reply)

    do_POST = _upload
    do_PUT = _upload


DRIVER = r'''
using System; using System.Collections; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Text.Json; using System.Text.Json.Nodes;
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == n);
var gh1 = A("Grasshopper"); var gh2 = A("Grasshopper2"); var sw1 = A("Swiftlet.Gh.Rhino8"); var sw2 = A("Swiftlet.Gh2"); var core = A("Swiftlet.Core");
var scenarios = JsonNode.Parse(@"__SCENARIOS__")!.AsArray();
var results = new List<object>();
object CoreHeader(JsonNode h) => Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.HttpHeader", true), (string)h[0], (string)h[1]);
object CoreQuery(JsonNode q) => Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.QueryParameter", true), (string)q[0], (string)q[1]);
object CoreField(JsonNode f) => f["text"] is not null
    ? Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.MultipartField", true), (string)f["name"], (string)f["text"], null, null)
    : Activator.CreateInstance(core.GetType("Swiftlet.Core.Http.MultipartField", true), (string)f["name"], Convert.FromBase64String((string)f["bytes"]), (string)f["fileName"], (string)f["contentType"]);
// A literal is {"text"|"bool"|"header"|"query"|"field": value} or a list of them; null = leave unset.
List<object> Values(JsonNode spec) => (spec is JsonArray arr ? arr.Select(x => x!) : new[] { spec }).Select(v => {
    var (k, x) = v.AsObject().Single();
    return k switch { "text" => (object)(string)x!, "bool" => (bool)x!, "header" => CoreHeader(x!), "query" => CoreQuery(x!), "field" => CoreField(x!), _ => throw new Exception(k) };
}).ToList();
object Gh1Goo(object v) => v switch {
    string s => Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_String", true), s),
    bool b => Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.Types.GH_Boolean", true), new object[] { b }),
    _ => Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Goo." + v.GetType().Name switch { "HttpHeader" => "HttpHeaderGoo", "QueryParameter" => "QueryParameterGoo", _ => "MultipartFieldGoo" }, true), v),
};
string Canon(object v) => v switch { null => null, string s => s, _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) };

// ---- GH1
dynamic gh1Doc = ((dynamic)gh1.GetType("Grasshopper.Instances").GetProperty("ActiveCanvas").GetValue(null))?.Document;
if (gh1Doc == null) { gh1Doc = Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_Document", true)); }
var modeT = gh1.GetType("Grasshopper.Kernel.GH_SolutionMode", true);
void Gh1Solve(dynamic comp) { comp.ExpireSolution(false); ((object)gh1Doc).GetType().GetMethod("NewSolution", new[] { typeof(bool), modeT })!.Invoke(gh1Doc, new object[] { false, Enum.Parse(modeT, "Silent") }); }
List<string> Gh1Out(dynamic comp) { var o = new List<string>(); foreach (dynamic p in comp.Params.Output) { string v = null; foreach (dynamic path in p.VolatileData.Paths) foreach (object item in p.VolatileData.get_Branch(path)) { var val = item?.GetType().GetProperty("Value")?.GetValue(item); v = Canon(val); } o.Add(v); } return o; }
List<string> Gh1Errors(dynamic comp) { var lv = gh1.GetType("Grasshopper.Kernel.GH_RuntimeMessageLevel", true); var rm = ((object)comp).GetType().GetMethod("RuntimeMessages", new[] { lv }); return ((IEnumerable)rm.Invoke(comp, new[] { Enum.Parse(lv, "Error") })).Cast<string>().ToList(); }

// ---- GH2
dynamic gh2Doc = ((dynamic)gh2.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null)).Canvas.Document;
void Gh2Solve(dynamic comp) { comp.Expire(); gh2Doc.Solution.StartWait(); }
List<string> Gh2Out(dynamic comp) { var o = new List<string>(); foreach (dynamic p in comp.Parameters.Outputs) { string v = null; dynamic tree = p.State.Data?.Tree(); if (tree != null) foreach (dynamic twig in (IEnumerable)tree.AllTwigs) for (int i = 0; i < (int)twig.LeafCount; i++) v = twig.NullAt(i) ? null : Canon((object)twig.ItemAt(i)); o.Add(v); } return o; }
List<string> Gh2Errors(dynamic comp) { var l = new List<string>(); dynamic m = comp.State.Data?.Messages; if (m != null) for (int i = 0; i < (int)m.Count; i++) if (m[i].Level.ToString() == "Error") l.Add((string)m[i].Text + " " + (string)m[i].Info); return l; }
void Gh2Set(dynamic param, List<object> values, Type elem) {
    var arr = Array.CreateInstance(elem, values.Count); for (int i = 0; i < values.Count; i++) arr.SetValue(values[i], i);
    ((object)param).GetType().GetMethods().Single(m => m.Name == "Set" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == arr.GetType()).Invoke(param, new object[] { arr }); param.Expire(); }
Type Elem(object p) { for (var t = p.GetType(); t != null; t = t.BaseType) if (t.IsGenericType && t.GetGenericTypeDefinition().Name == "Parameter`1") return t.GetGenericArguments()[0]; return typeof(object); }

foreach (var node in scenarios)
{
    var sc = node!.AsObject(); var r = new Dictionary<string, object> { ["name"] = (string)sc["name"] };
    foreach (var host in new[] { "gh1", "gh2" })
    {
        if (sc["host"] is not null && (string)sc["host"] != host) continue; // the Python side runs one host per call
        var created = new List<object>();
        try
        {
            dynamic comp;
            var inputs = sc["inputs"]!.AsArray();
            if (host == "gh1")
            {
                comp = Activator.CreateInstance(sw1.GetType("Swiftlet.Gh.Rhino8.Components." + (string)sc["component"], true)); comp.CreateAttributes();
                gh1Doc.AddObject(comp, false, int.MaxValue); created.Add(comp);
                var srcT = gh1.GetType("Grasshopper.Kernel.Parameters.Param_GenericObject", true);
                var srcs = new List<dynamic>();
                for (int i = 0; i < inputs.Count; i++) { srcs.Add(null); if (inputs[i] is null) continue;
                    dynamic src = Activator.CreateInstance(srcT); src.CreateAttributes(); gh1Doc.AddObject(src, false, int.MaxValue); created.Add(src); srcs[i] = src;
                    object pd = src.PersistentData; var app = pd.GetType().GetMethod("Append", new[] { gh1.GetType("Grasshopper.Kernel.Types.IGH_Goo", true) });
                    foreach (var v in Values(inputs[i]!)) app.Invoke(pd, new[] { Gh1Goo(v) });
                    comp.Params.Input[i].AddSource(src); }
                Gh1Solve(comp);
                var deadline = DateTime.Now.AddSeconds(15); List<string> outs = Gh1Out(comp);
                int? stopAfterMs = (int?)sc["stopAfterMs"];
                if (stopAfterMs is int ms) { System.Threading.Thread.Sleep(ms);
                    object pd = srcs[(int)sc["runIndex"]].PersistentData; pd.GetType().GetMethod("Clear").Invoke(pd, null);
                    pd.GetType().GetMethod("Append", new[] { gh1.GetType("Grasshopper.Kernel.Types.IGH_Goo", true) }).Invoke(pd, new[] { Gh1Goo(false) });
                    srcs[(int)sc["runIndex"]].ExpireSolution(false); Gh1Solve(comp); System.Threading.Thread.Sleep(500); outs = Gh1Out(comp); }
                else while (DateTime.Now < deadline && Gh1Errors(comp).Count == 0 && outs[0] != "True" && !(outs[outs.Count - 1] is string e && e.Length > 0)) { System.Threading.Thread.Sleep(100); Gh1Solve(comp); outs = Gh1Out(comp); }
                r[host] = new Dictionary<string, object> { ["outputs"] = outs, ["errors"] = Gh1Errors(comp) };
            }
            else
            {
                var t = sw2.GetTypes().Single(x => x.Name == (string)sc["component"] && x.Namespace!.StartsWith("Swiftlet.Gh2.Components"));
                comp = Activator.CreateInstance(t); gh2Doc.Objects.Add(comp); created.Add(comp);
                for (int i = 0; i < inputs.Count; i++) if (inputs[i] is not null) { dynamic p = comp.Parameters.Input(i); Gh2Set(p, Values(inputs[i]!), Elem((object)p)); }
                Gh2Solve(comp);
                var deadline = DateTime.Now.AddSeconds(15); List<string> outs = Gh2Out(comp);
                if ((int?)sc["stopAfterMs"] is int ms) { System.Threading.Thread.Sleep(ms);
                    dynamic run = comp.Parameters.Input((int)sc["runIndex"]); Gh2Set(run, new List<object> { false }, typeof(bool)); Gh2Solve(comp); System.Threading.Thread.Sleep(500); outs = Gh2Out(comp); }
                else while (DateTime.Now < deadline && Gh2Errors(comp).Count == 0 && outs[0] != "True" && !(outs[outs.Count - 1] is string e && e.Length > 0)) { System.Threading.Thread.Sleep(100); Gh2Solve(comp); outs = Gh2Out(comp); }
                r[host] = new Dictionary<string, object> { ["outputs"] = outs, ["errors"] = Gh2Errors(comp) };
            }
        }
        catch (Exception ex) { var x = ex is TargetInvocationException ti && ti.InnerException != null ? ti.InnerException : ex; r[host] = new Dictionary<string, object> { ["exception"] = x.GetType().Name + ": " + x.Message }; }
        finally
        {
            foreach (dynamic o in created) { if (host == "gh1") gh1Doc.RemoveObject(o, false); else gh2Doc.Objects.Remove(o); }
        }
        // Files written by this host for this scenario, for the Python side to inspect.
        Console.WriteLine("HOST_DONE " + (string)sc["name"] + " " + host);
    }
    results.Add(r);
}
Console.WriteLine("SWIFTLET_TRANSFERS:" + JsonSerializer.Serialize(results));
'''


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    a = ap.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", 0), Fixture)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    base = f"http://127.0.0.1:{server.server_address[1]}"
    work = pathlib.Path(tempfile.mkdtemp(prefix="swiftlet-transfers-"))
    src = work / "upload.dat"
    src.write_bytes(PAYLOAD[:50_000])
    src_sha = hashlib.sha256(src.read_bytes()).hexdigest()
    T = lambda s: {"text": s}
    B = lambda b: {"bool": b}

    def dl(name, url, target, extra=None, **kw):
        return {"name": name, "component": "DownloadFileComponent", "target": str(target), "host_suffix": True,
                "inputs": [T(url), T(str(target)), extra and extra.get("query"), extra and extra.get("headers"), B(True), B(True)], **kw}

    scenarios = [
        dl("download with length, query and header", base + "/file.bin", work / "dl1.bin",
           {"query": [{"query": ["a", "b c"]}], "headers": [{"header": ["X-Test", "yes"]}]}),
        dl("download without length (chunked)", base + "/nolen", work / "dl2.bin"),
        dl("download 404", base + "/missing", work / "dl3.bin"),
        dl("download cancelled by Run=false", base + "/slow", work / "dl4.bin", stopAfterMs=600, runIndex=4),
        {"name": "upload POST", "component": "UploadFileComponent",
         "inputs": [T(base + "/up"), T(str(src)), T("POST"), T(""), [{"query": ["k", "v"]}], [{"header": ["X-Test", "up"]}], B(True)]},
        {"name": "upload PUT with content type", "component": "UploadFileComponent",
         "inputs": [T(base + "/put"), T(str(src)), T("put"), T("application/x-test"), None, None, B(True)]},
        {"name": "upload server error", "component": "UploadFileComponent",
         "inputs": [T(base + "/fail"), T(str(src)), T("POST"), T(""), None, None, B(True)]},
        {"name": "upload bad method", "component": "UploadFileComponent",
         "inputs": [T(base + "/up"), T(str(src)), T("PATCH"), T(""), None, None, B(True)]},
        {"name": "upload missing file", "component": "UploadFileComponent",
         "inputs": [T(base + "/up"), T(str(work / "nope.dat")), T("POST"), T(""), None, None, B(True)]},
        {"name": "multipart with fields", "component": "UploadFileMultipartComponent",
         "inputs": [T(base + "/mp"), T(str(src)), T("doc"), T("renamed.dat"), T(""),
                    [{"field": {"name": "note", "text": "hello"}}, {"field": {"name": "blob", "bytes": "AAEC", "fileName": "b.bin", "contentType": "application/octet-stream"}}],
                    [{"header": ["X-Test", "mp"]}], B(True)]},
        {"name": "multipart defaults", "component": "UploadFileMultipartComponent",
         "inputs": [T(base + "/mp2"), T(str(src)), T(""), None, None, None, None, B(True)]},
    ]
    # Each host downloads to its own file so GH1 and GH2 results can be checked separately.
    out = []
    problems = []
    for sc in scenarios:
        per_host = {}
        received = {}
        for host in ("gh1", "gh2"):
            one = json.loads(json.dumps(sc))
            one["host"] = host
            if "target" in one:
                one["inputs"][1] = T(one["target"] + "." + host)
            before = len(RECEIVED)
            text = run_csharp(a.port, DRIVER.replace("__SCENARIOS__", json.dumps([one]).replace('"', '""')))
            line = next(l for l in text.splitlines() if l.startswith("SWIFTLET_TRANSFERS:"))
            res = json.loads(line[len("SWIFTLET_TRANSFERS:"):])[0]
            per_host[host] = res[host]
            received[host] = RECEIVED[before:]
            if "target" in one:
                path = pathlib.Path(one["target"] + "." + host)
                per_host[host]["file"] = hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
        g1, g2 = per_host["gh1"], per_host["gh2"]
        msgs = []
        for h, r in per_host.items():
            if "exception" in r:
                msgs.append(f"{h} exception {r['exception']}")
        if not msgs:
            # Each host downloads to its own file (suffix .gh1/.gh2); mask that in the Path output.
            same = lambda outs, h: [o.replace("." + h, ".host") if isinstance(o, str) else o for o in outs]
            if same(g1["outputs"], "gh1") != same(g2["outputs"], "gh2"):
                msgs.append(f"outputs differ: gh1 {g1['outputs']} gh2 {g2['outputs']}")
            if bool(g1["errors"]) != bool(g2["errors"]):
                msgs.append(f"errors differ: gh1 {g1['errors']} gh2 {g2['errors']}")
            if g1.get("file") != g2.get("file"):
                msgs.append("downloaded files differ")
            if received["gh1"] != received["gh2"]:
                msgs.append(f"server saw different uploads: gh1 {received['gh1']} gh2 {received['gh2']}")
        out.append({"scenario": sc["name"], "gh1": g1, "gh2": g2, "received": received})
        print(("PASS " if not msgs else "FAIL ") + sc["name"], "->", g2.get("outputs"), g2.get("errors") or "", ("file " + str(g2.get("file"))[:12]) if "file" in g2 else "")
        for m in msgs:
            print("    ", m)
        problems += msgs

    # Oracles beyond GH1 == GH2.
    by = {o["scenario"]: o for o in out}
    payload_sha = hashlib.sha256(PAYLOAD).hexdigest()

    def expect(cond, text, detail=None):
        print(("  ok  " if cond else "  BAD ") + text + ("" if cond or detail is None else "\n        " + json.dumps(detail)[:600]))
        if not cond:
            problems.append(text)

    expect(by["download with length, query and header"]["gh2"]["file"] == payload_sha, "download writes the exact payload")
    expect(by["download without length (chunked)"]["gh2"]["outputs"][1] == "100", "chunked download completes at 100%")
    expect(by["download 404"]["gh2"]["outputs"][5] == "HTTP 404: Not Found", "404 reports the HTTP error")
    expect(by["download cancelled by Run=false"]["gh2"]["file"] is None and by["download cancelled by Run=false"]["gh1"]["file"] is None,
           "cancelled download leaves no partial file")
    up = by["upload POST"]["received"]["gh2"]
    expect(len(up) == 1 and up[0]["method"] == "POST" and up[0]["sha"] == src_sha and up[0]["path"] == "/up?k=v" and up[0]["xtest"] == "up",
           "POST upload sends the file, query and header", up)
    put = by["upload PUT with content type"]["received"]["gh2"]
    expect(len(put) == 1 and put[0]["method"] == "PUT" and put[0]["contentType"] == "application/x-test", "PUT upload uses method and content type", put)
    mp = by["multipart with fields"]["received"]["gh2"]
    expect(len(mp) == 1 and [p["name"] for p in mp[0]["parts"]] == ["blob", "doc", "note"]
           and next(p for p in mp[0]["parts"] if p["name"] == "doc")["filename"] == "renamed.dat"
           and next(p for p in mp[0]["parts"] if p["name"] == "doc")["sha"] == src_sha, "multipart sends file part and extra fields", mp)
    mp2 = by["multipart defaults"]["received"]["gh2"]
    expect(len(mp2) == 1 and mp2[0]["parts"][0]["name"] == "file" and mp2[0]["parts"][0]["filename"] == "upload.dat",
           "multipart defaults to field 'file' and the real file name", mp2)
    server.shutdown()
    shutil.rmtree(work, ignore_errors=True)
    print("ALL PASS" if not problems else f"{len(problems)} PROBLEM(S)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
