"""Live GET spike checks in a dedicated GH2 slot with the latest Swiftlet build loaded."""
import argparse, json, sys, threading, time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[2] / "tests" / "Swiftlet.Parity"))
from parity import RhinoMcp

class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith("/slow"):
            time.sleep(10)
        data = json.dumps({"path": self.path, "header": self.headers.get("X-Swiftlet")}).encode()
        self.send_response(200)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        try:
            self.wfile.write(data)
        except (BrokenPipeError, ConnectionResetError):
            pass
    def log_message(self, *args):
        pass

p = argparse.ArgumentParser(description=__doc__)
p.add_argument("--port", required=True, type=int)
p.add_argument("--gh1", type=Path, help="Optional built GH1 .gha for saved-state migration check")
p.add_argument("--output", type=Path)
args = p.parse_args()
client = RhinoMcp(args.port)
server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
threading.Thread(target=server.serve_forever, daemon=True).start()
base = f"http://127.0.0.1:{server.server_port}"
target = None
results = []
try:
    client.call("g2_start")
    target = client.call("g2_place_component", {"selector": "3bad80b0-77e4-4c32-b35c-d0a73ff6bf59", "solve": False})["id"]
    def script(name, *, path="/ok", header=None):
        source = (HERE / name).read_text(encoding="utf-8-sig")
        source = source.replace("__TARGET_ID__", target).replace("__HTTP_BASE__", base)
        source = source.replace("__EXPECT_PATH_JSON__", json.dumps(path)).replace("__EXPECT_HEADER_JSON__", json.dumps(header))
        if args.gh1:
            source = source.replace("__GH1_PATH_JSON__", json.dumps(str(args.gh1.resolve(strict=True))))
        result = client.call("run_csharp", {"script": source})
        if result.get("stderr"):
            raise RuntimeError(result["stderr"])
        results.append({"check": name, "stdout": result.get("stdout", "")})
        print(result.get("stdout", ""), end="")
    def solve():
        result = client.call("g2_solve_canvas")
        if not result["solved"] or result["errors"] or result["warnings"]:
            raise RuntimeError(result)
    script("SetGetInputs.csx")
    solve()
    script("ValidateGet.csx")
    script("SetGetTypedInputs.csx")
    solve()
    script("ValidateGet.csx", path="/ok?q=a%20b%26%CE%BB", header="typed-header")
    script("CancelGet.csx")
    if args.gh1:
        client.call("g1_start")
        script("MigrateGet.csx")
    print("PASS GET spike suite")
finally:
    if target:
        client.call("g2_delete_component", {"selector": target, "solve": False})
    server.shutdown()
    server.server_close()
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(results, indent=2), encoding="utf-8")

