"""Actual Eto picker change events + native GH2 Undo/Redo, without desktop input."""
import argparse,json,sys,tempfile,threading,time
from pathlib import Path
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/"tests/Swiftlet.Cases"));import cases
ap=argparse.ArgumentParser();ap.add_argument("--port",type=int,required=True);ap.add_argument("--report",type=Path,required=True);a=ap.parse_args()
source=(HERE/"SettingsHarness.csx").read_text();evidence=[]
class Handler(BaseHTTPRequestHandler):
 def do_GET(self):
  if self.headers.get("Content-Length"):self.rfile.read(int(self.headers["Content-Length"]))
  time.sleep(1.5);body=b"settings-ok"
  try:self.send_response(200);self.send_header("Content-Length",str(len(body)));self.end_headers();self.wfile.write(body)
  except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):pass
 do_POST=do_PUT=do_PATCH=do_DELETE=do_GET
 def log_message(self,*args):pass
server=ThreadingHTTPServer(("127.0.0.1",0),Handler);threading.Thread(target=server.serve_forever,daemon=True).start()
def call(**command):
 out=cases.run_csharp(a.port,source.replace("__COMMAND__",json.dumps(command).replace('"','""')))
 line=next(x for x in out.splitlines() if x.startswith("SETTINGS_JSON:"));return json.loads(line.split(":",1)[1])
try:
 with tempfile.TemporaryDirectory(prefix="swiftlet-settings-") as temp:
  file=Path(temp)/"save.bin"
  names=["CreateTextBodyComponent","SaveWebResponseComponent","GetRequestComponent","PostRequestComponent","PutRequestComponent","PatchRequestComponent","DeleteRequestComponent","HttpRequestComponent"]
  for name in names:
   states=[]
   for action in ["setup","change","undo","redo"]:
    args={"action":action}
    if action=="setup":args.update(component=name,url=f"http://127.0.0.1:{server.server_port}/slow",file=str(file))
    if action=="change":args["index"]=1 if name=="SaveWebResponseComponent" else 0
    result=call(**args)
    deadline=time.monotonic()+10
    while result["running"] or result["phase"]!="Completed":
     if time.monotonic()>deadline:raise TimeoutError(result)
     time.sleep(.05);result=call(action="read")
    changed=action in ("change","redo")
    expected=("text/plain" if changed else "application/json") if name=="CreateTextBodyComponent" else changed if name=="SaveWebResponseComponent" else 1 if changed else 100
    assert result["state"]==expected and result["archived"]==expected,result
    assert result["phase"]=="Completed",result
    if name=="CreateTextBodyComponent":assert result["outputs"][0][0]["ContentType"]==expected,result
    elif name=="SaveWebResponseComponent":
     content=b"text fixture" if changed else bytes([1,2,3]);assert file.read_bytes()==content,result
     assert result["outputs"][0][0]==len(content),result
    else:assert result["outputs"][0][0]==(-1 if changed else 200),result
    states.append({"action":action,**result})
   evidence.append({"component":name,"passed":True,"states":states});print("PASS "+name+" picker / undo / redo: state, archive, outputs",flush=True)
finally:
 server.shutdown();server.server_close();a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(evidence,indent=2))
