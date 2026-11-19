"""Run one storm placement in each of several fresh, privately owned Rhino processes."""
import argparse,ctypes,json,os,queue,subprocess,sys,threading,time,urllib.error
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Swiftlet.Cases'))
from cases import stage_and_load,run_csharp
p=argparse.ArgumentParser();p.add_argument('--count',type=int,default=4);p.add_argument('--report',type=Path,required=True);p.add_argument('--base-port',type=int,default=24200);a=p.parse_args()
repo=Path(__file__).resolve().parents[2];out=a.report.parent;out.mkdir(parents=True,exist_ok=True);records=[]
k=ctypes.WinDLL('kernel32',use_last_error=True);k.OpenProcess.restype=ctypes.c_void_p
k.GetProcessTimes.argtypes=[ctypes.c_void_p]+[ctypes.c_void_p]*4;k.WaitForSingleObject.argtypes=[ctypes.c_void_p,ctypes.c_uint32];k.TerminateProcess.argtypes=[ctypes.c_void_p,ctypes.c_uint32];k.CloseHandle.argtypes=[ctypes.c_void_p]
for i in range(a.count):
 stop=out/f'fresh-storm-{i+1}.stop';env=dict(os.environ);env.pop('DOTNET_ROOT',None)
 slot=subprocess.Popen([sys.executable,str(repo/'tests/Swiftlet.Cases/slot.py'),'--stop-file',str(stop.resolve())],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,bufsize=1,env=env,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
 q=queue.Queue();handle=None;payload=None;r={'round':i+1}
 def pump():
  for line in slot.stdout:q.put(line)
  q.put(None)
 threading.Thread(target=pump,daemon=True).start()
 try:
  deadline=time.monotonic()+120
  while time.monotonic()<deadline:
   line=q.get(timeout=60)
   if line is None:raise RuntimeError('slot launcher exited before SLOT')
   if line.startswith('SLOT '):payload=json.loads(line[5:]);break
  assert payload,payload;r['slot']=payload;print('fresh storm slot',payload,flush=True)
  deadline=time.monotonic()+40
  while True:
   try:
    metadata=json.loads(run_csharp(payload['port'],'Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{pid=Environment.ProcessId,root=Environment.GetEnvironmentVariable("DOTNET_ROOT"),creation=System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime().ToFileTimeUtc()}));',timeout=20));break
   except urllib.error.URLError:
    if time.monotonic()>deadline:raise
    time.sleep(.5)
  assert metadata['pid']==payload['pid'] and metadata['root'] is None,metadata
  handle=k.OpenProcess(0x00100000|0x1000|1,False,payload['pid']);assert handle,ctypes.get_last_error()
  times=[ctypes.c_uint64() for _ in range(4)];assert k.GetProcessTimes(handle,*[ctypes.byref(t) for t in times]);assert times[0].value==metadata['creation'],metadata
  stage_and_load(payload['port'])
  report=out/f'fresh-storm-{i+1}.json'
  run=subprocess.run([sys.executable,str(repo/'tests/Swiftlet.Cases/checks/solve_storm.py'),'--port',str(payload['port']),'--rounds','1','--base-port',str(a.base_port+i*10),'--timeout','45','--report',str(report)],capture_output=True,text=True,timeout=90)
  print(run.stdout,flush=True);r['stdout']=run.stdout;r['stderr']=run.stderr;r['result']=json.loads(report.read_text()) if report.exists() else None
  assert run.returncode==0,(run.stdout,run.stderr)
  r['alive_pid']=int(run_csharp(payload['port'],'Console.WriteLine(Environment.ProcessId);').strip());assert r['alive_pid']==payload['pid']
  r['status']='passed'
 except Exception as e:r.update(status='failed',error=str(e));raise
 finally:
  if payload:
   try:run_csharp(payload['port'],'var gh=AppDomain.CurrentDomain.GetAssemblies().Single(x=>x.GetName().Name=="Grasshopper2");dynamic e=gh.GetType("Grasshopper2.UI.Editor").GetProperty("Instance").GetValue(null);e.Canvas.Document.Close();Console.WriteLine("closed scratch");',timeout=15)
   except Exception as e:r['cleanup_message']=str(e)
  stop.write_text('stop')
  try:slot.wait(timeout=25)
  except subprocess.TimeoutExpired:slot.terminate();r['launcher_forced_closed']=True
  if handle:
   if k.WaitForSingleObject(handle,15000)==258:
    # A shared slot-store ownership change can make close_slot refuse our own process. This held
    # handle is the exact freshly spawned Rhino whose PID and creation time were verified above.
    k.TerminateProcess(handle,0);r['owned_rhino_forced_closed']=True;k.WaitForSingleObject(handle,10000)
   else:r['owned_rhino_closed']=True
   k.CloseHandle(handle)
  records.append(r);a.report.write_text(json.dumps(records,indent=2),encoding='utf-8')
print('PASS fresh processes',len(records))
