"""Native MCP Server lifecycle checks; closes its dedicated scratch GH2 document."""
import argparse, concurrent.futures, json, sys, time, urllib.request
from pathlib import Path
from parity import RhinoMcp
from validate_server_input import free_port, wait_closed

class Validation:
    def __init__(self,client): self.client=client; self.cid=None; self.checks=[]; self.created=[]
    def script(self,source):
        common='''var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper2");
        var sw=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Swiftlet.Gh2");
        var core=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Swiftlet.Core");
        dynamic ed=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);dynamic doc=ed.Canvas.Document;
        '''
        if self.cid: common+=f'''object target=null;foreach(dynamic o in doc.Objects.Forwards)if(o.InstanceId.ToString()=="{self.cid}")target=o;
        if(target==null)throw new Exception("Missing MCPServer");dynamic c=target;'''
        r=self.client.call('run_csharp',{'script':common+source})
        if r.get('stderr'): raise RuntimeError(r['stderr'])
        return r.get('stdout','').strip()
    def tools(self,names):
        literals=','.join(json.dumps(n) for n in names)
        self.script('''var names=new string[]{'''+literals+'''};var tt=core.GetType("Swiftlet.Core.Mcp.McpToolDefinition");var pt=core.GetType("Swiftlet.Core.Mcp.McpToolParameter");
        var values=Array.CreateInstance(tt,names.Length);for(int i=0;i<names.Length;i++)values.SetValue(Activator.CreateInstance(tt,new object[]{names[i],"Echo test tool",Array.CreateInstance(pt,0)}),i);
        c.Parameters.Input(1).Set((dynamic)values);c.Parameters.Input(1).Expire();c.Expire();''')
        self.client.call('g2_solve_canvas')
        deadline=time.monotonic()+12
        while time.monotonic()<deadline:
            if set(json.loads(self.script('Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(((System.Collections.IEnumerable)c.Parameters.Outputs).Cast<object>().Select(p=>(string)((dynamic)p).UserName)));')))==set(names):
                # Synchronizing outputs schedules the solution which starts/reconfigures the server.
                self.script('c.Expire();');self.client.call('g2_solve_canvas');return
            time.sleep(.05)
        raise AssertionError('dynamic outputs did not appear')
    def setup(self,port):
        r=self.client.call('g2_place_component',{'selector':'1ea3f102-d640-4c21-955a-b8791728eb8a','x':450,'y':200,'solve':False}); self.cid=r['id'];self.created.append(self.cid)
        self.script(f'c.Parameters.Input(0).Set(new int[]{{{port}}});c.Parameters.Input(0).Expire();c.Parameters.Input(2).Set(new string[]{{"MCP lifecycle"}});c.Parameters.Input(2).Expire();')
        self.tools(['echo'])
    def rpc(self,port,method,params=None,sid=None,id=1):
        body={'jsonrpc':'2.0','id':id,'method':method,'params':params or {}}
        headers={'Content-Type':'application/json','Accept':'application/json,text/event-stream'}
        if sid:headers['Mcp-Session-Id']=sid
        req=urllib.request.Request(f'http://127.0.0.1:{port}/mcp/',json.dumps(body).encode(),headers)
        with urllib.request.urlopen(req,timeout=18) as r: raw=r.read().decode();sid=r.headers.get('Mcp-Session-Id',sid)
        if raw.startswith(('event:','data:')):raw=next(x[5:].strip() for x in raw.splitlines() if x.startswith('data:'))
        return json.loads(raw),sid
    def initialize(self,port):
        result,sid=self.rpc(port,'initialize');assert result['result']['serverInfo']['name']=='MCP lifecycle',result
        result,_=self.rpc(port,'tools/list',sid=sid);assert result['result']['tools'][0]['name']=='echo',result
        return sid
    def contexts(self,respond=False):
        s='''var found=new List<object>();foreach(dynamic output in c.Parameters.Outputs){dynamic tree=output.State.Data.Tree();if(tree==null)continue;foreach(dynamic call in tree.NonNullItems){found.Add(new{id=(string)call.CallId,tool=(string)call.ToolName,args=call.Arguments.ToJsonString(),responded=(bool)call.HasResponded});'''
        if respond:s+='call.TryRespondWithText(call.Arguments.ToJsonString());'
        s+='}}Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(found));'
        return json.loads(self.script(s))
    def wait_context(self,value):
        end=time.monotonic()+10
        while time.monotonic()<end:
            for c in self.contexts():
                if not c['responded'] and json.loads(c['args']).get('value')==value:return c
            time.sleep(.03)
        raise AssertionError(f'no context for {value}')
    def call(self,port,sid,value):return self.rpc(port,'tools/call',{'name':'echo','arguments':{'value':value}},sid,id=value)[0]
    def run(self):
        self.client.call("g2_start")
        self.script('ed.Canvas.Document=(dynamic)gh.GetType("Grasshopper2.Doc.Document").GetMethod("NewActiveDocument").Invoke(null,null);')
        host_pid = int(self.script("Console.WriteLine(Environment.ProcessId);"))
        port=free_port();self.setup(port);sid=self.initialize(port)
        with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:
            call=pool.submit(self.call,port,sid,101);first=self.wait_context(101)
            self.script('c.Expire();');self.client.call('g2_solve_canvas');again=self.wait_context(101);assert again['id']==first['id']
            self.contexts(True);answer=call.result(timeout=8);assert json.loads(answer['result']['content'][0]['text'])=={'value':101},answer
            self.checks.append('background call delivery, unanswered handle retained across recompute, one-shot response')
            calls=[pool.submit(self.call,port,sid,i) for i in range(8)]
            end=time.monotonic()+15
            while not all(f.done() for f in calls) and time.monotonic()<end:self.contexts(True);time.sleep(.02)
            answers=[f.result(timeout=1) for f in calls];assert sorted(json.loads(a['result']['content'][0]['text'])['value'] for a in answers)==list(range(8)),answers
            self.checks.append('8 concurrent same-tool calls complete without loss')
            response=self.client.call('g2_place_component',{'selector':'33d16e46-3796-511d-81ad-2fa414f3f023','x':750,'y':200,'solve':False})['id']
            text=self.client.call('g2_place_component',{'selector':'40c5ff0e-cb1f-5b2a-897e-e27c1231b226','x':450,'y':400,'solve':False})['id']
            self.created.extend([response,text])
            self.script(f'''foreach(dynamic obj in doc.Objects.Forwards)if(obj.InstanceId.ToString()=="{text}"){{obj.Parameters.Input(0).Set(new string[]{{"wired response"}});obj.Parameters.Input(0).Expire();obj.Expire();}}''')
            self.client.call('g2_connect',{'src_id':self.cid,'src':'0','dst_id':response,'dst':'0','solve':False})
            self.client.call('g2_connect',{'src_id':text,'src':'0','dst_id':response,'dst':'1','solve':False})
            self.client.call('g2_solve_canvas')
            answer=self.call(port,sid,58);assert answer['result']['content'][0]['text']=='wired response',answer
            self.checks.append('native graph MCP Server -> MCP Tool Response with typed text content completes real HTTP call')
            for node in [response,text]:self.client.call('g2_delete_component',{'selector':node,'solve':False});self.created.remove(node)

            self.script('''var io=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="GrasshopperIO").GetType("GrasshopperIO.IO");dynamic clone=io.GetMethods().Single(m=>m.Name=="Duplicate"&&!m.IsGenericMethod).Invoke(null,new object[]{c});
            if(clone.Parameters.OutputCount!=1||clone.Parameters.Output(0).UserName!="echo")throw new Exception("Dynamic output archive failed");
            var configTarget=core.GetType("Swiftlet.Core.Mcp.McpClientConfigTarget");foreach(object kind in Enum.GetValues(configTarget)){string text=c.GenerateClientConfig((dynamic)kind);if(kind.ToString()!="Codex")System.Text.Json.Nodes.JsonNode.Parse(text);if(!text.Contains("MCP lifecycle")||!text.Contains("mcp/"))throw new Exception("Config mismatch");}
            var panel=Activator.CreateInstance(gh.GetType("Grasshopper2.UI.InputPanel.InputPanel"));c.AppendToInputPanel((dynamic)panel);''')
            self.checks.append('archive clone preserves dynamic output, five client configs, Eto panel construction')
            output=self.script('Console.WriteLine(c.Parameters.Output(0).InstanceId);')
            self.tools(['echo','second']);assert self.script('Console.WriteLine(c.Parameters.Output(0).InstanceId);')==output
            self.tools(['echo']);assert self.script('Console.WriteLine(c.Parameters.Output(0).InstanceId);')==output
            self.checks.append('adding/removing another tool preserves existing output identity/wiring')
            pending=pool.submit(self.call,port,sid,201);self.wait_context(201);self.tools([]);answer=pending.result(timeout=8);assert answer['result']['isError'],answer
            self.checks.append('removing a tool completes its active request with an error')
            self.tools(['echo']);changed=free_port();self.script(f'c.Parameters.Input(0).Set(new int[]{{{changed}}});c.Parameters.Input(0).Expire();c.Expire();');self.client.call('g2_solve_canvas');wait_closed(port);sid=self.initialize(changed)
            self.checks.append('port change releases old port and starts new bridge')
            self.script('c.Activity=(dynamic)Enum.Parse(gh.GetType("Grasshopper2.Doc.ObjectActivity"),"Disabled");');wait_closed(changed)
            self.script('c.Activity=(dynamic)Enum.Parse(gh.GetType("Grasshopper2.Doc.ObjectActivity"),"Enabled");c.Expire();');self.client.call('g2_solve_canvas');sid=self.initialize(changed)
            self.checks.append('disable releases bridge; re-enable starts fresh generation')
            pending=pool.submit(self.call,changed,sid,301);self.wait_context(301);self.client.call('g2_delete_component',{'selector':self.cid,'solve':False});self.created.remove(self.cid);self.cid=None
            try:pending.result(timeout=8)
            except Exception:pass
            wait_closed(changed);self.checks.append('deletion with active call releases bridge port')
            self.setup(port);sid=self.initialize(port);pending=pool.submit(self.call,port,sid,401);self.wait_context(401);self.script('doc.Close();');self.created.clear();self.cid=None
            try:pending.result(timeout=8)
            except Exception:pass
            wait_closed(port);assert int(self.script('Console.WriteLine(Environment.ProcessId);'))==host_pid;self.checks.append('document close with active call releases bridge port and leaves Rhino responsive')
        return self.checks

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--port',type=int,required=True);ap.add_argument('--report',type=Path,required=True);a=ap.parse_args();v=Validation(RhinoMcp(a.port));r={'status':'failed','checks':v.checks}
    try:r['checks']=v.run();r['status']='passed'
    except Exception as e:r['error']=str(e);raise
    finally:
        for cid in v.created:
            try:v.client.call('g2_delete_component',{'selector':cid,'solve':False})
            except Exception:pass
        r['calls']=v.client.calls;a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(r,indent=2),encoding='utf-8');print(r['status'].upper(),r['checks'],r.get('error',''))
if __name__=='__main__':main()


