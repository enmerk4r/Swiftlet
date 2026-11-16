"""Prove both shells send exact HTTP requests and UDP bytes using local fixtures."""
import argparse, base64, json, socket, sys, threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tests/Swiftlet.Cases"))
from cases import run_csharp,evaluate,flatten
class Handler(BaseHTTPRequestHandler):
    def handle_request(self):
        body=self.rfile.read(int(self.headers.get("Content-Length","0"))).decode()
        payload={"method":self.command,"path":self.path,"body":body,"header":self.headers.get("X-Swiftlet")}
        encoded=json.dumps(payload).encode()
        self.send_response_only(200)
        self.send_header("Content-Length",str(len(encoded)));self.send_header("Content-Type","application/json")
        self.end_headers()
        if self.command!="HEAD":self.wfile.write(encoded)
    do_GET=do_DELETE=do_POST=do_PUT=do_PATCH=do_OPTIONS=do_HEAD=handle_request
    def log_message(self,*args):pass
p=argparse.ArgumentParser(description=__doc__);p.add_argument("--port",required=True,type=int);p.add_argument("--report",type=Path)
a=p.parse_args()
server=ThreadingHTTPServer(("127.0.0.1",0),Handler)
threading.Thread(target=server.serve_forever,daemon=True).start()
udp=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);udp.bind(("127.0.0.1",0));udp.settimeout(3)
try:
    evidence=run_csharp(a.port,(Path(__file__).parent/"MigrateHttp.csx").read_text())
    if evidence.count("PASS ")!=6:raise RuntimeError("Missing saved HTTP migration evidence: "+evidence)
    print(evidence,end="")
    cases=[];expected={}
    for cls,method in [("GetRequest","GET"),("DeleteRequest","DELETE"),("PostRequest","POST"),("PutRequest","PUT"),("PatchRequest","PATCH"),("HttpRequest","OPTIONS"),("HttpRequest","GET"),("HttpRequest","HEAD")]:
        body="hello ?" if method in ["POST","PUT","PATCH"] else ""
        inputs=[{"text":f"http://127.0.0.1:{server.server_port}/echo"}]
        if cls=="HttpRequest":inputs += [{"text":method},None]
        elif body:inputs += [{"bodytext":["text/plain",body]}]
        inputs += [[{"query":["q","a & ?"]}],[{"header":["X-Swiftlet","typed-header"]}]]
        name=f"{cls}: {method}"
        cases.append({"name":name,"gh1":cls+"Component","inputs":inputs})
        expected[name]="" if method=="HEAD" else json.dumps({"method":method,"path":"/echo?q=a%20%26%20%CE%BB","body":body,"header":"typed-header"})
    cases.append({"name":"UDP exact datagrams","gh1":"UdpStreamComponent","inputs":[{"text":"127.0.0.1"},{"int":udp.getsockname()[1]},{"bytes":"AP9oZWxsbw=="}],"expect":[[{"bool":True}],[{"int":7}]]})
    source=(ROOT/"tests/Swiftlet.Cases/runner.cs").read_text(encoding="utf-8").replace("__CASES_JSON__",json.dumps(cases).replace('"','""'))
    out=run_csharp(a.port,source)
    marker="SWIFTLET_CASES_JSON:"
    line=next((x for x in out.splitlines() if x.startswith(marker)),None)
    if line is None:raise RuntimeError("No case evidence: "+out)
    results=json.loads(line[len(marker):])
    if len(results)!=len(cases):raise RuntimeError("Missing case results")
    problems=[]
    for case,result in zip(cases,results):
        errors=evaluate(case,result)
        if case["name"] in expected:
            for host in ["gh1","gh2"]:
                if not errors and (flatten(result[host]["outputs"][0])!=[{"int":200}] or flatten(result[host]["outputs"][1])!=[{"text":expected[case["name"]]}]):errors.append(host+" exact HTTP response mismatch")
        print(("FAIL " if errors else "PASS ")+case["name"])
        for error in errors:print(error)
        problems += errors
    for _ in range(2):
        data,_=udp.recvfrom(65535)
        if data!=base64.b64decode("AP9oZWxsbw=="):problems.append("UDP data mismatch")
    if a.report:
        a.report.parent.mkdir(parents=True,exist_ok=True)
        a.report.write_text(json.dumps({"cases":cases,"results":results,"problems":problems},indent=2))
    if problems:raise RuntimeError(f"{len(problems)} network parity problems")
    print("PASS exact HTTP and UDP fixture suite")
finally:
    server.shutdown();server.server_close();udp.close()
