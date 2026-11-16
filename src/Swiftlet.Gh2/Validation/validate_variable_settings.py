"""Native variable-parameter/persistent-data actions, Undo/Redo and live route checks."""
import argparse,concurrent.futures,json,socket,sys,time,urllib.request
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];sys.path.insert(0,str(ROOT/"tests/Swiftlet.Cases"));import cases
ap=argparse.ArgumentParser();ap.add_argument("--port",type=int,required=True);ap.add_argument("--report",type=Path,required=True);a=ap.parse_args()
source=(HERE/"VariableSettingsHarness.csx").read_text();evidence=[]
def call(**cmd):
 out=cases.run_csharp(a.port,source.replace("__COMMAND__",json.dumps(cmd).replace('"','""')))
 return json.loads(next(x for x in out.splitlines() if x.startswith("VARIABLE_JSON:")).split(":",1)[1])
def freeport():
 with socket.socket() as sock:sock.bind(("127.0.0.1",0));return sock.getsockname()[1]
def settled(names=None,port=None,name=None):
 end=time.monotonic()+10
 while True:
  r=call(action="read")
  if not r["running"] and r["phase"]=="Completed" and (names is None or [p["name"] for p in r["pins"]]==names) and (port is None or r["port"]==port) and (name is None or r["serverName"]==name):
   assert r["archived"]==[p["name"] for p in r["pins"]],r;return r
  if time.monotonic()>end:raise TimeoutError(r)
  time.sleep(.05)
def get(port,path):
 with urllib.request.urlopen(f"http://127.0.0.1:{port}{path}",timeout=8) as r:return r.read().decode()
try:
 port=freeport();call(action="setup",component="ServerInputComponent",port=port);settled(["/"],port)
 with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
  original_id=None
  for action,names in [("add",["/","/new-route"]),("undo",["/"]),("redo",["/","/new-route"]),("wire",["/","/new-route"]),("remove",["/"]),("undo",["/","/new-route"]),("redo",["/"])]:
   call(action=action);r=settled(names,port)
   if len(names)==2:
    if original_id is None:original_id=r["pins"][1]["id"]
    assert r["pins"][1]["id"]==original_id,r
   if action=="wire":assert r["wires"]==1,r
   if action=="undo" and len(names)==2:assert r["wires"]==1,r
   pending=pool.submit(get,port,"/new-route/test");deadline=time.monotonic()+8
   while not pending.done():
    call(action="respond")
    if time.monotonic()>deadline:raise TimeoutError("route response")
    time.sleep(.03)
   assert pending.result()==names[-1],r
   evidence.append({"case":"Server Input "+action,"passed":True,"snapshot":r})
 print("PASS Server Input add/remove + Undo/Redo: pin IDs, downstream wire, archives and real route outputs",flush=True)
 call(action="close")
 port=freeport();other=freeport();call(action="setup",component="McpServerComponent",port=port);settled(["first"],port,"Swiftlet")
 for action,expected in [("port",other),("undo",port),("redo",other)]:
  call(action=action,port=other);r=settled(["first"],expected)
  with socket.create_connection(("127.0.0.1",expected),timeout=2):pass
  evidence.append({"case":"MCP port "+action,"passed":True,"snapshot":r})
 for action,expected in [("name","UI MCP"),("undo","Swiftlet"),("redo","UI MCP")]:
  call(action=action,name="UI MCP");r=settled(["first"],other,expected);evidence.append({"case":"MCP name "+action,"passed":True,"snapshot":r})
 print("PASS MCP Port/Name native input Undo/Redo and live session configuration",flush=True)
 call(action="tools",tools=["first","second"]);r=settled(["first","second"],other);pin_id=r["pins"][1]["id"]
 call(action="wire");r=settled(["first","second"],other);assert r["wires"]==1,r
 call(action="tools",tools=["first"]);settled(["first"],other)
 call(action="undo");r=settled(["first","second"],other)
 passed=r["wires"]==1 and r["pins"][1]["id"]==pin_id
 evidence.append({"case":"MCP tool removal Undo restores downstream wire and pin ID","passed":passed,"beforePin":pin_id,"snapshot":r})
 print(("PASS" if passed else "FAIL")+" MCP tool removal Undo restores downstream wire and pin ID",flush=True)
finally:
 try:call(action="close")
 except Exception as error:print("Cleanup",error,file=sys.stderr)
 a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(evidence,indent=2))
sys.exit(0 if all(r["passed"] for r in evidence) else 1)
