"""GH1 regression check for the shared MCP call-retention fix (real HTTP, recompute, burst, deletion)."""
import argparse,concurrent.futures,json,time
from pathlib import Path
from validate_mcp_server import Validation as Native
from validate_server_input import free_port,wait_closed
from parity import RhinoMcp
class Legacy(Native):
 def script(self,source):
  common='''var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper");var sw=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Swiftlet.Gh.Rhino8");var core=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Swiftlet.Core");dynamic canvas=gh.GetType("Grasshopper.Instances").GetProperty("ActiveCanvas").GetValue(null);dynamic doc=canvas.Document;'''
  if self.cid:common+=f'''dynamic c=null;foreach(dynamic obj in doc.Objects)if(obj.InstanceGuid.ToString()=="{self.cid}")c=obj;if(c==null)throw new Exception("Missing MCP Server");'''
  result=self.client.call('run_csharp',{'script':common+source})
  if result.get('stderr'):raise RuntimeError(result['stderr'])
  return result.get('stdout','').strip()
 def setup(self,port):
  self.cid=self.script('''dynamic c=Activator.CreateInstance(sw.GetType("Swiftlet.Gh.Rhino8.Components.McpServerComponent"));c.CreateAttributes();doc.AddObject(c,false);Console.WriteLine(c.InstanceGuid);''')
  self.script(f'''dynamic path=Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Data.GH_Path"),new object[]{{0}});
  c.Params.Input[0].PersistentData.Clear();c.Params.Input[0].PersistentData.Append((dynamic)Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Types.GH_Integer"),new object[]{{{port}}}),path);
  c.Params.Input[2].PersistentData.Clear();c.Params.Input[2].PersistentData.Append((dynamic)Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Types.GH_String"),new object[]{{"MCP lifecycle"}}),path);
  var def=Activator.CreateInstance(core.GetType("Swiftlet.Core.Mcp.McpToolDefinition"),new object[]{{"echo","Echo",null}});dynamic goo=Activator.CreateInstance(sw.GetType("Swiftlet.Gh.Rhino8.Goo.McpToolDefinitionGoo"),new object[]{{def}});
  dynamic toolSource=Activator.CreateInstance(gh.GetType("Grasshopper.Kernel.Parameters.Param_GenericObject"));toolSource.CreateAttributes();doc.AddObject(toolSource,false);toolSource.PersistentData.Append(goo,path);c.Params.Input[1].AddSource(toolSource);c.ExpireSolution(false);''')
  self.script('doc.NewSolution(false,(dynamic)Enum.Parse(gh.GetType("Grasshopper.Kernel.GH_SolutionMode"),"Silent"));')
  deadline=time.monotonic()+10
  while time.monotonic()<deadline:
   if int(self.script('Console.WriteLine(c.Params.Output.Count);'))==1:return
   time.sleep(.03)
  raise AssertionError('GH1 output did not appear')
 def contexts(self,respond=False):
  script='''var found=new List<object>();foreach(dynamic output in c.Params.Output)foreach(dynamic goo in output.VolatileData.AllData(true)){if(goo==null||goo.Value==null)continue;dynamic call=goo.Value;found.Add(new{id=(string)call.CallId,tool=(string)call.ToolName,args=call.Arguments.ToJsonString(),responded=(bool)call.HasResponded});'''
  if respond:script+='call.TryRespondWithText(call.Arguments.ToJsonString());'
  return json.loads(self.script(script+'}Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(found));'))
 def run(self):
  self.client.call('g1_start');port=free_port();self.setup(port);sid=self.initialize(port)
  try:
   with concurrent.futures.ThreadPoolExecutor(max_workers=10) as pool:
    pending=pool.submit(self.call,port,sid,101);first=self.wait_context(101);self.script('c.ExpireSolution(false);');self.script('doc.NewSolution(false,(dynamic)Enum.Parse(gh.GetType("Grasshopper.Kernel.GH_SolutionMode"),"Silent"));');again=self.wait_context(101);assert first['id']==again['id'];self.contexts(True);assert json.loads(pending.result(timeout=8)['result']['content'][0]['text'])=={'value':101}
    self.checks.append('GH1 retains unanswered request on unrelated recompute')
    calls=[pool.submit(self.call,port,sid,i) for i in range(8)];end=time.monotonic()+16
    while not all(c.done() for c in calls) and time.monotonic()<end:self.contexts(True);time.sleep(.02)
    answers=[c.result(timeout=1) for c in calls];assert sorted(json.loads(a['result']['content'][0]['text'])['value'] for a in answers)==list(range(8));self.checks.append('GH1 advances all8 concurrent same-tool calls')
  finally:self.script('var sources=((System.Collections.IEnumerable)c.Params.Input[1].Sources).Cast<object>().ToArray();doc.RemoveObject(c,false);foreach(dynamic source in sources)doc.RemoveObject(source,false);');self.cid=None;wait_closed(port)
  self.checks.append('GH1 removal stops bridge');return self.checks
p=argparse.ArgumentParser();p.add_argument('--port',type=int,required=True);p.add_argument('--report',type=Path,required=True);a=p.parse_args();v=Legacy(RhinoMcp(a.port));report={}
try:report['checks']=v.run();report['status']='passed'
except Exception as e:report['error']=str(e);raise
finally:a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2));print(report)
