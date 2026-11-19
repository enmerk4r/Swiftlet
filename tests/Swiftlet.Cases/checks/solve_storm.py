"""Solve-storm gate: many live Swiftlet components placed together must not keep cancelling solutions.

Places 13 GH2 components on an empty canvas in one go: HTTP Listener, UDP Listener, WebSocket Server
(Run = true), WebSocket Client, Server Input, MCP Server, Download File and Upload File (both pointed at
the HTTP Listener, so they make it receive requests), OAuth Authorize, OAuth Token, Create Text Body,
Create Form URL Encoded Body and Save Web Response. Sets their inputs (loopback ports only, nothing leaves
the machine), starts one solution and then watches the document's Solution events until they stop.

Found on 4248f80 (fresh process, one placement): 3,576 solutions started, 3,573 cancelled; other runs
2,807 and 50,357 started, the last one blocking Rhino for minutes; once the WebSocket Server ended
"Completed" without having run with Run = true (its port was not listening until expired by hand).

    python tests/Swiftlet.Cases/checks/solve_storm.py --port N [--rounds 5] [--max-starts 40]
    (after cases.py --port N --load, on a fresh process)

Each round clears the canvas first (which stops the previous listeners) and re-places everything.
A round fails when more than --max-starts solutions start before the document settles, when it has
not settled within --timeout seconds, when the WebSocket Server with Run = true is not listening, or
when any object is left Cancelled/Faulted. Exit status 1 if any round fails. Labels (GH1 Message
equivalents) are printed for information.
"""
import argparse, json, os, pathlib, socket, sys, tempfile, time

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
from cases import call, run_csharp  # noqa: E402

COMPONENTS = {
    "TextBody": "2147ae2b-8857-4d13-8f94-4ff852cb2b27",
    "Form": "b190b78a-7744-4dc6-a75b-91057fa461bc",
    "Save": "38812362-7441-4c00-9460-a0dbe4bbf654",
    "Http": "d3d3c952-59a5-49f6-abe3-e01ffe2b39d7",
    "Udp": "c7c258cc-f0e3-413c-a40b-11532b104120",
    "WsServer": "40e537a4-c2cf-438b-a8be-05566125fe1c",
    "WsClient": "de0f4b67-14d0-4cff-b207-1819ac5fffc5",
    "ServerInput": "ca0649f0-c07b-4e52-8c70-4c19d83fc409",
    "Mcp": "1ea3f102-d640-4c21-955a-b8791728eb8a",
    "Download": "c7f76527-3fec-4734-95de-80ae3b69628d",
    "Upload": "25938c28-bcf4-4326-a791-c1d27abd471c",
    "OAuthAuthorize": "ef869f1e-b996-49ca-a096-96aba430f38f",
    "OAuthToken": "0ea8f2ef-6912-4f76-8b87-537730b19bab",
}

PRELUDE = r'''
using System; using System.Linq; using System.Reflection; using System.Collections.Generic;
var gh2 = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Grasshopper2");
dynamic ed = gh2.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic doc = ed.Canvas.Document;
'''

# Counts Solution events for the lifetime of the process (hooked once, kept in AppDomain data).
COUNTERS = PRELUDE + r'''
object sol = doc.Solution; var et = sol.GetType();
var store = (Dictionary<string,int>)AppDomain.CurrentDomain.GetData("swiftlet.storm.counts");
if (store == null || !ReferenceEquals(AppDomain.CurrentDomain.GetData("swiftlet.storm.solution"), sol))
{
    store = new Dictionary<string,int>{{"Started",0},{"Completed",0},{"Cancelled",0},{"Faulted",0}};
    AppDomain.CurrentDomain.SetData("swiftlet.storm.counts", store);
    AppDomain.CurrentDomain.SetData("swiftlet.storm.solution", sol);
    foreach (var key in store.Keys.ToList())
    {
        var e = et.GetEvent("Solution" + key);
        var ps = e.EventHandlerType.GetMethod("Invoke").GetParameters().Select(x => System.Linq.Expressions.Expression.Parameter(x.ParameterType)).ToArray();
        var k = key; Action bump = () => { lock (store) store[k]++; };
        e.AddEventHandler(sol, System.Linq.Expressions.Expression.Lambda(e.EventHandlerType,
            System.Linq.Expressions.Expression.Invoke(System.Linq.Expressions.Expression.Constant(bump)), ps).Compile());
    }
}
lock (store) Console.WriteLine("STORM:" + string.Join(",", store.Select(kv => "\"" + kv.Key + "\":" + kv.Value)));
'''

