"""Real GH2 regressions for retained and discarded publisher results; exactly one echo each."""
import argparse,json,socket,time,shutil,tempfile
from pathlib import Path
from lifecycle_stress import Stress,WebSocket,NAMES
p=argparse.ArgumentParser();p.add_argument('--port',type=int,required=True);p.add_argument('--pid',type=int,required=True);p.add_argument('--report',type=Path,required=True);a=p.parse_args();s=Stress(a);client=None;r={'status':'running'}
def one_echo(expected):
 value=client.receive();assert value==expected,(expected,value);client.s.settimeout(.3)
 try:extra=client.receive()
 except TimeoutError:return value
 raise AssertionError(f'duplicate/unexpected echo after {expected}: {extra}')
def require_cancelled(snapshot,*names):
 phases={c['name']:c['phase'] for c in snapshot['diagnostics']}
 for name in names:assert phases.get(name)=='Cancelled',(name,phases)

def ws_state(snapshot):return next(c for c in snapshot['components'] if c['name']==NAMES[3])
try:
 s.setup();s.command('delay');s.command('suppress-ws');client=WebSocket(s.ports[3]);client.send('controlled');time.sleep(.1)
 case={};case['before']=s.command('snapshot');case['after_cancel']=s.command('publish-cancel');require_cancelled(case['after_cancel'],'ThrottleComponent','WebSocketSendComponent');assert ws_state(case['after_cancel'])['outbox']['count']==0,case
 assert next(d for d in case['after_cancel']['diagnostics'] if d['name']=='WebSocketServerComponent')['phase']=='Completed'
 s.command('resume-ws');case['after_unrelated']=s.command('unrelated');case['echo']=one_echo('controlled')
 assert ws_state(case['after_cancel'])['outbox']['publishedCount']==0
 case['status']='passed';r['retained_result']=case;client.close();client=None;s.command('close');s.resources(set(),0)
 s.setup();fixture=(Path(__file__).resolve().parents[1]/'Swiftlet.Gh2.Fixtures/bin/Debug/net8.0/Swiftlet.Gh2.Fixtures.dll').resolve();staged=Path(tempfile.mkdtemp(prefix='swiftlet-fixtures-'))/fixture.name;shutil.copy2(fixture,staged);s.command('fixture',path=str(staged));client=WebSocket(s.ports[3]);time.sleep(.1);s.command('solve')
 case={};case['after_cancel']=s.command('fixture-cancel');require_cancelled(case['after_cancel'],'ControlledPublisherComponent','WebSocketSendComponent');case['echo']=one_echo('mid-process');case['settled']=s.command('snapshot')
 state=case['settled']['fixture'];assert state['cancelled']==1 and state['attempts']>=2 and not state['pending'],state
 case['status']='passed';r['discarded_result']=case;r['status']='passed'
 print('PASS retained publisher result: exactly one echo; discarded mid-Process result: exactly one echo')
except Exception as e:r.update(status='failed',error=str(e));raise
finally:
 if client:client.close()
 s.command('close');r['cleanup']=s.resources(set(),0);a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(r,indent=2))
