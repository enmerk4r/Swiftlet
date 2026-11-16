"""Live OAuth token and authorization lifecycles in both hosts, using local fixtures only."""
import argparse,json,socket,sys,threading,time,urllib.parse,urllib.request
from collections import Counter
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(ROOT/"tests/Swiftlet.Parity"))
from parity import RhinoMcp
requests=Counter()
class Handler(BaseHTTPRequestHandler):
 def do_POST(self):
  form=urllib.parse.parse_qs(self.rfile.read(int(self.headers["Content-Length"])).decode())
  client=form["client_id"][0];requests[client]+=1
  if self.path=="/slow":time.sleep(10)
  refresh=form.get("grant_type")==["refresh_token"]
  payload={"access_token":"refreshed" if refresh else "access","expires_in":60,"token_type":"Bearer"}
  if not refresh:payload["refresh_token"]="refresh-1"
  body=json.dumps(payload).encode();self.send_response(200);self.send_header("Content-Length",str(len(body)));self.end_headers()
  try:self.wfile.write(body)
  except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):pass
 def log_message(self,*args):pass
p=argparse.ArgumentParser(description=__doc__);p.add_argument("--port",required=True,type=int);p.add_argument("--report",type=Path)
a=p.parse_args();client=RhinoMcp(a.port);targets=[];evidence=[]
initial=client.call("run_csharp",{"script":"Console.WriteLine(\"ALIVE \"+Environment.ProcessId);"}).get("stdout", "").strip()
assert initial.startswith("ALIVE "),initial
server=ThreadingHTTPServer(("127.0.0.1",0),Handler);threading.Thread(target=server.serve_forever,daemon=True).start()
source=(HERE/"OAuthHarness.csx").read_text()
def call(host,action,**values):
 command={"host":host,"action":action,**values}
 result=client.call("run_csharp",{"script":source.replace("__COMMAND_JSON__",json.dumps(command).replace('"','""'))})
 if result.get("stderr"):raise RuntimeError(result["stderr"])
 marker="OAUTH_JSON:";line=next((x for x in result.get("stdout","").splitlines() if x.startswith(marker)),None)
 if line is None:raise RuntimeError(result)
 return json.loads(line[len(marker):])
def until(host,target,predicate):
 deadline=time.monotonic()+8
 while True:
  value=call(host,"read",id=target)
  if predicate(value["values"]):return value["values"]
  if time.monotonic()>deadline:raise RuntimeError(f"OAuth wait failed: {value}")
  time.sleep(.05)
def passed(message):print("PASS "+message,flush=True);evidence.append(message)
def freeport():
 with socket.socket() as s:s.bind(("127.0.0.1",0));return s.getsockname()[1]
def released(port):
 with socket.socket() as s:s.bind(("127.0.0.1",port))
try:
 for host in [1,2]:
  name=f"host{host}";endpoint=f"http://127.0.0.1:{server.server_port}/token"
  result=call(host,"create",**{"class":"OAuthTokenComponent"},inputs={"0":endpoint,"1":name,"2":"one-use","4":"http://localhost/callback/"})
  target=result["id"];targets.append((host,target))
  values=until(host,target,lambda v:v[0]=="access")
  assert values[:4]==["access","refresh-1",60,"Bearer"] and values[4]=={"Key":"Authorization","Value":"Bearer access"},values
  for _ in range(2):call(host,"read",id=target)
  assert requests[name]==1,requests
  call(host,"update",id=target,inputs={"6":True});values=until(host,target,lambda v:v[0]=="refreshed")
  assert values[1]=="refresh-1",values
  call(host,"read",id=target);assert requests[name]==2,requests
  values=call(host,"update",id=target,inputs={"6":False})["values"]
  assert values[0]=="refreshed" and requests[name]==2,(values,requests)
  passed(f"GH{host} token exchange, duplicates, refresh edge and no authorization-code replay")
  assert call(host,"archive",id=target)["archived"]
  passed(f"GH{host} token archive excludes cached credentials")
  if host==1:
   call(host,"update",id=target,inputs={"0":endpoint.replace("/token","/slow"),"2":"new-code"})
   deadline=time.monotonic()+3
   while requests[name]<3 and time.monotonic()<deadline:time.sleep(.01)
   assert requests[name]>=3
  if host==2:
   cancelled=call(host,"cancel",id=target,inputs={"0":endpoint.replace("/token","/slow"),"2":"new-code"})
   assert cancelled["cancelled"];passed(f"GH2 token cancellation {cancelled['elapsed']}ms")
  call(host,"remove",id=target);targets.remove((host,target))
 for host in [1,2]:
  port=freeport();result=call(host,"create",**{"class":"OAuthAuthorizeComponent"},inputs={"0":"https://auth.example.test/authorize","1":"client","3":port,"4":False})
  target=result["id"];targets.append((host,target))
  assert result["values"][3]=="Ready (set Authorize to true to start)",result
  values=call(host,"update",id=target,inputs={"4":True})["values"]
  assert values[0] is None and len(values[1])>=43,values
  url=values[3][values[3].index("https://auth.example.test"):]
  state=urllib.parse.parse_qs(urllib.parse.urlsplit(url).query)["state"][0]
  with urllib.request.urlopen(f"http://localhost:{port}/callback/?code=live-code&state="+urllib.parse.quote(state),timeout=5) as response:assert response.status==200
  values=until(host,target,lambda v:v[0]=="live-code")
  assert values[3]=="Authorization successful",values
  released(port)
  assert call(host,"archive",id=target)["archived"]
  passed(f"GH{host} authorization archive excludes completed code and verifier")
  call(host,"update",id=target,inputs={"4":False});values=call(host,"update",id=target,inputs={"4":True})["values"]
  assert values[0] is None,values
  if host==2:
   call(host,"disable",id=target);released(port)
   values=call(host,"enable",id=target)["values"];assert values[0] is None,values
   passed("GH2 authorization disable/re-enable releases and recreates callback")
  call(host,"remove",id=target);targets.remove((host,target));released(port)
  passed(f"GH{host} real callback completion, fresh attempt, delete releases port")
 port=freeport()
 result=call(2,"create",**{"class":"OAuthAuthorizeComponent"},inputs={"0":"https://auth.example.test/authorize","1":"client","3":port,"4":True})
 target=result["id"];targets.append((2,target))
 call(2,"close",id=target);targets.remove((2,target));released(port)
 alive=client.call("run_csharp",{"script":"Console.WriteLine(\"ALIVE \"+Environment.ProcessId);"})
 assert alive.get("stdout", "").strip()==initial,alive
 passed("GH2 document close releases pending authorization callback; same PID/API stays alive")
 print("PASS OAuth live suite",flush=True)
finally:
 for host,target in targets:
  try:call(host,"remove",id=target)
  except Exception as e:print("Cleanup failed",e,file=sys.stderr)
 server.shutdown();server.server_close()
 if a.report:
  a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps({"passed":evidence,"requests":dict(requests)},indent=2))
