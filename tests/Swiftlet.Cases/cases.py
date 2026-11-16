"""Run GH1/GH2 component cases in a dedicated Rhino 9 slot and check parity and expectations.

    python tests/Swiftlet.Cases/cases.py --port 10502 --load            # stage + load fresh GH1 and GH2 builds
    python tests/Swiftlet.Cases/cases.py --port 10502 tests/Swiftlet.Cases/cases/request.json [...]

--load copies bin/Debug of Swiftlet.Gh1 (net7.0) and Swiftlet.Gh2 (net8.0) into ONE fresh temp
folder (so both shells bind the same shared Swiftlet.* assemblies, as in the unified package), starts
GH1 and GH2, then loads the .gha (GH_ComponentServer.LoadGHA) and the .rhp (PluginServer.ScopePlugin +
LoadPlugin). Use a freshly started slot: a Rhino process keeps the first Swiftlet.* assemblies it loads.

Case file: a JSON array of cases:
  {
    "name": "query param basic",
    "gh1": "CreateQueryParamComponent",        # GH1 class (Swiftlet.Gh.Rhino8.Components)
    "gh2": "CreateQueryParamComponent",        # GH2 class (Swiftlet.Gh2.Components); defaults to gh1
    "inputs": [{"text": "q"}, {"text": "a b"}],  # per input: value spec, list of specs, or null (missing)
    "expect": [[{"type": "QueryParameter", ...}]], # optional: per output, flat list of canonical values
    "expectError": true,                       # optional: both hosts must report an error and no data
    "gh1Skip": "reason"                        # optional: GH2-only case
  }
MCP specs: mcptext, mcplink, mcpembeddedtext, mcpbinary, mcpimage, mcpparam, mcptool, mcpcall; listenerrequest supplies a deterministic live request handle. MCP payloads compare parsed full ToJson data (including binary bytes), not display summaries.
Value specs: {"text"}, {"int"}, {"num"}, {"bool"}, {"query": [k, v]}, {"header": [k, v]}, {"bytes": b64},
{"json"|"jobject"|"jarray"|"jvalue": "<json text>"}, {"html"|"htmlel"|"xml": "<markup>"},
{"bodytext": [contentType, text]}, {"bodybytes": [contentType, b64]}, {"form": [[keys], [values]]},
{"field": {"name", "text", "contentType"?}} or {"field": {"name", "bytes": b64, "fileName"?, "contentType"?}},
{"multipart": [field objects]}, {"response": {"version", "status", "reason", "headers": [[k, v]], "success",
"content", "bytes": b64}} (all keys optional). A GH2 Process that throws (faulted solve) counts as an error.

Pass rules: GH1 and GH2 outputs (paths, null slots, canonical values) must be identical, both hosts must
agree on whether an error was reported, and any "expect"/"expectError" must hold. Exit status 0 only if
every case passes. --report writes the full raw results.
"""
import argparse, json, os, pathlib, re, shutil, subprocess, sys, tempfile, uuid
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent.parent