CLEAR = PRELUDE + r'''
var all = new List<object>(); foreach (var o in doc.Objects.Forwards) all.Add(o);
foreach (dynamic o in all) doc.Objects.Remove(o);
doc.Solution.StartWait();
Console.WriteLine("cleared " + all.Count);
'''

SET_INPUTS = PRELUDE + r'''
dynamic Find(string id) { foreach (dynamic o in doc.Objects.Forwards) if (o.InstanceId.ToString() == id) return o; throw new Exception("missing " + id); }
void Put(string id, string name, object v)
{
    dynamic comp = Find(id); dynamic p = null;
    foreach (dynamic q in comp.Parameters.Inputs) if ((string)q.Nomen.Name == name) p = q;
    if (p == null) throw new Exception("no input " + name + " on " + comp.Nomen.Name);
    var sets = ((object)p).GetType().GetMethods().Where(m => m.Name == "Set" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsArray).ToList();
    var set = sets.FirstOrDefault(m => m.GetParameters()[0].ParameterType.GetElementType() == v.GetType())
        ?? sets.First(m => m.GetParameters()[0].ParameterType.GetElementType().IsAssignableFrom(v.GetType()));
    var arr = Array.CreateInstance(set.GetParameters()[0].ParameterType.GetElementType(), 1); arr.SetValue(v, 0);
    set.Invoke(p, new object[] { arr }); p.Expire();
}
__PUTS__
Console.WriteLine("inputs set");
'''

READ_STATE = PRELUDE + r'''
var rows = new List<string>();
var names = new Dictionary<string,string>{ __NAMES__ };
foreach (dynamic o in doc.Objects.Forwards)
{
    string id = o.InstanceId.ToString(); if (!names.ContainsKey(id)) continue;
    string label = null; try { label = (string)o.Label; } catch {}
    rows.Add("{\"key\":\"" + names[id] + "\",\"phase\":\"" + o.State.Phase + "\",\"label\":" + (label == null ? "null" : "\"" + label.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"") + "}");
}
Console.WriteLine("STATE:[" + string.Join(",", rows) + "]");
'''


def tagged(out, tag):
    return json.loads(next(l for l in out.splitlines() if l.startswith(tag))[len(tag):])


def counts(port):
    return json.loads("{" + next(l for l in run_csharp(port, COUNTERS).splitlines() if l.startswith("STORM:"))[6:] + "}")


def listening(port):
    with socket.socket() as s:
        s.settimeout(1)
        return s.connect_ex(("127.0.0.1", port)) == 0


