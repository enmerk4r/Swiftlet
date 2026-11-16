"""Deterministic scheduler regressions; run on an exclusively owned Rhino slot after cases.py --load.

Forces SolutionStopped before Requeue with the batch outside the pending map. Separately injects
an actual transfer Changed notification after disable/Stop, waits for the scheduler to discard it,
then re-enables and verifies progress and completion from a real throttled loopback download.
Uses private reflection only to place the two race windows deterministically; no product hooks.
"""
import argparse, hashlib, http.server, json, pathlib, sys, tempfile, threading, time
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from cases import run_csharp

PRE = r'''
using System; using System.Linq; using System.Reflection; using System.Collections; using System.Collections.Generic; using System.Text.Json;
var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
var sw = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Swiftlet.Gh2");
var gh = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Grasshopper2");
dynamic editor = gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);
dynamic doc = editor.Canvas.Document;
void Save(string key, object value) => AppDomain.CurrentDomain.SetData("swiftlet.races."+key, value);
object Read(string key) => AppDomain.CurrentDomain.GetData("swiftlet.races."+key);
void Print(object value) => Console.WriteLine("RACE:"+JsonSerializer.Serialize(value));
'''
REQUEUE = r'''
if (doc.Solution.State.CurrentlyRunning != null) throw new Exception("slot not idle");
var c = Activator.CreateInstance(sw.GetTypes().First(t => t.Name == "CreateTextBodyComponent"));
doc.Objects.Add((dynamic)c);
var scheduler = sw.GetType("Swiftlet.Gh2.BackgroundSolveScheduler", true);
var ct = scheduler.GetNestedType("Coordinator", BindingFlags.NonPublic);
var pt = scheduler.GetNestedType("Pending", BindingFlags.NonPublic);
var coordinator = Activator.CreateInstance(ct, flags, null, new object[]{(object)doc}, null);
var counters = new int[3];
Action prepare = () => System.Threading.Interlocked.Increment(ref counters[0]);
Action release = () => System.Threading.Interlocked.Increment(ref counters[1]);
Func<bool> current = () => true;
var constructor = pt.GetConstructors(flags).OrderByDescending(x => x.GetParameters().Length).First();
object pending = constructor.Invoke(constructor.GetParameters().Length == 4 ? new object[]{c,current,prepare,release} : new object[]{c,current,prepare});
var batch = Array.CreateInstance(pt,1); batch.SetValue(pending,0);
// Initialize subscriptions while there is no pending work, then produce a REAL stop event.
ct.GetMethod("Pump",flags).Invoke(coordinator,null);
var ev = ((object)doc.Solution).GetType().GetEvent("SolutionStopped");
Action bump = () => System.Threading.Interlocked.Increment(ref counters[2]);
var ps = ev.EventHandlerType.GetMethod("Invoke").GetParameters().Select(x => System.Linq.Expressions.Expression.Parameter(x.ParameterType)).ToArray();
var stopped = System.Linq.Expressions.Expression.Lambda(ev.EventHandlerType,System.Linq.Expressions.Expression.Invoke(System.Linq.Expressions.Expression.Constant(bump)),ps).Compile();
ev.AddEventHandler((object)doc.Solution, stopped);
doc.Solution.StartWait();
if (counters[2] != 1) throw new Exception("expected exactly one controlled solution stop");
if (doc.Solution.State.CurrentlyRunning != null) throw new Exception("controlled solve did not stop");
// This is precisely the missed-stop ordering: callback fired with an empty map, THEN batch returns.
ct.GetMethod("Requeue",flags).Invoke(coordinator,new object[]{batch});
Save("requeue",coordinator); Save("requeueComponent",c); Save("counters",counters); Save("stoppedHandler",stopped);
Print(new { staged=true, stopped=counters[2], releaseSupported=constructor.GetParameters().Length == 4 });
'''
REQUEUE_READ = r'''
var counters = (int[])Read("counters");
var co = Read("requeue"); var pending = (IDictionary)co.GetType().GetField("_pending",flags).GetValue(co);
Print(new { prepared=counters[0], released=counters[1], stops=counters[2], pending=pending.Count });
'''
REQUEUE_CLEAN = r'''
var co = Read("requeue"); co.GetType().GetMethod("Dispose",flags).Invoke(co,null);
((object)doc.Solution).GetType().GetEvent("SolutionStopped").RemoveEventHandler((object)doc.Solution,(Delegate)Read("stoppedHandler"));
doc.Objects.Remove((dynamic)Read("requeueComponent"));
Print(new { cleaned=true });
'''
TRANSFER_DROP = r'''
var c = Activator.CreateInstance(sw.GetTypes().First(t => t.Name == "DownloadFileComponent"));
doc.Objects.Add((dynamic)c);
var bt = c.GetType().BaseType;
var activity = c.GetType().GetProperty("Activity");
Save("active",activity.GetValue(c));
activity.SetValue(c,Enum.Parse(activity.PropertyType,"Disabled")); // invokes the real Stop
Save("transfer",c);
// Invoke the same subscribed delegate that a worker captured before Stop can call afterwards.
var job = bt.GetProperty("Job",flags).GetValue(c);
var changed = (Delegate)job.GetType().BaseType.GetField("Changed",flags).GetValue(job);
changed.DynamicInvoke(false);
Print(new { queued=(int)bt.GetField("_expiryPosted",flags).GetValue(c), activity=activity.GetValue(c).ToString() });
'''
TRANSFER_FLAG = r'''
var c = Read("transfer");
Print(new { queued=(int)c.GetType().BaseType.GetField("_expiryPosted",flags).GetValue(c) });
'''
TRANSFER_START = r'''
dynamic c = Read("transfer");
((object)c).GetType().GetProperty("Activity").SetValue((object)c,Read("active"));
void Set(int i, object value) { dynamic p=c.Parameters.Input(i); p.Set((dynamic)value); p.Expire(); }
Set(0,new[]{"__URL__"}); Set(1,new[]{@"__PATH__"}); Set(4,new[]{true}); Set(5,new[]{true});
c.Expire(); doc.Solution.StartWait();
Print(new { started=true });
'''
TRANSFER_READ = r'''
dynamic c = Read("transfer"); var state = c.State; var data=state.Data;
object[] Values(int i) => data == null ? Array.Empty<object>() : ((IEnumerable)data.Tree((Guid)c.Parameters.Output(i).InstanceId).AllItems).Cast<object>().ToArray();
var done=Values(0); var progress=Values(1); var status=Values(3); var error=Values(5);
Print(new { done=done.Length>0 && done[0] is bool b && b, progress=progress.Length>0 ? Convert.ToDouble(progress[0]) : -1,
 status=status.Select(x=>x?.ToString()).ToArray(), errors=error.Select(x=>x?.ToString()).ToArray(), phase=state.Phase.ToString(),
 queued=(int)((object)c).GetType().BaseType.GetField("_expiryPosted",flags).GetValue((object)c) });
'''
TRANSFER_CLEAN = r'''
if (Read("transfer") is object c) doc.Objects.Remove((dynamic)c);
Print(new { cleaned=true });
'''