def call(port, tool, args, timeout=600):
    body = {"jsonrpc": "2.0", "id": str(uuid.uuid4()), "method": "tools/call", "params": {"name": tool, "arguments": args}}
    req = urllib.request.Request(f"http://127.0.0.1:{port}/", data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        raw = r.read().decode()
    if raw.lstrip().startswith(("event:", "data:")):
        raw = next(x[5:].strip() for x in raw.splitlines() if x.startswith("data:"))
    d = json.loads(raw)
    if "error" in d:
        raise RuntimeError(d["error"])
    text = "".join(c.get("text", "") for c in d["result"].get("content", []) if c.get("type") == "text")
    return d["result"], text


def run_csharp(port, script, timeout=600):
    result, text = call(port, "run_csharp", {"script": script}, timeout)
    try:
        payload = json.loads(text)
    except ValueError:
        raise RuntimeError("run_csharp: " + text[:2000])
    if "stdout" not in payload:
        raise RuntimeError("run_csharp failed: " + text[:4000])
    if payload.get("stderr"):
        print(payload["stderr"], file=sys.stderr)
    return payload["stdout"]


LOAD_SCRIPT = r'''
using System; using System.IO; using System.Linq; using System.Reflection;
var dir = @"__DIR__";
Assembly A(string n) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == n);
foreach (var n in new[]{"Swiftlet.Core","Swiftlet.Runtime","Swiftlet.Gh.Rhino8","Swiftlet.Gh2"})
  if (A(n) != null) throw new InvalidOperationException(n + " is already loaded in this process; use a fresh slot.");
var gh1 = A("Grasshopper") ?? throw new InvalidOperationException("GH1 not started");
var server = gh1.GetType("Grasshopper.Instances").GetProperty("ComponentServer").GetValue(null);
var ext = Activator.CreateInstance(gh1.GetType("Grasshopper.Kernel.GH_ExternalFile", true), Path.Combine(dir, "Swiftlet.Gh.Rhino8.gha"));
var loadGha = server.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
  .First(m => m.Name == "LoadGHA" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType.IsAssignableFrom(ext.GetType()));
var ok1 = loadGha.Invoke(loadGha.IsStatic ? null : server, new object[]{ ext, false });
Console.WriteLine("GH1 LoadGHA " + ok1);
var gh2 = A("Grasshopper2") ?? throw new InvalidOperationException("GH2 not started");
var ps = gh2.GetType("Grasshopper2.Framework.PluginServer", true);
var rhp = Path.Combine(dir, "Swiftlet.Gh2.rhp");
Console.WriteLine("GH2 Scope " + ps.GetMethod("ScopePlugin", new[]{typeof(string)}).Invoke(null, new object[]{ rhp }));
var load = ps.GetMethods().Single(m => m.Name == "LoadPlugin" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string));
var args = new object[]{ rhp, null };
Console.WriteLine("GH2 Load " + load.Invoke(null, args) + (args[1] == null ? "" : " failure: " + args[1]));
foreach (var n in new[]{"Swiftlet.Core","Swiftlet.Runtime","Swiftlet.Gh.Rhino8","Swiftlet.Gh2"})
  Console.WriteLine(n + " <- " + A(n)?.Location);
'''


def stage_and_load(port):
    stage = pathlib.Path(tempfile.mkdtemp(prefix="swiftlet-cases-"))
    for src in (REPO / "src/Swiftlet.Gh1/bin/Debug/net7.0", REPO / "src/Swiftlet.Gh2/bin/Debug/net8.0"):
        if not src.exists():
            sys.exit(f"missing build output {src}; build Swiftlet.sln first")
        shutil.copytree(src, stage, dirs_exist_ok=True)
    print("staged", stage)
    call(port, "g1_start", {})
    call(port, "g2_start", {})
    print(run_csharp(port, LOAD_SCRIPT.replace("__DIR__", str(stage))))


def normalise(outputs):
    """Mask values that are random by design (multipart boundaries)."""
    text = json.dumps(outputs, sort_keys=True)
    text = re.sub(r'boundary=\\"[0-9a-f]{32}\\"', 'boundary=*', text)
    return text


def flatten(branches):
    return [v for b in branches for v in b["values"]]


def has_error(host):
    return any(m.startswith("Error") for m in host.get("messages", []))


def evaluate(case, r):
    problems = []
    g1, g2 = r.get("gh1", {}), r.get("gh2", {})
    for name, h in (("gh1", g1), ("gh2", g2)):
        if "exception" in h:
            problems.append(f"{name} exception: {h['exception']}")
    if problems:
        return problems
    ran = [(n, h) for n, h in (("gh1", g1), ("gh2", g2)) if "skipped" not in h]
    if len(ran) == 2:
        # Output trees are compared strictly, also after errors: a GH1 exception leaves no branch, an
        # AddError-and-return leaves one null slot, and GH2 must match (see docs/gh2-porting.md).
        if normalise(g1["outputs"]) != normalise(g2["outputs"]):
            problems.append("GH1/GH2 outputs differ:\n    gh1 " + json.dumps(g1["outputs"]) + "\n    gh2 " + json.dumps(g2["outputs"]))
        if has_error(g1) != has_error(g2):
            problems.append(f"error mismatch: gh1 {g1['messages']} gh2 {g2['messages']}")
    for n, h in ran:
        if case.get("expectError"):
            if not has_error(h):
                problems.append(f"{n}: expected an error, got {h['messages']}")
            if any(v is not None for o in h["outputs"] for v in flatten(o)):
                problems.append(f"{n}: expected no output data, got {h['outputs']}")
        elif has_error(h):
            problems.append(f"{n}: unexpected error {h['messages']}")
        if "expect" in case:
            got = [flatten(o) for o in h["outputs"]]
            if got != case["expect"]:
                problems.append(f"{n}: expected {json.dumps(case['expect'])}\n    got {json.dumps(got)}")
    return problems


def check_migrations(port, panels):
    out = run_csharp(port, (HERE / "migrations.cs").read_text(encoding="utf-8"))
    line = next((l for l in out.splitlines() if l.startswith("SWIFTLET_MIGRATIONS_JSON:")), None)
    if line is None:
        sys.exit("migration script produced no result line:\n" + out[:4000])
    rows = json.loads(line[len("SWIFTLET_MIGRATIONS_JSON:"):])
    bad = 0
    covered = set()
    for r in rows:
        if "gh1Guid" in r:
            covered.add(r["gh1Guid"].lower())
        ok = "error" not in r and r.get("gh1In") == r.get("gh2In") and r.get("gh1Out") == r.get("gh2Out")
        bad += not ok
        print(("PASS " if ok else "FAIL ") + f"{r.get('table')}: {r.get('gh1Class', r.get('gh1Guid'))} -> {r.get('gh2Class')} "
              f"in {r.get('gh1In')}->{r.get('gh2In')} out {r.get('gh1Out')}->{r.get('gh2Out')}" + (f"  {r['error']}" if "error" in r else ""))
    if panels:
        entries = json.load(open(REPO / "docs/parity.json", encoding="utf-8"))["entries"]
        missing = [e for e in entries if e["kind"] == "component" and e["panel"] in panels and e["gh1Guid"].lower() not in covered]
        for e in missing:
            print(f"MISSING {e['panel']}: {e['class']} ({e['gh1Guid']})" + ("  [archived]" if e["archived"] else ""))
        bad += len(missing)
    print(f"\n{len(rows)} rules checked, {bad} problem(s)")
    return 1 if bad else 0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, required=True)
    ap.add_argument("--load", action="store_true")
    ap.add_argument("--report")
    ap.add_argument("--only", help="run only cases whose name contains this text")
    ap.add_argument("--script", action="append", default=[], metavar="CS",
                    help="run a C# check script (e.g. checks/*.cs) in the slot and print its output; "
                         "it fails if the output contains FAIL")
    ap.add_argument("--migrations", nargs="*", metavar="PANEL",
                    help="check every GH2 migration rule; with panel names (e.g. '2. Request'), also list GH1 "
                         "components in those panels (docs/parity.json) that have no rule")
    ap.add_argument("files", nargs="*")
    a = ap.parse_args()
    if a.load:
        stage_and_load(a.port)
    if a.script:
        bad = 0
        for f in a.script:
            out = run_csharp(a.port, pathlib.Path(f).read_text(encoding="utf-8"))
            print(out)
            bad += "FAIL" in out
        return 1 if bad else 0
    if a.migrations is not None:
        return check_migrations(a.port, a.migrations)
    if not a.files:
        return 0
    cases = []
    for f in a.files:
        # "__REPO__" in a case file stands for the repository root (forward slashes), e.g. for file paths.
        text = open(f, encoding="utf-8").read().replace("__REPO__", REPO.as_posix())
        cases += json.loads(text)
    if a.only:
        cases = [c for c in cases if a.only in c["name"]]
    template = (HERE / "runner.cs").read_text(encoding="utf-8")
    script = template.replace("__CASES_JSON__", json.dumps(cases).replace('"', '""'))
    out = run_csharp(a.port, script)
    line = next((l for l in out.splitlines() if l.startswith("SWIFTLET_CASES_JSON:")), None)
    if line is None:
        sys.exit("runner produced no result line:\n" + out[:4000])
    results = json.loads(line[len("SWIFTLET_CASES_JSON:"):])
    failed = 0
    for case, r in zip(cases, results):
        problems = evaluate(case, r)
        status = "PASS" if not problems else "FAIL"
        failed += bool(problems)
        skipped = [h for h in ("gh1", "gh2") if "skipped" in r.get(h, {})]
        print(f"{status} {case['name']}" + (f"  (skipped: {', '.join(skipped)})" if skipped else ""))
        for p in problems:
            print("   ", p)
    if a.report:
        pathlib.Path(a.report).parent.mkdir(parents=True, exist_ok=True)
        json.dump({"cases": cases, "results": results}, open(a.report, "w"), indent=2)
    print(f"\n{len(cases) - failed}/{len(cases)} passed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