def one_round(a, n, upload_file, download_file):
    base = a.base_port
    run_csharp(a.port, CLEAR)
    time.sleep(2)  # listeners of the previous round release their ports
    chosen = {k: v for k, v in COMPONENTS.items() if not a.only or k in a.only}
    comps = [{"Key": k, "Selector": v, "X": 100 + (i % 4) * 260, "Y": 100 + (i // 4) * 200}
             for i, (k, v) in enumerate(chosen.items())]
    _, text = call(a.port, "g2_apply_graph", {"sliders": [], "components": comps, "wires": [], "solve": False})
    ids = {p["key"]: p["id"] for p in json.loads(text)["placed"]}
    inputs = {
        "TextBody": [("Content", '"hello"')],
        "Form": [("Keys", '"a"'), ("Values", '"1"')],
        "Http": [("Port", str(base))],
        "Udp": [("Port", str(base + 1))],
        "WsServer": [("Port", str(base + 2)), ("Run", "true")],
        "WsClient": [("URL", f'"ws://localhost:{base + 2}/"'), ("Run", "false")],
        "ServerInput": [("Port", str(base + 3))],
        "Mcp": [("Port", str(base + 4))],
        "Download": [("URL", f'"http://localhost:{base}/"'), ("Path", json.dumps(download_file)), ("Run", "true")],
        "Upload": [("URL", f'"http://localhost:{base}/"'), ("Path", json.dumps(upload_file)), ("Run", "true")],
    }
    puts = "\n".join(f'Put("{ids[k]}", "{name}", {value});' for k, pairs in inputs.items() if k in ids for name, value in pairs)
    run_csharp(a.port, SET_INPUTS.replace("__PUTS__", puts))

    before = counts(a.port)
    t0 = time.time()
    call(a.port, "g2_solve_canvas", {})
    solve_seconds = time.time() - t0

    # Wait until no new solution has started for --quiet seconds.
    last, last_change, settled, probes = counts(a.port), time.time(), False, []
    while time.time() - t0 < a.timeout:
        time.sleep(1)
        p0 = time.time()
        now = counts(a.port)
        probes.append(time.time() - p0)
        if now["Started"] != last["Started"]:
            last, last_change = now, time.time()
        elif time.time() - last_change >= a.quiet:
            settled = True
            break
    after = last
    delta = {k: after[k] - before[k] for k in after}
    names = ",".join(f'{{"{v}","{k}"}}' for k, v in ids.items())
    state = tagged(run_csharp(a.port, READ_STATE.replace("__NAMES__", names)), "STATE:")
    ws_listening = listening(base + 2) if "WsServer" in ids else None
    stale = [s["key"] for s in state if s["phase"] in ("Cancelled", "Faulted")]

    failures = []
    if delta["Started"] > a.max_starts:
        failures.append(f"{delta['Started']} solutions started (max {a.max_starts})")
    if not settled:
        failures.append(f"still solving after {a.timeout}s")
    if ws_listening is False:
        failures.append(f"WebSocket Server (Run = true) not listening on {base + 2}")
    if stale:
        failures.append("left Cancelled/Faulted: " + ", ".join(stale))
    return {
        "round": n, "solutions": delta, "settled": settled, "settleSeconds": round(last_change - t0, 1),
        "solveCallSeconds": round(solve_seconds, 1), "slowestProbeSeconds": round(max(probes, default=0), 2),
        "wsListening": ws_listening, "labels": {s["key"]: s["label"] for s in state},
        "phases": {s["key"]: s["phase"] for s in state}, "failures": failures,
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True, help="Rhino slot port")
    ap.add_argument("--rounds", type=int, default=1)
    ap.add_argument("--max-starts", type=int, default=40, help="solutions allowed to start per round")
    ap.add_argument("--timeout", type=float, default=300, help="seconds a round may take to settle")
    ap.add_argument("--quiet", type=float, default=5, help="seconds without a new solution that count as settled")
    ap.add_argument("--base-port", type=int, default=18091, help="first of five loopback ports used")
    ap.add_argument("--only", type=lambda v: [k for k in v.split(",") if k], default=None,
                    help="comma-separated subset of: " + ",".join(COMPONENTS))
    ap.add_argument("--report", help="write the per-round results here as JSON")
    a = ap.parse_args()
    if a.only and [k for k in a.only if k not in COMPONENTS]:
        ap.error("unknown component key(s): " + ", ".join(k for k in a.only if k not in COMPONENTS))

    work = tempfile.mkdtemp(prefix="swiftlet-storm-")
    upload_file = os.path.join(work, "upload.txt")
    pathlib.Path(upload_file).write_text("upload me", encoding="utf-8")
    download_file = os.path.join(work, "download.bin")

    results = []
    for n in range(1, a.rounds + 1):
        r = one_round(a, n, upload_file, download_file)
        results.append(r)
        s = r["solutions"]
        print(f"round {n}: started {s['Started']} completed {s['Completed']} cancelled {s['Cancelled']} faulted {s['Faulted']}"
              f"; settled {r['settled']} after {r['settleSeconds']}s; WS listening {r['wsListening']}"
              f"; slowest probe {r['slowestProbeSeconds']}s -> {'FAIL: ' + '; '.join(r['failures']) if r['failures'] else 'ok'}")
    run_csharp(a.port, CLEAR)
    print("labels (last round):", json.dumps(results[-1]["labels"]))
    if a.report:
        pathlib.Path(a.report).write_text(json.dumps(results, indent=2), encoding="utf-8")
    failed = [r["round"] for r in results if r["failures"]]
    print(f"{a.rounds - len(failed)}/{a.rounds} rounds passed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
