# Live HTTP/lifecycle checks for an already loaded Server Input spike in a dedicated slot.
import argparse
import concurrent.futures
import json
import socket
import time
import urllib.error
import urllib.request
from pathlib import Path

from parity import RhinoMcp


def free_port():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def wait_closed(port):
    end = time.monotonic() + 8
    while time.monotonic() < end:
        with socket.socket() as sock:
            if sock.connect_ex(("127.0.0.1", port)) != 0:
                return
        time.sleep(0.05)
    raise AssertionError(f"Listener still owns port {port}")


def request(port, path):
    try:
        with urllib.request.urlopen(f"http://127.0.0.1:{port}{path}", timeout=15) as response:
            return {"status": response.status, "body": response.read().decode()}
    except urllib.error.HTTPError as error:
        return {"status": error.code, "body": error.read().decode()}
    except Exception as error:
        return {"closed": type(error).__name__}


class Validation:
    def __init__(self, client):
        self.client = client
        self.component_id = None
        self.checks = []

    def script(self, source):
        common = '''var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper2");
        dynamic ed=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
        dynamic doc=ed.Canvas.Document;
        '''
        if self.component_id:
            common += f'''object target=null; foreach(dynamic o in doc.Objects.Forwards)if(o.InstanceId.ToString()=="{self.component_id}")target=o;
            if(target==null)throw new Exception("Missing ServerInput"); dynamic c=target;
            '''
        result = self.client.call("run_csharp", {"script": common + source})
        if result.get("stderr"):
            raise RuntimeError(result["stderr"])
        return result.get("stdout", "").strip()

    def setup(self, port):
        result = self.client.call("g2_place_component", {"selector": "ca0649f0-c07b-4e52-8c70-4c19d83fc409", "x": 500, "y": 250, "solve": False})
        self.component_id = result["id"]
        self.script(f'''c.Parameters.Input(0).Set(new int[]{{{port}}});c.Parameters.Input(0).Expire();
            c.DoCreateParameter((dynamic)Enum.Parse(gh.GetType("Grasshopper2.Components.Side"),"Output"),1,null);
            c.Parameters.Output(1).UserName="/api";c.VariableParameterMaintenance();c.Expire();''')
        solved = self.client.call("g2_solve_canvas")
        if not solved["solved"]:
            raise AssertionError(solved)

    def contexts(self, respond=False):
        source = '''var found=new List<object>();var runtime=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Swiftlet.Runtime");
        foreach(dynamic output in c.Parameters.Outputs){dynamic tree=output.State.Data.Tree();if(tree==null)continue;
        foreach(dynamic value in tree.NonNullItems){found.Add(new {path=(string)value.Path,route=(string)value.MatchedRoute,received=value.ReceivedAtUtc.Ticks,responded=(bool)value.HasResponded});'''
        if respond:
            source += '''object reply=runtime.GetType("Swiftlet.Runtime.ModernServerHttpResponse").GetMethod("PlainText").Invoke(null,new object[]{201,(string)value.Path,null});value.TrySetResponse((dynamic)reply);'''
        source += '''}}Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(found));'''
        return json.loads(self.script(source))

    def wait_context(self, path):
        end = time.monotonic() + 8
        while time.monotonic() < end:
            contexts = self.contexts()
            if any(value["path"] == path and not value["responded"] for value in contexts):
                return next(value for value in contexts if value["path"] == path)
            time.sleep(0.05)
        raise AssertionError(f"No background delivery for {path}")

    def run(self):
        self.client.call("g2_start")
        self.script('ed.Canvas.Document=(dynamic)gh.GetType("Grasshopper2.Doc.Document").GetMethod("NewActiveDocument").Invoke(null,null);')
        host_pid = int(self.script("Console.WriteLine(Environment.ProcessId);"))
        self.client.call("g2_start")
        listener = free_port()
        self.setup(listener)
        with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:
            pending = pool.submit(request, listener, "/api/first?unicode=%CE%BB")
            first = self.wait_context("/api/first")
            assert first["route"] == "/api"
            self.script("c.Expire();")
            self.client.call("g2_solve_canvas")
            again = self.wait_context("/api/first")
            assert again["received"] == first["received"], "A repeated solve lost/replaced an unanswered request"
            self.contexts(respond=True)
            assert pending.result(timeout=8) == {"status": 201, "body": "/api/first"}
            self.checks.append("background route delivery, exact request reuse across recompute, one-shot HTTP response")

            bursts = [pool.submit(request, listener, f"/api/burst/{index}") for index in range(8)]
            end = time.monotonic() + 12
            while not all(future.done() for future in bursts) and time.monotonic() < end:
                self.contexts(respond=True)
                time.sleep(0.02)
            answers = [future.result(timeout=2) for future in bursts]
            assert sorted(answer.get("body", "") for answer in answers) == [f"/api/burst/{index}" for index in range(8)], answers
            assert all(answer["status"] == 201 for answer in answers)
            self.checks.append("8 simultaneous requests on one route complete without dropped requests")

            self.script('''var io=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GrasshopperIO").GetType("GrasshopperIO.IO");
                dynamic clone=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod).Invoke(null,new object[]{c});
                if(clone.Parameters.OutputCount!=2||clone.Parameters.Output(1).UserName!="/api")throw new Exception("Variable routes were not persisted");
                if(c.CanRemoveParameter((dynamic)Enum.Parse(gh.GetType("Grasshopper2.Components.Side"),"Output"),0))throw new Exception("Root can be removed");''')
            self.checks.append("variable output route archive clone and protected Root output")

            pending = pool.submit(request, listener, "/api/delete")
            self.wait_context("/api/delete")
            self.client.call("g2_delete_component", {"selector": self.component_id, "solve": False})
            pending.result(timeout=8)
            wait_closed(listener)
            self.component_id = None
            self.checks.append("deletion releases listener port with a request in flight")

            self.setup(listener)
            changed = free_port()
            self.script(f"c.Parameters.Input(0).Set(new int[]{{{changed}}});c.Parameters.Input(0).Expire();c.Expire();")
            solved = self.client.call("g2_solve_canvas")
            assert solved["solved"], solved
            wait_closed(listener)
            self.checks.append("port change releases old port and starts new listener")

            self.script('c.Activity=(dynamic)Enum.Parse(gh.GetType("Grasshopper2.Doc.ObjectActivity"),"Disabled");')
            wait_closed(changed)
            self.script('c.Activity=(dynamic)Enum.Parse(gh.GetType("Grasshopper2.Doc.ObjectActivity"),"Enabled");c.Expire();')
            assert self.client.call("g2_solve_canvas")["solved"]
            self.checks.append("disable releases port; re-enable starts a fresh listener generation")

            pending = pool.submit(request, changed, "/api/close")
            self.wait_context("/api/close")
            self.script("doc.Close();")
            pending.result(timeout=8)
            wait_closed(changed)
            self.component_id = None
            assert int(self.script("Console.WriteLine(Environment.ProcessId);")) == host_pid
            self.checks.append("document close cancels in-flight request, releases listener port, and leaves Rhino responsive")
        return self.checks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    client = RhinoMcp(args.port)
    validation = Validation(client)
    report = {"status": "failed", "checks": validation.checks}
    try:
        report["checks"] = validation.run()
        report["status"] = "passed"
    except Exception as error:
        report["error"] = str(error)
        raise
    finally:
        if validation.component_id:
            try: client.call("g2_delete_component", {"selector": validation.component_id, "solve": False})
            except Exception as cleanup: report["cleanup_error"] = str(cleanup)
        report["calls"] = client.calls
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(report["status"].upper(), report.get("checks"), report.get("error", ""))


if __name__ == "__main__":
    main()
