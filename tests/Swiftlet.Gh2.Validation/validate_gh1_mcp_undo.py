"""GH1: remove a connected tool, native wire Undo/Redo/Undo; the output must come back with its pin id and downstream wire."""
import argparse,json,socket,sys,time
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];sys.path.insert(0,str(ROOT/"tests/Swiftlet.Cases"));import cases
ap=argparse.ArgumentParser();ap.add_argument("--port",required=True,type=int);ap.add_argument("--report",required=True,type=Path);a=ap.parse_args()
source=(HERE/"Gh1McpUndoHarness.csx").read_text();rows=[]
def call(**cmd):
 out=cases.run_csharp(a.port,source.replace("__COMMAND__",json.dumps(cmd).replace('"','""')))
 return json.loads(next(x for x in out.splitlines() if x.startswith("GH1UNDO_JSON:")).split(":",1)[1])
def settled(names):
 end=time.monotonic()+8
 while True:
  r=call(action="read")
  if [p["name"] for p in r["pins"]]==names:return r
  if time.monotonic()>end:raise TimeoutError(r)
  time.sleep(.05)
with socket.socket() as sock:sock.bind(("127.0.0.1",0));port=sock.getsockname()[1]
try:
 call(action="setup",port=port);settled(["first","second"])
 r=call(action="wire");assert r["wires"]==1,r;rows.append({"action":"before","passed":True,**r});pin=r["pins"][1]["id"]
 call(action="tools");r=settled(["first"]);rows.append({"action":"removed","passed":r["wires"]==0,**r})
 for action,names in [("undo",["first","second"]),("redo",["first"]),("undo",["first","second"])]:
  call(action=action);r=settled(names);restored=len(names)==2
  ok=r["wires"]==1 and r["pins"][1]["id"]==pin if restored else r["wires"]==0
  rows.append({"action":action,"passed":ok,**r})
 ok=all(x["passed"] for x in rows)
 print(("PASS" if ok else "FAIL")+" GH1 MCP tool removal Undo/Redo/Undo restores pin id and downstream wire",flush=True)
finally:
 call(action="close");a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(rows,indent=2))
sys.exit(0 if rows and all(x["passed"] for x in rows) else 1)