def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--port',type=int,required=True); ap.add_argument('--report',required=True)
    args=ap.parse_args(); report={'port':args.port,'checks':{}}
    def run(body):
        text=run_csharp(args.port,PRE+body)
        rows=[line[5:] for line in text.splitlines() if line.startswith('RACE:')]
        if not rows: raise RuntimeError(text)
        return json.loads(rows[-1])
    def poll(body,predicate,seconds):
        until=time.monotonic()+seconds; values=[]
        while True:
            row=run(body); values.append(row)
            if predicate(row) or time.monotonic()>=until: return row,values
            time.sleep(.15)
    report['assembly']=run('Print(new { pid=System.Environment.ProcessId,path=sw.Location,mvid=sw.ManifestModule.ModuleVersionId.ToString() });')
    report['assembly']['sha256']=hashlib.sha256(pathlib.Path(report['assembly']['path']).read_bytes()).hexdigest()
    try:
        setup=run(REQUEUE)
        row,history=poll(REQUEUE_READ,lambda r:r['prepared']==1 and r['pending']==0,3)
        report['checks']['post_stop_requeue']={'passed':row['prepared']==1 and row['pending']==0 and (not setup['releaseSupported'] or row['released']==1),'setup':setup,'result':row,'history':history}
    finally: run(REQUEUE_CLEAN)
    payload=bytes(range(256))*8192
    class Handler(http.server.BaseHTTPRequestHandler):
        def do_GET(self):
            self.send_response(200); self.send_header('Content-Length',str(len(payload))); self.end_headers()
            try:
                for offset in range(0,len(payload),32768):
                    self.wfile.write(payload[offset:offset+32768]); self.wfile.flush(); time.sleep(.09)
            except (BrokenPipeError,ConnectionResetError): pass
        def log_message(self,*_): pass
    server=http.server.ThreadingHTTPServer(('127.0.0.1',0),Handler)
    thread=threading.Thread(target=server.serve_forever,daemon=True); thread.start()
    target=pathlib.Path(tempfile.mkdtemp(prefix='swiftlet-race-'))/'download.bin'
    try:
        setup=run(TRANSFER_DROP)
        released,release_history=poll(TRANSFER_FLAG,lambda r:r['queued']==0,3)
        run(TRANSFER_START.replace('__URL__',f'http://127.0.0.1:{server.server_port}/payload').replace('__PATH__',str(target)))
        row,history=poll(TRANSFER_READ,lambda r:r['done'],16)
        progress=any(0<float(r['progress'])<100 and not r['done'] for r in history)
        file_ok=target.exists() and target.read_bytes()==payload
        report['checks']['late_transfer_after_stop']={'passed':released['queued']==0 and progress and row['done'] and file_ok,'setup':setup,'release':released,'releaseHistory':release_history,'progressObserved':progress,'fileMatches':file_ok,'result':row,'history':history}
    finally:
        run(TRANSFER_CLEAN); server.shutdown(); server.server_close()
    report['alive']=run('Print(new { pid=System.Environment.ProcessId });')
    report['passed']=all(r['passed'] for r in report['checks'].values()) and report['alive']['pid']==report['assembly']['pid']
    path=pathlib.Path(args.report); path.parent.mkdir(parents=True,exist_ok=True); path.write_text(json.dumps(report,indent=2))
    for name,result in report['checks'].items(): print(('PASS ' if result['passed'] else 'FAIL ')+name,flush=True)
    return 0 if report['passed'] else 1

if __name__=='__main__': sys.exit(main())
